from types import SimpleNamespace
import pytest
from app.catalog import HOUSES, OBJECTS
from app.schemas import VerifyRequest
from app.verification import verify
from app.camera import CameraError
from app.detector import ModelError

class Clock:
    def __init__(self): self.now = 0
    def __call__(self):
        self.now += .05
        return self.now

def run(settings, detections, error=None):
    request = VerifyRequest(request_id="a"*32, house_id=detections[0], expected_object=detections[1])
    def predict(_):
        if error: raise error
        return detections[2]
    return verify(request, settings, SimpleNamespace(read=lambda: object()),
                  SimpleNamespace(predict=predict), lambda _, rid: rid+".jpg", clock=Clock())

@pytest.mark.parametrize("house,obj", zip(HOUSES, OBJECTS))
def test_five_real_algorithm_paths(settings, house, obj):
    result = run(settings, (house, obj, [(obj, .88)]))
    assert result.status == "success"
    assert result.frames == 3 and result.confidence == .88
    assert result.snapshot == "a"*32+".jpg" and not result.simulated

@pytest.mark.parametrize("items,status", [([("clock", .99)], "failed"), ([], "timeout"), ([("mouse", .5)], "timeout")])
def test_negative_outcomes(settings, items, status):
    assert run(settings, ("carpenter", "mouse", items)).status == status

@pytest.mark.parametrize("error,status", [(CameraError("unplugged"), "camera_error"), (ModelError("bad model"), "model_error")])
def test_component_errors(settings, error, status):
    assert run(settings, ("elder", "keyboard", []), error).status == status

def test_deadline_prevents_late_success(settings):
    settings.inference_timeout = .1
    assert run(settings, ("elder", "keyboard", [("keyboard", .99)])).status == "timeout"
