import time
from threading import Event
import pytest
from fastapi.testclient import TestClient
from app.main import create_app
from app.schemas import RecognitionResult
from app.catalog import HOUSES, OBJECTS

class Engine:
    camera = model = ready = True
    message = "Test double: no hardware"
    def __init__(self):
        self.calls = 0
        self.release = Event()
        self.release.set()
        self.failure = False
    def start(self): pass
    def close(self): self.release.set()
    def run(self, req):
        self.calls += 1
        self.release.wait(2)
        if self.failure: raise RuntimeError("test failure")
        return RecognitionResult(requestId=req.request_id, questIndex=HOUSES.index(req.house_id), status="success")

def payload(house=0, rid="a"*32):
    return dict(request_id=rid, house_id=HOUSES[house], expected_object=OBJECTS[house])

def terminal(client, rid="a"*32):
    for _ in range(100):
        result = client.get("/verify/"+rid).json()
        if result["status"] != "pending": return result
        time.sleep(.01)
    pytest.fail("Job never completed")

@pytest.fixture
def api(settings):
    engine = Engine()
    with TestClient(create_app(settings, engine), headers={"X-FV-Token": settings.access_token.get_secret_value()}) as client:
        yield client, engine

@pytest.mark.parametrize("house", range(5))
def test_five_requests(api, house):
    client, engine = api
    assert client.post("/verify", json=payload(house)).status_code == 202
    assert terminal(client)["questIndex"] == house
    assert engine.calls == 1

def test_idempotency_conflict_and_unknown(api):
    client, engine = api
    client.post("/verify", json=payload())
    terminal(client)
    assert client.post("/verify", json=payload()).status_code == 202
    assert engine.calls == 1
    assert client.post("/verify", json=payload(1)).status_code == 409
    assert client.get("/verify/"+"b"*32).status_code == 404

def test_busy_camera_still_has_responsive_health(api):
    client, engine = api
    engine.release.clear()
    try:
        client.post("/verify", json=payload())
        health = client.get("/health").json()
        assert health["busy"] and not health["ready"]
        assert client.post("/verify", json=payload(1, "b"*32)).status_code == 429
        assert client.post("/verify", json=payload()).status_code == 202
    finally: engine.release.set()

def test_authentication_and_authoritative_mapping(api):
    client, engine = api
    assert client.get("/health", headers={"X-FV-Token": "bad"}).status_code == 401
    bad = payload(); bad["expected_object"] = "mouse"
    assert client.post("/verify", json=bad).status_code == 422
    assert engine.calls == 0

def test_failed_model_health_and_server_error(api):
    client, engine = api
    engine.ready = False
    assert not client.get("/health").json()["ready"]
    assert client.post("/verify", json=payload()).status_code == 503
    engine.ready = True; engine.failure = True
    client.post("/verify", json=payload())
    assert terminal(client)["status"] == "model_error"

def test_result_expiry(api):
    client, _ = api
    client.post("/verify", json=payload()); terminal(client)
    service = client.app.state.service
    with service.lock:
        req, result, stamp = service.results["a"*32]
        service.results["a"*32] = (req, result, stamp - 10000)
    assert client.get("/verify/"+"a"*32).status_code == 404
