import pytest
from app.catalog import OBJECTS, HOUSES, canonical, validate_names
from app.schemas import VerifyRequest

@pytest.mark.parametrize("house,obj", zip(HOUSES, OBJECTS))
def test_house_mapping(house, obj):
    value = VerifyRequest(request_id="a"*32, house_id=house, expected_object=obj)
    assert value.expected_object == obj

def test_clock_and_phone():
    assert canonical("Phone") == "cell phone"
    validate_names(["keyboard", "laptop", "mouse", "phone", "clock"])
    with pytest.raises(ValueError, match="clock"):
        validate_names(OBJECTS[:-1])

@pytest.mark.parametrize("payload", [
    dict(request_id="../bad", house_id="elder", expected_object="keyboard"),
    dict(request_id="a"*32, house_id="elder", expected_object="mouse"),
    dict(request_id="a"*32, house_id="abandoned", expected_object="clock"),
])
def test_reject_malformed_request(payload):
    with pytest.raises(ValueError):
        VerifyRequest(**payload)
