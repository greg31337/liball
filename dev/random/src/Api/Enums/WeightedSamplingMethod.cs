namespace random;

/// <summary>
/// Selects the preprocessing and draw algorithm used by <see cref="WeightedSampler"/>.
/// </summary>
public enum WeightedSamplingMethod
{
    /// <summary>Uses one cumulative table and a linear search per draw.</summary>
    Cumulative = 0,

    /// <summary>Uses a guide table to narrow each cumulative search.</summary>
    GuideTable = 1,

    /// <summary>Uses Vose alias preprocessing for constant-time draws.</summary>
    AliasTable = 2
}
