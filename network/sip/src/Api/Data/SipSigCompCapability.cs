namespace sip;

/// <summary>Holds capabilities reported by an external SigComp implementation.</summary>
public class SipSigCompCapability
{
    /// <summary>The supported SigComp version, or negative one when unknown.</summary>
    public int Version = -1;
    /// <summary>Indicates support for SigComp negative acknowledgements.</summary>
    public bool NegativeAcknowledgements;
    /// <summary>The state memory size in bytes, or negative one when unknown.</summary>
    public int StateMemorySize = -1;
    /// <summary>The cycles-per-bit capability, or negative one when unknown.</summary>
    public int CyclesPerBit = -1;
    /// <summary>The decompression memory size in bytes, or negative one when unknown.</summary>
    public int DecompressionMemorySize = -1;
    /// <summary>Opaque identifiers of locally available SigComp states.</summary>
    public byte[][] StateIdentifiers = new byte[0][];
}
