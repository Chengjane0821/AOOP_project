from .camera import CameraError

def save_snapshot(directory, frame, request_id):
    import cv2
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / (request_id + ".jpg")
    try:
        if not cv2.imwrite(str(path), frame):
            raise OSError("JPEG writer returned false")
        path.chmod(0o600)
        return path.name
    except Exception as exc:
        raise CameraError(f"Could not save camera evidence: {exc}") from exc
