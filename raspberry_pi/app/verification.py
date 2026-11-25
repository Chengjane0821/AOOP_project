from datetime import datetime, timezone
import time
from .catalog import HOUSES, OBJECTS
from .camera import CameraError
from .detector import ModelError
from .schemas import RecognitionResult
from .stability import Stability

def verify(request, settings, camera, detector, save_snapshot, clock=time.monotonic):
    house = HOUSES.index(request.house_id)
    result = RecognitionResult(requestId=request.request_id, questIndex=house)
    stable = Stability(OBJECTS[house], settings.confidence_threshold, settings.required_stable_frames)
    deadline = clock() + settings.inference_timeout
    try:
        while clock() < deadline:
            frame = camera.read()
            detections = detector.predict(frame)
            if clock() >= deadline:
                break
            if stable.observe(detections):
                result.snapshot = save_snapshot(frame, request.request_id)
                result.status = "success"
                result.objectName = OBJECTS[house]
                result.confidence = stable.minimum
                result.frames = stable.frames
                result.capturedUtc = datetime.now(timezone.utc).isoformat()
                result.message = "Stable camera observation verified"
                return result
        result.status = "failed" if stable.wrong else "timeout"
        result.message = "Wrong object; expected " + OBJECTS[house] if stable.wrong else "No stable matching object before deadline"
    except CameraError as exc:
        result.status, result.message = "camera_error", str(exc)
    except ModelError as exc:
        result.status, result.message = "model_error", str(exc)
    return result
