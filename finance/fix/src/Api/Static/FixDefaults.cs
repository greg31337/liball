namespace fix;

/// <summary>Defines conservative defaults and hard limits for core processing.</summary>
public static class FixDefaults
{
    /// <summary>The FIX field delimiter byte.</summary>
    public const byte Soh = 0x01;
    /// <summary>The first valid FIX sequence number.</summary>
    public const int InitialSequenceNumber = 1;
    /// <summary>The number of decimal digits in a checksum value.</summary>
    public const int ChecksumDigits = 3;
    /// <summary>The modulus used to calculate a FIX checksum.</summary>
    public const int ChecksumModulo = 256;
    /// <summary>The size in bytes of a Simple Open Framing Header.</summary>
    public const int SofhHeaderSize = 6;
    /// <summary>The default heartbeat interval in seconds.</summary>
    public const int HeartbeatIntervalSeconds = 30;
    /// <summary>The default maximum absolute inbound SendingTime latency in seconds.</summary>
    public const int MaxLatencySeconds = 120;
    /// <summary>The largest accepted FIX tag number.</summary>
    public const int MaxTagNumber = 999999;
    /// <summary>The maximum field value size in bytes.</summary>
    public const int MaxFieldSize = 1048576;
    /// <summary>The maximum complete message size in bytes.</summary>
    public const int MaxMessageSize = 16777216;
    /// <summary>The maximum number of fields in one message.</summary>
    public const int MaxFieldsPerMessage = 4096;
    /// <summary>The maximum nesting depth of repeating groups.</summary>
    public const int MaxGroupDepth = 16;
    /// <summary>The maximum number of entries in one repeating group.</summary>
    public const int MaxGroupEntries = 100000;
    /// <summary>The maximum number of definitions loaded from one dictionary.</summary>
    public const int MaxDictionaryDefinitions = 1000000;
    /// <summary>The maximum number of diagnostics retained by one operation.</summary>
    public const int MaxDiagnostics = 1000;
    /// <summary>The maximum accepted XML element nesting depth.</summary>
    public const int MaxXmlDepth = 64;
    /// <summary>The default single-byte text encoding name.</summary>
    public const string Latin1EncodingName = "iso-8859-1";
}
