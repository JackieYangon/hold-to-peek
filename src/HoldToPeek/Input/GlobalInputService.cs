using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using HoldToPeek.Core;

namespace HoldToPeek.Input;

/// <summary>
/// Installs global hooks on a dedicated message thread. Event handlers must only queue work;
/// they must not draw, wait for the UI thread, or access files.
/// </summary>
public sealed class GlobalInputService : IDisposable
{
    private const uint SyncTimerMessage = 0x8001;
    private readonly object _gate = new();
    private readonly HoldState _state = new();
    private readonly ManualResetEventSlim _started = new(false);
    private readonly Native.HookProc _mouseProc;
    private readonly Native.HookProc _keyboardProc;
    private Thread? _thread;
    private Exception? _startupError;
    private volatile uint _threadId;
    private nint _mouseHook;
    private nint _keyboardHook;
    private nuint _timerId;
    private bool _disposed;
    private bool _escapeDown;
    private bool _escapeConsumed;
    private bool _sawAsyncDown;
    private bool _watchdogResetRequested;
    private int? _watchedTrigger;

    public GlobalInputService()
    {
        _mouseProc = MouseHook;
        _keyboardProc = KeyboardHook;
    }

    public event Action<bool>? HoldChanged;
    public event Action<int>? WheelMoved;
    public event Action? CancelRequested;
    public event Action<Exception>? Failed;

