namespace tiff;

/// <summary>Contains the values and diagnostics produced by a vendor metadata read operation.</summary>
public class TiffVendorMetadataReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffVendorMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Vendor Metadata Read Result class.</summary>
    public TiffVendorMetadataReadResult()
    {
        Success = true;
        Metadata = new TiffVendorMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Vendor Metadata Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffVendorMetadataReadResult Create(TiffVendorMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffVendorMetadataReadResult value = new TiffVendorMetadataReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffVendorMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffVendorMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
