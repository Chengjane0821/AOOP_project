#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
[[ "$EUID" -ne 0 ]] || { echo "Run as the ordinary Pi user, not sudo." >&2; exit 1; }
# Require a conventional path so systemd does not misparse ExecStart.
[[ "$PWD" =~ ^/[a-zA-Z0-9_./-]+$ ]] || { echo "Use a checkout path without spaces or special characters." >&2; exit 1; }
[[ -f .env && -x .venv/bin/python ]] || { echo "Install and configure .env first." >&2; exit 1; }
unit=$(mktemp)
trap 'rm -f "$unit"' EXIT
sed -e "s|@USER@|$(id -un)|g" -e "s|@ROOT@|$PWD|g" systemd/recognition.service > "$unit"
sudo install -m 644 "$unit" /etc/systemd/system/recognition.service
sudo systemctl daemon-reload
sudo systemctl enable --now recognition.service
sudo systemctl status recognition.service --no-pager
