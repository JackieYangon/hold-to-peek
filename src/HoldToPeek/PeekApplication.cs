using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HoldToPeek.Core;
using HoldToPeek.Input;
using HoldToPeek.Services;
using HoldToPeek.Views;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace HoldToPeek;

internal sealed class PeekApplication : Application
{
    private readonly EventWaitHandle _showSignal;
    private readonly SettingsStore _store = new();
    private readonly ReadingProgress _progress = new();
    private readonly GlobalInputService _input = new();
    private readonly string _version = typeof(PeekApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
    private AppSettings _settings = new();
    private PeekWindow? _peek;
    private SettingsWindow? _settingsWindow;
    private CaptureWindow? _captureWindow;
    private Forms.NotifyIcon? _tray;
    private Forms.ContextMenuStrip? _menu;
    private Forms.ToolStripMenuItem? _pauseItem;
    private Forms.ToolStripMenuItem? _statusItem;
    private Forms.ToolStripMenuItem? _captureItem;
    private Forms.ToolStripMenuItem? _resetItem;
    private Forms.ToolStripMenuItem? _clearItem;
    private Icon? _icon;
    private RegisteredWaitHandle? _showRegistration;
    private bool _hasReference;
    private bool _paused;
    private bool _busy;
    private bool _exiting;
    private bool _inputFailed;
    private int _captureEpoch;

    public PeekApplication(EventWaitHandle showSignal)
    {
        _showSignal = showSignal;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _settings = _store.Load();
            _progress.LineHeight = _settings.LineHeight;
            _peek = new PeekWindow();
            CreateTray();
            _input.Trigger = _settings.Trigger;
            _input.HoldChanged += held => Post(() => OnHoldChanged(held));
            _input.WheelMoved += delta =>
            {
                bool fine = GetAsyncKeyState(0x10) < 0;
                Post(() => OnWheel(delta, fine));
            };
            // Escape is already applied by the hook and emits HoldChanged(false).
            // Do not queue another CancelHold: it could cancel a newer press.
            _input.Failed += error => Post(() => OnInputFailed(error));
            _input.Start();
            _showRegistration = ThreadPool.RegisterWaitForSingleObject(_showSignal,
                (_, _) => Post(ShowSettings), null, Timeout.Infinite, false);
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            DispatcherUnhandledException += OnDispatcherException;
            RefreshState();
            // An empty session always needs a reference. Opening this small window
            // makes a newly launched tray app discoverable without persisting captures.
            ShowSettings();
            if (_store.LoadWarning is string warning) Notify("已恢复默认设置", warning);
        }
        catch (Exception error)
        {
            MessageBox.Show($"无法启动瞄一眼：{error.Message}", "瞄一眼", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void CreateTray()
    {
        _icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (Icon)SystemIcons.Application.Clone();
        _menu = new Forms.ContextMenuStrip();
        _statusItem = new Forms.ToolStripMenuItem("还没有参考截图") { Enabled = false };
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _captureItem = new Forms.ToolStripMenuItem("框选参考…", null, (_, _) => Post(BeginCapture));
        _menu.Items.Add(_captureItem);
        _menu.Items.Add("设置与使用说明…", null, (_, _) => Post(ShowSettings));
        _pauseItem = new Forms.ToolStripMenuItem("暂停瞄一眼", null, (_, _) => Post(TogglePaused));
        _menu.Items.Add(_pauseItem);
        _resetItem = new Forms.ToolStripMenuItem("高亮回到顶部", null, (_, _) => Post(() => { _progress.ResetToTop(); HidePeek(); }));
        _clearItem = new Forms.ToolStripMenuItem("清除参考截图", null, (_, _) => Post(ClearReference));
        _menu.Items.Add(_resetItem);
        _menu.Items.Add(_clearItem);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("退出", null, (_, _) => Post(() => Shutdown()));
        _tray = new Forms.NotifyIcon { Icon = _icon, Text = "瞄一眼 · 按住看参考", ContextMenuStrip = _menu, Visible = true };
        _tray.DoubleClick += (_, _) => Post(BeginCapture);
    }

    private void ShowSettings()
    {
        if (_exiting) return;
        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized) _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Activate();
            return;
        }
        if (_busy) return;
        HidePeek();
        _busy = true;
        RefreshState();
        var window = new SettingsWindow(_settings, _version, _peek?.Reference, _progress.Position, _paused);
        _settingsWindow = window;
        window.Closed += (_, _) =>
        {
            _settingsWindow = null;
            _busy = false;
            if (window.SavedSettings is { } changed)
            {
                _settings = changed;
                _progress.LineHeight = changed.LineHeight;
                _input.Trigger = changed.Trigger;
                try { _store.Save(changed); }
                catch (Exception error) { Notify("设置暂未保存", $"本次仍会使用新设置。{error.Message}"); }
            }
            RefreshState();
            if (window.CaptureRequested) Post(() => BeginCapture(returnToSettings: true));
        };
        window.Show();
        window.Activate();
    }

    private void BeginCapture() => BeginCapture(returnToSettings: false);

    private async void BeginCapture(bool returnToSettings)
    {
        if (_exiting || _busy) return;
        _busy = true;
        int captureEpoch = ++_captureEpoch;
        bool captured = false;
        HidePeek();
        RefreshState();
        try
        {
            // Let the tray menu disappear before reading the desktop.
            await System.Threading.Tasks.Task.Delay(180);
            if (_exiting || captureEpoch != _captureEpoch) return;
            var screen = ScreenCapture.CaptureAtCursor();
            var selection = new CaptureWindow(screen);
            _captureWindow = selection;
            if (selection.ShowDialog() == true && selection.SelectedImage is BitmapSource image)
            {
                _peek!.SetReference(image);
                _progress.Reset(image.PixelHeight);
                _progress.LineHeight = _settings.LineHeight;
                _hasReference = true;
                _paused = false;
                captured = true;
                Notify("参考已准备好", $"按住{TriggerName()}查看，滚轮移动高亮；松手收起。\nShift + 滚轮精调位置；右键托盘可调整行距。");
            }
        }
        catch (Exception error)
        {
            Notify("这次没有截取成功", $"已有参考会保留。{error.Message}");
        }
        finally
        {
            _captureWindow = null;
            _busy = false;
            RefreshState();
            // Return to the place that started capture, but never reopen UI after
            // a lock/display/power interruption or an application shutdown.
            if (returnToSettings && !captured && !_exiting && captureEpoch == _captureEpoch)
                Post(() =>
                {
                    if (captureEpoch == _captureEpoch) ShowSettings();
                });
        }
    }

    private void OnHoldChanged(bool held)
    {
        if (_exiting) return;
        if (held && _input.IsHeld && _hasReference && !_busy && !_paused)
        {
            try { _peek!.ShowNearCursor(_progress.Position, _progress.LineHeight); }
            catch (Exception error) { HidePeek(); Notify("暂时无法显示参考", error.Message); }
        }
        else _peek?.Hide();
    }

    private void OnWheel(int delta, bool fine)
    {
        // The hook already owns this wheel event. Preserve its reading movement
        // even when release arrives before the UI dispatcher processes the queue.
        if (_exiting || !_hasReference || _paused || _busy) return;
        _progress.Move(delta, fine);
        _peek?.UpdateHighlight(_progress.Position, _progress.LineHeight);
    }

    private void HidePeek()
    {
        _input.CancelHold();
        _peek?.Hide();
    }

    private void ClearReference()
    {
        HidePeek();
        _hasReference = false;
        _paused = false;
        _progress.Reset(0);
        _peek?.ClearReference();
        RefreshState();
    }

    private void TogglePaused()
    {
        _paused = !_paused;
        HidePeek();
        RefreshState();
    }

    private void RefreshState()
    {
        _input.Enabled = !_exiting && !_inputFailed && !_busy && !_paused && _hasReference;
        if (_pauseItem is not null)
        {
            _pauseItem.Checked = _paused;
            _pauseItem.Text = _paused ? "恢复瞄一眼" : "暂停瞄一眼";
            _pauseItem.Enabled = !_busy && !_inputFailed && _hasReference;
        }
        if (_captureItem is not null) _captureItem.Enabled = !_busy;
        if (_resetItem is not null) _resetItem.Enabled = !_busy && _hasReference;
        if (_clearItem is not null) _clearItem.Enabled = !_busy && _hasReference;
        string status = _inputFailed ? "输入监听不可用，请重启" : _busy ? "设置或框选中 · 暂不接管按键" : _paused
            ? "已暂停 · 输入保持原样" : _hasReference ? $"按住{TriggerName()}查看参考" : "还没有参考截图";
        if (_statusItem is not null) _statusItem.Text = status;
        if (_tray is not null) _tray.Text = "瞄一眼 · " + status;
    }

    private string TriggerName() => _settings.Trigger switch
    {
        PeekTrigger.MouseForward => "鼠标前进侧键",
        PeekTrigger.F8 => " F8 ",
        _ => "鼠标后退侧键"
    };

    private void OnInputFailed(Exception error)
    {
        if (_exiting) return;
        _inputFailed = true;
        HidePeek();
        RefreshState();
        Notify("输入监听已停止", $"请退出后重新启动瞄一眼。{error.Message}");
    }

    private void Notify(string title, string message)
    {
        if (!_exiting) _tray?.ShowBalloonTip(3500, title, message, Forms.ToolTipIcon.Info);
    }

    private void Post(Action action)
    {
        if (!_exiting && !Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(action, DispatcherPriority.Input);
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e) => Post(CancelTransientUi);
    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Post(CancelTransientUi);
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e) => Post(CancelTransientUi);

    private void CancelTransientUi()
    {
        ++_captureEpoch;
        _input.ResetAfterInterruption();
        _peek?.Hide();
        _captureWindow?.Close();
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        // Stop on an unexpected UI failure instead of leaving an input hook active
        // in an uncertain state. Captures are never written to diagnostic files.
        _input.Enabled = false;
        _input.Dispose();
        _peek?.Hide();
        MessageBox.Show($"瞄一眼遇到错误，将安全退出：{e.Exception.Message}", "瞄一眼", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _exiting = true;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _showRegistration?.Unregister(null);
        _input.Dispose();
        _peek?.ClearReference();
        _peek?.Close();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        _menu?.Dispose();
        _icon?.Dispose();
        base.OnExit(e);
    }
}
