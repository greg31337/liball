namespace pdf;

/// <summary>
/// Stores the standardized XMP metadata identifying a Factur-X/ZUGFeRD invoice attachment.
/// </summary>
public class PdfFacturXMetadata
{
    /// <summary>Selects the structured invoice-data profile.</summary>
    public PdfFacturXProfile Profile;
    /// <summary>Stores the embedded XML file name, which must be <c>factur-x.xml</c>.</summary>
    public string DocumentFileName;
    /// <summary>Stores the hybrid-document type, which must be <c>INVOICE</c>.</summary>
    public string DocumentType;
    /// <summary>Stores the Factur-X XMP schema version, which must be <c>1.0</c>.</summary>
    public string Version;
    /// <summary>Stores the standardized Factur-X invoice XMP namespace.</summary>
    public string NamespaceUri;

    /// <summary>Creates standardized metadata for the requested Factur-X/ZUGFeRD profile.</summary>
    /// <param name="profile">The structured invoice-data profile.</param>
    public PdfFacturXMetadata(PdfFacturXProfile profile)
    {
        Profile = profile;
        DocumentFileName = "factur-x.xml";
        DocumentType = "INVOICE";
        Version = "1.0";
        NamespaceUri = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";
    }

    internal PdfFacturXMetadata Copy()
    {
        PdfFacturXMetadata copied = new PdfFacturXMetadata(Profile);
        copied.DocumentFileName = DocumentFileName;
        copied.DocumentType = DocumentType;
        copied.Version = Version;
        copied.NamespaceUri = NamespaceUri;
        return copied;
    }
}
