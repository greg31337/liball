namespace random;

/// <summary>
/// Defines public convergence limits used by Conway-Maxwell-Poisson sampling.
/// </summary>
public static class ConwayMaxwellPoissonLimits
{
    /// <summary>
    /// The maximum number of terms used to normalize a Conway-Maxwell-Poisson distribution.
    /// </summary>
    public const int MaximumNormalizationTerms = 100001;

    /// <summary>
    /// The relative convergence tolerance used while computing the normalization series.
    /// </summary>
    public const double NormalizationRelativeTolerance = 1.0e-13;
}
