namespace random;

/// <summary>
/// Defines public limits used by Poisson sampling.
/// </summary>
public static class PoissonLimits
{
    /// <summary>
    /// The largest supported Poisson mean within the safe signed 64-bit count range.
    /// </summary>
    public const double MaximumMean = 9.2233720064847708e18;
}
