namespace tiff;

/// <summary>Contains the values and diagnostics produced by a GPS write operation.</summary>
public class TiffGpsWriteResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Contains the GPS offset.</summary>
    public ulong GpsOffset;
    /// <summary>Contains the number of bytes written by the operation.</summary>
    public ulong BytesWritten;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF GPS Write Result class.</summary>
    public TiffGpsWriteResult()
    {
        Success = false;
        ParentOffset = 0;
        GpsOffset = 0;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF GPS Write Result class.</summary>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="gpsOffset">The gps offset.</param>
    /// <param name="bytesWritten">The bytes written.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffGpsWriteResult Create(ulong parentOffset, ulong gpsOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffGpsWriteResult value = new TiffGpsWriteResult();
        value.Initialize(parentOffset, gpsOffset, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong parentOffset, ulong gpsOffset, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        ParentOffset = parentOffset;
        GpsOffset = gpsOffset;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
