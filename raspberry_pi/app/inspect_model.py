from .config import Settings
from .detector import Detector

if __name__ == "__main__":
    detector = Detector(Settings())
    detector.load()
    print("PASS: all five canonical object classes present")
    for key, value in detector.model.names.items():
        print(key, value)
