namespace docx;

/// <summary>
/// Controls package serialization and ZIP compression.
/// </summary>
public class DocxSaveOptions
{
    /// <summary>
    /// Whether whitespace-only XML nodes are preserved in serialized XML parts.
    /// </summary>
    public bool PreserveXmlWhitespace;
    /// <summary>
    /// Whether package parts with unsupported content types are written.
    /// </summary>
    public bool PreserveUnknownParts;
    /// <summary>
    /// Whether relationships with unsupported relationship types are written.
    /// </summary>
    public bool PreserveUnknownRelationships;
    /// <summary>
    /// Whether markup in unsupported namespaces is written in WordprocessingML parts.
    /// </summary>
    public bool PreserveUnknownMarkup;
    /// <summary>
    /// Whether the deterministic behavior is enabled.
    /// </summary>
    public bool Deterministic;
    /// <summary>
    /// The save mode. Deterministic mode also takes effect when <see cref="Deterministic"/> is true.
    /// </summary>
    public DocxSaveMode Mode;
    /// <summary>
    /// The ZIP compression setting applied to package entries.
    /// </summary>
    public DocxCompressionMode CompressionMode;
    /// <summary>
    /// The Word document or template content type to emit.
    /// </summary>
    public DocxOutputType OutputType;

    /// <summary>
    /// Creates a save options instance.
    /// </summary>
    public DocxSaveOptions()
    {
        this.PreserveXmlWhitespace = true;
        this.PreserveUnknownParts = true;
        this.PreserveUnknownRelationships = true;
        this.PreserveUnknownMarkup = true;
        this.Deterministic = false;
        this.Mode = DocxSaveMode.Standard;
        this.CompressionMode = DocxCompressionMode.Optimal;
        this.OutputType = DocxOutputType.Preserve;
    }

    internal bool UsesDeterministicOutput()
    {
        if (this.Deterministic)
        {
            return true;
        }

        return this.Mode == DocxSaveMode.DeterministicRoundTrip;
    }

    internal void ValidateValues()
    {
        if (this.Mode != DocxSaveMode.Standard && this.Mode != DocxSaveMode.DeterministicRoundTrip)
        {
            throw new ArgumentOutOfRangeException("Mode");
        }

        if (this.CompressionMode != DocxCompressionMode.Optimal
            && this.CompressionMode != DocxCompressionMode.Fastest
            && this.CompressionMode != DocxCompressionMode.NoCompression)
        {
            throw new ArgumentOutOfRangeException("CompressionMode");
        }

        if (this.OutputType != DocxOutputType.Preserve
            && this.OutputType != DocxOutputType.Document
            && this.OutputType != DocxOutputType.Template)
        {
            throw new ArgumentOutOfRangeException("OutputType");
        }
    }
}
