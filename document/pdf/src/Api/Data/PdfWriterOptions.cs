namespace pdf;

/// <summary>
/// Controls the PDF version, deterministic identifiers, and content-stream compression used during serialization.
/// </summary>
public class PdfWriterOptions
{
    /// <summary>
    /// Stores the PDF specification version written by the serializer.
    /// </summary>
    public PdfVersion Version;
    /// <summary>
    /// Indicates whether serialization uses the configured stable document identifier for repeatable output.
    /// </summary>
    public bool Deterministic;
    /// <summary>
    /// Stores the 32-character hexadecimal document identifier used when deterministic output is enabled.
    /// </summary>
    public string DeterministicDocumentId;
    /// <summary>
    /// Indicates whether page and form content streams are compressed with Flate compression.
    /// </summary>
    public bool CompressContentStreams;

    /// <summary>
    /// Initializes writer options for PDF 1.4 with deterministic output and content compression disabled.
    /// </summary>
    public PdfWriterOptions()
    {
        Version = PdfVersion.Pdf14;
        Deterministic = false;
        DeterministicDocumentId = "00000000000000000000000000000000";
        CompressContentStreams = false;
    }
}
