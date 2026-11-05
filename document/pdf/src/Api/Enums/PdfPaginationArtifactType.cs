namespace pdf;

/// <summary>
/// Specifies a repeating pagination artifact subtype.
/// </summary>
public enum PdfPaginationArtifactType
{
    /// <summary>A repeating page header.</summary>
    Header = 1,
    /// <summary>A repeating page footer.</summary>
    Footer = 2,
    /// <summary>A page watermark.</summary>
    Watermark = 3
}
