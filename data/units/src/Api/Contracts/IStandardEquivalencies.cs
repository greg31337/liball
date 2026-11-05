namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardEquivalencies"/>.
/// </summary>
public interface IStandardEquivalencies
{
    /// <summary>
    /// Creates the standard wavelength frequency equivalency definition.
    /// </summary>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition WavelengthFrequency();
    /// <summary>
    /// Creates the standard mass energy equivalency definition.
    /// </summary>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition MassEnergy();
    /// <summary>
    /// Creates the collection of standard equivalencies.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public EquivalencyDefinition[] CreateStandardEquivalencies();
    /// <summary>
    /// Registers standard equivalencies in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterStandardEquivalencies(UnitRegistry registry);
}
