namespace fix;

/// <summary>Controls TagValue message decoding and its resource bounds.</summary>
public class DecodeOptions
{
    /// <summary>The maximum complete message size in bytes.</summary>
    public int MaxMessageSize = FixDefaults.MaxMessageSize;
    /// <summary>The maximum field value size in bytes.</summary>
    public int MaxFieldSize = FixDefaults.MaxFieldSize;
    /// <summary>The maximum number of fields accepted in one message.</summary>
    public int MaxFieldsPerMessage = FixDefaults.MaxFieldsPerMessage;
    /// <summary>The maximum repeating-group nesting depth.</summary>
    public int MaxGroupDepth = FixDefaults.MaxGroupDepth;
    /// <summary>The maximum number of entries accepted in one group.</summary>
    public int MaxGroupEntries = FixDefaults.MaxGroupEntries;
    /// <summary>Whether BodyLength must match the encoded body size.</summary>
    public bool VerifyBodyLength = true;
    /// <summary>Whether CheckSum must match the encoded message bytes.</summary>
    public bool VerifyCheckSum = true;
    /// <summary>Whether the mandatory envelope fields must use canonical order.</summary>
    public bool RequireCanonicalEnvelope = true;
    /// <summary>Whether empty field values are accepted by the decoder.</summary>
    public bool AllowEmptyValues;
    /// <summary>Whether fields absent from the dictionary are retained.</summary>
    public bool PreserveUnknownFields = true;
    /// <summary>Whether the complete source message is copied into the result.</summary>
    public bool PreserveOriginalBytes = true;
    /// <summary>The single-byte text encoding registered name.</summary>
    public string TextEncodingName = FixDefaults.Latin1EncodingName;
}
