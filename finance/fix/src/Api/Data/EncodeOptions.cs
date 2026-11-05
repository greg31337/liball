namespace fix;

/// <summary>Controls TagValue message encoding and its resource bounds.</summary>
public class EncodeOptions
{
    /// <summary>The maximum encoded message size in bytes.</summary>
    public int MaxMessageSize = FixDefaults.MaxMessageSize;
    /// <summary>The maximum encoded field value size in bytes.</summary>
    public int MaxFieldSize = FixDefaults.MaxFieldSize;
    /// <summary>Whether retained field order is preferred over canonical dictionary order.</summary>
    public bool PreserveFieldOrder = true;
    /// <summary>Whether fields absent from the dictionary are emitted.</summary>
    public bool IncludeUnknownFields = true;
    /// <summary>Whether structured groups are flattened into TagValue fields.</summary>
    public bool FlattenGroups = true;
    /// <summary>Whether BodyLength is calculated during encoding.</summary>
    public bool RecalculateBodyLength = true;
    /// <summary>Whether CheckSum is calculated during encoding.</summary>
    public bool RecalculateCheckSum = true;
    /// <summary>The single-byte text encoding registered name.</summary>
    public string TextEncodingName = FixDefaults.Latin1EncodingName;
}
