using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HoldToPeek.Core;
using HoldToPeek.Services;

namespace HoldToPeek.Views;

/// <summary>A small welcome/settings surface; closing the window does not save changes.</summary>
public sealed class SettingsWindow : Window
{
    private static readonly Brush Ink = Brush("#173E35");
    private static readonly Brush Muted = Brush("#597168");
    private static readonly Brush Paper = Brush("#F4F6EF");
    private static readonly Brush Line = Brush("#D7E2D6");
    private static readonly Brush Accent = Brush("#C4EB9F");
    private readonly BindingEditor _peekBinding;
    private readonly BindingEditor _captureBinding;
    private readonly AppSettings _current;
    private readonly TabControl _pages = new();
    private readonly CheckBox _startAtLogin;
    private readonly CheckBox _highPriority;
    private readonly Slider _lineHeightSlider;
    private readonly TextBlock _lineHeightText;
    private readonly TextBlock _triggerHint;
    private readonly ReferencePreview? _referencePreview;

    public SettingsWindow(AppSettings current, string version, BitmapSource? reference = null, double position = 0, bool paused = false)
    {
        ArgumentNullException.ThrowIfNull(current);
        _current = current;
        Title = "瞄一眼 · 设置";
        FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");
        FontSize = 14;
        Foreground = Ink;
        Background = Paper;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.CanResize;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        MaxWidth = Math.Max(320, SystemParameters.WorkArea.Width - 24);
        MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 24);
        MinWidth = Math.Min(540, MaxWidth);
        MinHeight = Math.Min(460, MaxHeight);
        Width = Math.Min(650, MaxWidth);
        Height = Math.Min(780, MaxHeight);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != System.Windows.Input.Key.Escape)
                return;

            if (_peekBinding is { IsRecording: true } || _captureBinding is { IsRecording: true })
            {
                _peekBinding?.CancelRecording();
                _captureBinding?.CancelRecording();
                e.Handled = true;
                return;
            }
            // This window is modeless; IsCancel alone only handles modal dialogs.
            e.Handled = true;
            Close();
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = layout;

        var body = new StackPanel { Margin = new Thickness(28, 26, 28, 24) };
        var scroll = new ScrollViewer
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false
        };
        _pages.Items.Add(new TabItem { Header = "参考设置", Content = scroll, Padding = new Thickness(16, 10, 16, 10) });
        layout.Children.Add(_pages);

        var brand = new Grid { Margin = new Thickness(0, 0, 0, 24) };
        brand.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        brand.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        brand.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var mark = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(13),
            Background = Ink,
            Child = new TextBlock
            {
                Text = "瞄",
                Foreground = Accent,
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        brand.Children.Add(mark);
        var brandWords = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        brandWords.Children.Add(Text("瞄一眼", 19, Ink, FontWeights.SemiBold));
        brandWords.Children.Add(Text("HOLD TO PEEK", 10, Muted));
        Grid.SetColumn(brandWords, 1);
        brand.Children.Add(brandWords);
        var versionLabel = Text("v" + version, 12, Muted);
        versionLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(versionLabel, 2);
        brand.Children.Add(versionLabel);
        body.Children.Add(brand);

        body.Children.Add(Text(current.HasSeenWelcome ? "让这一眼更顺手。" : "按住看参考，松手接着干。",
            25, Ink, FontWeights.SemiBold));
        var lead = Text("把教程、聊天内容或参数表框下来，回到正在做的事。需要时看一眼，松手就收起。", 14, Muted);
        lead.LineHeight = 23;
        lead.Margin = new Thickness(0, 10, 0, 21);
        body.Children.Add(lead);
        var status = Text(reference is null ? "还没有参考 · 先框住要看的内容" : paused
            ? "已有参考 · 当前已暂停，关闭设置后可在托盘恢复" : "参考已准备好 · 保存后继续使用", 13, Ink);
        status.Margin = new Thickness(0, 0, 0, 16);
        body.Children.Add(status);

        var steps = new Grid { Margin = new Thickness(0, 0, 0, 23) };
        steps.ColumnDefinitions.Add(new ColumnDefinition());
        steps.ColumnDefinitions.Add(new ColumnDefinition());
        steps.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        steps.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddStep(steps, 0, 0, "01", "框住参考", "选择需要反复查看的一块内容。");
        AddStep(steps, 0, 1, "02", "按住查看", "参考浮在鼠标旁，工作窗口保持原位。");
        AddStep(steps, 1, 0, "03", "滚轮标记", "移动高亮线，记住读到了哪里。");
        AddStep(steps, 1, 1, "04", "松手继续", "收起参考，下次按住接着看。");
        var guide = new StackPanel { Margin = new Thickness(26) };
        guide.Children.Add(Text("四个动作，接着干", 24, Ink, FontWeights.SemiBold));
        steps.Margin = new Thickness(0, 20, 0, 20);
        guide.Children.Add(steps);
        guide.Children.Add(Text("快速框选", 16, Ink, FontWeights.SemiBold));
        guide.Children.Add(Text("默认按 F1 开始框选，也可从托盘或下方按钮开始。拖动选取内容，Esc 或右键取消；取消后保留原参考。可在设置中修改快捷键。", 14, Muted));
        guide.Children.Add(Text("\n查看与定位", 16, Ink, FontWeights.SemiBold));
        guide.Children.Add(Text("按住查看键显示参考，松开隐藏。滚轮移动高亮，Shift + 滚轮精调；再次查看保留位置。Esc 可立即收起，松开后再按继续。高亮只标记位置，不识别文字。", 14, Muted));
        guide.Children.Add(Text("\n暂停、退出与隐私", 16, Ink, FontWeights.SemiBold));
        guide.Children.Add(Text("关闭设置后仍在托盘运行。右键托盘可暂停、恢复或退出。截图仅存内存，退出后清除；偏好保存在本机。设置编辑期间暂停接管查看键。", 14, Muted));
        _pages.Items.Add(new TabItem { Header = "使用说明", Padding = new Thickness(16, 10, 16, 10),
            Content = new ScrollViewer { Content = guide, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });

        var settings = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = settings
        });

        var keys = new StackPanel { Margin = new Thickness(26) };
        keys.Children.Add(Text("按你顺手的方式来", 24, Ink, FontWeights.SemiBold));
        var keyLead = Text("框选按一下，查看按住不放。点击下面的按键，再按下你想用的新组合。", 14, Muted);
        keyLead.Margin = new Thickness(0, 12, 0, 24);
        keys.Children.Add(keyLead);
        keys.Children.Add(Text("快速框选键", 15, Ink, FontWeights.SemiBold));
        _captureBinding = new BindingEditor(current.CaptureBinding, false);
        AutomationProperties.SetName(_captureBinding, "快速框选快捷键");
        keys.Children.Add(_captureBinding);
        keys.Children.Add(Text("按住哪个键查看？", 15, Ink, FontWeights.SemiBold));
        _peekBinding = new BindingEditor(current.EffectivePeekBinding, true);
        AutomationProperties.SetName(_peekBinding, "按住查看快捷键");
        keys.Children.Add(_peekBinding);
        _triggerHint = Text("点击后按下想用的按键，可搭配 Ctrl、Alt、Shift；查看还支持鼠标中键和侧键。Esc、系统保留组合和 F12 不可用。", 12, Muted);
        _triggerHint.Margin = new Thickness(0, 8, 0, 20);
        _triggerHint.LineHeight = 19;
        keys.Children.Add(_triggerHint);

        keys.Children.Add(Text("两个动作需要不同的主按键。保存后生效；若被其他程序占用，会提示你更换框选快捷键。Esc 取消录入，关闭或取消不会保存改动。", 13, Muted));
        _pages.Items.Insert(1, new TabItem { Header = "按键设置", Padding = new Thickness(16, 10, 16, 10),
            Content = new ScrollViewer { Content = keys, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
        _pages.SelectionChanged += (_, e) =>
        {
            if (e.Source != _pages) return;
            _peekBinding.CancelRecording();
            _captureBinding.CancelRecording();
        };

        var runtime = new StackPanel { Margin = new Thickness(26) };
        runtime.Children.Add(Text("启动与运行", 24, Ink, FontWeights.SemiBold));
        _startAtLogin = new CheckBox { Content = "登录 Windows 后自动启动", IsChecked = current.StartAtLogin, Margin = new Thickness(0, 24, 0, 10) };
        runtime.Children.Add(_startAtLogin);
        runtime.Children.Add(Text("启动后留在托盘，不弹出设置。移动程序目录后，请重新开关此选项；Windows 启动应用设置也可能禁用它。", 13, Muted));
        _highPriority = new CheckBox { Content = "提高 CPU 调度优先级（High）", IsChecked = current.HighPriority, Margin = new Thickness(0, 24, 0, 10) };
        runtime.Children.Add(_highPriority);
        runtime.Children.Add(Text("默认关闭。繁忙时优先获得 CPU 时间，也可能影响其他程序；关闭即可恢复普通优先级。不会启用实时优先级或要求管理员运行。", 13, Muted));
        _pages.Items.Insert(2, new TabItem { Header = "启动与运行", Padding = new Thickness(16, 10, 16, 10),
            Content = new ScrollViewer { Content = runtime, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });

        var lineHeading = new Grid();
        lineHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lineHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        lineHeading.Children.Add(Text("每格滚轮移动多少？", 15, Ink, FontWeights.SemiBold));
        _lineHeightText = Text(string.Empty, 13, Muted);
        _lineHeightText.Margin = new Thickness(8, 2, 0, 0);
        Grid.SetColumn(_lineHeightText, 1);
        lineHeading.Children.Add(_lineHeightText);
        settings.Children.Add(lineHeading);
        _lineHeightSlider = new Slider
        {
            Minimum = 8,
            Maximum = 120,
            Value = double.IsFinite(current.LineHeight) ? Math.Clamp(current.LineHeight, 8, 120) : 28,
            SmallChange = 1,
            LargeChange = 8,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 12, 0, 7),
            Foreground = Ink,
            TabIndex = 2
        };
        AutomationProperties.SetName(_lineHeightSlider, "高亮移动行距，8 到 120 原图像素");
        _lineHeightSlider.ValueChanged += (_, _) => UpdateLineHeightText();
        UpdateLineHeightText();
        settings.Children.Add(_lineHeightSlider);
        var spacingHelp = Text("按原截图像素计算。文字行距较大时调高；高亮线只标记位置，不会自动识别教程步骤。", 12, Muted);
        spacingHelp.LineHeight = 19;
        settings.Children.Add(spacingHelp);
        var previewHeading = Text(reference is null ? "用示例试一下行距" : "用当前参考试一下行距", 13, Ink, FontWeights.SemiBold);
        previewHeading.Margin = new Thickness(0, 14, 0, 0);
        settings.Children.Add(previewHeading);
        _referencePreview = new ReferencePreview(reference, position, _lineHeightSlider.Value);
        settings.Children.Add(_referencePreview);
        var rehearsal = new DockPanel { LastChildFill = true };
        var next = ActionButton("下一行 ↓", false);
        next.Padding = new Thickness(10, 6, 10, 6);
        next.MinHeight = 34;
        next.TabIndex = 4;
        next.Click += (_, _) => _referencePreview.Move(-120);
        DockPanel.SetDock(next, Dock.Right);
        rehearsal.Children.Add(next);
        var previous = ActionButton("上一行 ↑", false);
        previous.Padding = new Thickness(10, 6, 10, 6);
        previous.MinHeight = 34;
        previous.TabIndex = 3;
        previous.Click += (_, _) => _referencePreview.Move(120);
        DockPanel.SetDock(previous, Dock.Right);
        rehearsal.Children.Add(previous);
        rehearsal.Children.Add(Text("也可在图上滚轮试读。\n不改变原来的阅读位置。", 12, Muted));
        settings.Children.Add(rehearsal);

        var footer = new Border
        {
            Background = Brush("#EDF2E8"),
            BorderBrush = Line,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 12, 20, 12)
        };
        Grid.SetRow(footer, 1);
        layout.Children.Add(footer);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        footer.Child = actions;
        Button cancel = ActionButton("取消", false);
        cancel.IsCancel = true;
        cancel.TabIndex = 5;
        cancel.Click += (_, _) => Close();
        actions.Children.Add(cancel);
        bool preferCapture = reference is null && !current.HasSeenWelcome;
        Button save = ActionButton("保存设置", !preferCapture);
        save.TabIndex = 6;
        save.IsDefault = !preferCapture;
        save.Click += (_, _) => Accept(capture: false);
        actions.Children.Add(save);
        Button capture = ActionButton("框选参考  →", preferCapture);
        capture.IsDefault = !save.IsDefault;
        capture.TabIndex = 7;
        capture.Click += (_, _) => Accept(capture: true);
        actions.Children.Add(capture);
    }

    public AppSettings? SavedSettings { get; private set; }
    public bool CaptureRequested { get; private set; }

    private void Accept(bool capture)
    {
        if (_peekBinding.IsRecording || _captureBinding.IsRecording) return;
        if (_peekBinding.Binding.Key == _captureBinding.Binding.Key)
        {
            MessageBox.Show(this, "框选与查看请使用不同的主按键，避免输入冲突。", "快捷键冲突", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        SavedSettings = _current with
        {
            PeekBinding = _peekBinding.Binding,
            CaptureBinding = _captureBinding.Binding,
            LineHeight = Math.Round(_lineHeightSlider.Value),
            HasSeenWelcome = true,
            StartAtLogin = _startAtLogin.IsChecked == true,
            HighPriority = _highPriority.IsChecked == true
        };
        CaptureRequested = capture;
        Close();
    }

    private void UpdateLineHeightText()
    {
        _lineHeightText.Text = Math.Round(_lineHeightSlider.Value).ToString(CultureInfo.CurrentCulture) + " 像素 / 格";
        _referencePreview?.SetLineHeight(_lineHeightSlider.Value);
    }

    private static void AddStep(Grid grid, int row, int column, string number, string heading, string description)
    {
        var stack = new StackPanel();
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        title.Children.Add(new Border
        {
            Background = Accent,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 8, 0),
            Child = Text(number, 11, Ink, FontWeights.SemiBold)
        });
        title.Children.Add(Text(heading, 14, Ink, FontWeights.SemiBold));
        stack.Children.Add(title);
        var explanation = Text(description, 12, Muted);
        explanation.LineHeight = 19;
        explanation.Margin = new Thickness(0, 7, 0, 0);
        stack.Children.Add(explanation);
        var container = new Border
        {
            Padding = new Thickness(13),
            Background = Brush("#EAF0E5"),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(column == 0 ? 0 : 5, row == 0 ? 0 : 5, column == 0 ? 5 : 0, row == 0 ? 5 : 0),
            Child = stack
        };
        Grid.SetRow(container, row);
        Grid.SetColumn(container, column);
        grid.Children.Add(container);
    }

    private static Button ActionButton(string caption, bool primary)
    {
        var button = new Button
        {
            Content = caption,
            Foreground = primary ? Brushes.White : Ink,
            Background = primary ? Ink : Brushes.Transparent,
            BorderBrush = primary ? Ink : Line,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(primary ? 20 : 14, 10, primary ? 20 : 14, 10),
            Margin = new Thickness(4),
            MinHeight = 42,
            FontSize = 14,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        // Keep the system focus visual while giving the control a rounded body.
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ButtonBorder";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        presenter.SetValue(ContentPresenter.ContentTemplateSelectorProperty, new TemplateBindingExtension(ContentControl.ContentTemplateSelectorProperty));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(OpacityProperty, 0.85, "ButtonBorder"));
        template.Triggers.Add(hover);
        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(OpacityProperty, 0.7, "ButtonBorder"));
        template.Triggers.Add(pressed);
        button.Template = template;
        return button;
    }

    private static TextBlock Text(string value, double size, Brush color, FontWeight? weight = null) =>
        new()
        {
            Text = value,
            FontSize = size,
            Foreground = color,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap
        };

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
