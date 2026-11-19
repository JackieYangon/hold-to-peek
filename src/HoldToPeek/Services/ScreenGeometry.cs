using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace HoldToPeek.Services;

/// <summary>Coordinates returned here are physical desktop pixels (PerMonitorV2).</summary>
internal static class ScreenGeometry
{
    internal const uint NoActivate = 0x0010;
    internal const uint ShowWindow = 0x0040;
    internal const uint NoSize = 0x0001;
    internal static readonly IntPtr Topmost = new(-1);

    internal static Point CursorPosition()
    {
        if (!GetCursorPos(out NativePoint point))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取鼠标位置。");
        return new Point(point.X, point.Y);
    }

    internal static (Rectangle Bounds, Rectangle WorkArea) MonitorAt(Point point)
    {
        IntPtr monitor = MonitorFromPoint(new NativePoint { X = point.X, Y = point.Y }, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取显示器范围。");
        return (info.Monitor.ToRectangle(), info.Work.ToRectangle());
    }

    internal static double DpiScale(IntPtr window)
    {
        uint dpi = GetDpiForWindow(window);
        return dpi > 0 ? dpi / 96.0 : 1.0;
    }

    internal static void PositionWindow(IntPtr window, Rectangle rectangle, bool show = false)
    {
        if (!SetWindowPos(window, Topmost, rectangle.X, rectangle.Y,
                rectangle.Width, rectangle.Height, NoActivate | (show ? ShowWindow : 0)))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法定位参考窗口。");
    }

    internal static void MoveWindow(IntPtr window, Point point)
    {
        if (!SetWindowPos(window, Topmost, point.X, point.Y, 0, 0, NoActivate | NoSize))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法定位参考窗口。");
    }

    internal static void MakePassive(IntPtr window)
    {
        const int extendedStyle = -20;
        const long noActivate = 0x08000000;
        const long toolWindow = 0x00000080;
        const long transparent = 0x00000020;
        IntPtr current = IntPtr.Size == 8 ? GetWindowLongPtr(window, extendedStyle) : new IntPtr(GetWindowLong(window, extendedStyle));
        var updated = new IntPtr(current.ToInt64() | noActivate | toolWindow | transparent);
        if (IntPtr.Size == 8)
            SetWindowLongPtr(window, extendedStyle, updated);
        else
            SetWindowLong(window, extendedStyle, updated.ToInt32());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left; public int Top; public int Right; public int Bottom;
        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRectangle Monitor;
        public NativeRectangle Work;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr window, int index, int value);
}
