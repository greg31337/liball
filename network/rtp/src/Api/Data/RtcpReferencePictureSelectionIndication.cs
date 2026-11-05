namespace rtp;

/// <summary>
/// Contains an RFC 4585 reference picture selection indication.
/// </summary>
public class RtcpReferencePictureSelectionIndication
{
    /// <summary>The number of unused padding bits at the end of the FCI.</summary>
    public byte PaddingBitCount;

    /// <summary>The seven-bit RTP payload type defining the native bit string.</summary>
    public byte PayloadType;

    /// <summary>The number of meaningful bits in the native bit string.</summary>
    public int BitLength;

    /// <summary>The native codec-defined RPSI bit string.</summary>
    public byte[] NativeBitString = [];

    internal RtcpReferencePictureSelectionIndication Copy()
    {
        RtcpReferencePictureSelectionIndication copy =
            new RtcpReferencePictureSelectionIndication();
        copy.PaddingBitCount = PaddingBitCount;
        copy.PayloadType = PayloadType;
        copy.BitLength = BitLength;
        copy.NativeBitString = (byte[])NativeBitString.Clone();
        return copy;
    }
}
