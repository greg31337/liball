namespace tiff;

/// <summary>Contains the values and diagnostics produced by a GPS read operation.</summary>
public class TiffGpsReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffGpsMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF GPS Read Result class.</summary>
    public TiffGpsReadResult()
    {
        Success = true;
        Metadata = new TiffGpsMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF GPS Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGpsReadResult Create(TiffGpsMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffGpsReadResult value = new TiffGpsReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffGpsMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffGpsMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
