namespace tiff;

/// <summary>Contains the values and diagnostics produced by a pyramid write operation.</summary>
public class TiffPyramidWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the directory offset.</summary>
    public ulong DirectoryOffset;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Pyramid Write Result class.</summary>
    public TiffPyramidWriteResult()
    {
        Success = false;
        DirectoryOffset = 0;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Pyramid Write Result class.</summary>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffPyramidWriteResult Create(ulong directoryOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffPyramidWriteResult value = new TiffPyramidWriteResult();
        value.Initialize(directoryOffset, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong directoryOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        DirectoryOffset = directoryOffset;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