    public InputBinding Binding
    {
        get { lock (_gate) return _state.Binding; }
        set
        {
            if (value is not { IsValid: true })
                throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate)
            {
                bool before = _state.IsHeld;
                _state.Binding = value;
                PublishTransition(before);
            }
            RequestTimerSync();
        }
    }

    public bool Enabled
    {
        get { lock (_gate) return _state.Enabled; }
        set
        {
            lock (_gate)
            {
                bool before = _state.IsHeld;
                _state.Enabled = value && !_disposed;
                PublishTransition(before);
            }
            RequestTimerSync();
        }
    }

    public bool IsHeld { get { lock (_gate) return _state.IsHeld; } }

    public void Start()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_thread is not null)
                throw new InvalidOperationException("输入监听已经启动。");
            _thread = new Thread(MessageLoop) { IsBackground = true, Name = "HoldToPeek.Input" };
            _thread.Start();
        }
        _started.Wait();
        if (_startupError is not null)
            throw new InvalidOperationException("无法启动全局输入监听。", _startupError);
    }

    public void CancelHold()
    {
        lock (_gate)
        {
            bool before = _state.IsHeld;
            _state.Cancel();
            PublishTransition(before);
        }
        RequestTimerSync();
    }

    /// <summary>
    /// Use only after session, power or display transitions that may drop release events.
    /// Ordinary Escape/pause cancellation must continue to use CancelHold instead.
    /// </summary>
    public void ResetAfterInterruption()
    {
        lock (_gate)
        {
            bool before = _state.IsHeld;
            _state.ResetAfterInterruption();
            _escapeDown = false;
            _watchdogResetRequested = true;
            // _escapeConsumed also retains its release debt across the transition.
            PublishTransition(before);
        }
        RequestTimerSync();
    }

    private void MessageLoop()
    {
        bool startupComplete = false;
        try
        {
            _threadId = Native.GetCurrentThreadId();
            Native.PeekMessage(out _, 0, 0, 0, 0); // Create the queue before Start returns.
            nint module = Native.GetModuleHandle(null);
            _mouseHook = Native.SetWindowsHookEx(14, _mouseProc, module, 0);
            if (_mouseHook == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "鼠标监听注册失败。");
            _keyboardHook = Native.SetWindowsHookEx(13, _keyboardProc, module, 0);
            if (_keyboardHook == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "键盘监听注册失败。");
            startupComplete = true;
            _started.Set();
            lock (_gate)
            {
                if (_disposed)
                    return;
            }

            while (true)
            {
                int result = Native.GetMessage(out Native.Message message, 0, 0, 0);
                if (result == 0)
                    break;
                if (result < 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (message.Id == SyncTimerMessage)
                    SyncWatchdog();
                else if (message.Id == 0x0113)
                    CheckReleased();
                else
                {
                    Native.TranslateMessage(ref message);
                    Native.DispatchMessage(ref message);
                }
            }
        }
        catch (Exception exception)
        {
            if (!startupComplete)
                _startupError = exception;
            ReportFailure(exception);
        }
        finally
        {
            StopWatchdog();
            if (_mouseHook != 0)
                Native.UnhookWindowsHookEx(_mouseHook);
            if (_keyboardHook != 0)
                Native.UnhookWindowsHookEx(_keyboardHook);
            _mouseHook = _keyboardHook = 0;
            lock (_gate)
            {
                bool before = _state.IsHeld;
                _state.Enabled = false;
                PublishTransition(before);
            }
            _threadId = 0;
            _started.Set();
        }
    }

    private nint MouseHook(int code, nuint message, nint data)
    {
        if (code < 0)
            return Native.CallNextHookEx(0, code, message, data);
        try
        {
            if (message is 0x020B or 0x020C)
            {
                Native.MouseHookData input = Marshal.PtrToStructure<Native.MouseHookData>(data);
                uint button = input.MouseData >> 16;
                if (button is 1 or 2)
                {
                    if (ProcessTrigger(button == 1 ? 5 : 6, message == 0x020B))
                        return 1;
                }
            }
            else if (message is 0x0207 or 0x0208)
            {
                if (ProcessTrigger(4, message == 0x0207)) return 1;
            }
            else if (message is 0x020A or 0x020E)
            {
                lock (_gate)
                {
                    if (_state.IsHeld)
                    {
                        if (message == 0x020A)
                        {
                            Native.MouseHookData input = Marshal.PtrToStructure<Native.MouseHookData>(data);
                            int delta = unchecked((short)(input.MouseData >> 16));
                            Invoke(() => WheelMoved?.Invoke(delta));
                        }
                        return 1; // Horizontal wheel cannot accidentally scroll the underlying app either.
                    }
                }
            }
        }
        catch (Exception exception)
        {
            ReportFailure(exception);
            CancelHold();
        }
        return Native.CallNextHookEx(0, code, message, data);
    }

    private nint KeyboardHook(int code, nuint message, nint data)
    {
        if (code < 0)
            return Native.CallNextHookEx(0, code, message, data);
        try
        {
            bool down = message is 0x0100 or 0x0104;
            bool up = message is 0x0101 or 0x0105;
            if (!down && !up)
                return Native.CallNextHookEx(0, code, message, data);
            Native.KeyboardData input = Marshal.PtrToStructure<Native.KeyboardData>(data);
            if (ProcessTrigger((int)input.VirtualKey, down))
                return 1;
            if (input.VirtualKey == 0x1B)
            {
                bool consume;
                lock (_gate)
                {
                    if (down && !_escapeDown)
                    {
                        // A fresh down supersedes any release debt retained across a system reset.
                        _escapeConsumed = _state.IsHeld;
                        if (_escapeConsumed)
                        {
                            bool before = _state.IsHeld;
                            _state.Cancel();
                            PublishTransition(before);
                            Invoke(() => CancelRequested?.Invoke());
                        }
                    }
                    consume = _escapeConsumed;
                    if (up)
                        _escapeConsumed = false;
                    _escapeDown = down;
                }
                if (consume)
                {
                    SyncWatchdog();
                    return 1;
                }
            }
        }
        catch (Exception exception)
        {
            ReportFailure(exception);
            CancelHold();
        }
        return Native.CallNextHookEx(0, code, message, data);
    }

    private bool ProcessTrigger(int key, bool down)
    {
        bool consume;
        lock (_gate)
        {
            bool before = _state.IsHeld;
            consume = _state.ProcessKey(key, down, CurrentModifiers(key, down));
            PublishTransition(before);
        }
        SyncWatchdog();
        return consume;
    }

    private static int CurrentModifiers(int key, bool down)
    {
        int modifiers = (Native.GetAsyncKeyState(0x12) < 0 ? 1 : 0) |
            (Native.GetAsyncKeyState(0x11) < 0 ? 2 : 0) | (Native.GetAsyncKeyState(0x10) < 0 ? 4 : 0);
        int changed = key switch { 18 or 164 or 165 => 1, 17 or 162 or 163 => 2, 16 or 160 or 161 => 4, _ => 0 };
        return down ? modifiers | changed : modifiers & ~changed;
    }

    private void PublishTransition(bool previous)
    {
        if (previous != _state.IsHeld)
        {
            bool held = _state.IsHeld;
            Invoke(() => HoldChanged?.Invoke(held));
        }
    }

    private void RequestTimerSync()
    {
        uint id = _threadId;
        if (id != 0)
            Native.PostThreadMessage(id, SyncTimerMessage, 0, 0);
    }

    private void SyncWatchdog()
    {
        lock (_gate)
        {
            if (_watchdogResetRequested)
            {
                StopWatchdog();
                _watchdogResetRequested = false;
            }
            if (!_state.IsHeld)
            {
                StopWatchdog();
                return;
            }
            if (_timerId != 0 && _watchedTrigger != _state.HeldKey)
                StopWatchdog();
            if (_timerId == 0)
            {
                _watchedTrigger = _state.HeldKey;
                _sawAsyncDown = false;
                _timerId = Native.SetTimer(0, 0, 150, 0);
            }
        }
    }

    private void CheckReleased()
    {
        lock (_gate)
        {
            if (!_state.IsHeld || _watchedTrigger is not int key)
            {
                StopWatchdog();
                return;
            }
            bool down = Native.GetAsyncKeyState(key) < 0;
            // Suppressed low-level input may never update async key state. Only infer a
            // missing release after a real down was observed, not from an initial false.
            if (down)
                _sawAsyncDown = true;
            else if (_sawAsyncDown)
            {
                bool before = _state.IsHeld;
                _state.RecoverReleased(key);
                PublishTransition(before);
                StopWatchdog();
            }
        }
    }

    private void StopWatchdog()
    {
        if (_timerId != 0)
            Native.KillTimer(0, _timerId);
        _timerId = 0;
        _watchedTrigger = null;
        _sawAsyncDown = false;
    }

    private void Invoke(Action action)
    {
        try { action(); }
        catch (Exception exception) { ReportFailure(exception); }
    }

    private void ReportFailure(Exception exception)
    {
        try { Failed?.Invoke(exception); }
        catch { /* Never propagate managed exceptions through a native hook callback. */ }
    }

    public void Dispose()
    {
        Thread? thread;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            bool before = _state.IsHeld;
            _state.Enabled = false;
            PublishTransition(before);
            thread = _thread;
        }
        if (_threadId != 0)
            Native.PostThreadMessage(_threadId, 0x0012, 0, 0);
        if (thread is not null && Thread.CurrentThread != thread)
            thread.Join();
        GC.KeepAlive(_mouseProc);
        GC.KeepAlive(_keyboardProc);
    }

    private static class Native
    {
        internal delegate nint HookProc(int code, nuint message, nint data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseHookData
        {
            public Point Point;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public nuint ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardData
        {
            public uint VirtualKey;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public nuint ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Message
        {
            public nint Window;
            public uint Id;
            public nuint WParam;
            public nint LParam;
            public uint Time;
            public Point Point;
            public uint Private;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        internal static extern nint SetWindowsHookEx(int id, HookProc callback, nint module, uint threadId);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(nint hook);
        [DllImport("user32.dll")]
        internal static extern nint CallNextHookEx(nint hook, int code, nuint message, nint data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint GetModuleHandle(string? name);
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetMessage(out Message message, nint window, uint min, uint max);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(out Message message, nint window, uint min, uint max, uint remove);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TranslateMessage(ref Message message);
        [DllImport("user32.dll")]
        internal static extern nint DispatchMessage(ref Message message);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam);
        [DllImport("user32.dll")]
        internal static extern nuint SetTimer(nint window, nuint id, uint milliseconds, nint callback);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool KillTimer(nint window, nuint id);
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int key);
    }
}
