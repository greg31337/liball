namespace tiff;

/// <summary>Contains the values and diagnostics produced by a XMP read operation.</summary>
public class TiffXmpReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the packet.</summary>
    public TiffXmpPacket Packet;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF XMP Read Result class.</summary>
    public TiffXmpReadResult()
    {
        Success = true;
        Packet = new TiffXmpPacket();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF XMP Read Result class.</summary>
    /// <param name="packet">The packet.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffXmpReadResult Create(TiffXmpPacket packet, TiffDiagnostics diagnostics)
    {
        TiffXmpReadResult value = new TiffXmpReadResult();
        value.Initialize(packet, diagnostics);
        return value;
    }

    internal void Initialize(TiffXmpPacket packet, TiffDiagnostics diagnostics)
    {
        if (packet == null)
        {
            Packet = new TiffXmpPacket();
        }
        else
        {
            Packet = packet.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
