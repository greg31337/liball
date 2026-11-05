namespace tiff;

/// <summary>Contains the values and diagnostics produced by a image description read operation.</summary>
public class TiffImageDescriptionReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffImageDescriptionMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Image Description Read Result class.</summary>
    public TiffImageDescriptionReadResult()
    {
        Success = true;
        Metadata = new TiffImageDescriptionMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Image Description Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffImageDescriptionReadResult Create(TiffImageDescriptionMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffImageDescriptionReadResult value = new TiffImageDescriptionReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffImageDescriptionMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffImageDescriptionMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
