using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardQuantityKindsTests
{
    [Test]
    public void CreateCoreQuantityKinds_ReturnsCoreKindsInStableOrder()
    {
        QuantityKindDefinition[] quantityKinds = new StandardQuantityKinds().CreateCoreQuantityKinds();

        Assert.That(quantityKinds.Length, Is.EqualTo(30));
        AssertKind(quantityKinds[0], "length", "Length", new StandardDimensions().Length(), "meter");
        AssertKind(quantityKinds[1], "mass", "Mass", new StandardDimensions().Mass(), "kilogram");
        AssertKind(quantityKinds[2], "time", "Time", new StandardDimensions().Time(), "second");
        AssertKind(quantityKinds[3], "area", "Area", new StandardDimensions().Length().Pow(2), "(meter^2)");
        AssertKind(quantityKinds[4], "volume", "Volume", new StandardDimensions().Length().Pow(3), "(meter^3)");
        AssertKind(quantityKinds[5], "speed", "Speed", new StandardDimensions().Length().Divide(new StandardDimensions().Time()), "(meter/second)");
        AssertKind(quantityKinds[6], "acceleration", "Acceleration", new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2)), "(meter/(second^2))");
        AssertKind(quantityKinds[7], "force", "Force", new StandardUnits().Newton().GetDimension(), "newton");
        AssertKind(quantityKinds[8], "pressure", "Pressure", new StandardUnits().Pascal().GetDimension(), "pascal");
        AssertKind(quantityKinds[9], "energy", "Energy", new StandardUnits().Joule().GetDimension(), "joule");
        AssertKind(quantityKinds[10], "power", "Power", new StandardUnits().Watt().GetDimension(), "watt");
        AssertKind(quantityKinds[11], "frequency", "Frequency", new StandardUnits().Hertz().GetDimension(), "hertz");
        AssertKind(quantityKinds[12], "angle", "Angle", new StandardDimensions().PlaneAngle(), "radian");
        AssertKind(quantityKinds[13], "angular_speed", "Angular speed", new StandardDimensions().PlaneAngle().Divide(new StandardDimensions().Time()), "(radian/second)");
        AssertKind(quantityKinds[14], "temperature", "Temperature", new StandardDimensions().ThermodynamicTemperature(), "kelvin");
        AssertKind(quantityKinds[15], "temperature_interval", "Temperature interval", new StandardDimensions().ThermodynamicTemperature(), "kelvin_interval");
        AssertKind(quantityKinds[16], "electric_charge", "Electric charge", new StandardUnits().Coulomb().GetDimension(), "coulomb");
        AssertKind(quantityKinds[17], "current", "Current", new StandardDimensions().ElectricCurrent(), "ampere");
        AssertKind(quantityKinds[18], "voltage", "Voltage", new StandardUnits().Volt().GetDimension(), "volt");
        AssertKind(quantityKinds[19], "resistance", "Resistance", new StandardUnits().Ohm().GetDimension(), "ohm");
        AssertKind(quantityKinds[20], "capacitance", "Capacitance", new StandardUnits().Farad().GetDimension(), "farad");
        AssertKind(quantityKinds[21], "luminous_intensity", "Luminous intensity", new StandardDimensions().LuminousIntensity(), "candela");
        AssertKind(quantityKinds[22], "amount", "Amount", new StandardDimensions().AmountOfSubstance(), "mole");
        AssertKind(quantityKinds[23], "density", "Density", new StandardDimensions().Mass().Divide(new StandardDimensions().Length().Pow(3)), "(kilogram/(meter^3))");
        AssertKind(quantityKinds[24], "flow_rate", "Flow rate", new StandardDimensions().Length().Pow(3).Divide(new StandardDimensions().Time()), "((meter^3)/second)");
        AssertKind(quantityKinds[25], "data_size", "Data size", new StandardDimensions().Information(), "byte");
        AssertKind(quantityKinds[26], "data_rate", "Data rate", new StandardDimensions().Information().Divide(new StandardDimensions().Time()), "(bit/second)");
        AssertKind(quantityKinds[27], "ratio", "Ratio", new StandardDimensions().Dimensionless(), "one");
        AssertKind(quantityKinds[28], "count", "Count", new StandardDimensions().Count(), "count");
        AssertKind(quantityKinds[29], "currency", "Currency", new StandardDimensions().Currency(), "currency_unit");
    }

    [Test]
    public void CoreQuantityKinds_IncludeAliasesAndAcceptedUnits()
    {
        QuantityKindDefinition length = new StandardQuantityKinds().Length();
        QuantityKindDefinition pressure = new StandardQuantityKinds().Pressure();
        QuantityKindDefinition dataSize = new StandardQuantityKinds().DataSize();

        Assert.That(length.MatchesText("distance"), Is.True);
        Assert.That(length.AcceptsUnitId("meter"), Is.True);
        Assert.That(length.AcceptsUnitId("foot"), Is.True);
        Assert.That(length.AcceptsUnit(new StandardUnits().Inch()), Is.True);
        Assert.That(pressure.MatchesText("stress"), Is.True);
        Assert.That(pressure.AcceptsUnitId("psi"), Is.True);
        Assert.That(dataSize.MatchesText("information size"), Is.True);
        Assert.That(dataSize.AcceptsUnitId("bit"), Is.True);
        Assert.That(dataSize.AcceptsUnit(new StandardUnits().Mebibyte()), Is.True);
    }

    [Test]
    public void CoreQuantityKinds_MatchCompleteStandardAcceptedUnitMappings()
    {
        QuantityKindDefinition[] quantityKinds = new StandardQuantityKinds().CreateCoreQuantityKinds();
        string[][] expectedAcceptedUnitIds = new string[][]
        {
            new string[] { "foot", "inch", "mile", "meter" },
            new string[] { "gram", "tonne", "pound", "ounce", "kilogram" },
            new string[] { "minute", "hour", "day", "second" },
            new string[] { "hectare", "acre", "(meter^2)" },
            new string[] { "liter", "us_gallon", "us_quart", "us_pint", "us_cup", "(meter^3)" },
            new string[] { "(meter/second)" },
            new string[] { "(meter/(second^2))" },
            new string[] { "newton" },
            new string[] { "psi", "pascal" },
            new string[] { "electronvolt", "british_thermal_unit", "joule" },
            new string[] { "horsepower", "watt" },
            new string[] { "hertz" },
            new string[] { "radian" },
            new string[] { "(radian/second)" },
            new string[] { "degree_celsius", "degree_fahrenheit", "rankine", "kelvin" },
            new string[] { "celsius_interval", "fahrenheit_interval", "rankine_interval", "kelvin_interval" },
            new string[] { "coulomb" },
            new string[] { "ampere" },
            new string[] { "volt" },
            new string[] { "ohm" },
            new string[] { "farad" },
            new string[] { "candela" },
            new string[] { "mole" },
            new string[] { "(kilogram/(meter^3))" },
            new string[] { "((meter^3)/second)" },
            new string[] { "bit", "kilobyte", "kibibyte", "megabyte", "mebibyte", "byte" },
            new string[] { "(bit/second)" },
            new string[] { "percent", "one" },
            new string[] { "count" },
            new string[] { "currency_unit" }
        };

        Assert.That(quantityKinds.Length, Is.EqualTo(expectedAcceptedUnitIds.Length));
        int index = 0;
        while (index < quantityKinds.Length)
        {
            Assert.That(
                quantityKinds[index].GetAcceptedUnitIds(),
                Is.EqualTo(expectedAcceptedUnitIds[index]),
                "accepted units for " + quantityKinds[index].GetId());
            index = index + 1;
        }
    }

    [Test]
    public void CoreQuantityKinds_MatchCompleteStandardAliasMappings()
    {
        QuantityKindDefinition[] quantityKinds = new StandardQuantityKinds().CreateCoreQuantityKinds();
        string[][] expectedAliases = new string[][]
        {
            new string[] { "distance", "width", "height" },
            new string[] { "weight" },
            new string[] { "duration" },
            new string[0],
            new string[] { "capacity" },
            new string[] { "velocity" },
            new string[0],
            new string[0],
            new string[] { "stress" },
            new string[] { "work" },
            new string[0],
            new string[0],
            new string[] { "plane angle" },
            new string[] { "angular velocity" },
            new string[] { "temperature point" },
            new string[] { "temperature difference" },
            new string[] { "charge" },
            new string[] { "electric current" },
            new string[] { "electric potential" },
            new string[] { "electrical resistance" },
            new string[0],
            new string[0],
            new string[] { "amount of substance" },
            new string[] { "mass density" },
            new string[] { "volume flow rate" },
            new string[] { "information size" },
            new string[] { "bit rate" },
            new string[] { "fraction" },
            new string[] { "quantity count" },
            new string[] { "money" }
        };

        Assert.That(quantityKinds.Length, Is.EqualTo(expectedAliases.Length));
        int index = 0;
        while (index < quantityKinds.Length)
        {
            Assert.That(
                quantityKinds[index].GetAliases(),
                Is.EqualTo(expectedAliases[index]),
                "aliases for " + quantityKinds[index].GetId());
            index = index + 1;
        }
    }

    [Test]
    public void CoreQuantityKinds_DistinguishSameDimensionQuantityKinds()
    {
        QuantityKindDefinition temperature = new StandardQuantityKinds().Temperature();
        QuantityKindDefinition temperatureInterval = new StandardQuantityKinds().TemperatureInterval();
        QuantityKindDefinition angle = new StandardQuantityKinds().Angle();
        QuantityKindDefinition angularSpeed = new StandardQuantityKinds().AngularSpeed();

        Assert.That(temperature.GetDimension().EqualsDimension(temperatureInterval.GetDimension()), Is.True);
        Assert.That(temperature.GetId(), Is.Not.EqualTo(temperatureInterval.GetId()));
        Assert.That(temperature.GetDefaultUnitId(), Is.EqualTo("kelvin"));
        Assert.That(temperatureInterval.GetDefaultUnitId(), Is.EqualTo("kelvin_interval"));
        Assert.That(angle.GetDimension().EqualsDimension(angularSpeed.GetDimension()), Is.False);
        Assert.That(angle.GetDefaultUnitId(), Is.EqualTo("radian"));
        Assert.That(angularSpeed.GetDefaultUnitId(), Is.EqualTo("(radian/second)"));
    }

    [Test]
    public void CoreQuantityKindFactories_ReturnIndependentEquivalentDefinitions()
    {
        QuantityKindDefinition first = new StandardQuantityKinds().Length();
        QuantityKindDefinition second = new StandardQuantityKinds().Length();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.GetId(), Is.EqualTo(second.GetId()));
        Assert.That(first.GetDimension().EqualsDimension(second.GetDimension()), Is.True);
        Assert.That(first.GetAcceptedUnitIds(), Is.EqualTo(second.GetAcceptedUnitIds()));
    }

    [Test]
    public void RegisterCoreQuantityKinds_AddsDefinitionsToRegistry()
    {
        UnitRegistry registry = CreateRegistryWithStandardUnits();

        OperationResult result = new StandardQuantityKinds().RegisterCoreQuantityKinds(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetQuantityKinds().Length, Is.EqualTo(30));
        AssertRegisteredKind(registry.GetQuantityKinds()[0], "length", "meter");
        AssertRegisteredKind(registry.GetQuantityKinds()[14], "temperature", "kelvin");
        AssertRegisteredKind(registry.GetQuantityKinds()[15], "temperature_interval", "kelvin_interval");
        AssertRegisteredKind(registry.GetQuantityKinds()[25], "data_size", "byte");
    }

    [Test]
    public void RegisterCoreQuantityKinds_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardQuantityKinds().RegisterCoreQuantityKinds(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[0]));
    }

    [Test]
    public void RegisterCoreQuantityKinds_RejectsMissingRegistry()
    {
        Assert.Throws<System.ArgumentNullException>(() => new StandardQuantityKinds().RegisterCoreQuantityKinds(null!));
    }

    static void AssertKind(QuantityKindDefinition quantityKind, string id, string name, DimensionVector dimension, string defaultUnitId)
    {
        Assert.That(quantityKind.GetId(), Is.EqualTo(id));
        Assert.That(quantityKind.GetName(), Is.EqualTo(name));
        Assert.That(quantityKind.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(quantityKind.GetDefaultUnitId(), Is.EqualTo(defaultUnitId));
        Assert.That(quantityKind.AcceptsUnitId(defaultUnitId), Is.True);
    }

    static void AssertRegisteredKind(QuantityKindDefinition quantityKind, string id, string defaultUnitId)
    {
        Assert.That(quantityKind.GetId(), Is.EqualTo(id));
        Assert.That(quantityKind.GetDefaultUnitId(), Is.EqualTo(defaultUnitId));
    }

    static UnitRegistry CreateRegistryWithStandardUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        StandardUnits units = new StandardUnits();
        units.RegisterSiBaseUnits(registry);
        units.RegisterSiDerivedUnits(registry);
        units.RegisterMetricUnits(registry);
        units.RegisterTemperatureUnits(registry);
        units.RegisterDimensionlessUnits(registry);
        units.RegisterCgsUnits(registry);
        units.RegisterImperialAndUsCustomaryUnits(registry);
        units.RegisterDataUnits(registry);
        units.RegisterCompoundUnits(registry);
        return registry;
    }
}
