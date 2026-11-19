using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HoldToPeek.Services;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace HoldToPeek.Views;

/// <summary>A passive reference window. It never takes focus or receives mouse input.</summary>
public sealed class PeekWindow : Window
{
    private readonly Image _image;
    private readonly Grid _imageSurface;
    private readonly Canvas _highlightLayer;
    private readonly Border _highlight;
    private readonly ScrollViewer _reader;
    private readonly TextBlock _progress;
    private BitmapSource? _reference;
    private double _pixelsToDip = 1;
    private HwndSource? _source;

    public PeekWindow()
    {
        Title = "瞄一眼 · 参考";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        var header = new DockPanel { Margin = new Thickness(12, 0, 12, 0), LastChildFill = true };
        _progress = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(83, 114, 94)),
            FontSize = 12
        };
        DockPanel.SetDock(_progress, Dock.Right);
        header.Children.Add(_progress);
        header.Children.Add(new TextBlock
        {
            Text = "瞄一眼",
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 62, 53))
        });
        layout.Children.Add(header);
        _image = new Image { Stretch = Stretch.Fill, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);
        _highlight = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(70, 196, 235, 159)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(73, 126, 71)),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };
        _highlightLayer = new Canvas { IsHitTestVisible = false };
        _highlightLayer.Children.Add(_highlight);
        _imageSurface = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ClipToBounds = true
        };
        _imageSurface.Children.Add(_image);
        _imageSurface.Children.Add(_highlightLayer);
        _reader = new ScrollViewer
        {
            Content = _imageSurface,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Focusable = false,
            Margin = new Thickness(8, 0, 8, 0),
            Background = Brushes.White
        };
        Grid.SetRow(_reader, 1);
        layout.Children.Add(_reader);
        var footer = new TextBlock
        {
            Text = "滚轮移动高亮 · 松手收起",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(83, 114, 94)),
            FontSize = 11
        };
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);
        Content = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(138, 169, 144)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(246, 249, 242)),
            Child = layout
        };
        SourceInitialized += (_, _) =>
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            ScreenGeometry.MakePassive(handle);
            _source = HwndSource.FromHwnd(handle);
            _source?.AddHook(WindowMessage);
        };
        Closed += (_, _) =>
        {
            _source?.RemoveHook(WindowMessage);
            _source = null;
            _image.Source = null;
            _reference = null;
        };
    }

    public void SetReference(BitmapSource image)
    {
        _reference = image ?? throw new ArgumentNullException(nameof(image));
        _image.Source = image;
        _reader.ScrollToTop();
    }

    public void ClearReference()
    {
        if (IsVisible) Hide();
        _image.Source = null;
        _reference = null;
    }

    public void ShowNearCursor(double position, double lineHeight)
    {
        if (_reference is null) return;
        DrawingPoint cursor = ScreenGeometry.CursorPosition();
        DrawingRectangle work = ScreenGeometry.MonitorAt(cursor).WorkArea;
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();

        // Move the hidden HWND to its target monitor first, so its DPI matches that monitor.
        ScreenGeometry.MoveWindow(handle, cursor);
        double dpi = ScreenGeometry.DpiScale(handle);
        double maxWindowWidth = Math.Max(1, work.Width * 0.5 / dpi);
        double maxWindowHeight = Math.Max(1, work.Height * 0.7 / dpi);
        double contentWidth = Math.Max(1, maxWindowWidth - 18);
        _pixelsToDip = Math.Min(1 / dpi, contentWidth / _reference.PixelWidth);
        double imageWidth = _reference.PixelWidth * _pixelsToDip;
        double imageHeight = _reference.PixelHeight * _pixelsToDip;
        _image.Width = _imageSurface.Width = _highlightLayer.Width = imageWidth;
        _image.Height = _imageSurface.Height = _highlightLayer.Height = imageHeight;
        _highlight.Width = imageWidth;
        Width = Math.Min(maxWindowWidth, Math.Max(Math.Min(240, maxWindowWidth), imageWidth + 18));
        Height = Math.Min(maxWindowHeight, Math.Max(Math.Min(130, maxWindowHeight), imageHeight + 66));

        if (!IsVisible) Show();
        UpdateLayout();
        int physicalWidth = Math.Max(1, (int)Math.Ceiling(Width * dpi));
        int physicalHeight = Math.Max(1, (int)Math.Ceiling(Height * dpi));
        int gap = (int)Math.Round(18 * dpi);
        int left = cursor.X + gap;
        int top = cursor.Y + gap;
        if (left + physicalWidth > work.Right) left = cursor.X - gap - physicalWidth;
        if (top + physicalHeight > work.Bottom) top = cursor.Y - gap - physicalHeight;
        left = Math.Clamp(left, work.Left, Math.Max(work.Left, work.Right - physicalWidth));
        top = Math.Clamp(top, work.Top, Math.Max(work.Top, work.Bottom - physicalHeight));
        ScreenGeometry.PositionWindow(handle, new DrawingRectangle(left, top, physicalWidth, physicalHeight), show: true);
        UpdateHighlight(position, lineHeight);
    }

    public void UpdateHighlight(double position, double lineHeight)
    {
        if (_reference is null) return;
        position = double.IsFinite(position) ? Math.Clamp(position, 0, _reference.PixelHeight - 1) : 0;
        lineHeight = double.IsFinite(lineHeight) ? Math.Max(1, lineHeight) : 24;
        double imageHeight = _reference.PixelHeight * _pixelsToDip;
        double y = position * _pixelsToDip;
        double height = Math.Min(imageHeight, Math.Max(2, lineHeight * _pixelsToDip));
        _highlight.Height = height;
        Canvas.SetTop(_highlight, Math.Clamp(y - height / 2, 0, Math.Max(0, imageHeight - height)));
        _progress.Text = $"{Math.Round(100 * position / Math.Max(1, _reference.PixelHeight - 1)):0}%";
        _reader.UpdateLayout();
        // A tall reference keeps readable width and scrolls to the current reading position.
        double viewport = _reader.ViewportHeight;
        if (double.IsFinite(viewport) && viewport > 0)
            _reader.ScrollToVerticalOffset(Math.Clamp(y - viewport / 2, 0, Math.Max(0, imageHeight - viewport)));
    }

    private static IntPtr WindowMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0021) // WM_MOUSEACTIVATE
        {
            handled = true;
            return new IntPtr(3); // MA_NOACTIVATE
        }
        if (message == 0x0084) // WM_NCHITTEST
        {
            handled = true;
            return new IntPtr(-1); // HTTRANSPARENT
        }
        return IntPtr.Zero;
    }
}
