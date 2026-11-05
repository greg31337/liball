using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of external binary decoding.</summary>
public class BinaryDecodeResult
{
    /// <summary>The decode status.</summary>
    public DecodeStatus Status = DecodeStatus.Incomplete;
    /// <summary>The decoded canonical message when available.</summary>
    public FixMessage? Message;
    /// <summary>The effective encoding, schema, template, version, and dictionary metadata discovered by the codec.</summary>
    public BinaryMessageContext Context = new BinaryMessageContext();
    /// <summary>The number of source bytes consumed.</summary>
    public int Consumed;
    /// <summary>The total source size required when incomplete.</summary>
    public int Required;
    /// <summary>The ordered codec diagnostics.</summary>
    public List<ValidationIssue> Diagnostics = [];
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
