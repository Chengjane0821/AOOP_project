import math
from .catalog import canonical, validate_names

class ModelError(RuntimeError):
    pass

class Detector:
    def __init__(self, settings):
        self.settings = settings
        self.model = None

    def load(self):
        try:
            path = self.settings.model_location()
            if not path.is_file():
                raise FileNotFoundError(f"Missing model: {path}; run scripts/download_model.sh")
            import torch
            from ultralytics import YOLO
            torch.set_num_threads(self.settings.cpu_threads)
            self.model = YOLO(str(path), task="detect")
            validate_names(self.model.names.values())
        except Exception as exc:
            raise ModelError(f"Model initialization failed: {exc}") from exc

    def predict(self, frame):
        try:
            result = self.model.predict(frame, device="cpu", imgsz=self.settings.inference_size,
                conf=self.settings.confidence_threshold, verbose=False)[0]
            detections = []
            for box in result.boxes:
                score = float(box.conf.item())
                if math.isfinite(score) and 0 <= score <= 1:
                    detections.append((canonical(result.names[int(box.cls.item())]), score))
            return detections
        except Exception as exc:
            raise ModelError(f"Inference failed: {exc}") from exc
