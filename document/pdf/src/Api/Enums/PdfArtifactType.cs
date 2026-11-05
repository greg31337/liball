namespace pdf;

/// <summary>
/// Specifies the semantic kind of content excluded from the tagged reading order.
/// </summary>
public enum PdfArtifactType
{
    /// <summary>Layout-only content such as rules or decoration.</summary>
    Layout = 1,
    /// <summary>Page-level production content.</summary>
    Page = 2,
    /// <summary>Background content.</summary>
    Background = 3
}
