namespace sdp;

/// <summary>
/// Represents an obsolete SDP encryption-key field retained for compatibility.
/// </summary>
public class SdpEncryptionKey
{
    /// <summary>Contains the registered or extension key method.</summary>
    public string Method = string.Empty;

    /// <summary>Indicates whether a colon-delimited key value was present.</summary>
    public bool HasValue = false;

    /// <summary>Contains the key value and must be treated as sensitive data.</summary>
    public string Value = string.Empty;

    /// <summary>Contains the original text following <c>k=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the encryption-key field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
