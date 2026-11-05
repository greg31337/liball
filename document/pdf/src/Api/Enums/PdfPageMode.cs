namespace pdf;

/// <summary>
/// Specifies the document panel or presentation mode selected when a PDF opens.
/// </summary>
public enum PdfPageMode
{
    /// <summary>
    /// Opens without outlines or thumbnail panels.
    /// </summary>
    UseNone = 0,
    /// <summary>
    /// Opens with the document outline panel visible.
    /// </summary>
    UseOutlines = 1,
    /// <summary>
    /// Opens with the page thumbnail panel visible.
    /// </summary>
    UseThumbnails = 2,
    /// <summary>
    /// Opens in full-screen presentation mode.
    /// </summary>
    FullScreen = 3,
    /// <summary>
    /// Opens with the optional-content panel visible; requires PDF 1.5 or later.
    /// </summary>
    UseOptionalContent = 4,
    /// <summary>
    /// Opens with the attachments panel visible; requires PDF 1.6 or later.
    /// </summary>
    UseAttachments = 5
}
