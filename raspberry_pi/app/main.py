from contextlib import asynccontextmanager
import asyncio
import secrets
from fastapi import FastAPI, Depends, Header, HTTPException
from .config import Settings
from .schemas import VerifyRequest, RecognitionResult, Health
from .service import VerificationService, RequestConflict, ServiceBusy
from .worker import ProcessEngine
from .logging_config import configure_logging

def create_app(settings=None, engine=None):
    settings = settings or Settings()
    engine = engine or ProcessEngine(settings)
    service = VerificationService(settings, engine)

    @asynccontextmanager
    async def lifespan(app):
        configure_logging()
        token = settings.access_token.get_secret_value()
        if len(token) < 32 or token.startswith("CHANGE"):
            raise RuntimeError("Set ACCESS_TOKEN to a random value of at least 32 characters")
        await asyncio.to_thread(engine.start)
        try:
            yield
        finally:
            await asyncio.to_thread(service.close)

    app = FastAPI(title="V=IR Camera", version="1.0", lifespan=lifespan)
    app.state.service = service

    def authorize(x_fv_token: str = Header(default="")):
        if not secrets.compare_digest(x_fv_token, settings.access_token.get_secret_value()):
            raise HTTPException(401, "Invalid camera token")

    @app.get("/health", response_model=Health, dependencies=[Depends(authorize)])
    def health():
        ready = bool(engine.ready)
        return Health(status="ok" if ready else "degraded", camera=bool(engine.camera and ready),
            model=bool(engine.model and ready), ready=ready and not service.busy,
            busy=service.busy, message=engine.message)

    @app.post("/verify", response_model=RecognitionResult, status_code=202, dependencies=[Depends(authorize)])
    def submit(request: VerifyRequest):
        try:
            return service.submit(request)
        except RequestConflict as exc:
            raise HTTPException(409, str(exc)) from exc
        except ServiceBusy as exc:
            raise HTTPException(429, str(exc), headers={"Retry-After": "2"}) from exc
        except RuntimeError as exc:
            raise HTTPException(503, str(exc)) from exc

    @app.get("/verify/{request_id}", response_model=RecognitionResult, dependencies=[Depends(authorize)])
    def poll(request_id: str):
        result = service.get(request_id)
        if result is None:
            raise HTTPException(404, "Unknown or expired request; do not award a clue")
        return result

    return app
