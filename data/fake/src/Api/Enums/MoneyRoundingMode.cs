namespace fake;

/// <summary>
/// Selects the rounding rule applied to generated and supplied monetary values.
/// </summary>
public enum MoneyRoundingMode
{
    /// <summary>Rounds midpoint values to the nearest value with an even final digit.</summary>
    ToEven = 0,

    /// <summary>Rounds midpoint values to the nearest value away from zero.</summary>
    AwayFromZero = 1,

    /// <summary>Rounds toward zero.</summary>
    ToZero = 2,

    /// <summary>Rounds toward negative infinity.</summary>
    ToNegativeInfinity = 3,

    /// <summary>Rounds toward positive infinity.</summary>
    ToPositiveInfinity = 4
}
