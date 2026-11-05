namespace sip;

/// <summary>
/// Holds a parsed CSeq sequence number and method.
/// </summary>
public class SipCSeqValue
{
    /// <summary>The unsigned 31-bit CSeq number represented as a signed integer.</summary>
    public int SequenceNumber;

    /// <summary>The recognized CSeq method.</summary>
    public SipMethod Method;

    /// <summary>The original method token, including an extension method.</summary>
    public string? MethodToken;
}
