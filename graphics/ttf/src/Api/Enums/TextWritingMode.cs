namespace ttf;

/// <summary>Identifies the inline axis and direction used to construct lines.</summary>
public enum TextWritingMode
{
    /// <summary>Place visual runs from left to right on a horizontal baseline.</summary>
    Horizontal = 0,

    /// <summary>Place visual runs downward on a vertical baseline.</summary>
    VerticalTopToBottom = 1,

    /// <summary>Place visual runs upward on a vertical baseline.</summary>
    VerticalBottomToTop = 2
}
