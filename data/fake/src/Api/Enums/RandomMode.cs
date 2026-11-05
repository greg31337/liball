namespace fake;

/// <summary>
/// Selects the source of random values.
/// </summary>
public enum RandomMode
{
    /// <summary>
    /// Uses the library's stable deterministic random stream.
    /// </summary>
    Deterministic = 0,

    /// <summary>
    /// Uses the platform cryptographic random-number generator.
    /// </summary>
    Cryptographic = 1
}
