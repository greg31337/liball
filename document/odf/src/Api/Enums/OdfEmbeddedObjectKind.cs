namespace odf;

/// <summary>
/// Identifies embedded or linked object content.
/// </summary>
public enum OdfEmbeddedObjectKind
{
    /// <summary>The object kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>An embedded OpenDocument subdocument.</summary>
    Subdocument = 1,
    /// <summary>An OLE object or other opaque binary object.</summary>
    Ole = 2,
    /// <summary>A MathML formula object.</summary>
    MathMl = 3,
    /// <summary>An embedded OpenDocument chart object.</summary>
    Chart = 4
}
