namespace units;

/// <summary>
/// Provides and registers commonly used physical constants.
/// </summary>
public class PhysicalConstants : IPhysicalConstants
{
    /// <summary>
    /// Initializes a physical-constant catalog.
    /// </summary>
    public PhysicalConstants()
    {
    }

    /// <summary>
    /// Creates the caesium-133 hyperfine transition frequency definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition Caesium133HyperfineTransitionFrequency()
    {
        return Constant(
            "caesium_133_hyperfine_transition_frequency",
            "hyperfine transition frequency of caesium-133",
            "Δν_Cs",
            "Defining caesium-133 ground-state hyperfine transition frequency.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(9192631770.0d, new StandardUnits().Hertz()),
            new string[] { "cesium-133 hyperfine transition frequency", "Cs-133 hyperfine transition frequency" });
    }

    /// <summary>
    /// Creates the speed of light physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition SpeedOfLight()
    {
        return Constant(
            "speed_of_light",
            "speed of light in vacuum",
            "c",
            "Defined speed of light in vacuum.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(299792458.0d, new StandardUnits().Meter().Divide(new StandardUnits().Second())),
            new string[] { "light speed" });
    }

    /// <summary>
    /// Creates the planck constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition PlanckConstant()
    {
        return Constant(
            "planck_constant",
            "Planck constant",
            "h",
            "Defined Planck constant.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(6.62607015E-34d, new StandardUnits().Joule().Multiply(new StandardUnits().Second())),
            new string[0]);
    }

    /// <summary>
    /// Creates the elementary charge physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition ElementaryCharge()
    {
        return Constant(
            "elementary_charge",
            "elementary charge",
            "e",
            "Defined elementary charge.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(1.602176634E-19d, new StandardUnits().Coulomb()),
            new string[0]);
    }

    /// <summary>
    /// Creates the Boltzmann constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition BoltzmannConstant()
    {
        return Constant(
            "boltzmann_constant",
            "Boltzmann constant",
            "k",
            "Defined Boltzmann constant.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(1.380649E-23d, new StandardUnits().Joule().Divide(new StandardUnits().KelvinInterval())),
            new string[0]);
    }

    /// <summary>
    /// Creates the avogadro constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition AvogadroConstant()
    {
        return Constant(
            "avogadro_constant",
            "Avogadro constant",
            "N_A",
            "Defined Avogadro constant.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(6.02214076E23d, new StandardUnits().Mole().Inverse()),
            new string[] { "avogadro" });
    }

    /// <summary>
    /// Creates the luminous efficacy physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition LuminousEfficacy()
    {
        return Constant(
            "luminous_efficacy",
            "luminous efficacy of monochromatic radiation",
            "K_cd",
            "Defined luminous efficacy for monochromatic radiation of frequency 540 terahertz.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(683.0d, new StandardUnits().Lumen().Divide(new StandardUnits().Watt())),
            new string[] { "luminous efficacy" });
    }

    /// <summary>
    /// Creates the standard gravity physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition StandardGravity()
    {
        return Constant(
            "standard_gravity",
            "standard gravity",
            "g0",
            "Conventional standard acceleration due to gravity.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            new QuantityValue(9.80665d, new StandardUnits().Meter().Divide(new StandardUnits().Second().Pow(2))),
            new string[0]);
    }

    /// <summary>
    /// Creates the gravitational constant physical constant definition.
    /// </summary>
    /// <returns>The resulting physical constant definition.</returns>
    public PhysicalConstantDefinition GravitationalConstant()
    {
        return Constant(
            "gravitational_constant",
            "Newtonian constant of gravitation",
            "G",
            "Measured Newtonian gravitational constant.",
            "measured",
            "NIST Fundamental Physical Constants",
            "2022 CODATA adjustment",
            1.5E-15d,
            new QuantityValue(6.67430E-11d, new StandardUnits().Meter().Pow(3).Divide(new StandardUnits().Kilogram()).Divide(new StandardUnits().Second().Pow(2))),
            new string[] { "big_g" });
    }

    /// <summary>
    /// Creates the collection of fundamental constants.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PhysicalConstantDefinition[] CreateFundamentalConstants()
    {
        return new PhysicalConstantDefinition[]
        {
            Caesium133HyperfineTransitionFrequency(),
            SpeedOfLight(),
            PlanckConstant(),
            ElementaryCharge(),
            BoltzmannConstant(),
            AvogadroConstant(),
            LuminousEfficacy(),
            StandardGravity(),
            GravitationalConstant()
        };
    }

    /// <summary>
    /// Registers fundamental constants in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterFundamentalConstants(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        PhysicalConstantDefinition[] constants = CreateFundamentalConstants();
        int index = 0;

        while (index < constants.Length)
        {
            OperationResult result = registry.AddConstant(constants[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal static PhysicalConstantDefinition Constant(string id, string name, string symbol, string description, string exactnessLabel, string sourceLabel, string adjustmentOrEdition, double standardUncertainty, QuantityValue quantity, string[] aliases)
    {
        return new PhysicalConstantDefinition(id, name, symbol, description, exactnessLabel, sourceLabel, adjustmentOrEdition, standardUncertainty, quantity, aliases);
    }
}
