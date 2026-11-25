from types import SimpleNamespace
import sys
import pytest
from app.detector import Detector, ModelError
from app.catalog import OBJECTS


def fake_dependencies(monkeypatch, names, predict=None):
    model=SimpleNamespace(names=dict(enumerate(names)),predict=predict)
    monkeypatch.setitem(sys.modules,'torch',SimpleNamespace(set_num_threads=lambda _:None))
    monkeypatch.setitem(sys.modules,'ultralytics',SimpleNamespace(YOLO=lambda *a,**kw:model))
    return model


def test_missing_weights_do_not_trigger_automatic_download(settings, monkeypatch, tmp_path):
    settings.model_path=tmp_path/'missing.pt'
    detector=Detector(settings)
    with pytest.raises(ModelError,match='Missing model'):detector.load()


def test_loaded_class_table_is_validated(settings, monkeypatch, tmp_path):
    settings.model_path=tmp_path/'fake.pt';settings.model_path.touch()
    fake_dependencies(monkeypatch,OBJECTS[:-1])
    with pytest.raises(ModelError,match='clock'):Detector(settings).load()


def test_inference_is_cpu_and_normalizes_model_labels(settings, monkeypatch, tmp_path):
    settings.model_path=tmp_path/'fake.pt';settings.model_path.touch()
    calls=[]
    scalar=lambda x:SimpleNamespace(item=lambda:x)
    def predict(frame,**kwargs):
        calls.append(kwargs)
        return [SimpleNamespace(names={0:'phone'},boxes=[SimpleNamespace(conf=scalar(.9),cls=scalar(0))])]
    fake_dependencies(monkeypatch,OBJECTS,predict)
    detector=Detector(settings);detector.load()
    assert detector.predict(object())==[('cell phone',.9)]
    assert calls[0]['device']=='cpu'
    assert calls[0]['conf']==.7


def test_inference_exception_is_explicit_model_error(settings, monkeypatch, tmp_path):
    settings.model_path=tmp_path/'fake.pt';settings.model_path.touch()
    def broken(*args,**kwargs):raise RuntimeError('inference unavailable')
    fake_dependencies(monkeypatch,OBJECTS,broken)
    detector=Detector(settings);detector.load()
    with pytest.raises(ModelError,match='inference unavailable'):detector.predict(object())
