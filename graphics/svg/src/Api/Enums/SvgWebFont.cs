namespace svg;

/// <summary>Identifies a supported web-font container.</summary>
public enum SvgWebFontFormat
{
    /// <summary>The input is not a recognized web-font container.</summary>
    Unknown = 0,
    /// <summary>The input uses Web Open Font Format 1.0.</summary>
    Woff = 1,
    /// <summary>The input uses Web Open Font Format 2.0.</summary>
    Woff2 = 2
}
