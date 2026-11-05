namespace svg;

/// <summary>Identifies the source selected for one filter primitive input.</summary>
public enum SvgFilterInputKind
{
    /// <summary>One of the six standard filter input images.</summary>
    Standard = 0,
    /// <summary>The immediately preceding primitive output.</summary>
    Previous = 1,
    /// <summary>A named preceding primitive output.</summary>
    NamedResult = 2,
    /// <summary>Transparent black used after an unresolved input name.</summary>
    TransparentBlack = 3
}

/// <summary>Identifies a standard filter input image.</summary>
public enum SvgFilterStandardInput
{
    /// <summary>The original filtered graphics.</summary>
    SourceGraphic = 0,
    /// <summary>The alpha channel of the original filtered graphics.</summary>
    SourceAlpha = 1,
    /// <summary>The backdrop behind the filter region.</summary>
    BackgroundImage = 2,
    /// <summary>The alpha channel of the backdrop.</summary>
    BackgroundAlpha = 3,
    /// <summary>The filtered element's fill paint.</summary>
    FillPaint = 4,
    /// <summary>The filtered element's stroke paint.</summary>
    StrokePaint = 5
}

/// <summary>Identifies the color space requested by a filter primitive.</summary>
public enum SvgFilterColorSpace
{
    /// <summary>The user agent may select a color space.</summary>
    Auto = 0,
    /// <summary>Operations use sRGB values.</summary>
    Srgb = 1,
    /// <summary>Operations use linearized RGB values.</summary>
    LinearRgb = 2
}
