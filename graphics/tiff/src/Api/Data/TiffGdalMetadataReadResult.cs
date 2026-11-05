namespace tiff;

/// <summary>Contains the values and diagnostics produced by a GDAL metadata read operation.</summary>
public class TiffGdalMetadataReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffGdalMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF GDAL Metadata Read Result class.</summary>
    public TiffGdalMetadataReadResult()
    {
        Success = true;
        Metadata = new TiffGdalMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF GDAL Metadata Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGdalMetadataReadResult Create(TiffGdalMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffGdalMetadataReadResult value = new TiffGdalMetadataReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffGdalMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffGdalMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
