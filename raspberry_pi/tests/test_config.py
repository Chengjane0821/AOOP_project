import pytest
from app.config import Settings

@pytest.mark.parametrize("value", [dict(confidence_threshold=.5), dict(required_stable_frames=1),
    dict(server_port=0), dict(inference_size=415), dict(camera_backend="mock")])
def test_invalid_configuration_fails_closed(value):
    with pytest.raises(ValueError): Settings(_env_file=None, **value)

def test_default_has_no_public_binding_or_mock():
    settings = Settings(_env_file=None)
    assert settings.server_host == "127.0.0.1"
    assert settings.camera_backend == "picamera2"
