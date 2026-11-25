HOUSES = ("elder", "alchemist", "carpenter", "technician", "clockmaker")
OBJECTS = ("keyboard", "laptop", "mouse", "cell phone", "clock")
ALIASES = {"phone": "cell phone", "mobile phone": "cell phone", **{x: x for x in OBJECTS}}

def canonical(name: str) -> str:
    return ALIASES.get(name.strip().lower(), name.strip().lower())

def validate_names(names):
    available = {canonical(x) for x in names}
    missing = set(OBJECTS) - available
    if missing:
        raise ValueError("Model missing required classes: " + ", ".join(sorted(missing)))
