using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace HoldToPeek.Services;

public sealed record CapturedScreen(BitmapSource Image, Rectangle Bounds);

public static class ScreenCapture
{
    /// <summary>Captures only the monitor under the cursor. The image never touches disk.</summary>
    public static CapturedScreen CaptureAtCursor()
    {
        Rectangle bounds = ScreenGeometry.MonitorAt(ScreenGeometry.CursorPosition()).Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("当前显示器没有可截图的区域。");

        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        IntPtr handle = bitmap.GetHbitmap();
        if (handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法创建参考截图。");
        try
        {
            BitmapSource image = Imaging.CreateBitmapSourceFromHBitmap(
                handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return new CapturedScreen(image, bounds);
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr value);
}
