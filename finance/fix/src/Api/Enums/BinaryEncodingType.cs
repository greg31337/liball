namespace fix;

/// <summary>Identifies the encoding carried by a binary message boundary.</summary>
public enum BinaryEncodingType
{
    /// <summary>The encoding is not known.</summary>
    Unknown = 0,
    /// <summary>FIX TagValue encoding.</summary>
    TagValue = 1,
    /// <summary>Simple Binary Encoding.</summary>
    Sbe = 2,
    /// <summary>FIXML encoding.</summary>
    Fixml = 3,
    /// <summary>JSON encoding.</summary>
    Json = 4,
    /// <summary>Protocol Buffers encoding.</summary>
    Protobuf = 5,
    /// <summary>A venue-defined or proprietary encoding.</summary>
    Proprietary = 6
}
