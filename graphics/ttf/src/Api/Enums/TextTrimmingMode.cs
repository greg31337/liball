namespace ttf;

/// <summary>Identifies how content hidden by a line limit or width is indicated.</summary>
public enum TextTrimmingMode
{
    /// <summary>Do not add an ellipsis; content after the line limit is clipped.</summary>
    None = 0,

    /// <summary>Trim at a shaping-safe grapheme boundary and append the ellipsis.</summary>
    CharacterEllipsis = 1,

    /// <summary>Trim at a legal non-emergency line boundary and append the ellipsis.</summary>
    WordEllipsis = 2
}
