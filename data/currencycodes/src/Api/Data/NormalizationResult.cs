namespace currencycodes;

/// <summary>
/// Stores the structured outcome of code normalization.
/// </summary>
public class NormalizationResult
{
    /// <summary>
    /// Indicates whether normalization succeeded.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// Identifies the normalization outcome category.
    /// </summary>
    public NormalizationError Error = NormalizationError.None;

    /// <summary>
    /// Stores the original input, including <c>null</c> when no input was supplied.
    /// </summary>
    public string? OriginalText = null;

    /// <summary>
    /// Stores the normalized text when normalization succeeds.
    /// </summary>
    public string NormalizedText = string.Empty;
}
