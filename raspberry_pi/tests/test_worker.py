from types import SimpleNamespace
from app.worker import ProcessEngine
from app.schemas import VerifyRequest

def test_native_hang_is_terminated_without_fake_success(settings):
    engine=ProcessEngine(settings)
    closed=[]
    engine.connection=SimpleNamespace(send=lambda _:None,poll=lambda _:False,close=lambda:closed.append(True))
    engine.camera=engine.model=True
    result=engine.run(VerifyRequest(request_id="a"*32,house_id="elder",expected_object="keyboard"))
    assert result.status=="timeout"
    assert not engine.ready and not engine.camera
    assert closed
