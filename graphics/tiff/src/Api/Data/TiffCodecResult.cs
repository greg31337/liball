namespace tiff;

/// <summary>Contains the values and diagnostics produced by a codec operation.</summary>
public class TiffCodecResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Codec Result class.</summary>
    public TiffCodecResult()
    {
        Success = false;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Codec Result class.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffCodecResult Create(byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffCodecResult value = new TiffCodecResult();
        value.Initialize(bytes, diagnostics);
        return value;
    }

    internal void Initialize(byte[] bytes, TiffDiagnostics diagnostics)
    {
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffCodecResult Copy()
    {
        TiffCodecResult copy = TiffCodecResult.Create(Bytes, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
