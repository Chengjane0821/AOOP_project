#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
uname -m
cat /etc/os-release
.venv/bin/python --version
.venv/bin/python -m pip check
.venv/bin/python -c 'import cv2,numpy; from picamera2 import Picamera2; import torch,ultralytics; print("OpenCV",cv2.__version__,"NumPy",numpy.__version__,"Torch",torch.__version__,"YOLO",ultralytics.__version__)'
.venv/bin/python -m app.inspect_model
