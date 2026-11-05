namespace fake;

/// <summary>Selects an explicit line-ending sequence.</summary>
public enum LineEndingStyle
{
    /// <summary>Uses a single line-feed character.</summary>
    LineFeed = 0,

    /// <summary>Uses a carriage return followed by a line feed.</summary>
    CarriageReturnLineFeed = 1,

    /// <summary>Uses a single carriage-return character.</summary>
    CarriageReturn = 2
}
