import json
from urllib.request import Request, urlopen
from .config import Settings

if __name__ == "__main__":
    s = Settings()
    host = "127.0.0.1" if s.server_host == "0.0.0.0" else s.server_host
    request = Request(f"http://{host}:{s.server_port}/health",
                      headers={"X-FV-Token": s.access_token.get_secret_value()})
    with urlopen(request, timeout=5) as response:
        result = json.load(response)
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result.get("ready") else 1)
