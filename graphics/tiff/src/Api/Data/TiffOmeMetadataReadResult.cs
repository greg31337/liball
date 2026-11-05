namespace tiff;

/// <summary>Contains the values and diagnostics produced by a OME metadata read operation.</summary>
public class TiffOmeMetadataReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffOmeMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF OME Metadata Read Result class.</summary>
    public TiffOmeMetadataReadResult()
    {
        Success = true;
        Metadata = new TiffOmeMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF OME Metadata Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffOmeMetadataReadResult Create(TiffOmeMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffOmeMetadataReadResult value = new TiffOmeMetadataReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffOmeMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffOmeMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
