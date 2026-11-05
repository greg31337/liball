namespace units;

/// <summary>
/// Identifies the built-in nonlinear formula used by an explicitly configured special unit.
/// </summary>
public enum SpecialConversionStrategy
{
    /// <summary>
    /// Uses a base-ten logarithmic ratio between the special value and its system value.
    /// </summary>
    Base10LogarithmicRatio = 1,
    /// <summary>
    /// Uses a natural-logarithmic ratio between the special value and its system value.
    /// </summary>
    NaturalLogarithmicRatio = 2
}
