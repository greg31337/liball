namespace tiff;

/// <summary>Contains the values and diagnostics produced by a photoshop read operation.</summary>
public class TiffPhotoshopReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed metadata when it was present and valid.</summary>
    public TiffPhotoshopMetadata Metadata;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Photoshop Read Result class.</summary>
    public TiffPhotoshopReadResult()
    {
        Success = true;
        Metadata = new TiffPhotoshopMetadata();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Photoshop Read Result class.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffPhotoshopReadResult Create(TiffPhotoshopMetadata metadata, TiffDiagnostics diagnostics)
    {
        TiffPhotoshopReadResult value = new TiffPhotoshopReadResult();
        value.Initialize(metadata, diagnostics);
        return value;
    }

    internal void Initialize(TiffPhotoshopMetadata metadata, TiffDiagnostics diagnostics)
    {
        if (metadata == null)
        {
            Metadata = new TiffPhotoshopMetadata();
        }
        else
        {
            Metadata = metadata.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
