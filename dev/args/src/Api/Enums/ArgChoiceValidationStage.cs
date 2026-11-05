namespace args;

/// <summary>
/// Selects which representation is compared during choice validation.
/// </summary>
public enum ArgChoiceValidationStage
{
    /// <summary>
    /// Compares source text exactly as resolved by the parser.
    /// </summary>
    Raw = 0,
    /// <summary>
    /// Compares text after applying the symbol's configured normalizer.
    /// </summary>
    Normalized = 1,
    /// <summary>
    /// Compares values produced after normalization by the symbol's configured choice converter.
    /// </summary>
    Converted = 2
}
