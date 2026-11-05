namespace pdf;

/// <summary>
/// Specifies normal, superscript, or subscript presentation for a Unicode text span.
/// </summary>
public enum PdfTextScript
{
    /// <summary>
    /// Uses the normal baseline and font size.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Uses a smaller font raised above the normal baseline.
    /// </summary>
    Superscript = 1,
    /// <summary>
    /// Uses a smaller font lowered below the normal baseline.
    /// </summary>
    Subscript = 2
}
