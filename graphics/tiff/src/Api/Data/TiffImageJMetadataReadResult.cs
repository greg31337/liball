namespace tiff;

/// <summary>Contains the values and diagnostics produced by a image J metadata read operation.</summary>
public class TiffImageJMetadataReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffImageJMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Image J Metadata Read Result class.</summary>
    public TiffImageJMetadataReadResult()
    {
        Success = true;
        Metadata = new TiffImageJMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Image J Metadata Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffImageJMetadataReadResult Create(TiffImageJMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffImageJMetadataReadResult value = new TiffImageJMetadataReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffImageJMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffImageJMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
