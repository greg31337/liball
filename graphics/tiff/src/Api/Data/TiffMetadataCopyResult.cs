namespace tiff;

/// <summary>Contains the values and diagnostics produced by a metadata copy operation.</summary>
public class TiffMetadataCopyResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the values.</summary>
    public TiffTagValue[] Values;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty successful metadata-copy result.</summary>
    public TiffMetadataCopyResult()
    {
        Success = true;
        Values = new TiffTagValue[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffMetadataCopyResult Create(TiffTagValue[] values, TiffDiagnostics diagnostics)
    {
        TiffMetadataCopyResult value = new TiffMetadataCopyResult();
        value.Initialize(values, diagnostics);
        return value;
    }

    internal void Initialize(TiffTagValue[] values, TiffDiagnostics diagnostics)
    {
        Values = TiffRawUnknownTagPreservationResult.CopyValues(values);
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    internal int ValueCount()
    {
        return Values.Length;
    }
}
