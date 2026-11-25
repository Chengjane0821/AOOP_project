from types import SimpleNamespace
import sys
import pytest
from app.camera import Camera, CameraError

class Capture:
    def __init__(self, index): self.released=False; self.read_ok=True
    def set(self,*args): pass
    def isOpened(self): return True
    def read(self): return self.read_ok, SimpleNamespace(size=1)
    def release(self): self.released=True

def test_usb_single_owner_read_failure_and_release(settings, monkeypatch):
    device=Capture(0)
    calls=[]
    def create(index): calls.append(index); return device
    monkeypatch.setitem(sys.modules,"cv2",SimpleNamespace(VideoCapture=create,CAP_PROP_FRAME_WIDTH=3,CAP_PROP_FRAME_HEIGHT=4))
    settings.camera_backend="opencv"
    camera=Camera(settings);camera.open();camera.open()
    assert calls==[0]
    device.read_ok=False
    with pytest.raises(CameraError):camera.read()
    camera.close();camera.close()
    assert device.released
    with pytest.raises(CameraError):camera.read()
