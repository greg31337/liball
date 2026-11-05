namespace units;

/// <summary>
/// Provides and registers the standard named unit systems.
/// </summary>
public class StandardUnitSystems : IStandardUnitSystems
{
    /// <summary>
    /// Initializes a standard unit-system catalog.
    /// </summary>
    public StandardUnitSystems()
    {
    }

    /// <summary>
    /// Creates the standard SI unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Si()
    {
        return new UnitSystemDefinition(
            "si",
            "SI",
            new string[] { "meter", "kilogram", "second", "ampere", "kelvin", "mole", "candela" },
            CoreQuantityKindIds(),
            new string[]
            {
                "meter",
                "kilogram",
                "second",
                "(meter^2)",
                "(meter^3)",
                "(meter/second)",
                "(meter/(second^2))",
                "newton",
                "pascal",
                "joule",
                "watt",
                "hertz",
                "radian",
                "(radian/second)",
                "kelvin",
                "kelvin_interval",
                "coulomb",
                "ampere",
                "volt",
                "ohm",
                "farad",
                "candela",
                "mole",
                "(kilogram/(meter^3))",
                "((meter^3)/second)",
                "byte",
                "(bit/second)",
                "one",
                "count",
                "currency_unit"
            });
    }

    /// <summary>
    /// Creates the standard cgs unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Cgs()
    {
        return new UnitSystemDefinition(
            "cgs",
            "CGS",
            new string[] { "centimeter", "gram", "second" },
            CoreQuantityKindIds(),
            new string[]
            {
                "centimeter",
                "gram",
                "second",
                "(centimeter^2)",
                "(centimeter^3)",
                "(centimeter/second)",
                "(centimeter/(second^2))",
                "dyne",
                "barye",
                "erg",
                "(erg/second)",
                "hertz",
                "radian",
                "(radian/second)",
                "kelvin",
                "kelvin_interval",
                "statcoulomb",
                "statampere",
                "statvolt",
                "statohm",
                "statfarad",
                "candela",
                "mole",
                "(gram/(centimeter^3))",
                "((centimeter^3)/second)",
                "byte",
                "(bit/second)",
                "one",
                "count",
                "currency_unit"
            });
    }

    /// <summary>
    /// Creates the standard imperial unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition Imperial()
    {
        return new UnitSystemDefinition(
            "imperial",
            "Imperial",
            new string[] { "foot", "pound", "second", "rankine" },
            CoreQuantityKindIds(),
            new string[]
            {
                "foot",
                "pound",
                "second",
                "(foot^2)",
                "(foot^3)",
                "(foot/second)",
                "(foot/(second^2))",
                "pound_force",
                "psi",
                "british_thermal_unit",
                "horsepower",
                "hertz",
                "radian",
                "(radian/second)",
                "rankine",
                "rankine_interval",
                "coulomb",
                "ampere",
                "volt",
                "ohm",
                "farad",
                "candela",
                "mole",
                "(pound/(foot^3))",
                "((foot^3)/second)",
                "byte",
                "(bit/second)",
                "one",
                "count",
                "currency_unit"
            });
    }

    /// <summary>
    /// Creates the standard US customary unit-system definition.
    /// </summary>
    /// <returns>The resulting unit system definition.</returns>
    public UnitSystemDefinition UsCustomary()
    {
        return new UnitSystemDefinition(
            "us_customary",
            "US customary",
            new string[] { "foot", "pound", "second", "rankine" },
            CoreQuantityKindIds(),
            new string[]
            {
                "foot",
                "pound",
                "second",
                "acre",
                "us_gallon",
                "(mile/hour)",
                "(foot/(second^2))",
                "pound_force",
                "psi",
                "british_thermal_unit",
                "horsepower",
                "hertz",
                "radian",
                "(radian/second)",
                "rankine",
                "rankine_interval",
                "coulomb",
                "ampere",
                "volt",
                "ohm",
                "farad",
                "candela",
                "mole",
                "(pound/(foot^3))",
                "(us_gallon/minute)",
                "byte",
                "(bit/second)",
                "one",
                "count",
                "currency_unit"
            });
    }

    /// <summary>
    /// Creates the collection of standard unit systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] CreateStandardUnitSystems()
    {
        return new UnitSystemDefinition[]
        {
            Si(),
            Cgs(),
            Imperial(),
            UsCustomary()
        };
    }

    /// <summary>
    /// Registers standard unit systems in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterStandardUnitSystems(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        UnitSystemDefinition[] systems = CreateStandardUnitSystems();
        int index = 0;

        while (index < systems.Length)
        {
            OperationResult result = registry.AddSystem(systems[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal static string[] CoreQuantityKindIds()
    {
        return new string[]
        {
            "length",
            "mass",
            "time",
            "area",
            "volume",
            "speed",
            "acceleration",
            "force",
            "pressure",
            "energy",
            "power",
            "frequency",
            "angle",
            "angular_speed",
            "temperature",
            "temperature_interval",
            "electric_charge",
            "current",
            "voltage",
            "resistance",
            "capacitance",
            "luminous_intensity",
            "amount",
            "density",
            "flow_rate",
            "data_size",
            "data_rate",
            "ratio",
            "count",
            "currency"
        };
    }
}
