"""GTK surfaces. Reference images are kept only as in-memory GdkPixbuf objects."""
import gi
gi.require_version("Gtk", "3.0")
from gi.repository import Gdk, Gtk
from .core import Binding, clipped_selection


def paragraph(text):
    label = Gtk.Label(label=text, xalign=0)
    label.set_line_wrap(True)
    label.set_max_width_chars(65)
    label.set_selectable(True)
    return label


def page(notebook, title):
    box = Gtk.Box(orientation=Gtk.Orientation.VERTICAL, spacing=16)
    box.set_border_width(24)
    scroll = Gtk.ScrolledWindow()
    scroll.set_policy(Gtk.PolicyType.NEVER, Gtk.PolicyType.AUTOMATIC)
    scroll.add(box)
    notebook.append_page(scroll, Gtk.Label(label=title))
    return box


class BindingButton(Gtk.Button):
    def __init__(self, binding, capture):
        super().__init__(label=binding.label() + " · 点击修改 / Change")
        self.binding = binding
        self.capture = capture
        self.recording = False
        self.connect("clicked", self.record)
        self.connect("key-press-event", self.key)
        self.connect("button-press-event", self.mouse)
        self.connect("focus-out-event", lambda *_: self.cancel())

    def record(self, *_):
        self.recording = True
        self.set_label("请按快捷键 / Press a shortcut · Esc cancels")

    def cancel(self):
        self.recording = False
        self.set_label(self.binding.label() + " · 点击修改 / Change")
        return False

    def accept(self, binding):
        try:
            binding.validate(self.capture)
        except ValueError as error:
            self.set_label(str(error))
            return
        self.binding = binding
        self.cancel()

    def key(self, widget, event):
        if not self.recording:
            return False
        if event.keyval == Gdk.KEY_Escape:
            self.cancel()
        else:
            key = Gdk.keyval_name(Gdk.keyval_to_lower(event.keyval))
            if key:
                self.accept(Binding("key", key, int(event.state) & 13))
        return True

    def mouse(self, widget, event):
        if self.recording and not self.capture and event.button in (2, 8, 9):
            self.accept(Binding("button", str(event.button), int(event.state) & 13))
            return True
        return False


class PeekWindow(Gtk.Window):
    def __init__(self):
        super().__init__(type=Gtk.WindowType.TOPLEVEL)
        self.set_decorated(False)
        self.set_accept_focus(False)
        self.set_focus_on_map(False)
        self.set_keep_above(True)
        self.set_skip_taskbar_hint(True)
        self.set_skip_pager_hint(True)
        self.set_type_hint(Gdk.WindowTypeHint.TOOLTIP)
        self.image = None
        self.position = 0
        self.spacing = 28
        self.scale = 1
        self.area = Gtk.DrawingArea()
        self.area.connect("draw", self.draw)
        self.add(self.area)

    def show_reference(self, image, position, spacing):
        self.image = image
        self.position = position
        self.spacing = spacing
        display = Gdk.Display.get_default()
        _, x, y = display.get_default_seat().get_pointer().get_position()
        monitor = display.get_monitor_at_point(x, y)
        work = monitor.get_workarea()
        width = max(1, min(560, work.width - 32, image.get_width()))
        self.scale = width / image.get_width()
        height = max(1, min(int(image.get_height() * self.scale), work.height - 40, 650))
        self.area.set_size_request(width, height)
        self.resize(width, height)
        px = x + 24 if x + width + 24 <= work.x + work.width else x - width - 24
        py = y + 20 if y + height + 20 <= work.y + work.height else y - height - 20
        self.move(max(work.x, min(px, work.x + work.width - width)), max(work.y, min(py, work.y + work.height - height)))
        self.show_all()
        self.get_window().set_pass_through(True)
        self.area.queue_draw()

    def draw(self, area, context):
        if self.image is None:
            return False
        height = area.get_allocated_height()
        offset = max(0, min(self.position * self.scale - height / 2, self.image.get_height() * self.scale - height))
        context.save()
        context.scale(self.scale, self.scale)
        Gdk.cairo_set_source_pixbuf(context, self.image, 0, -offset / self.scale)
        context.paint()
        context.restore()
        y = self.position * self.scale - offset
        context.set_source_rgba(0.60, 0.88, 0.35, 0.28)
        context.rectangle(0, y - self.spacing * self.scale / 2, area.get_allocated_width(), self.spacing * self.scale)
        context.fill()
        context.set_source_rgb(0.2, 0.55, 0.24)
        context.set_line_width(2)
        context.move_to(0, y)
        context.line_to(area.get_allocated_width(), y)
        context.stroke()
        return False


