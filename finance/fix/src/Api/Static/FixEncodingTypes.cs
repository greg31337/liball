namespace fix;

/// <summary>Defines standard SOFH encoding type identifiers.</summary>
public static class FixEncodingTypes
{
    /// <summary>The first encoding identifier reserved for private use.</summary>
    public const ushort PrivateUseMinimum = 0x0001;
    /// <summary>The last encoding identifier reserved for private use.</summary>
    public const ushort PrivateUseMaximum = 0x00FF;
    /// <summary>Google Protocol Buffers version 1.0.</summary>
    public const ushort GoogleProtocolBuffersVersion10 = 0x4700;
    /// <summary>FIX SBE version 1.0 in big-endian byte order.</summary>
    public const ushort SbeVersion10BigEndian = 0x5BE0;
    /// <summary>FIX SBE version 2.0 in big-endian byte order.</summary>
    public const ushort SbeVersion20BigEndian = 0x5BE1;
    /// <summary>ASN.1 PER version 1.0.</summary>
    public const ushort Asn1PerVersion10 = 0xA500;
    /// <summary>ASN.1 BER version 1.0.</summary>
    public const ushort Asn1BerVersion10 = 0xA501;
    /// <summary>ASN.1 OER version 1.0.</summary>
    public const ushort Asn1OerVersion10 = 0xA502;
    /// <summary>FIX SBE version 1.0 in little-endian byte order.</summary>
    public const ushort SbeVersion10LittleEndian = 0xEB50;
    /// <summary>FIX SBE version 2.0 in little-endian byte order.</summary>
    public const ushort SbeVersion20LittleEndian = 0xEB51;
    /// <summary>FIX TagValue encoding.</summary>
    public const ushort TagValue = 0xF000;
    /// <summary>FIXML schema encoding version 1.0.</summary>
    public const ushort FixmlVersion10 = 0xF100;
    /// <summary>FIX JSON encoding.</summary>
    public const ushort Json = 0xF500;
    /// <summary>The first identifier reserved for FIX FAST encodings.</summary>
    public const ushort FastMinimum = 0xFA01;
    /// <summary>The last identifier reserved for FIX FAST encodings.</summary>
    public const ushort FastMaximum = 0xFAFF;
    /// <summary>FIX BSON encoding.</summary>
    public const ushort Bson = 0xFB00;
}
