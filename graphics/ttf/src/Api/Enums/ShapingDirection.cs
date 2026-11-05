namespace ttf;

/// <summary>Identifies the resolved direction of one shaped run.</summary>
public enum ShapingDirection
{
    /// <summary>Glyphs advance from left to right.</summary>
    LeftToRight = 0,

    /// <summary>Glyphs advance from right to left.</summary>
    RightToLeft = 1,

    /// <summary>Glyphs advance from top to bottom.</summary>
    TopToBottom = 2,

    /// <summary>Glyphs advance from bottom to top.</summary>
    BottomToTop = 3
}
