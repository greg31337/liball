namespace pptx;

/// <summary>
/// Represents content type override data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxContentTypeOverride
{
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;

    /// <summary>
    /// Initializes a new content type override with safe defaults or the supplied values.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    public PptxContentTypeOverride(string partName, string contentType)
    {
        PartName = PptxDiagnostic.CleanText(partName);
        ContentType = PptxDiagnostic.CleanText(contentType);
    }
}

