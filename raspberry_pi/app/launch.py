import argparse
import uvicorn
from .config import Settings

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--dev", action="store_true", help="Debug logs; still one camera owner, no reload")
    args = parser.parse_args()
    settings = Settings()
    uvicorn.run("app.main:create_app", factory=True, host=settings.server_host,
                port=settings.server_port, workers=1, log_level="debug" if args.dev else "info")
