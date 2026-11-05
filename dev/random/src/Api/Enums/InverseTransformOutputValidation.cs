namespace random;

/// <summary>
/// Selects validation applied to values returned by an inverse transform.
/// </summary>
public enum InverseTransformOutputValidation
{
    /// <summary>
    /// Returns every delegate output unchanged, including NaN and infinities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Rejects NaN while allowing finite values and signed infinities.
    /// </summary>
    RejectNaN = 1,

    /// <summary>
    /// Requires every output to be finite.
    /// </summary>
    RequireFinite = 2
}
