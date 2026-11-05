using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of a message decode operation.</summary>
public class DecodeResult
{
    /// <summary>The operation status.</summary>
    public DecodeStatus Status = DecodeStatus.Incomplete;
    /// <summary>The decoded message when decoding succeeded.</summary>
    public FixMessage? Message;
    /// <summary>The number of source bytes consumed.</summary>
    public int Consumed;
    /// <summary>The total number of source bytes required when input is incomplete.</summary>
    public int Required;
    /// <summary>The source offset associated with an error, or <c>-1</c> when unavailable.</summary>
    public int ErrorOffset = -1;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
    /// <summary>The ordered field scan outcomes retained for diagnostics.</summary>
    public List<FieldParseResult> FieldResults = [];
}
