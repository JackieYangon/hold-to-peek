namespace HoldToPeek.Core;

/// <summary>Windows virtual key plus Alt=1, Control=2 and Shift=4 modifiers.</summary>
public sealed record InputBinding(int Key, int Modifiers = 0)
{
    public bool IsMouse => Key is 4 or 5 or 6;
    public bool IsValid => Modifiers is >= 0 and <= 7 &&
        (IsMouse || Key is >= 8 and <= 254) &&
        Key is not (16 or 17 or 18 or 27 or 91 or 92 or 93 or 123 or 160 or 161 or 162 or 163 or 164 or 165) &&
        !(Key == 46 && (Modifiers & 3) == 3) && !(Key == 9 && (Modifiers & 1) != 0) &&
        !(Key == 115 && (Modifiers & 1) != 0);

    public static InputBinding FromLegacy(PeekTrigger trigger) => new(trigger switch
    { PeekTrigger.MouseForward => 6, PeekTrigger.F8 => 0x77, _ => 5 });
}
