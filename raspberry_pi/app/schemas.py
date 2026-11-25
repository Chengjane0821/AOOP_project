from typing import Literal
from pydantic import BaseModel, ConfigDict, Field, model_validator
from .catalog import HOUSES, OBJECTS, canonical

class VerifyRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    request_id: str = Field(pattern=r"^[0-9a-f]{32}$")
    house_id: Literal["elder", "alchemist", "carpenter", "technician", "clockmaker"]
    expected_object: str = Field(max_length=32)

    @model_validator(mode="after")
    def matches_house(self):
        self.expected_object = canonical(self.expected_object)
        if self.expected_object != OBJECTS[HOUSES.index(self.house_id)]:
            raise ValueError("expected_object does not match server house mapping")
        return self

class RecognitionResult(BaseModel):
    requestId: str
    questIndex: int
    status: Literal["pending", "success", "failed", "timeout", "camera_error", "model_error"] = "pending"
    objectName: str = ""
    confidence: float = 0
    frames: int = 0
    snapshot: str = ""
    capturedUtc: str = ""
    simulated: bool = False
    message: str = "Waiting for camera"

class Health(BaseModel):
    status: str
    camera: bool
    model: bool
    ready: bool
    busy: bool
    message: str
    protocol: int = 1
