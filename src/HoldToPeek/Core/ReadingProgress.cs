using System;

namespace HoldToPeek.Core;

/// <summary>Reading position in original image pixels, independent of preview scaling.</summary>
public sealed class ReadingProgress
{
    private double _lineHeight = 28;
    private long _wheelRemainder;
    private bool _lastFine;

    public int ImageHeight { get; private set; }
    public double Position { get; private set; }

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            _lineHeight = Math.Clamp(value, 1, 256);
        }
    }

    public void Reset(int imageHeight)
    {
        if (imageHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(imageHeight));
        ImageHeight = imageHeight;
        ResetToTop();
    }

    public void ResetToTop()
    {
        _wheelRemainder = 0;
        SetPosition(LineHeight / 2);
    }

    public void SetPosition(double position)
    {
        if (!double.IsFinite(position))
            throw new ArgumentOutOfRangeException(nameof(position));
        Position = Math.Clamp(position, 0, Math.Max(0, ImageHeight - 1));
    }

    public void Move(int wheelDelta, bool fine = false)
    {
        if (ImageHeight == 0)
            return;
        if (_lastFine != fine)
            _wheelRemainder = 0;
        _lastFine = fine;
        _wheelRemainder += wheelDelta;
        long notches = _wheelRemainder / 120;
        _wheelRemainder %= 120;
        if (notches != 0)
            SetPosition(Position - notches * (fine ? 1 : LineHeight));
    }
}
