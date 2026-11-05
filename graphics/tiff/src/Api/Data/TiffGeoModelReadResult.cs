namespace tiff;

/// <summary>Contains the values and diagnostics produced by a geographic model read operation.</summary>
public class TiffGeoModelReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffGeoModelMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF geographic Model Read Result class.</summary>
    public TiffGeoModelReadResult()
    {
        Success = true;
        Metadata = new TiffGeoModelMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF geographic Model Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGeoModelReadResult Create(TiffGeoModelMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffGeoModelReadResult value = new TiffGeoModelReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffGeoModelMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffGeoModelMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
