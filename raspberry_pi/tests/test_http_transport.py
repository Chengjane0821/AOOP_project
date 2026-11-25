"""Real loopback HTTP transport; test-only engine, never a YOLO claim."""
import json
import socket
import threading
import time
from urllib.request import Request, urlopen
from urllib.error import URLError
import pytest
import uvicorn
from app.main import create_app
from test_api import Engine, payload


def test_real_http_json_and_server_disconnect(settings):
    listener = socket.socket()
    listener.bind(("127.0.0.1", 0))
    port = listener.getsockname()[1]
    server = uvicorn.Server(uvicorn.Config(create_app(settings, Engine()), log_level="error"))
    thread = threading.Thread(target=server.run, kwargs={"sockets": [listener]}, daemon=True)
    thread.start()
    base = f"http://127.0.0.1:{port}"
    headers = {"X-FV-Token": settings.access_token.get_secret_value(), "Content-Type": "application/json"}
    try:
        for _ in range(100):
            if server.started: break
            time.sleep(.01)
        assert server.started
        with urlopen(Request(base+"/health", headers=headers), timeout=2) as response:
            assert json.load(response)["ready"]
        with urlopen(Request(base+"/verify", data=json.dumps(payload()).encode(), headers=headers), timeout=2) as response:
            assert response.status == 202
            assert json.load(response)["requestId"] == "a"*32
    finally:
        server.should_exit = True
        thread.join(5)
        listener.close()
    assert not thread.is_alive()
    with pytest.raises(URLError):
        urlopen(Request(base+"/health", headers=headers), timeout=1)
