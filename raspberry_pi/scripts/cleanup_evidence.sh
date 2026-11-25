#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Explicit local maintenance: remove captures older than seven days only.
exec .venv/bin/python -m app.cleanup_evidence "$@"