class CaptureWindow(Gtk.Window):
    def __init__(self, image, monitor, complete):
        super().__init__(type=Gtk.WindowType.TOPLEVEL)
        self.image = image
        self.complete_callback = complete
        self.finished = False
        self.start = None
        self.end = (0, 0)
        self.set_decorated(False)
        self.set_keep_above(True)
        geometry = monitor.get_geometry()
        self.move(geometry.x, geometry.y)
        self.set_default_size(geometry.width, geometry.height)
        self.area = Gtk.DrawingArea()
        self.area.add_events(Gdk.EventMask.BUTTON_PRESS_MASK | Gdk.EventMask.BUTTON_RELEASE_MASK | Gdk.EventMask.POINTER_MOTION_MASK)
        self.area.connect("draw", self.draw)
        self.area.connect("button-press-event", self.press)
        self.area.connect("motion-notify-event", self.motion)
        self.area.connect("button-release-event", self.release)
        self.connect("key-press-event", self.key)
        self.connect("delete-event", lambda *_: self.finish(None))
        self.add(self.area)
        screen = self.get_screen()
        # fullscreen_on_monitor takes the legacy screen index, not the GdkMonitor object.
        monitor_number = screen.get_monitor_at_point(geometry.x, geometry.y)
        self.fullscreen_on_monitor(screen, monitor_number)
        self.show_all()
        self.present()

    def finish(self, image):
        if not self.finished:
            self.finished = True
            self.destroy()
            self.image = None
            self.complete_callback(image)
        return True

    def key(self, widget, event):
        return self.finish(None) if event.keyval == Gdk.KEY_Escape else False

    def press(self, widget, event):
        if event.button == 3:
            return self.finish(None)
        if event.button == 1:
            self.start = (event.x, event.y)
            self.end = self.start
            self.area.queue_draw()
        return True

    def motion(self, widget, event):
        if self.start:
            self.end = (event.x, event.y)
            self.area.queue_draw()
        return True

    def release(self, widget, event):
        if event.button != 1 or self.start is None:
            return True
        self.end = (event.x, event.y)
        width, height = self.area.get_allocated_width(), self.area.get_allocated_height()
        x, y, w, h = clipped_selection(self.start, self.end, width, height)
        if w < 4 or h < 4:
            self.start = None
            self.area.queue_draw()
            return True
        sx, sy = self.image.get_width() / width, self.image.get_height() / height
        return self.finish(self.image.new_subpixbuf(int(x * sx), int(y * sy), max(1, int(w * sx)), max(1, int(h * sy))).copy())

    def draw(self, area, context):
        width, height = area.get_allocated_width(), area.get_allocated_height()
        context.save()
        context.scale(width / self.image.get_width(), height / self.image.get_height())
        Gdk.cairo_set_source_pixbuf(context, self.image, 0, 0)
        context.paint()
        context.restore()
        context.set_source_rgba(0, 0, 0, 0.35)
        context.paint()
        context.set_source_rgb(0.75, 0.95, 0.50)
        if self.start:
            x, y, w, h = clipped_selection(self.start, self.end, width, height)
            context.set_line_width(2)
            context.rectangle(x, y, w, h)
            context.stroke()
        context.move_to(24, 34)
        context.set_font_size(18)
        context.show_text("Drag to capture  |  Esc / right-click to cancel")
        return False
