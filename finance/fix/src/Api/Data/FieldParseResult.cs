namespace fix;

/// <summary>Contains the explicit outcome of scanning or parsing one FIX field.</summary>
public class FieldParseResult
{
    /// <summary>The parse status.</summary>
    public DecodeStatus Status = DecodeStatus.Incomplete;
    /// <summary>The parsed field when parsing succeeded.</summary>
    public FixField? Field;
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
}
