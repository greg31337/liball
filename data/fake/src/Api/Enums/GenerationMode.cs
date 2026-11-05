namespace fake;

/// <summary>
/// Selects the broad category of values to generate.
/// </summary>
public enum GenerationMode
{
    /// <summary>
    /// Produces ordinary representative values.
    /// </summary>
    Typical = 0,

    /// <summary>
    /// Produces values at documented boundaries.
    /// </summary>
    Boundary = 1,

    /// <summary>
    /// Produces uncommon values that remain valid.
    /// </summary>
    ValidUnusual = 2,

    /// <summary>
    /// Produces deliberately invalid values.
    /// </summary>
    Invalid = 3,

    /// <summary>
    /// Selects deterministically from typical and edge categories.
    /// </summary>
    Mixed = 4,

    /// <summary>
    /// Produces inert strings intended to exercise unsafe input handling.
    /// </summary>
    Adversarial = 5
}
