namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityValue"/>.
/// </summary>
public interface IQuantityValue
{
    /// <summary>
    /// Gets the numeric value expressed in the stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetValue();
    /// <summary>
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetUnit();
    /// <summary>
    /// Converts this quantity to the target unit and returns a nonthrowing result.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertTo(UnitDefinition unit);
    /// <summary>
    /// Converts this quantity to the target unit, throwing if conversion is invalid.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The target unit has an incompatible dimension or conversion kind.</exception>
    public QuantityValue ToUnit(UnitDefinition unit);
    /// <summary>
    /// Returns this quantity's numeric value expressed in the target unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.InvalidOperationException">The target unit has an incompatible dimension or conversion kind.</exception>
    public double As(UnitDefinition unit);
    /// <summary>
    /// Reports whether this quantity has the same dimension as the supplied value or unit.
    /// </summary>
    /// <param name="value">The quantity to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWith(QuantityValue? value);
    /// <summary>
    /// Reports whether this quantity has the same dimension as the supplied value or unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWithUnit(UnitDefinition? unit);
    /// <summary>
    /// Reports whether every dimension exponent is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDimensionless();
    /// <summary>
    /// Creates a typed length facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length ToLength();
    /// <summary>
    /// Creates a typed mass facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass ToMass();
    /// <summary>
    /// Creates a typed duration facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration ToDuration();
    /// <summary>
    /// Creates a typed area facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area ToArea();
    /// <summary>
    /// Creates a typed volume facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume ToVolume();
    /// <summary>
    /// Creates a typed speed facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed ToSpeed();
    /// <summary>
    /// Creates a typed acceleration facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration ToAcceleration();
    /// <summary>
    /// Creates a typed force facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force ToForce();
    /// <summary>
    /// Creates a typed pressure facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure ToPressure();
    /// <summary>
    /// Creates a typed energy facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy ToEnergy();
    /// <summary>
    /// Creates a typed power facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power ToPower();
    /// <summary>
    /// Creates a typed frequency facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency ToFrequency();
    /// <summary>
    /// Creates a typed angle facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting angle.</returns>
    public Angle ToAngle();
    /// <summary>
    /// Creates a typed temperature-point facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting temperature.</returns>
    public Temperature ToTemperature();
    /// <summary>
    /// Creates a typed temperature-interval facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval ToTemperatureInterval();
    /// <summary>
    /// Creates a typed data-size facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize ToDataSize();
    /// <summary>
    /// Creates a typed data-rate facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate ToDataRate();
}
