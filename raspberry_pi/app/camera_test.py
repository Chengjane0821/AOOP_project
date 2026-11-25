import argparse
from pathlib import Path
from .camera import Camera
from .config import Settings

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path("camera-test.jpg"))
    args = parser.parse_args()
    camera = Camera(Settings())
    try:
        camera.open()
        import cv2
        if not cv2.imwrite(str(args.output), camera.read()):
            raise RuntimeError("Could not write test image")
        print("PASS: real camera captured", args.output.resolve())
    finally:
        camera.close()
