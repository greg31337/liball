namespace pdf;

/// <summary>
/// Selects a level-B PDF/A archival conformance profile supported by the writer.
/// </summary>
public enum PdfAConformance
{
    /// <summary>Uses PDF/A-1b with PDF 1.4 and no embedded files or transparency.</summary>
    PdfA1B = 1,
    /// <summary>Uses PDF/A-2b with PDF 1.7 and no arbitrary embedded files.</summary>
    PdfA2B = 2,
    /// <summary>Uses PDF/A-3b with PDF 1.7 and associated embedded files.</summary>
    PdfA3B = 3
}
