using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace HoldToPeek.Input;

/// <summary>Owns a non-repeating OS shortcut; conflicts never disable the tray capture entry.</summary>
internal sealed class CaptureHotkey : IDisposable
{
    private readonly HwndSource _source;
    private bool _registered;
    public event Action? Pressed;

    public CaptureHotkey()
    {
        _source = new HwndSource(new HwndSourceParameters("HoldToPeek.Shortcuts")
        { ParentWindow = new nint(-3), WindowStyle = 0, Width = 0, Height = 0 });
        _source.AddHook(OnMessage);
    }

    public void Register(uint key = 0x70, uint modifiers = 0)
    {
        if (_registered) UnregisterHotKey(_source.Handle, 1);
        _registered = RegisterHotKey(_source.Handle, 1, modifiers | 0x4000, key);
        if (!_registered) throw new Win32Exception(Marshal.GetLastWin32Error(), "框选快捷键被占用或不可用，请更换按键；仍可从托盘框选。");
    }

    private nint OnMessage(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x0312 && wParam == 1)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return 0;
    }

    public void Suspend()
    {
        if (_registered) UnregisterHotKey(_source.Handle, 1);
        _registered = false;
    }

    public void Dispose()
    {
        if (_registered) UnregisterHotKey(_source.Handle, 1);
        _registered = false;
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
