import pytest
from app.stability import Stability

def test_interrupted_sequence_resets_confidence_and_frames():
    s = Stability("mouse", .7, 3)
    assert not s.observe([("mouse", .8)])
    assert not s.observe([("clock", .99)])
    assert s.frames == 0
    assert not s.observe([("mouse", .9)])
    assert not s.observe([("mouse", .85)])
    assert s.observe([("mouse", .95)])
    assert s.minimum == .85

@pytest.mark.parametrize("score", [.69, float("nan"), float("inf"), -1, 1.1])
def test_invalid_confidence_never_passes(score):
    s = Stability("clock", .7, 3)
    for _ in range(10):
        assert not s.observe([("clock", score)])

def test_multiple_detections_in_one_image_count_as_one_frame():
    s = Stability("keyboard", .7, 3)
    assert not s.observe([("keyboard", .8)] * 10)
    assert s.frames == 1
