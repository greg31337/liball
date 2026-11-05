namespace svg;

/// <summary>Identifies the embedded-content element represented by a host record.</summary>
public enum SvgEmbeddedContentKind
{
    /// <summary>No supported embedded-content element.</summary>
    None = 0,
    /// <summary>An SVG <c>image</c> element.</summary>
    Image = 1,
    /// <summary>An SVG <c>foreignObject</c> element.</summary>
    ForeignObject = 2,
    /// <summary>An SVG <c>audio</c> element.</summary>
    Audio = 3,
    /// <summary>An SVG <c>video</c> element.</summary>
    Video = 4,
    /// <summary>An SVG <c>iframe</c> element.</summary>
    Iframe = 5,
    /// <summary>An SVG <c>canvas</c> element.</summary>
    Canvas = 6
}

/// <summary>Identifies the CORS mode requested by an image element.</summary>
public enum SvgCrossOriginMode
{
    /// <summary>No crossorigin attribute was authored.</summary>
    Unspecified = 0,
    /// <summary>Anonymous CORS credentials are requested.</summary>
    Anonymous = 1,
    /// <summary>CORS credentials are included.</summary>
    UseCredentials = 2
}
