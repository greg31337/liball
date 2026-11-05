namespace units;

/// <summary>
/// Provides and registers the standard quantity-kind metadata.
/// </summary>
public class StandardQuantityKinds : IStandardQuantityKinds
{
    /// <summary>
    /// Initializes a standard quantity-kind catalog.
    /// </summary>
    public StandardQuantityKinds()
    {
    }

    /// <summary>
    /// Creates the standard length quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Length()
    {
        return Kind("length", "Length", new StandardDimensions().Length(), "meter", new string[] { "distance", "width", "height" }, new string[] { "foot", "inch", "mile" });
    }

    /// <summary>
    /// Creates the standard mass quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Mass()
    {
        return Kind("mass", "Mass", new StandardDimensions().Mass(), "kilogram", new string[] { "weight" }, new string[] { "gram", "tonne", "pound", "ounce" });
    }

    /// <summary>
    /// Creates the standard time quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Time()
    {
        return Kind("time", "Time", new StandardDimensions().Time(), "second", new string[] { "duration" }, new string[] { "minute", "hour", "day" });
    }

    /// <summary>
    /// Creates the standard area quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Area()
    {
        return Kind("area", "Area", new StandardDimensions().Length().Pow(2), "(meter^2)", new string[0], new string[] { "hectare", "acre" });
    }

    /// <summary>
    /// Creates the standard volume quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Volume()
    {
        return Kind("volume", "Volume", new StandardDimensions().Length().Pow(3), "(meter^3)", new string[] { "capacity" }, new string[] { "liter", "us_gallon", "us_quart", "us_pint", "us_cup" });
    }

