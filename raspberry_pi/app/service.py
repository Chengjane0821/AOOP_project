from concurrent.futures import ThreadPoolExecutor
from threading import RLock
import time
import logging
from .catalog import HOUSES
from .schemas import RecognitionResult

log = logging.getLogger(__name__)

class RequestConflict(ValueError):
    pass

class ServiceBusy(RuntimeError):
    pass

class VerificationService:
    def __init__(self, settings, engine):
        self.settings, self.engine = settings, engine
        self.results = {}
        self.lock = RLock()
        self.pool = ThreadPoolExecutor(max_workers=1, thread_name_prefix="recognition")
        self.busy = False

    def prune(self):
        now = time.monotonic()
        for rid, (_, result, stamp) in list(self.results.items()):
            if result.status != "pending" and now - stamp > self.settings.result_ttl:
                del self.results[rid]

    def submit(self, request):
        with self.lock:
            self.prune()
            if request.request_id in self.results:
                previous, result, _ = self.results[request.request_id]
                if previous != request:
                    raise RequestConflict("Request ID already belongs to a different house")
                return result
            if self.busy or len(self.results) >= self.settings.max_results:
                raise ServiceBusy("Camera busy or result cache full; retry later")
            if not self.engine.ready:
                raise RuntimeError(self.engine.message)
            result = RecognitionResult(requestId=request.request_id, questIndex=HOUSES.index(request.house_id))
            self.results[request.request_id] = (request, result, time.monotonic())
            self.busy = True
            self.pool.submit(self.execute, request)
            return result

    def execute(self, request):
        try:
            result = self.engine.run(request)
        except Exception:
            log.exception("Verification worker failed for %s", request.request_id)
            result = RecognitionResult(requestId=request.request_id,
                questIndex=HOUSES.index(request.house_id), status="model_error", message="Internal recognition error; inspect server log")
        with self.lock:
            self.results[request.request_id] = (request, result, time.monotonic())
            self.busy = False
        log.info("request=%s house=%s status=%s", request.request_id, request.house_id, result.status)

    def get(self, request_id):
        with self.lock:
            self.prune()
            row = self.results.get(request_id)
            return row[1] if row else None

    def close(self):
        self.pool.shutdown(wait=True, cancel_futures=True)
        self.engine.close()
