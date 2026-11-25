#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
[[ -x .venv/bin/python ]] || { echo "Run bash scripts/install.sh first" >&2; exit 1; }
export OMP_NUM_THREADS=2
export OPENBLAS_NUM_THREADS=2
# One worker only: multiple workers would contend for the same camera.
exec .venv/bin/python -m app.launch "$@"
