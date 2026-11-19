using HoldToPeek.Core;

namespace HoldToPeek.Input;

/// <summary>
/// A consumed down owns its corresponding up, even after pause, cancellation or a binding change.
/// The caller serializes access. Keeping this independent of Win32 makes cancellation testable.
/// </summary>
internal sealed class HoldState
{
    private readonly bool[] _consumed = new bool[3];
    private readonly bool[] _down = new bool[3];
    private PeekTrigger _trigger;
    private bool _enabled;

    public PeekTrigger Trigger
    {
        get => _trigger;
        set
        {
            if (_trigger != value)
                Cancel();
            _trigger = value;
        }
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
    public PeekTrigger? HeldTrigger { get; private set; }

    public bool Process(PeekTrigger trigger, bool down)
    {
        int index = (int)trigger;
        if (!down)
        {
            bool consumed = _consumed[index];
            _down[index] = false;
            _consumed[index] = false;
            if (HeldTrigger == trigger)
                Cancel();
            return consumed;
        }

        if (_down[index])
            return _consumed[index]; // Auto-repeat cannot reactivate an Escape-cancelled hold.

        _down[index] = true;
        if (!_enabled || trigger != _trigger)
        {
            // A new native down supersedes any release debt left by watchdog recovery.
            _consumed[index] = false;
            return false;
        }

        _consumed[index] = true;
        IsHeld = true;
        HeldTrigger = trigger;
        return true;
    }

    public void Cancel()
    {
        IsHeld = false;
        HeldTrigger = null;
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
    {
        _down[(int)trigger] = false;
        if (HeldTrigger == trigger)
            Cancel();
        // Retain _consumed so a delayed up does not reach an app that never received its down.
    }
}
