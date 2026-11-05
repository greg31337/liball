namespace argon2;

/// <summary>
/// Identifies an Argon2 algorithm variant.
/// </summary>
public enum Argon2Variant
{
    /// <summary>
    /// Uses data-dependent memory addressing.
    /// </summary>
    D = 0,

    /// <summary>
    /// Uses data-independent memory addressing.
    /// </summary>
    I = 1,

    /// <summary>
    /// Uses hybrid data-independent and data-dependent memory addressing.
    /// </summary>
    Id = 2
}
