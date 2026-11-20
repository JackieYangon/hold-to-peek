import os
import sys
from pathlib import Path
import gi
gi.require_version("Gtk", "3.0")
from gi.repository import Gdk, Gio, GLib, Gtk
from .core import Preferences, ReadingProgress, config_root, load_preferences, save_preferences
from .views import BindingButton, CaptureWindow, PeekWindow, page, paragraph


class Application(Gtk.Application):
    def __init__(self):
        super().__init__(application_id="io.github.JackieYangon.HoldToPeek",
                         flags=Gio.ApplicationFlags.HANDLES_COMMAND_LINE)
        self.input = None
        self.indicator = None
        self.capture_window = None
        self.reference = None
        self.busy = False
        self.settings_open = False
        self.paused = False
        self.epoch = 0
        self.progress = ReadingProgress()
        self.settings_path = config_root() / "hold-to-peek" / "settings.json"
        self.autostart_path = config_root() / "autostart" / "hold-to-peek.desktop"
        self.preferences, self.load_warning = load_preferences(self.settings_path)
        self.startup_error = None
        self.screen_subscription = None
        self.sleep_subscription = None

    def do_startup(self):
        Gtk.Application.do_startup(self)
        self.hold()
        self.peek = PeekWindow()
        version_path = Path(__file__).resolve().parent.parent / "VERSION"
        if not version_path.exists():
            version_path = Path(__file__).resolve().parents[3] / "VERSION"
        self.version = version_path.read_text(encoding="utf-8").strip()
        self.build_settings()
        self.create_indicator()
        display = Gdk.Display.get_default()
        if os.environ.get("XDG_SESSION_TYPE") == "wayland" or "X11" not in display.__gtype__.name:
            self.startup_error = ("此版本需要 X11/Xorg 会话，Wayland 暂不支持全局按住查看。\n"
                                  "This build requires an X11/Xorg session. Wayland is not supported yet.\n"
                                  "若登录界面提供 Ubuntu on Xorg，可选择后重新登录。")
            self.capture_button.set_sensitive(False)
        else:
            try:
                from .x11 import X11Input
                self.input = X11Input(lambda: GLib.idle_add(self.begin_capture),
                                      lambda held: GLib.idle_add(self.on_hold, held),
                                      lambda steps, fine: GLib.idle_add(self.on_wheel, steps, fine),
                                      lambda error: GLib.idle_add(self.input_failed, error))
                GLib.timeout_add(16, self.input.poll)
                self.screen_subscription = Gdk.Screen.get_default().connect("monitors-changed", lambda *_: self.monitor_changed())
                self.subscribe_session_changes()
            except Exception as error:
                self.startup_error = "Cannot start X11 shortcuts: " + str(error)
        if self.preferences.high_priority:
            try:
                os.setpriority(os.PRIO_PROCESS, 0, -5)
            except OSError:
                self.preferences.high_priority = False
                self.high_priority.set_active(False)
                self.load_warning = "Linux denied higher CPU priority; continuing at normal priority. No administrator privileges are requested."

    def do_command_line(self, command_line):
        arguments = command_line.get_arguments()
        if "--autostart" not in arguments or self.indicator is None or self.startup_error:
            self.show_settings()
        else:
            self.refresh_input()
        if self.startup_error:
            self.status.set_text(self.startup_error)
        elif self.load_warning:
            self.status.set_text(self.load_warning)
            self.load_warning = None
        return 0

    def build_settings(self):
        self.window = Gtk.ApplicationWindow(application=self, title="瞄一眼 · Hold to Peek " + self.version)
        self.window.set_default_size(660, 600)
        self.window.connect("delete-event", self.close_settings)
        outer = Gtk.Box(orientation=Gtk.Orientation.VERTICAL, spacing=8)
        self.window.add(outer)
        notebook = Gtk.Notebook()
        outer.pack_start(notebook, True, True, 0)
        reference = page(notebook, "参考 / Reference")
        reference.pack_start(paragraph("按住看参考，松手接着干。\nHold to peek. Release to work."), False, False, 0)
        reference.pack_start(paragraph("每格滚轮移动的原图像素 / Pixels per wheel step"), False, False, 0)
        self.spacing = Gtk.Scale.new_with_range(Gtk.Orientation.HORIZONTAL, 8, 120, 1)
        self.spacing.set_value(self.preferences.line_height)
        self.spacing.set_digits(0)
        reference.pack_start(self.spacing, False, False, 0)
        reference.pack_start(paragraph("截图只存内存；退出后清除。滚轮标记位置，Shift 精调。\nScreenshots stay in memory. Shift + wheel fine-tunes the highlight."), False, False, 0)
        self.status = paragraph("先框选一张参考 / Capture a reference to begin.")
        reference.pack_start(self.status, False, False, 0)
        keys = page(notebook, "按键 / Shortcuts")
        keys.pack_start(paragraph("快速框选 / Capture"), False, False, 0)
        self.capture_binding = BindingButton(self.preferences.capture, True)
        keys.pack_start(self.capture_binding, False, False, 0)
        keys.pack_start(paragraph("按住查看 / Hold to peek"), False, False, 0)
        self.peek_binding = BindingButton(self.preferences.peek, False)
        keys.pack_start(self.peek_binding, False, False, 0)
        keys.pack_start(paragraph("点击后按键录入，可用 Ctrl / Alt / Shift。查看支持鼠标中键、前后侧键。\nClick then press a key or chord. Peek also supports middle and side mouse buttons."), False, False, 0)
        notebook.connect("switch-page", lambda *_: (self.capture_binding.cancel(), self.peek_binding.cancel()))
        runtime = page(notebook, "运行 / Startup")
        self.autostart = Gtk.CheckButton(label="登录后启动 / Start at login")
        self.autostart.set_active(self.autostart_path.exists())
        runtime.pack_start(self.autostart, False, False, 0)
        runtime.pack_start(paragraph("安装 .deb 后可用；默认关闭，自启时留在托盘。\nAvailable after installing the .deb. Disabled by default; launches in the tray."), False, False, 0)
        self.high_priority = Gtk.CheckButton(label="提高 CPU 优先级 / Higher CPU priority")
        self.high_priority.set_active(self.preferences.high_priority)
        runtime.pack_start(self.high_priority, False, False, 0)
        runtime.pack_start(paragraph("Linux 通常不允许普通进程提高优先级。失败会提示，不索取管理员权限。关闭会恢复普通优先级。\nLinux may refuse a negative nice value. Failure is reported; the app never requests root access."), False, False, 0)
        guide = page(notebook, "说明 / Guide")
        guide.pack_start(paragraph("1. F1 框选 / Capture with F1.\n2. 切回工作窗口 / Return to your task.\n3. 按住查看键，滚轮标记 / Hold to peek; wheel to mark.\n4. 松手收起，下次接着看 / Release; resume at the same line next time."), False, False, 0)
        guide.pack_start(paragraph("Esc 或右键取消框选，保留旧图。托盘支持暂停、恢复和退出。\nEsc/right-click cancels capture and preserves the old image. Pause, resume, or quit from the tray."), False, False, 0)
        guide.pack_start(paragraph("Ubuntu 20.04+，X11/Xorg 会话。按住查看期间暂时接管键盘和鼠标以隔离滚轮，松手恢复；原窗口焦点不变。Wayland 尚不支持。\nRequires Ubuntu 20.04+ with X11/Xorg. During a peek, keyboard/pointer input is grabbed to isolate scrolling, then released. Wayland is not supported yet."), False, False, 0)
        actions = Gtk.Box(spacing=8)
        actions.set_border_width(12)
        outer.pack_end(actions, False, False, 0)
        for title, callback in (("退出 / Quit", lambda *_: self.quit()), ("取消 / Cancel", self.close_settings), ("保存 / Save", self.save_and_close)):
            button = Gtk.Button(label=title)
            button.connect("clicked", callback)
            actions.pack_start(button, True, True, 0)
        self.capture_button = Gtk.Button(label="框选 / Capture")
        self.capture_button.connect("clicked", self.save_and_capture)
        actions.pack_start(self.capture_button, True, True, 0)

    def create_indicator(self):
        for namespace in ("AyatanaAppIndicator3", "AppIndicator3"):
            try:
                gi.require_version(namespace, "0.1")
                import importlib
                indicator_api = importlib.import_module("gi.repository." + namespace)
                self.indicator = indicator_api.Indicator.new("hold-to-peek", "view-reveal-symbolic", indicator_api.IndicatorCategory.APPLICATION_STATUS)
                self.indicator.set_status(indicator_api.IndicatorStatus.ACTIVE)
                menu = Gtk.Menu()
                for title, callback in (("框选 / Capture", self.begin_capture), ("设置 / Settings", self.show_settings),
                                        ("暂停 / 恢复 · Pause / Resume", self.toggle_pause), ("清除参考 / Clear", self.clear_reference), ("退出 / Quit", self.quit)):
                    item = Gtk.MenuItem(label=title)
                    item.connect("activate", lambda widget, action=callback: action())
                    menu.append(item)
                menu.show_all()
                self.indicator.set_menu(menu)
                return
            except (ImportError, ValueError):
                self.indicator = None

    def show_settings(self, *_):
        if self.busy:
            return False
        if self.input and not self.input.closed:
            self.input.suspend()
        self.peek.hide()
        self.capture_binding.binding = self.preferences.capture
        self.peek_binding.binding = self.preferences.peek
        self.capture_binding.cancel()
        self.peek_binding.cancel()
        self.spacing.set_value(self.preferences.line_height)
        self.autostart.set_active(self.autostart_path.exists())
        self.high_priority.set_active(self.preferences.high_priority)
        self.settings_open = True
        self.window.show_all()
        self.window.present()
        return False

    def close_settings(self, *_):
        self.settings_open = False
        self.capture_binding.cancel()
        self.peek_binding.cancel()
        if self.indicator is None:
            # Keep a launcher reachable when the desktop does not expose a tray indicator.
            self.window.iconify()
        else:
            self.window.hide()
        self.refresh_input(settings_closed=True)
        return True

    def set_autostart(self, enabled):
        if enabled:
            if not Path("/usr/bin/hold-to-peek").exists():
                raise ValueError("Install the .deb before enabling login startup.")
            self.autostart_path.parent.mkdir(parents=True, exist_ok=True)
            self.autostart_path.write_text("[Desktop Entry]\nType=Application\nName=Hold to Peek\nExec=/usr/bin/hold-to-peek --autostart\nTerminal=false\nX-GNOME-Autostart-enabled=true\n", encoding="utf-8")
        elif self.autostart_path.exists():
            self.autostart_path.unlink()

    def save(self):
        if self.capture_binding.recording or self.peek_binding.recording:
            self.error("Finish or cancel shortcut recording first.")
            return False
        changed = Preferences(self.capture_binding.binding, self.peek_binding.binding, int(self.spacing.get_value()), self.autostart.get_active(), self.high_priority.get_active())
        old = self.preferences
        old_startup = self.autostart_path.exists()
        old_nice = os.getpriority(os.PRIO_PROCESS, 0)
        try:
            changed.validate()
            if self.input and not self.input.closed:
                self.input.configure(changed, True, True)
                self.input.suspend()
            self.set_autostart(changed.start_at_login)
            try:
                if changed.high_priority != old.high_priority:
                    os.setpriority(os.PRIO_PROCESS, 0, -5 if changed.high_priority else 0)
            except OSError as error:
                changed.high_priority = old.high_priority
                self.high_priority.set_active(old.high_priority)
                self.error("CPU priority was not changed: " + str(error))
            save_preferences(self.settings_path, changed)
        except Exception as error:
            try:
                self.set_autostart(old_startup)
                os.setpriority(os.PRIO_PROCESS, 0, old_nice)
            except (OSError, ValueError):
                pass
            self.error(str(error))
            return False
        self.preferences = changed
        return True

    def save_and_close(self, *_):
        if self.save():
            self.close_settings()

    def save_and_capture(self, *_):
        if self.save():
            self.begin_capture()

    def refresh_input(self, settings_closed=False):
        if not self.input or self.input.closed:
            return
        try:
            if self.busy or (self.settings_open and not settings_closed):
                self.input.suspend()
            else:
                self.input.configure(self.preferences, True, self.reference is not None and not self.paused)
        except Exception as error:
            self.error(str(error))

    def begin_capture(self, *_):
        if self.busy or self.startup_error or not self.input or self.input.closed:
            if self.startup_error:
                self.show_settings()
            return False
        self.busy = True
        self.epoch += 1
        epoch = self.epoch
        return_to_settings = self.settings_open
        self.settings_open = False
        self.input.suspend()
        self.peek.hide()
        self.window.hide()

        def start():
            if epoch != self.epoch:
                return False
            try:
                display = Gdk.Display.get_default()
                _, x, y = display.get_default_seat().get_pointer().get_position()
                monitor = display.get_monitor_at_point(x, y)
                rect = monitor.get_geometry()
                image = Gdk.pixbuf_get_from_window(Gdk.get_default_root_window(), rect.x, rect.y, rect.width, rect.height)
                if image is None:
                    raise RuntimeError("Cannot capture this X11 screen.")
                self.capture_window = CaptureWindow(image, monitor, lambda result: self.capture_complete(result, return_to_settings, epoch))
            except Exception as error:
                self.busy = False
                self.show_settings()
                self.error(str(error))
            return False
        GLib.timeout_add(180, start)
        return False

    def capture_complete(self, image, return_to_settings, epoch):
        self.capture_window = None
        self.busy = False
        if epoch != self.epoch:
            return
        if image is not None:
            self.reference = image
            self.progress.reset(image.get_height(), self.preferences.line_height)
            self.paused = False
            self.status.set_text("参考已准备好 / Reference ready: " + self.preferences.peek.label())
        if image is None and return_to_settings:
            self.show_settings()
        else:
            self.refresh_input()

    def on_hold(self, held):
        if held and self.input and self.input.held and self.reference is not None and not self.busy and not self.paused:
            self.peek.show_reference(self.reference, self.progress.position, self.preferences.line_height)
        else:
            self.peek.hide()
        return False

    def on_wheel(self, steps, fine):
        if self.reference is not None and not self.busy and not self.paused:
            self.progress.move(steps, self.preferences.line_height, fine)
            self.peek.position = self.progress.position
            self.peek.area.queue_draw()
        return False

    def toggle_pause(self, *_):
        self.paused = not self.paused
        self.status.set_text("已暂停 / Paused" if self.paused else "已恢复 / Ready")
        self.peek.hide()
        self.refresh_input()

    def clear_reference(self, *_):
        self.reference = None
        self.peek.image = None
        self.peek.hide()
        self.progress.reset(0, self.preferences.line_height)
        self.paused = False
        self.status.set_text("参考已清除 / Reference cleared")
        self.refresh_input()

    def interrupt(self):
        self.epoch += 1
        if self.input and not self.input.closed:
            self.input.suspend()
        self.peek.hide()
        if self.capture_window:
            self.capture_window.finish(None)
        self.busy = False

    def monitor_changed(self):
        self.interrupt()
        GLib.idle_add(self.refresh_input)

    def subscribe_session_changes(self):
        try:
            bus = Gio.bus_get_sync(Gio.BusType.SYSTEM, None)
            self.sleep_subscription = (bus, bus.signal_subscribe("org.freedesktop.login1", "org.freedesktop.login1.Manager", "PrepareForSleep", "/org/freedesktop/login1", None, Gio.DBusSignalFlags.NONE, self.power_changed, None))
        except GLib.Error:
            pass
        try:
            bus = Gio.bus_get_sync(Gio.BusType.SESSION, None)
            self.lock_subscription = (bus, bus.signal_subscribe(None, "org.gnome.ScreenSaver", "ActiveChanged", "/org/gnome/ScreenSaver", None, Gio.DBusSignalFlags.NONE, self.power_changed, None))
        except GLib.Error:
            self.lock_subscription = None

    def power_changed(self, connection, sender, path, interface, signal, parameters, data):
        active = parameters.unpack()[0]
        self.interrupt()
        if not active:
            self.refresh_input()

    def input_failed(self, message):
        self.peek.hide()
        self.status.set_text(message)
        self.show_settings()
        return False

    def error(self, message):
        self.status.set_text(message)
        dialog = Gtk.MessageDialog(transient_for=self.window, modal=True, message_type=Gtk.MessageType.INFO,
                                   buttons=Gtk.ButtonsType.OK, text=message)
        dialog.run()
        dialog.destroy()

    def do_shutdown(self):
        self.epoch += 1
        if self.input:
            self.input.close()
        for subscription in (self.sleep_subscription, getattr(self, "lock_subscription", None)):
            if subscription:
                subscription[0].signal_unsubscribe(subscription[1])
        self.reference = None
        self.peek.image = None
        self.peek.destroy()
        Gtk.Application.do_shutdown(self)


def main():
    return Application().run(sys.argv)
