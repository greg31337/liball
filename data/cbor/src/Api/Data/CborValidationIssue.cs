namespace cbor;

/// <summary>
/// Describes one issue found while validating CBOR data.
/// </summary>
public class CborValidationIssue
{
    /// <summary>The issue severity.</summary>
    public CborIssueSeverity Severity = CborIssueSeverity.Error;
    /// <summary>The stable issue code.</summary>
    public string? Code;
    /// <summary>The explanatory issue message.</summary>
    public string? Message;
    /// <summary>The encoded byte offset, or negative one when it is unavailable.</summary>
    public long ByteOffset = -1;
    /// <summary>The path to the affected value.</summary>
    public string? Path;
}
