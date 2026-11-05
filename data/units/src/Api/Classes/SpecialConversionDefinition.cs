namespace units;

/// <summary>
/// Describes the explicit built-in nonlinear strategy used by one special unit.
/// </summary>
/// <remarks>
/// The special value is a configured coefficient times the logarithm of the ratio between its system value and a positive reference system value. A negative coefficient supports inverse logarithmic scales without introducing a cross-dimension equivalency.
/// </remarks>
public class SpecialConversionDefinition : ISpecialConversionDefinition
{
    internal string _unitId;
    internal SpecialConversionStrategy _strategy;
    internal RationalNumber _referenceSystemValue;
    internal RationalNumber _coefficient;

    /// <summary>
    /// Initializes explicit strategy metadata for one special unit.
    /// </summary>
    /// <param name="unitId">The stable identifier of the special unit.</param>
    /// <param name="strategy">The built-in nonlinear strategy to use.</param>
    /// <param name="referenceSystemValue">The positive reference value expressed in system units.</param>
    /// <param name="coefficient">The nonzero coefficient applied to the logarithm.</param>
    public SpecialConversionDefinition(string unitId, SpecialConversionStrategy strategy, RationalNumber referenceSystemValue, RationalNumber coefficient)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new System.ArgumentException("Special conversion unit id must not be empty.");
        }

        if (strategy != SpecialConversionStrategy.Base10LogarithmicRatio && strategy != SpecialConversionStrategy.NaturalLogarithmicRatio)
        {
            throw new System.ArgumentException("Special conversion strategy is not supported.");
        }

        if (referenceSystemValue == null)
        {
            throw new System.ArgumentNullException("referenceSystemValue");
        }

        if (!referenceSystemValue.IsPositive())
        {
            throw new System.ArgumentException("Special conversion reference system value must be positive.");
        }

        if (coefficient == null)
        {
            throw new System.ArgumentNullException("coefficient");
        }

        if (coefficient.IsZero())
        {
            throw new System.ArgumentException("Special conversion coefficient must not be zero.");
        }

        _unitId = unitId;
        _strategy = strategy;
        _referenceSystemValue = referenceSystemValue;
        _coefficient = coefficient;
    }

    /// <summary>
    /// Gets the special unit identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetUnitId()
    {
        return _unitId;
    }

    /// <summary>
    /// Gets the built-in special conversion strategy.
    /// </summary>
    /// <returns>The resulting special conversion strategy.</returns>
    public SpecialConversionStrategy GetStrategy()
    {
        return _strategy;
    }

    /// <summary>
    /// Gets the positive reference value expressed in system units.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetReferenceSystemValue()
    {
        return _referenceSystemValue;
    }

    /// <summary>
    /// Gets the nonzero logarithmic coefficient.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetCoefficient()
    {
        return _coefficient;
    }

    /// <summary>
    /// Reports whether the supplied unit is the configured special unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesUnit(UnitDefinition? unit)
    {
        if (unit == null)
        {
            return false;
        }

        return unit.GetConversionKind() == UnitConversionKind.Special && string.Equals(unit.GetId(), _unitId, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts a special-unit value to its system value.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertToSystemValue(double value)
    {
        double exponent = value / _coefficient.ToDouble();
        if (_strategy == SpecialConversionStrategy.Base10LogarithmicRatio)
        {
            return _referenceSystemValue.ToDouble() * System.Math.Pow(10.0d, exponent);
        }

        return _referenceSystemValue.ToDouble() * System.Math.Exp(exponent);
    }

    /// <summary>
    /// Converts a positive system value to its special-unit value.
    /// </summary>
    /// <param name="value">The positive system value to use.</param>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.ArgumentException">The system value is not positive.</exception>
    public double ConvertFromSystemValue(double value)
    {
        if (value <= 0.0d || double.IsNaN(value))
        {
            throw new System.ArgumentException("Special conversion system value must be positive.");
        }

        double ratio = value / _referenceSystemValue.ToDouble();
        if (_strategy == SpecialConversionStrategy.Base10LogarithmicRatio)
        {
            return _coefficient.ToDouble() * System.Math.Log10(ratio);
        }

        return _coefficient.ToDouble() * System.Math.Log(ratio);
    }
}
