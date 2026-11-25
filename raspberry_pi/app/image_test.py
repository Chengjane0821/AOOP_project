import argparse
import json
from .config import Settings
from .detector import Detector

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Real YOLO on a saved image; no game progress changes")
    parser.add_argument("image")
    args = parser.parse_args()
    import cv2
    frame = cv2.imread(args.image)
    if frame is None:
        parser.error("Image could not be read")
    detector = Detector(Settings())
    detector.load()
    print(json.dumps(detector.predict(frame), ensure_ascii=False))
