namespace units;

/// <summary>
/// Provides fresh vectors for the canonical base dimensions.
/// </summary>
public class StandardDimensions : IStandardDimensions
{
    /// <summary>
    /// Initializes a standard-dimension catalog.
    /// </summary>
    public StandardDimensions()
    {
    }

    /// <summary>
    /// Creates the canonical dimensionless dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Dimensionless()
    {
        return DimensionVector.Dimensionless();
    }

    /// <summary>
    /// Creates the canonical length dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Length()
    {
        return DimensionVector.Length();
    }

    /// <summary>
    /// Creates the canonical mass dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Mass()
    {
        return DimensionVector.Mass();
    }

    /// <summary>
    /// Creates the canonical time dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Time()
    {
        return DimensionVector.Time();
    }

    /// <summary>
    /// Creates the canonical electric current dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector ElectricCurrent()
    {
        return DimensionVector.ElectricCurrent();
    }

    /// <summary>
    /// Creates the canonical thermodynamic temperature dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector ThermodynamicTemperature()
    {
        return DimensionVector.ThermodynamicTemperature();
    }

    /// <summary>
    /// Creates the canonical amount of substance dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector AmountOfSubstance()
    {
        return DimensionVector.AmountOfSubstance();
    }

    /// <summary>
    /// Creates the canonical luminous intensity dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector LuminousIntensity()
    {
        return DimensionVector.LuminousIntensity();
    }

    /// <summary>
    /// Creates the canonical plane angle dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector PlaneAngle()
    {
        return DimensionVector.PlaneAngle();
    }

    /// <summary>
    /// Creates the canonical solid angle dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector SolidAngle()
    {
        return DimensionVector.SolidAngle();
    }

    /// <summary>
    /// Creates the canonical information dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Information()
    {
        return DimensionVector.Information();
    }

    /// <summary>
    /// Creates the canonical currency dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Currency()
    {
        return DimensionVector.Currency();
    }

    /// <summary>
    /// Creates the canonical count dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Count()
    {
        return DimensionVector.Count();
    }
}
