class CameraError(RuntimeError):
    pass

class Camera:
    """One owner in the inference process. Arrays use OpenCV BGR ordering."""
    def __init__(self, settings):
        self.settings = settings
        self.device = None

    def open(self):
        if self.device is not None:
            return
        try:
            s = self.settings
            if s.camera_backend == "picamera2":
                from picamera2 import Picamera2
                self.device = Picamera2(s.camera_index)
                # Picamera2 RGB888 memory order is BGR, as expected by OpenCV/YOLO.
                self.device.configure(self.device.create_video_configuration(
                    main={"size": (s.camera_width, s.camera_height), "format": "RGB888"},
                    buffer_count=2))
                self.device.start()
            else:
                import cv2
                self.device = cv2.VideoCapture(s.camera_index)
                self.device.set(cv2.CAP_PROP_FRAME_WIDTH, s.camera_width)
                self.device.set(cv2.CAP_PROP_FRAME_HEIGHT, s.camera_height)
                if not self.device.isOpened():
                    raise CameraError("USB camera could not open")
            self.read()
        except Exception as exc:
            self.close()
            raise CameraError(f"Camera initialization failed: {exc}") from exc

    def read(self):
        try:
            if self.device is None:
                raise CameraError("Camera is closed")
            if self.settings.camera_backend == "picamera2":
                frame = self.device.capture_array("main")
            else:
                ok, frame = self.device.read()
                if not ok:
                    raise CameraError("USB camera read failed")
            if frame is None or frame.size == 0:
                raise CameraError("Camera returned empty frame")
            return frame
        except Exception as exc:
            raise CameraError(f"Camera capture failed: {exc}") from exc

    def close(self):
        device, self.device = self.device, None
        if device is None:
            return
        try:
            if self.settings.camera_backend == "picamera2":
                try:
                    device.stop()
                finally:
                    device.close()
            else:
                device.release()
        except Exception:
            pass
