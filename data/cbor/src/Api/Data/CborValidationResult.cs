namespace cbor;

/// <summary>
/// Holds the outcome of validating CBOR data.
/// </summary>
public class CborValidationResult
{
    /// <summary>Indicates whether the input passed the selected validation level.</summary>
    public bool IsValid;
    /// <summary>The validation issues.</summary>
    public CborValidationIssue[] Issues = [];
}
