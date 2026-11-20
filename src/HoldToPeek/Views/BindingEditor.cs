using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InputBinding = HoldToPeek.Core.InputBinding;

namespace HoldToPeek.Views;

/// <summary>Records a chord without text parsing or requiring the key to be in a preset list.</summary>
public sealed class BindingEditor : Button
{
    private readonly bool _allowMouse;
    public bool IsRecording { get; private set; }
    public InputBinding Binding { get; private set; }
    public event Action? Changed;
    public BindingEditor(InputBinding binding, bool allowMouse)
    {
        Binding = binding;
        _allowMouse = allowMouse;
        Padding = new Thickness(12, 10, 12, 10);
        Margin = new Thickness(0, 8, 0, 8);
        HorizontalContentAlignment = HorizontalAlignment.Left;
        Refresh();
        Click += (_, _) => { IsRecording = true; Content = "请按快捷键（Esc 取消）…"; Focus(); };
        PreviewKeyDown += RecordKey;
        PreviewMouseDown += (_, e) =>
        {
            if (!IsRecording || !_allowMouse) return;
            int key = e.ChangedButton switch { MouseButton.Middle => 4, MouseButton.XButton1 => 5, MouseButton.XButton2 => 6, _ => 0 };
            if (key == 0) return;
            SetBinding(new InputBinding(key, (int)Keyboard.Modifiers));
            e.Handled = true;
        };
        LostKeyboardFocus += (_, _) => CancelRecording();
    }
    private void RecordKey(object sender, KeyEventArgs e)
    {
        if (!IsRecording) return;
        e.Handled = true;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { CancelRecording(); return; }
        if (e.IsRepeat) return;
        var binding = new InputBinding(KeyInterop.VirtualKeyFromKey(key), (int)Keyboard.Modifiers);
        if (!binding.IsValid || binding.IsMouse)
        {
            Content = "请选择普通按键或组合键；Esc 取消";
            return;
        }
        SetBinding(binding);
    }
    private void SetBinding(InputBinding binding)
    {
        Binding = binding;
        IsRecording = false;
        Refresh();
        Changed?.Invoke();
    }
    public void CancelRecording() { IsRecording = false; Refresh(); }
    private void Refresh() => Content = Display(Binding) + "  · 点击修改";
    public static string Display(InputBinding binding)
    {
        string key = binding.Key switch { 4 => "鼠标中键", 5 => "鼠标后退侧键 X1", 6 => "鼠标前进侧键 X2", _ => KeyInterop.KeyFromVirtualKey(binding.Key).ToString() };
        return ((binding.Modifiers & 2) != 0 ? "Ctrl + " : "") +
            ((binding.Modifiers & 1) != 0 ? "Alt + " : "") + ((binding.Modifiers & 4) != 0 ? "Shift + " : "") + key;
    }
}
