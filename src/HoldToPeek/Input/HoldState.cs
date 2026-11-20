using HoldToPeek.Core;

namespace HoldToPeek.Input;

/// <summary>
/// A consumed down owns its corresponding up, even after pause, cancellation or a binding change.
/// The caller serializes access. Keeping this independent of Win32 makes cancellation testable.
/// </summary>
internal sealed class HoldState
{
    private readonly bool[] _consumed = new bool[256];
    private readonly bool[] _down = new bool[256];
    private InputBinding _binding = new(5);
    private bool _enabled;

    public PeekTrigger Trigger
    {
        set => Binding = InputBinding.FromLegacy(value);
    }

    public InputBinding Binding
    {
        get => _binding;
        set { if (_binding != value) Cancel(); _binding = value; }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value)
                Cancel();
        }
    }

    public bool IsHeld { get; private set; }
    public int? HeldKey { get; private set; }

    public bool Process(PeekTrigger trigger, bool down)
        => ProcessKey(InputBinding.FromLegacy(trigger).Key, down, 0);

    public bool ProcessKey(int key, bool down, int modifiers)
    {
        if (key < 0 || key >= 256) return false;
        int index = key;
        if (IsHeld && (modifiers & _binding.Modifiers) != _binding.Modifiers) Cancel();
        if (!down)
        {
            bool consumed = _consumed[index];
            _down[index] = false;
            _consumed[index] = false;
            if (HeldKey == key)
                Cancel();
            return consumed;
        }

        if (_down[index])
            return _consumed[index]; // Auto-repeat cannot reactivate an Escape-cancelled hold.

        _down[index] = true;
        if (!_enabled || key != _binding.Key || modifiers != _binding.Modifiers)
        {
            // A new native down supersedes any release debt left by watchdog recovery.
            _consumed[index] = false;
            return false;
        }

        _consumed[index] = true;
        IsHeld = true;
        HeldKey = key;
        return true;
    }

    public void Cancel()
    {
        IsHeld = false;
        HeldKey = null;
    }

    public void ResetAfterInterruption()
    {
        Cancel();
        for (int index = 0; index < _down.Length; index++)
            _down[index] = false;
        // A system transition can lose key-up notifications. Accept a fresh down, but keep
        // release ownership so a delayed up from a consumed press still cannot leak through.
    }

    public void RecoverReleased(PeekTrigger trigger)
        => RecoverReleased(InputBinding.FromLegacy(trigger).Key);

    public void RecoverReleased(int key)
    {
        _down[key] = false;
        if (HeldKey == key)
            Cancel();
        // Retain _consumed so a delayed up does not reach an app that never received its down.
    }
}
