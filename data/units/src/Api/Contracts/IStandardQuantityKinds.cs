namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardQuantityKinds"/>.
/// </summary>
public interface IStandardQuantityKinds
{
    /// <summary>
    /// Creates the standard length quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Length();
    /// <summary>
    /// Creates the standard mass quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Mass();
    /// <summary>
    /// Creates the standard time quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Time();
    /// <summary>
    /// Creates the standard area quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Area();
    /// <summary>
    /// Creates the standard volume quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Volume();
    /// <summary>
    /// Creates the standard speed quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Speed();
    /// <summary>
    /// Creates the standard acceleration quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Acceleration();
    /// <summary>
    /// Creates the standard force quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Force();
    /// <summary>
    /// Creates the standard pressure quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Pressure();
    /// <summary>
    /// Creates the standard energy quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Energy();
    /// <summary>
    /// Creates the standard power quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Power();
    /// <summary>
    /// Creates the standard frequency quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Frequency();
    /// <summary>
    /// Creates the standard angle quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Angle();
    /// <summary>
    /// Creates the standard angular speed quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition AngularSpeed();
    /// <summary>
    /// Creates the standard temperature quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Temperature();
    /// <summary>
    /// Creates the standard temperature interval quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition TemperatureInterval();
    /// <summary>
    /// Creates the standard electric charge quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition ElectricCharge();
    /// <summary>
    /// Creates the standard current quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Current();
    /// <summary>
    /// Creates the standard voltage quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Voltage();
    /// <summary>
    /// Creates the standard resistance quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Resistance();
    /// <summary>
    /// Creates the standard capacitance quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Capacitance();
    /// <summary>
    /// Creates the standard luminous intensity quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition LuminousIntensity();
    /// <summary>
    /// Creates the standard amount quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Amount();
    /// <summary>
    /// Creates the standard density quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Density();
    /// <summary>
    /// Creates the standard flow rate quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition FlowRate();
    /// <summary>
    /// Creates the standard data size quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition DataSize();
    /// <summary>
    /// Creates the standard data rate quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition DataRate();
    /// <summary>
    /// Creates the standard ratio quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Ratio();
    /// <summary>
    /// Creates the standard count quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Count();
    /// <summary>
    /// Creates the standard currency quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Currency();
    /// <summary>
    /// Creates the collection of core quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] CreateCoreQuantityKinds();
    /// <summary>
    /// Registers core quantity kinds in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCoreQuantityKinds(UnitRegistry registry);
}
