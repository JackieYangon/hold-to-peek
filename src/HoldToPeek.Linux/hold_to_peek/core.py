"""Preferences and reading position, independent of GTK and X11 (Python 3.8+)."""
import json
import math
import os
import re
from dataclasses import asdict, dataclass, field
from pathlib import Path


@dataclass(frozen=True)
class Binding:
    kind: str
    key: str
    modifiers: int = 0  # X11/GDK: Shift=1, Control=4, Alt=8

    def validate(self, capture=False):
        if type(self.modifiers) is not int or self.modifiers & ~13:
            raise ValueError("Only Ctrl, Alt and Shift modifiers are supported.")
        if self.kind == "button" and not capture and self.key in ("2", "8", "9"):
            return
        if self.kind != "key" or not isinstance(self.key, str) or not re.fullmatch(r"[A-Za-z0-9_]{1,40}", self.key):
            raise ValueError("Choose a keyboard key, or a middle/side button for peek.")
        if self.key in ("Escape", "F12", "Control_L", "Control_R", "Shift_L", "Shift_R", "Alt_L", "Alt_R", "Super_L", "Super_R", "Meta_L", "Meta_R"):
            raise ValueError("Choose a non-reserved key, optionally with Ctrl, Alt or Shift.")
        if (self.key in ("Tab", "F4") and self.modifiers & 8) or (self.key == "Delete" and self.modifiers & 12 == 12):
            raise ValueError("This combination is reserved by the desktop.")

    def label(self):
        key = {"2": "Middle mouse", "8": "Mouse back", "9": "Mouse forward"}.get(self.key, self.key) if self.kind == "button" else self.key
        return ("Ctrl + " if self.modifiers & 4 else "") + ("Alt + " if self.modifiers & 8 else "") + ("Shift + " if self.modifiers & 1 else "") + key


@dataclass
class Preferences:
    capture: Binding = field(default_factory=lambda: Binding("key", "F1"))
    peek: Binding = field(default_factory=lambda: Binding("button", "8"))
    line_height: int = 28
    start_at_login: bool = False
    high_priority: bool = False

    def validate(self):
        self.capture.validate(capture=True)
        self.peek.validate()
        if self.capture.kind == self.peek.kind and self.capture.key == self.peek.key:
            raise ValueError("Capture and peek need different main keys.")
        if type(self.line_height) is not int or not 8 <= self.line_height <= 120:
            raise ValueError("Line spacing must be between 8 and 120 pixels.")
        if type(self.start_at_login) is not bool or type(self.high_priority) is not bool:
            raise ValueError("Runtime options must be true or false.")


def config_root():
    candidate = os.environ.get("XDG_CONFIG_HOME", "")
    return Path(candidate) if candidate and Path(candidate).is_absolute() else Path.home() / ".config"


def load_preferences(path):
    try:
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        result = Preferences(capture=Binding(**data["capture"]), peek=Binding(**data["peek"]),
                             line_height=data.get("line_height", 28),
                             start_at_login=data.get("start_at_login", False),
                             high_priority=data.get("high_priority", False))
        result.validate()
        return result, None
    except FileNotFoundError:
        return Preferences(), None
    except (ValueError, TypeError, KeyError, OSError) as error:
        return Preferences(), "Could not read preferences; using defaults: " + str(error)


def save_preferences(path, preferences):
    preferences.validate()
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(".tmp")
    try:
        temporary.write_text(json.dumps(asdict(preferences), indent=2), encoding="utf-8")
        temporary.replace(path)
    finally:
        if temporary.exists():
            temporary.unlink()


class ReadingProgress:
    def __init__(self):
        self.height = 0
        self.position = 0.0

    def reset(self, height, spacing):
        self.height = max(0, height)
        self.position = min(max(0, self.height - 1), spacing / 2)

    def move(self, steps, spacing, fine=False):
        if not math.isfinite(steps):
            return
        self.position = min(max(0, self.height - 1), max(0, self.position + steps * (1 if fine else spacing)))


def clipped_selection(start, end, width, height):
    x1, x2 = sorted((max(0, min(width, start[0])), max(0, min(width, end[0]))))
    y1, y2 = sorted((max(0, min(height, start[1])), max(0, min(height, end[1]))))
    return int(x1), int(y1), int(x2 - x1), int(y2 - y1)
