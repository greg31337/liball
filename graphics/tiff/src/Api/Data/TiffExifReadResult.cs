namespace tiff;

/// <summary>Contains the values and diagnostics produced by a EXIF read operation.</summary>
public class TiffExifReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffExifMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF EXIF Read Result class.</summary>
    public TiffExifReadResult()
    {
        Success = true;
        Metadata = new TiffExifMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF EXIF Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffExifReadResult Create(TiffExifMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffExifReadResult value = new TiffExifReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffExifMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffExifMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffExifReadResult Copy()
    {
        return TiffExifReadResult.Create(Metadata, Diagnostics);
    }
}
