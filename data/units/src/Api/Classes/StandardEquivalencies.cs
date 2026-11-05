namespace units;

/// <summary>
/// Provides and registers the library's opt-in cross-dimension equivalencies.
/// </summary>
public class StandardEquivalencies : IStandardEquivalencies
{
    /// <summary>
    /// Initializes a standard-equivalency catalog.
    /// </summary>
    public StandardEquivalencies()
    {
    }

    /// <summary>
    /// Creates the standard wavelength frequency equivalency definition.
    /// </summary>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition WavelengthFrequency()
    {
        RationalNumber speedOfLight = SpeedOfLightScale();
        return new EquivalencyDefinition(
            "wavelength_frequency",
            "Wavelength-frequency equivalency",
            "Frequency equals the speed of light divided by wavelength.",
            "length",
            new StandardDimensions().Length(),
            "frequency",
            new StandardUnits().Hertz().GetDimension(),
            speedOfLight,
            -1,
            speedOfLight,
            -1,
            new string[] { "photon", "light" });
    }

    /// <summary>
    /// Creates the standard mass energy equivalency definition.
    /// </summary>
    /// <returns>The resulting equivalency definition.</returns>
    public EquivalencyDefinition MassEnergy()
    {
        RationalNumber speedOfLightSquared = SpeedOfLightScale().Multiply(SpeedOfLightScale());
        return new EquivalencyDefinition(
            "mass_energy",
            "Mass-energy equivalency",
            "Energy equals mass times the speed of light squared.",
            "mass",
            new StandardDimensions().Mass(),
            "energy",
            new StandardUnits().Joule().GetDimension(),
            speedOfLightSquared,
            1,
            new RationalNumber(1, 1).Divide(speedOfLightSquared),
            1,
            new string[] { "einstein" });
    }

    /// <summary>
    /// Creates the collection of standard equivalencies.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public EquivalencyDefinition[] CreateStandardEquivalencies()
    {
        return new EquivalencyDefinition[]
        {
            WavelengthFrequency(),
            MassEnergy()
        };
    }

    /// <summary>
    /// Registers standard equivalencies in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterStandardEquivalencies(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        EquivalencyDefinition[] equivalencies = CreateStandardEquivalencies();
        int index = 0;

        while (index < equivalencies.Length)
        {
            OperationResult result = registry.AddEquivalency(equivalencies[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal static RationalNumber SpeedOfLightScale()
    {
        return new RationalNumber(299792458, 1);
    }
}
