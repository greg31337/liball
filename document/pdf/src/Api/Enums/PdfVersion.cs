namespace pdf;

/// <summary>
/// Specifies the PDF specification version written by the document serializer.
/// </summary>
public enum PdfVersion
{
    /// <summary>
    /// Writes PDF 1.4 output.
    /// </summary>
    Pdf14 = 14,
    /// <summary>
    /// Writes PDF 1.5 output.
    /// </summary>
    Pdf15 = 15,
    /// <summary>
    /// Writes PDF 1.6 output.
    /// </summary>
    Pdf16 = 16,
    /// <summary>
    /// Writes PDF 1.7 output.
    /// </summary>
    Pdf17 = 17,
    /// <summary>
    /// Writes PDF 2.0 output.
    /// </summary>
    Pdf20 = 20
}
