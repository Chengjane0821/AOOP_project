"""A persistent child owns the camera; stuck native calls can be terminated."""
import multiprocessing as mp
from .camera import Camera, CameraError
from .detector import Detector, ModelError
from .evidence import save_snapshot
from .verification import verify


def worker_main(connection, settings):
    camera = Camera(settings)
    camera_ok = model_ok = False
    try:
        camera.open()
        camera_ok = True
        detector = Detector(settings)
        detector.load()
        detector.predict(camera.read())  # Real inference warmup, not just file existence.
        model_ok = True
        connection.send((camera_ok, model_ok, "Camera capture and model warmup passed"))
        while True:
            request = connection.recv()
            if request is None:
                break
            result = verify(request, settings, camera, detector,
                lambda frame, rid: save_snapshot(settings.evidence_dir, frame, rid))
            connection.send(result)
    except (CameraError, ModelError, Exception) as exc:
        try:
            connection.send((camera_ok, model_ok, str(exc)))
        except (OSError, EOFError):
            pass
    finally:
        camera.close()
        connection.close()

class ProcessEngine:
    def __init__(self, settings):
        self.settings = settings
        self.camera = self.model = False
        self.message = "Not initialized"
        self.process = self.connection = None

    def start(self):
        context = mp.get_context("spawn")
        self.connection, child = context.Pipe()
        self.process = context.Process(target=worker_main, args=(child, self.settings), daemon=True)
        self.process.start()
        child.close()
        try:
            if not self.connection.poll(self.settings.startup_timeout):
                raise TimeoutError("Camera/model startup timed out")
            self.camera, self.model, self.message = self.connection.recv()
        except (EOFError, OSError, TimeoutError) as exc:
            self.message = str(exc)
        if not (self.camera and self.model):
            self.close()

    @property
    def ready(self):
        return self.camera and self.model and self.process is not None and self.process.is_alive()

    def run(self, request):
        from .schemas import RecognitionResult
        from .catalog import HOUSES
        result = RecognitionResult(requestId=request.request_id, questIndex=HOUSES.index(request.house_id))
        try:
            self.connection.send(request)
            if not self.connection.poll(self.settings.inference_timeout + 5):
                result.status = "timeout"
                result.message = "Camera/inference stalled; restart recognition service"
                self.close()
                return result
            value = self.connection.recv()
            if not isinstance(value, RecognitionResult):
                raise RuntimeError(str(value))
            if value.status in ("camera_error", "model_error"):
                self.message = value.message
                self.close()
            return value
        except Exception as exc:
            result.status, result.message = "model_error", f"Worker stopped: {exc}; restart service"
            self.message = result.message
            self.close()
            return result

    def close(self):
        self.camera = self.model = False
        if self.process is not None:
            if self.process.is_alive():
                self.process.terminate()
            self.process.join(timeout=3)
            if self.process.is_alive():
                self.process.kill()
                self.process.join(timeout=2)
        if self.connection is not None:
            self.connection.close()
