namespace currencycodes;

/// <summary>
/// Identifies the outcome category of code normalization.
/// </summary>
public enum NormalizationError
{
    /// <summary>
    /// Normalization completed without an error.
    /// </summary>
    None = 0,

    /// <summary>
    /// The supplied text is <c>null</c>.
    /// </summary>
    NullInput = 1,

    /// <summary>
    /// The supplied text is empty after permitted trimming.
    /// </summary>
    EmptyInput = 2,

    /// <summary>
    /// The supplied text has an invalid length.
    /// </summary>
    InvalidLength = 3,

    /// <summary>
    /// The supplied text contains an invalid character.
    /// </summary>
    InvalidCharacter = 4,

    /// <summary>
    /// The supplied value is outside the accepted range.
    /// </summary>
    OutOfRange = 5
}
