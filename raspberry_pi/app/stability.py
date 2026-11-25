import math

class Stability:
    """Require consecutive fresh observations of the expected category."""
    def __init__(self, expected, threshold, required):
        self.expected, self.threshold, self.required = expected, threshold, required
        self.frames, self.minimum = 0, 1.0
        self.wrong = False

    def observe(self, detections):
        valid = [(name, score) for name, score in detections
                 if math.isfinite(score) and self.threshold <= score <= 1]
        expected = [score for name, score in valid if name == self.expected]
        self.wrong |= any(name != self.expected for name, _ in valid)
        if not expected:
            self.frames, self.minimum = 0, 1.0
            return False
        self.frames += 1
        self.minimum = min(self.minimum, max(expected))
        return self.frames >= self.required
