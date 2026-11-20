using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HoldToPeek.Core;

namespace HoldToPeek.Views;

/// <summary>In-memory spacing rehearsal; never changes the session's reading position.</summary>
public sealed class ReferencePreview : Border
{
    private readonly BitmapSource _reference;
    private readonly ReadingProgress _reading = new();
    private readonly Image _image;
    private readonly Grid _surface;
    private readonly Border _highlight;
    private readonly ScrollViewer _reader;
    private double _scale = 1;

    public ReferencePreview(BitmapSource? reference, double position, double lineHeight)
    {
        _reference = reference ?? CreateExample();
        _reading.LineHeight = lineHeight;
        _reading.Reset(_reference.PixelHeight);
        if (reference is not null) _reading.SetPosition(position);
        BorderBrush = new SolidColorBrush(Color.FromRgb(215, 226, 214));
        BorderThickness = new Thickness(1);
        Background = Brushes.White;
        ClipToBounds = true;
        Height = reference is null ? 100 : 166;
        Margin = new Thickness(0, 10, 0, 8);

        _image = new Image { Source = _reference, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);
        _highlight = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(70, 196, 235, 159)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(73, 126, 71)),
            BorderThickness = new Thickness(0, 0, 0, 2),
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false
        };
        _surface = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, ClipToBounds = true };
        _surface.Children.Add(_image);
        _surface.Children.Add(_highlight);
        _reader = new ScrollViewer
        {
            Content = _surface,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Focusable = false
        };
        Child = _reader;
        SizeChanged += (_, _) => Refresh();
        PreviewMouseWheel += (_, e) =>
        {
            _reading.Move(e.Delta, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
            Refresh();
            e.Handled = true;
        };
    }

    public void SetLineHeight(double lineHeight)
    {
        _reading.LineHeight = lineHeight;
        Refresh();
    }

    public void Move(int wheelDelta)
    {
        _reading.Move(wheelDelta);
        Refresh();
    }

    private void Refresh()
    {
        _scale = Math.Min(1, Math.Max(1, ActualWidth - 2) / _reference.PixelWidth);
        _image.Width = _surface.Width = _reference.PixelWidth * _scale;
        _image.Height = _surface.Height = _reference.PixelHeight * _scale;
        double height = Math.Min(_surface.Height, Math.Max(2, _reading.LineHeight * _scale));
        double top = Math.Clamp(_reading.Position * _scale - height / 2, 0, Math.Max(0, _surface.Height - height));
        _highlight.Height = height;
        _highlight.Margin = new Thickness(0, top, 0, 0);
        _reader.UpdateLayout();
        double viewport = _reader.ViewportHeight;
        if (double.IsFinite(viewport) && viewport > 0)
            _reader.ScrollToVerticalOffset(Math.Clamp(top + height / 2 - viewport / 2, 0, Math.Max(0, _surface.Height - viewport)));
    }

    private static BitmapSource CreateExample()
    {
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, 420, 96));
            string[] lines = { "01  框住需要参考的内容", "02  按住查看，滚轮标记", "03  松手继续，下次接着看" };
            for (int i = 0; i < lines.Length; i++)
            {
                var text = new FormattedText(lines[i], CultureInfo.GetCultureInfo("zh-CN"),
                    FlowDirection.LeftToRight, new Typeface("Microsoft YaHei UI"), 14,
                    new SolidColorBrush(Color.FromRgb(23, 62, 53)), 1);
                drawing.DrawText(text, new Point(12, 4 + i * 28));
            }
        }
        var image = new RenderTargetBitmap(420, 96, 96, 96, PixelFormats.Pbgra32);
        image.Render(visual);
        image.Freeze();
        return image;
    }
}
