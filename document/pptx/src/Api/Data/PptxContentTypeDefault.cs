namespace pptx;

/// <summary>
/// Represents content type default data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxContentTypeDefault
{
    /// <summary>
    /// Stores the extension associated with this object.
    /// </summary>
    public string Extension;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;

    /// <summary>
    /// Initializes a new content type default with safe defaults or the supplied values.
    /// </summary>
    /// <param name="extension">The extension value to use.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    public PptxContentTypeDefault(string extension, string contentType)
    {
        Extension = PptxDiagnostic.CleanText(extension);
        ContentType = PptxDiagnostic.CleanText(contentType);
    }
}

