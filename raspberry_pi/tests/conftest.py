import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import pytest
from app.config import Settings

@pytest.fixture
def settings(tmp_path):
    return Settings(_env_file=None, access_token="test-only-" + "x" * 40,
                    evidence_dir=tmp_path, inference_timeout=1)
