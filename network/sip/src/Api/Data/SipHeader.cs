namespace sip;

/// <summary>
/// Holds one ordered SIP header field without discarding unknown names or values.
/// </summary>
public class SipHeader
{
    /// <summary>The header field name with its original spelling.</summary>
    public string? Name;

    /// <summary>The unfolded header field value.</summary>
    public string? Value;

    /// <summary>The original complete header field bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = new byte[0];
}
