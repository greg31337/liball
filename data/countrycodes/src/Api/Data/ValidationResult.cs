namespace countrycodes;

/// <summary>
/// Holds the structured outcome of input validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the input passed all requested validation rules.
    /// </summary>
    public bool IsValid;

    /// <summary>
    /// The normalized input when normalization produced a value.
    /// </summary>
    public string? NormalizedValue;

    /// <summary>
    /// All validation issues in stable input order.
    /// </summary>
    public ValidationIssue[] Issues;

    /// <summary>
    /// Creates a detached validation result.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="normalizedValue">The normalized value, when available.</param>
    /// <param name="issues">The validation issues.</param>
    public ValidationResult(
        bool isValid,
        string? normalizedValue,
        ValidationIssue[] issues)
    {
        IsValid = isValid;
        NormalizedValue = normalizedValue;
        Issues = ValidationIssue.CopyArray(issues);
    }
}
