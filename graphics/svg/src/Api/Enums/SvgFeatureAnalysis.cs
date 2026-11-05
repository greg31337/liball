namespace svg;

/// <summary>Defines stable per-document feature-analysis failures.</summary>
public enum SvgFeatureAnalysisError
{
    /// <summary>Analysis completed without an operational failure.</summary>
    None = 0,
    /// <summary>The document or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured node or report-entry bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Analysis was cancelled by the caller.</summary>
    Cancelled = 3
}

/// <summary>Identifies one granular public capability family.</summary>
public enum SvgFeatureFamily
{
    /// <summary>An individual SVG element.</summary>
    Element = 0,
    /// <summary>An individual CSS property.</summary>
    CssProperty = 1,
    /// <summary>An individual CSS at-rule.</summary>
    CssAtRule = 2,
    /// <summary>An individual CSS selector form.</summary>
    CssSelector = 3,
    /// <summary>An individual SVG filter primitive.</summary>
    FilterPrimitive = 4,
    /// <summary>An individual typed animation value family.</summary>
    AnimationValue = 5,
    /// <summary>An individual image format.</summary>
    ImageFormat = 6,
    /// <summary>An individual font format.</summary>
    FontFormat = 7,
    /// <summary>An individual document processor job.</summary>
    ProcessorJob = 8,
    /// <summary>An individual host-callback boundary.</summary>
    HostCallback = 9
}
