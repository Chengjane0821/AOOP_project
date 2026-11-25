import argparse
import re
import time
from .config import Settings

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Preview expired camera captures; --delete applies deletion")
    parser.add_argument("--delete", action="store_true")
    args = parser.parse_args()
    directory = Settings().evidence_dir
    for path in directory.glob("*.jpg"):
        if not path.is_symlink() and re.fullmatch(r"[0-9a-f]{32}\.jpg", path.name) and time.time()-path.stat().st_mtime > 7*86400:
            print("delete" if args.delete else "would delete", path.name)
            if args.delete: path.unlink()
