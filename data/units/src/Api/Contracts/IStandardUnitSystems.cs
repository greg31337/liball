namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardUnitSystems"/>.
/// </summary>
public interface IStandardUnitSystems
{
    /// <summary>
    /// Creates the standard SI unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Si();
    /// <summary>
    /// Creates the standard cgs unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Cgs();
    /// <summary>
    /// Creates the standard imperial unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Imperial();
    /// <summary>
    /// Creates the standard US customary unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition UsCustomary();
    /// <summary>
    /// Creates the collection of standard unit systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] CreateStandardUnitSystems();
    /// <summary>
    /// Registers standard unit systems in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterStandardUnitSystems(UnitRegistry registry);
}
