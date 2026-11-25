#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source /etc/os-release
if [[ "$(uname -m)" != aarch64 || "${VERSION_CODENAME:-}" != bookworm ]]; then
  echo "Supported installer baseline: Raspberry Pi OS Bookworm 64-bit (aarch64). Other releases require separate ABI validation." >&2
  exit 1
fi
sudo apt-get update
sudo apt-get install -y python3-venv python3-pip python3-picamera2 python3-opencv python3-numpy rpicam-apps git libopenblas-dev
python3 -m venv --system-site-packages .venv
.venv/bin/python -m pip install --upgrade pip
.venv/bin/python -m pip install -r requirements.txt -r requirements-inference.txt
.venv/bin/python -m pip check
if [[ ! -e .env ]]; then
  cp .env.example .env
  chmod 600 .env
  .venv/bin/python -c 'import pathlib,secrets; p=pathlib.Path(".env"); p.write_text(p.read_text().replace("CHANGE_ME_GENERATE_A_RANDOM_64_HEX_TOKEN",secrets.token_hex(32)))'
fi
printf '%s\n' 'Installed. Set SERVER_HOST to your Pi LAN IP in .env, then run bash scripts/download_model.sh.'
