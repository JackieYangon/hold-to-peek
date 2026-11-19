using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
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
    private readonly ComboBox _triggerSelector;
    private readonly Slider _lineHeightSlider;
    private readonly TextBlock _lineHeightText;
    private readonly TextBlock _triggerHint;

    public SettingsWindow(AppSettings current, string version)
    {
        ArgumentNullException.ThrowIfNull(current);
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
        layout.Children.Add(scroll);

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

        var steps = new Grid { Margin = new Thickness(0, 0, 0, 23) };
        steps.ColumnDefinitions.Add(new ColumnDefinition());
        steps.ColumnDefinitions.Add(new ColumnDefinition());
        steps.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        steps.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddStep(steps, 0, 0, "01", "框住参考", "选择需要反复查看的一块内容。");
        AddStep(steps, 0, 1, "02", "按住查看", "参考浮在鼠标旁，工作窗口保持原位。");
        AddStep(steps, 1, 0, "03", "滚轮标记", "移动高亮线，记住读到了哪里。");
        AddStep(steps, 1, 1, "04", "松手继续", "收起参考，下次按住接着看。");
        body.Children.Add(steps);

        var settings = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = settings
        });

        settings.Children.Add(Text("按住哪个键查看？", 15, Ink, FontWeights.SemiBold));
        _triggerSelector = new ComboBox
        {
            Margin = new Thickness(0, 10, 0, 0),
            Padding = new Thickness(10, 7, 10, 7),
            MinHeight = 39,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.White,
            BorderBrush = Line,
            FontSize = 14,
            TabIndex = 0
        };
        AutomationProperties.SetName(_triggerSelector, "按住触发键");
        _triggerSelector.Items.Add(TriggerChoice(PeekTrigger.MouseBack, "鼠标后退侧键 · X1"));
        _triggerSelector.Items.Add(TriggerChoice(PeekTrigger.MouseForward, "鼠标前进侧键 · X2"));
        _triggerSelector.Items.Add(TriggerChoice(PeekTrigger.F8, "键盘 F8"));
        _triggerSelector.SelectedIndex = current.Trigger switch
        {
            PeekTrigger.MouseForward => 1,
            PeekTrigger.F8 => 2,
            _ => 0
        };
        settings.Children.Add(_triggerSelector);
        _triggerHint = Text(string.Empty, 12, Muted);
        _triggerHint.Margin = new Thickness(0, 8, 0, 20);
        _triggerHint.LineHeight = 19;
        settings.Children.Add(_triggerHint);
        _triggerSelector.SelectionChanged += (_, _) => UpdateTriggerHint();
        UpdateTriggerHint();

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
            TabIndex = 1
        };
        AutomationProperties.SetName(_lineHeightSlider, "高亮移动行距，8 到 120 原图像素");
        _lineHeightSlider.ValueChanged += (_, _) => UpdateLineHeightText();
        UpdateLineHeightText();
        settings.Children.Add(_lineHeightSlider);
        var spacingHelp = Text("按原截图像素计算。文字行距较大时调高；高亮线只标记位置，不会自动识别教程步骤。", 12, Muted);
        spacingHelp.LineHeight = 19;
        settings.Children.Add(spacingHelp);

        var note = Text("Esc 可取消框选或收起本次预览。截图仅保留在本次运行中，退出后清除；这里只保存触发键和行距设置。", 12, Muted);
        note.Margin = new Thickness(2, 18, 2, 0);
        note.LineHeight = 20;
        body.Children.Add(note);

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
        cancel.TabIndex = 4;
        cancel.Click += (_, _) => Close();
        actions.Children.Add(cancel);
        Button save = ActionButton("保存设置", false);
        save.TabIndex = 3;
        save.Click += (_, _) => Accept(capture: false);
        actions.Children.Add(save);
        Button capture = ActionButton("框选参考  →", true);
        capture.IsDefault = true;
        capture.TabIndex = 2;
        capture.Click += (_, _) => Accept(capture: true);
        actions.Children.Add(capture);
    }

    public AppSettings? SavedSettings { get; private set; }
    public bool CaptureRequested { get; private set; }

    private void Accept(bool capture)
    {
        SavedSettings = new AppSettings
        {
            Trigger = SelectedTrigger,
            LineHeight = Math.Round(_lineHeightSlider.Value),
            HasSeenWelcome = true
        };
        CaptureRequested = capture;
        Close();
    }

    private PeekTrigger SelectedTrigger =>
        (_triggerSelector.SelectedItem as ComboBoxItem)?.Tag is PeekTrigger trigger
            ? trigger : PeekTrigger.MouseBack;

    private void UpdateTriggerHint() => _triggerHint.Text = SelectedTrigger == PeekTrigger.F8
        ? "已有参考时，按住 F8 查看，松开收起。暂停后 F8 恢复原来的功能。"
        : "已有参考时接管这个侧键。暂停后，侧键恢复原来的前进或后退功能。";

    private void UpdateLineHeightText() => _lineHeightText.Text =
        Math.Round(_lineHeightSlider.Value).ToString(CultureInfo.CurrentCulture) + " 像素 / 格";

    private static ComboBoxItem TriggerChoice(PeekTrigger trigger, string label) =>
        new() { Content = label, Tag = trigger, Padding = new Thickness(4, 5, 4, 5) };

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
