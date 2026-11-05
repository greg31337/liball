namespace random;

/// <summary>
/// Selects the mathematical implementation used by distribution transforms.
/// </summary>
/// <remarks>
/// The mode is part of a distribution replay specification. It does not change raw engine words
/// or integer-to-floating grid conversions, and it is supplied separately when restoring a
/// <see cref="RandomState"/> because engine-state envelopes do not encode it.
/// </remarks>
public enum RandomMathMode
{
    /// <summary>
    /// Uses versioned library kernels to provide bit-for-bit replay on supported platforms.
    /// </summary>
    Portable = 0,

    /// <summary>
    /// Uses platform math functions and provides statistical, but not last-bit, compatibility.
    /// </summary>
    System = 1
}
