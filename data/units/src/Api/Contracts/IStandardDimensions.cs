namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardDimensions"/>.
/// </summary>
public interface IStandardDimensions
{
    /// <summary>
    /// Creates the canonical dimensionless dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Dimensionless();
    /// <summary>
    /// Creates the canonical length dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Length();
    /// <summary>
    /// Creates the canonical mass dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Mass();
    /// <summary>
    /// Creates the canonical time dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Time();
    /// <summary>
    /// Creates the canonical electric current dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector ElectricCurrent();
    /// <summary>
    /// Creates the canonical thermodynamic temperature dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector ThermodynamicTemperature();
    /// <summary>
    /// Creates the canonical amount of substance dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector AmountOfSubstance();
    /// <summary>
    /// Creates the canonical luminous intensity dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector LuminousIntensity();
    /// <summary>
    /// Creates the canonical plane angle dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector PlaneAngle();
    /// <summary>
    /// Creates the canonical solid angle dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector SolidAngle();
    /// <summary>
    /// Creates the canonical information dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Information();
    /// <summary>
    /// Creates the canonical currency dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Currency();
    /// <summary>
    /// Creates the canonical count dimension vector.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Count();
}
