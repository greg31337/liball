namespace units;

/// <summary>
/// Holds the caller-supplied molar mass required for an opt-in mass and amount conversion.
/// </summary>
public class MolarMassContext : IMolarMassContext
{
    internal QuantityValue _molarMass;

    /// <summary>
    /// Initializes context from a positive scale-only quantity with mass-per-amount dimension.
    /// </summary>
    /// <param name="molarMass">The molar-mass quantity to use.</param>
    public MolarMassContext(QuantityValue molarMass)
    {
        ValidateMolarMass(molarMass);
        _molarMass = new QuantityValue(molarMass.GetValue(), molarMass.GetUnit());
    }

    /// <summary>
    /// Gets the positive scale-only mass-per-amount quantity supplied by the caller.
    /// </summary>
    /// <returns>The molar-mass quantity.</returns>
    public QuantityValue GetMolarMass()
    {
        return new QuantityValue(_molarMass.GetValue(), _molarMass.GetUnit());
    }

    internal static void ValidateMolarMass(QuantityValue molarMass)
    {
        if (molarMass == null)
        {
            throw new System.ArgumentNullException("molarMass");
        }

        UnitDefinition unit = molarMass.GetUnit();
        DimensionVector expectedDimension = new StandardDimensions().Mass().Divide(new StandardDimensions().AmountOfSubstance());
        if (!unit.GetDimension().EqualsDimension(expectedDimension))
        {
            throw new System.ArgumentException("Molar mass must use the mass-per-amount dimension.");
        }

        if (unit.GetConversionKind() != UnitConversionKind.Scale || !unit.GetOffsetToSystemUnit().IsZero())
        {
            throw new System.ArgumentException("Molar mass must use a scale-only unit.");
        }

        double systemValue = molarMass.GetValue() * unit.GetScaleToSystemUnit().ToDouble();
        if (systemValue <= 0.0d || double.IsNaN(systemValue) || double.IsInfinity(systemValue))
        {
            throw new System.ArgumentException("Molar mass must be positive and finite.");
        }
    }
}
