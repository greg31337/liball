namespace pdf;

/// <summary>
/// Selects the Factur-X/ZUGFeRD structured-data conformance profile.
/// </summary>
public enum PdfFacturXProfile
{
    /// <summary>Contains the minimum invoice identification data.</summary>
    Minimum = 1,
    /// <summary>Contains document-level basic data without invoice lines.</summary>
    BasicWl = 2,
    /// <summary>Contains basic document and invoice-line data.</summary>
    Basic = 3,
    /// <summary>Contains all invoice information defined by EN 16931.</summary>
    En16931 = 4,
    /// <summary>Contains EN 16931 data and Factur-X extensions.</summary>
    Extended = 5
}
