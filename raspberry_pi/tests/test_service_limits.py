import time
import pytest
from app.service import VerificationService, ServiceBusy
from app.schemas import VerifyRequest
from app.catalog import HOUSES, OBJECTS
from test_api import Engine


def test_result_capacity_is_bounded_and_reclaims_expired_entries(settings):
    settings.max_results=5
    engine=Engine()
    service=VerificationService(settings,engine)
    try:
        for house in range(5):
            req=VerifyRequest(request_id=f"{house:032x}",house_id=HOUSES[house],expected_object=OBJECTS[house])
            service.submit(req)
            for _ in range(100):
                if service.get(req.request_id).status!='pending':break
                time.sleep(.01)
        next_request=VerifyRequest(request_id='f'*32,house_id='elder',expected_object='keyboard')
        with pytest.raises(ServiceBusy):service.submit(next_request)
        with service.lock:
            for key,(req,result,stamp) in list(service.results.items()):
                service.results[key]=(req,result,stamp-10000)
        assert service.submit(next_request).status=='pending'
    finally:service.close()
