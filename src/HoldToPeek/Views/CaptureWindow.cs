using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HoldToPeek.Services;

namespace HoldToPeek.Views;

/// <summary>A single-monitor selection surface; cancelling never replaces the current reference.</summary>
public sealed class CaptureWindow : Window
{
    private readonly CapturedScreen _screen;
    private readonly Grid _surface;
    private readonly Path _shade;
    private readonly Rectangle _outline;
    private readonly Canvas _adornments;
    private readonly TextBlock _selectionSize;
    private readonly Border _instructions;
    private Point? _start;
    private Rect _selection;

    public BitmapSource? SelectedImage { get; private set; }

    public CaptureWindow(CapturedScreen screen)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        Title = "瞄一眼 · 框选参考";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;
        Cursor = Cursors.Cross;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _surface = new Grid { ClipToBounds = true, Background = Brushes.Black };
        _surface.Children.Add(new Image { Source = screen.Image, Stretch = Stretch.Fill, IsHitTestVisible = false });
        _shade = new Path
        {
            Fill = new SolidColorBrush(Color.FromArgb(125, 10, 25, 20)),
            IsHitTestVisible = false
        };
        _surface.Children.Add(_shade);
        _adornments = new Canvas { IsHitTestVisible = false };
        _outline = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(196, 235, 159)),
            StrokeThickness = 2,
            Visibility = Visibility.Collapsed
        };
        _adornments.Children.Add(_outline);
        _selectionSize = new TextBlock
        {
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(23, 62, 53)),
            Padding = new Thickness(8, 4, 8, 4),
            FontSize = 12,
            Visibility = Visibility.Collapsed
        };
        _adornments.Children.Add(_selectionSize);
        _surface.Children.Add(_adornments);
        _instructions = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(23, 62, 53)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(22, 14, 22, 14),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 28, 16, 0),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "拖动框住要看的内容　·　松开完成　·　Esc / 右键取消",
                Foreground = Brushes.White,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            }
        };
        _surface.Children.Add(_instructions);
        Content = _surface;

        SourceInitialized += (_, _) => PositionOverMonitor();
        Loaded += (_, _) => { PositionOverMonitor(); Activate(); Focus(); };
        _surface.SizeChanged += (_, _) => RefreshSelection();
        _surface.MouseLeftButtonDown += StartSelection;
        _surface.MouseMove += ContinueSelection;
        _surface.MouseLeftButtonUp += FinishSelection;
        _surface.MouseRightButtonDown += (_, args) => { args.Handled = true; DialogResult = false; };
        _surface.LostMouseCapture += (_, _) =>
        {
            if (_start is not null)
            {
                _start = null;
                _selection = Rect.Empty;
                _instructions.Visibility = Visibility.Visible;
                RefreshSelection();
            }
        };
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; DialogResult = false; }
        };
    }

    private void PositionOverMonitor()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        ScreenGeometry.MoveWindow(handle, _screen.Bounds.Location);
        double scale = ScreenGeometry.DpiScale(handle);
        Width = _screen.Bounds.Width / scale;
        Height = _screen.Bounds.Height / scale;
        ScreenGeometry.PositionWindow(handle, _screen.Bounds);
    }

    private void StartSelection(object sender, MouseButtonEventArgs args)
    {
        _start = Clamp(args.GetPosition(_surface));
        _selection = new Rect(_start.Value, _start.Value);
        _instructions.Visibility = Visibility.Collapsed;
        _surface.CaptureMouse();
        RefreshSelection();
        args.Handled = true;
    }

    private void ContinueSelection(object sender, MouseEventArgs args)
    {
        if (_start is not Point start) return;
        _selection = new Rect(start, Clamp(args.GetPosition(_surface)));
        RefreshSelection();
    }

    private void FinishSelection(object sender, MouseButtonEventArgs args)
    {
        if (_start is not Point start) return;
        _selection = new Rect(start, Clamp(args.GetPosition(_surface)));
        _start = null;
        _surface.ReleaseMouseCapture();
        args.Handled = true;

        Int32Rect pixels = PixelSelection();
        if (pixels.Width < 8 || pixels.Height < 8)
        {
            _selection = Rect.Empty;
            _instructions.Visibility = Visibility.Visible;
            RefreshSelection();
            return;
        }

        // Copy the selected pixels so retaining a small reference does not retain the full desktop.
        var cropped = new CroppedBitmap(_screen.Image, pixels);
        int stride = checked((pixels.Width * cropped.Format.BitsPerPixel + 7) / 8);
        byte[] data = new byte[checked(stride * pixels.Height)];
        cropped.CopyPixels(data, stride, 0);
        BitmapSource selected = BitmapSource.Create(pixels.Width, pixels.Height, 96, 96,
            cropped.Format, cropped.Palette, data, stride);
        selected.Freeze();
        SelectedImage = selected;
        DialogResult = true;
    }

    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, _surface.ActualWidth),
        Math.Clamp(point.Y, 0, _surface.ActualHeight));

    private Int32Rect PixelSelection()
    {
        if (_selection.IsEmpty || _surface.ActualWidth <= 0 || _surface.ActualHeight <= 0)
            return Int32Rect.Empty;
        double scaleX = _screen.Image.PixelWidth / _surface.ActualWidth;
        double scaleY = _screen.Image.PixelHeight / _surface.ActualHeight;
        int left = Math.Clamp((int)Math.Floor(_selection.Left * scaleX), 0, _screen.Image.PixelWidth);
        int top = Math.Clamp((int)Math.Floor(_selection.Top * scaleY), 0, _screen.Image.PixelHeight);
        int right = Math.Clamp((int)Math.Ceiling(_selection.Right * scaleX), left, _screen.Image.PixelWidth);
        int bottom = Math.Clamp((int)Math.Ceiling(_selection.Bottom * scaleY), top, _screen.Image.PixelHeight);
        return new Int32Rect(left, top, right - left, bottom - top);
    }

    private void RefreshSelection()
    {
        var shade = new GeometryGroup { FillRule = FillRule.EvenOdd };
        shade.Children.Add(new RectangleGeometry(new Rect(0, 0, _surface.ActualWidth, _surface.ActualHeight)));
        bool hasSelection = !_selection.IsEmpty && _selection.Width > 0 && _selection.Height > 0;
        if (hasSelection) shade.Children.Add(new RectangleGeometry(_selection));
        _shade.Data = shade;
        _outline.Visibility = _selectionSize.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        if (!hasSelection) return;

        _outline.Width = _selection.Width;
        _outline.Height = _selection.Height;
        Canvas.SetLeft(_outline, _selection.Left);
        Canvas.SetTop(_outline, _selection.Top);
        Int32Rect pixels = PixelSelection();
        _selectionSize.Text = $"{pixels.Width} × {pixels.Height}";
        Canvas.SetLeft(_selectionSize, Math.Clamp(_selection.Left, 0, Math.Max(0, _surface.ActualWidth - 130)));
        Canvas.SetTop(_selectionSize, _selection.Top >= 34 ? _selection.Top - 32 : Math.Min(_selection.Bottom + 6, Math.Max(0, _surface.ActualHeight - 30)));
    }
}
