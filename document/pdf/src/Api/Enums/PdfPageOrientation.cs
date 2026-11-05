namespace pdf;

/// <summary>
/// Specifies whether a document section uses the shorter or longer page edge as its width.
/// </summary>
public enum PdfPageOrientation
{
    /// <summary>
    /// Uses the shorter page edge as the width.
    /// </summary>
    Portrait = 0,
    /// <summary>
    /// Uses the longer page edge as the width.
    /// </summary>
    Landscape = 1
}
