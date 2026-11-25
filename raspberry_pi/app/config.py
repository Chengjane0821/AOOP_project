from pathlib import Path
from pydantic import Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict

ROOT = Path(__file__).resolve().parents[1]

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=ROOT / ".env", extra="forbid")
    confidence_threshold: float = Field(default=.70, ge=.70, le=1)
    required_stable_frames: int = Field(default=3, ge=3, le=30)
    camera_width: int = Field(default=640, ge=160, le=1920)
    camera_height: int = Field(default=480, ge=120, le=1080)
    camera_backend: str = Field(default="picamera2", pattern="^(picamera2|opencv)$")
    camera_index: int = Field(default=0, ge=0, le=16)
    inference_timeout: float = Field(default=45, ge=1, le=90)
    model_path: Path = ROOT / "models/yolo11n.pt"
    inference_size: int = Field(default=416, ge=160, le=640, multiple_of=32)
    cpu_threads: int = Field(default=2, ge=1, le=4)
    server_host: str = "127.0.0.1"
    server_port: int = Field(default=8765, ge=1024, le=65535)
    access_token: SecretStr = SecretStr("")
    evidence_dir: Path = ROOT / "evidence"
    result_ttl: int = Field(default=600, ge=120, le=3600)
    max_results: int = Field(default=128, ge=5, le=1000)
    startup_timeout: float = Field(default=90, ge=5, le=300)

    def model_location(self):
        return self.model_path if self.model_path.is_absolute() else ROOT / self.model_path
