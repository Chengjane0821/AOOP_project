#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p models
cd models
# Explicit download step. Server itself never fetches an unknown model.
../.venv/bin/python -c 'from ultralytics import YOLO; YOLO("yolo11n.pt")'
cd ..
.venv/bin/python -m app.inspect_model
sha256sum models/yolo11n.pt > models/yolo11n.pt.sha256.local
