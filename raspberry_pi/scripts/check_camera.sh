#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if systemctl is-active --quiet recognition.service; then
  echo "Stop recognition.service before running an exclusive camera test." >&2
  exit 1
fi
exec .venv/bin/python -m app.camera_test "$@"
