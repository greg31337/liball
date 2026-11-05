namespace sip;

/// <summary>
/// Holds the outcome and bytes of a codec write operation.
/// </summary>
public class SipWriteResult
{
    /// <summary>Indicates whether serialization completed successfully.</summary>
    public bool Success;

    /// <summary>The serialized bytes, empty when serialization failed.</summary>
    public byte[] Bytes = new byte[0];

    /// <summary>The number of bytes written.</summary>
    public int Written;

    /// <summary>The required destination size, or zero when unavailable.</summary>
    public int RequiredBytes;

    /// <summary>The failure message, or null when serialization succeeded.</summary>
    public string? ErrorMessage;
}
