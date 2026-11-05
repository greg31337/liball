namespace ttf;

/// <summary>Selects text shaping direction.</summary>
public enum TextShapingDirection
{
    /// <summary>Resolve horizontal direction from the Unicode bidirectional algorithm.</summary>
    Auto = 0,

    /// <summary>Shape from left to right.</summary>
    LeftToRight = 1,

    /// <summary>Shape from right to left.</summary>
    RightToLeft = 2,

    /// <summary>Shape vertically from top to bottom.</summary>
    TopToBottom = 3,

    /// <summary>Shape vertically from bottom to top.</summary>
    BottomToTop = 4
}
