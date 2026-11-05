namespace tiff;

/// <summary>Contains the values and diagnostics produced by a geographic parameter read operation.</summary>
public class TiffGeoParameterReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffGeoParameterMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF geographic Parameter Read Result class.</summary>
    public TiffGeoParameterReadResult()
    {
        Success = true;
        Metadata = new TiffGeoParameterMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF geographic Parameter Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGeoParameterReadResult Create(TiffGeoParameterMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffGeoParameterReadResult value = new TiffGeoParameterReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffGeoParameterMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffGeoParameterMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
