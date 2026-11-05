namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="PhysicalConstants"/>.
/// </summary>
public interface IPhysicalConstants
{
    /// <summary>
    /// Creates the caesium-133 hyperfine transition frequency definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition Caesium133HyperfineTransitionFrequency();
    /// <summary>
    /// Creates the speed of light physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition SpeedOfLight();
    /// <summary>
    /// Creates the planck constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition PlanckConstant();
    /// <summary>
    /// Creates the elementary charge physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition ElementaryCharge();
    /// <summary>
    /// Creates the Boltzmann constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition BoltzmannConstant();
    /// <summary>
    /// Creates the avogadro constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition AvogadroConstant();
    /// <summary>
    /// Creates the luminous efficacy physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition LuminousEfficacy();
    /// <summary>
    /// Creates the standard gravity physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition StandardGravity();
    /// <summary>
    /// Creates the gravitational constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition GravitationalConstant();
    /// <summary>
    /// Creates the collection of fundamental constants.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PhysicalConstantDefinition[] CreateFundamentalConstants();
    /// <summary>
    /// Registers fundamental constants in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterFundamentalConstants(UnitRegistry registry);
}