    /// <summary>
    /// Creates the standard speed quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Speed()
    {
        return Kind("speed", "Speed", new StandardDimensions().Length().Divide(new StandardDimensions().Time()), "(meter/second)", new string[] { "velocity" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard acceleration quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Acceleration()
    {
        return Kind("acceleration", "Acceleration", new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2)), "(meter/(second^2))", new string[0], new string[0]);
    }

    /// <summary>
    /// Creates the standard force quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Force()
    {
        return Kind("force", "Force", new StandardUnits().Newton().GetDimension(), "newton", new string[0], new string[0]);
    }

    /// <summary>
    /// Creates the standard pressure quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Pressure()
    {
        return Kind("pressure", "Pressure", new StandardUnits().Pascal().GetDimension(), "pascal", new string[] { "stress" }, new string[] { "psi" });
    }

    /// <summary>
    /// Creates the standard energy quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Energy()
    {
        return Kind("energy", "Energy", new StandardUnits().Joule().GetDimension(), "joule", new string[] { "work" }, new string[] { "electronvolt", "british_thermal_unit" });
    }

    /// <summary>
    /// Creates the standard power quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Power()
    {
        return Kind("power", "Power", new StandardUnits().Watt().GetDimension(), "watt", new string[0], new string[] { "horsepower" });
    }

    /// <summary>
    /// Creates the standard frequency quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Frequency()
    {
        return Kind("frequency", "Frequency", new StandardUnits().Hertz().GetDimension(), "hertz", new string[0], new string[0]);
    }

    /// <summary>
    /// Creates the standard angle quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Angle()
    {
        return Kind("angle", "Angle", new StandardDimensions().PlaneAngle(), "radian", new string[] { "plane angle" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard angular speed quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition AngularSpeed()
    {
        return Kind("angular_speed", "Angular speed", new StandardDimensions().PlaneAngle().Divide(new StandardDimensions().Time()), "(radian/second)", new string[] { "angular velocity" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard temperature quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Temperature()
    {
        return Kind("temperature", "Temperature", new StandardDimensions().ThermodynamicTemperature(), "kelvin", new string[] { "temperature point" }, new string[] { "degree_celsius", "degree_fahrenheit", "rankine" });
    }

    /// <summary>
    /// Creates the standard temperature interval quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition TemperatureInterval()
    {
        return Kind("temperature_interval", "Temperature interval", new StandardDimensions().ThermodynamicTemperature(), "kelvin_interval", new string[] { "temperature difference" }, new string[] { "celsius_interval", "fahrenheit_interval", "rankine_interval" });
    }

    /// <summary>
    /// Creates the standard electric charge quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition ElectricCharge()
    {
        return Kind("electric_charge", "Electric charge", new StandardUnits().Coulomb().GetDimension(), "coulomb", new string[] { "charge" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard current quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Current()
    {
        return Kind("current", "Current", new StandardDimensions().ElectricCurrent(), "ampere", new string[] { "electric current" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard voltage quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Voltage()
    {
        return Kind("voltage", "Voltage", new StandardUnits().Volt().GetDimension(), "volt", new string[] { "electric potential" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard resistance quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Resistance()
    {
        return Kind("resistance", "Resistance", new StandardUnits().Ohm().GetDimension(), "ohm", new string[] { "electrical resistance" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard capacitance quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Capacitance()
    {
        return Kind("capacitance", "Capacitance", new StandardUnits().Farad().GetDimension(), "farad", new string[0], new string[0]);
    }

    /// <summary>
    /// Creates the standard luminous intensity quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition LuminousIntensity()
    {
        return Kind("luminous_intensity", "Luminous intensity", new StandardDimensions().LuminousIntensity(), "candela", new string[0], new string[0]);
    }

    /// <summary>
    /// Creates the standard amount quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Amount()
    {
        return Kind("amount", "Amount", new StandardDimensions().AmountOfSubstance(), "mole", new string[] { "amount of substance" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard density quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Density()
    {
        return Kind("density", "Density", new StandardDimensions().Mass().Divide(new StandardDimensions().Length().Pow(3)), "(kilogram/(meter^3))", new string[] { "mass density" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard flow rate quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition FlowRate()
    {
        return Kind("flow_rate", "Flow rate", new StandardDimensions().Length().Pow(3).Divide(new StandardDimensions().Time()), "((meter^3)/second)", new string[] { "volume flow rate" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard data size quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition DataSize()
    {
        return Kind("data_size", "Data size", new StandardDimensions().Information(), "byte", new string[] { "information size" }, new string[] { "bit", "kilobyte", "kibibyte", "megabyte", "mebibyte" });
    }

    /// <summary>
    /// Creates the standard data rate quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition DataRate()
    {
        return Kind("data_rate", "Data rate", new StandardDimensions().Information().Divide(new StandardDimensions().Time()), "(bit/second)", new string[] { "bit rate" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard ratio quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Ratio()
    {
        return Kind("ratio", "Ratio", new StandardDimensions().Dimensionless(), "one", new string[] { "fraction" }, new string[] { "percent" });
    }

    /// <summary>
    /// Creates the standard count quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Count()
    {
        return Kind("count", "Count", new StandardDimensions().Count(), "count", new string[] { "quantity count" }, new string[0]);
    }

    /// <summary>
    /// Creates the standard currency quantity-kind definition.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition Currency()
    {
        return Kind("currency", "Currency", new StandardDimensions().Currency(), "currency_unit", new string[] { "money" }, new string[0]);
    }

    /// <summary>
    /// Creates the collection of core quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] CreateCoreQuantityKinds()
    {
        return new QuantityKindDefinition[]
        {
            Length(),
            Mass(),
            Time(),
            Area(),
            Volume(),
            Speed(),
            Acceleration(),
            Force(),
            Pressure(),
            Energy(),
            Power(),
            Frequency(),
            Angle(),
            AngularSpeed(),
            Temperature(),
            TemperatureInterval(),
            ElectricCharge(),
            Current(),
            Voltage(),
            Resistance(),
            Capacitance(),
            LuminousIntensity(),
            Amount(),
            Density(),
            FlowRate(),
            DataSize(),
            DataRate(),
            Ratio(),
            Count(),
            Currency()
        };
    }

    /// <summary>
    /// Registers core quantity kinds in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult RegisterCoreQuantityKinds(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        QuantityKindDefinition[] quantityKinds = CreateCoreQuantityKinds();
        int index = 0;

        while (index < quantityKinds.Length)
        {
            OperationResult result = registry.AddQuantityKind(quantityKinds[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }

    internal static QuantityKindDefinition Kind(string id, string name, DimensionVector dimension, string defaultUnitId, string[] aliases, string[] acceptedUnitIds)
    {
        return new QuantityKindDefinition(id, name, dimension, defaultUnitId, aliases, acceptedUnitIds);
    }
}
