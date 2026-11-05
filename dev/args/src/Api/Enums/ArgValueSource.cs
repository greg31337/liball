namespace args;

/// <summary>
/// Identifies where a resolved option or argument value came from.
/// </summary>
public enum ArgValueSource
{
    /// <summary>
    /// No applicable category or source.
    /// </summary>
    None = 0,
    /// <summary>
    /// A value explicitly supplied in the argument array.
    /// </summary>
    CommandLine = 1,
    /// <summary>
    /// A configured default used because no explicit value was supplied.
    /// </summary>
    Default = 2,
    /// <summary>
    /// A value read from a configured environment variable.
    /// </summary>
    Environment = 3,
    /// <summary>
    /// A configured fallback used when an optional-value option appeared without a value.
    /// </summary>
    Fallback = 4,
    /// <summary>
    /// A value supplied by configured implication or inserted programmatically rather than read from a token.
    /// </summary>
    Implied = 5
}
