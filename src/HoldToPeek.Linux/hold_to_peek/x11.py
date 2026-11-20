"""X11 passive grabs, owned by the GTK main thread. No files or drawing in callbacks."""
from Xlib import X, XK, display


class X11Input:
    def __init__(self, capture, hold, wheel, failure):
        self.display = display.Display()
        self.root = self.display.screen().root
        self.capture_callback = capture
        self.hold_callback = hold
        self.wheel_callback = wheel
        self.failure = failure
        self.preferences = None
        self.grabs = []
        self.active = None
        self.held = False
        self.capture_down = False
        self.enabled = False
        self.closed = False
        self.escape = self.display.keysym_to_keycode(XK.string_to_keysym("Escape"))
        # Determine NumLock dynamically; it is not required to be Mod2 on every keyboard.
        numcode = self.display.keysym_to_keycode(XK.string_to_keysym("Num_Lock"))
        numlock = 0
        for index, codes in enumerate(self.display.get_modifier_mapping()):
            if numcode in codes:
                numlock |= 1 << index
        self.ignored_locks = sorted(set((0, X.LockMask, numlock, X.LockMask | numlock)))

    def keycode(self, binding):
        if binding.kind == "button":
            return int(binding.key)
        code = self.display.keysym_to_keycode(XK.string_to_keysym(binding.key))
        if not code:
            raise ValueError("Key is not available in this keyboard layout: " + binding.key)
        return code

    def configure(self, preferences, capture_enabled=True, peek_enabled=True):
        self.suspend()
        preferences.validate()
        self.preferences = preferences
        self.capture_code = self.keycode(preferences.capture)
        self.peek_code = self.keycode(preferences.peek)
        errors = []
        try:
            for binding, enabled in ((preferences.capture, capture_enabled), (preferences.peek, peek_enabled)):
                if not enabled:
                    continue
                code = self.keycode(binding)
                for locks in self.ignored_locks:
                    modifiers = binding.modifiers | locks
                    if binding.kind == "key":
                        self.root.grab_key(code, modifiers, False, X.GrabModeAsync, X.GrabModeAsync,
                                           onerror=lambda error, request: errors.append(error))
                    else:
                        self.root.grab_button(code, modifiers, False, X.ButtonPressMask | X.ButtonReleaseMask,
                                              X.GrabModeAsync, X.GrabModeAsync, X.NONE, X.NONE,
                                              onerror=lambda error, request: errors.append(error))
                    self.grabs.append((binding.kind, code, modifiers))
            self.display.sync()
            if errors:
                raise RuntimeError("A shortcut is already used by another application. Choose different keys; tray capture still works.")
            self.enabled = peek_enabled
        except Exception:
            self.suspend()
            raise

    def suspend(self):
        self.end_hold()
        for kind, code, modifiers in self.grabs:
            if kind == "key":
                self.root.ungrab_key(code, modifiers)
            else:
                self.root.ungrab_button(code, modifiers)
        self.grabs.clear()
        self.enabled = False
        self.display.flush()

    def begin_hold(self, binding):
        self.active = binding
        # While reading, own wheel input and Escape without activating the preview window.
        if binding.kind == "key":
            result = self.root.grab_pointer(False, X.ButtonPressMask | X.ButtonReleaseMask,
                                            X.GrabModeAsync, X.GrabModeAsync, X.NONE, X.NONE, X.CurrentTime)
        else:
            result = self.root.grab_keyboard(False, X.GrabModeAsync, X.GrabModeAsync, X.CurrentTime)
        if result != X.GrabSuccess:
            self.end_hold()
            self.failure("Input is currently grabbed by another application; try again after closing its menu.")
            return
        self.held = True
        self.hold_callback(True)

    def end_hold(self):
        was_held = self.held
        self.held = False
        self.active = None
        self.display.ungrab_keyboard(X.CurrentTime)
        self.display.ungrab_pointer(X.CurrentTime)
        self.display.flush()
        if was_held:
            self.hold_callback(False)

    def cancel_visible_hold(self):
        # Keep ownership until the main key is released; repeat cannot reopen the overlay.
        if self.held:
            self.held = False
            self.hold_callback(False)

    def process(self, event):
        if self.preferences is None:
            return
        if event.type == X.MappingNotify:
            self.display.refresh_keyboard_mapping(event)
            self.suspend()
            self.failure("Keyboard layout changed. Open settings and save to register shortcuts again.")
            return
        if event.type not in (X.KeyPress, X.KeyRelease, X.ButtonPress, X.ButtonRelease):
            return
        is_key = event.type in (X.KeyPress, X.KeyRelease)
        down = event.type in (X.KeyPress, X.ButtonPress)
        if self.active is not None:
            if is_key and down and event.detail == self.escape:
                self.cancel_visible_hold()
            if not is_key and down and event.detail in (4, 5) and self.held:
                self.wheel_callback(-1 if event.detail == 4 else 1, bool(event.state & X.ShiftMask))
            if not down and is_key:
                # A required modifier release hides the peek, but keeps the main release owned.
                mask = self.root.query_pointer().mask & 13
                if mask & self.active.modifiers != self.active.modifiers:
                    self.cancel_visible_hold()
            matches = (self.active.kind == "key") == is_key and event.detail == self.peek_code
            if matches and not down:
                if is_key and self.key_down(self.peek_code):
                    return  # Legacy X11 auto-repeat sends a synthetic release before each press.
                self.end_hold()
            return
        if is_key and event.detail == self.capture_code:
            if down and not self.capture_down:
                self.capture_down = True
                self.display.ungrab_keyboard(X.CurrentTime)
                self.display.flush()
                self.capture_callback()
            elif not down:
                if not self.key_down(self.capture_code):
                    self.capture_down = False
            return
        binding = self.preferences.peek
        if self.enabled and down and (binding.kind == "key") == is_key and event.detail == self.peek_code:
            self.begin_hold(binding)

    def key_down(self, code):
        keys = self.display.query_keymap()
        return bool(keys[code // 8] & (1 << (code % 8)))

    def poll(self):
        if self.closed:
            return False
        try:
            for _ in range(200):
                if not self.display.pending_events():
                    break
                self.process(self.display.next_event())
            if self.capture_down and not self.key_down(self.capture_code):
                self.capture_down = False
            if self.active and self.active.kind == "key" and not self.key_down(self.peek_code):
                self.end_hold()
        except Exception as error:
            self.close()
            self.failure("Input monitoring stopped: " + str(error))
            return False
        return True

    def close(self):
        if not self.closed:
            try:
                self.suspend()
            except Exception:
                # A dead X connection has already released its server-side grabs.
                self.held = False
                self.active = None
            finally:
                self.closed = True
                try:
                    self.display.close()
                except Exception:
                    pass
