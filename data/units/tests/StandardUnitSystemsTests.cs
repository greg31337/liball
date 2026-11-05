using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardUnitSystemsTests
{
    [Test]
    public void CreateStandardUnitSystems_ReturnsSystemsInStableOrder()
    {
        UnitSystemDefinition[] systems = new StandardUnitSystems().CreateStandardUnitSystems();

        Assert.That(systems.Length, Is.EqualTo(4));
        AssertSystem(systems[0], "si", "SI", new string[] { "meter", "kilogram", "second", "ampere", "kelvin", "mole", "candela" });
        AssertSystem(systems[1], "cgs", "CGS", new string[] { "centimeter", "gram", "second" });
        AssertSystem(systems[2], "imperial", "Imperial", new string[] { "foot", "pound", "second", "rankine" });
        AssertSystem(systems[3], "us_customary", "US customary", new string[] { "foot", "pound", "second", "rankine" });
    }

    [Test]
    public void Si_ReturnsCanonicalSiPreferredUnits()
    {
        UnitSystemDefinition system = new StandardUnitSystems().Si();

        Assert.That(system.GetPreferredQuantityKindIds().Length, Is.EqualTo(30));
        Assert.That(system.FindPreferredUnitId("length"), Is.EqualTo("meter"));
        Assert.That(system.FindPreferredUnitId("mass"), Is.EqualTo("kilogram"));
        Assert.That(system.FindPreferredUnitId("force"), Is.EqualTo("newton"));
        Assert.That(system.FindPreferredUnitId("pressure"), Is.EqualTo("pascal"));
        Assert.That(system.FindPreferredUnitId("temperature"), Is.EqualTo("kelvin"));
        Assert.That(system.FindPreferredUnitId("data_size"), Is.EqualTo("byte"));
    }

    [Test]
    public void Cgs_ReturnsCentimeterGramSecondPreferredUnits()
    {
        UnitSystemDefinition system = new StandardUnitSystems().Cgs();

        Assert.That(system.UsesBaseUnitId("centimeter"), Is.True);
        Assert.That(system.UsesBaseUnitId("gram"), Is.True);
        Assert.That(system.FindPreferredUnitId("length"), Is.EqualTo("centimeter"));
        Assert.That(system.FindPreferredUnitId("mass"), Is.EqualTo("gram"));
        Assert.That(system.FindPreferredUnitId("force"), Is.EqualTo("dyne"));
        Assert.That(system.FindPreferredUnitId("energy"), Is.EqualTo("erg"));
        Assert.That(system.FindPreferredUnitId("density"), Is.EqualTo("(gram/(centimeter^3))"));
    }

    [Test]
    public void ImperialAndUsCustomary_ReturnDistinctPreferredUnitsForVolumeAreaAndSpeed()
    {
        UnitSystemDefinition imperial = new StandardUnitSystems().Imperial();
        UnitSystemDefinition usCustomary = new StandardUnitSystems().UsCustomary();

        Assert.That(imperial.FindPreferredUnitId("area"), Is.EqualTo("(foot^2)"));
        Assert.That(imperial.FindPreferredUnitId("volume"), Is.EqualTo("(foot^3)"));
        Assert.That(imperial.FindPreferredUnitId("speed"), Is.EqualTo("(foot/second)"));
        Assert.That(usCustomary.FindPreferredUnitId("area"), Is.EqualTo("acre"));
        Assert.That(usCustomary.FindPreferredUnitId("volume"), Is.EqualTo("us_gallon"));
        Assert.That(usCustomary.FindPreferredUnitId("speed"), Is.EqualTo("(mile/hour)"));
        Assert.That(imperial.FindPreferredUnitId("pressure"), Is.EqualTo("psi"));
        Assert.That(usCustomary.FindPreferredUnitId("pressure"), Is.EqualTo("psi"));
    }

    [Test]
    public void StandardUnitSystems_ExposeCompletePreferredMappingsForEveryQuantityKind()
    {
        string[] expectedQuantityKindIds = new string[]
        {
            "length", "mass", "time", "area", "volume", "speed", "acceleration", "force", "pressure", "energy",
            "power", "frequency", "angle", "angular_speed", "temperature", "temperature_interval", "electric_charge",
            "current", "voltage", "resistance", "capacitance", "luminous_intensity", "amount", "density", "flow_rate",
            "data_size", "data_rate", "ratio", "count", "currency"
        };
        UnitSystemDefinition[] systems = new StandardUnitSystems().CreateStandardUnitSystems();
        string[][] expectedPreferredUnitIds = new string[][]
        {
            new string[]
            {
                "meter", "kilogram", "second", "(meter^2)", "(meter^3)", "(meter/second)", "(meter/(second^2))",
                "newton", "pascal", "joule", "watt", "hertz", "radian", "(radian/second)", "kelvin", "kelvin_interval",
                "coulomb", "ampere", "volt", "ohm", "farad", "candela", "mole", "(kilogram/(meter^3))",
                "((meter^3)/second)", "byte", "(bit/second)", "one", "count", "currency_unit"
            },
            new string[]
            {
                "centimeter", "gram", "second", "(centimeter^2)", "(centimeter^3)", "(centimeter/second)",
                "(centimeter/(second^2))", "dyne", "barye", "erg", "(erg/second)", "hertz", "radian",
                "(radian/second)", "kelvin", "kelvin_interval", "statcoulomb", "statampere", "statvolt", "statohm",
                "statfarad", "candela", "mole", "(gram/(centimeter^3))", "((centimeter^3)/second)", "byte",
                "(bit/second)", "one", "count", "currency_unit"
            },
            new string[]
            {
                "foot", "pound", "second", "(foot^2)", "(foot^3)", "(foot/second)", "(foot/(second^2))",
                "pound_force", "psi", "british_thermal_unit", "horsepower", "hertz", "radian", "(radian/second)",
                "rankine", "rankine_interval", "coulomb", "ampere", "volt", "ohm", "farad", "candela", "mole",
                "(pound/(foot^3))", "((foot^3)/second)", "byte", "(bit/second)", "one", "count", "currency_unit"
            },
            new string[]
            {
                "foot", "pound", "second", "acre", "us_gallon", "(mile/hour)", "(foot/(second^2))", "pound_force",
                "psi", "british_thermal_unit", "horsepower", "hertz", "radian", "(radian/second)", "rankine",
                "rankine_interval", "coulomb", "ampere", "volt", "ohm", "farad", "candela", "mole",
                "(pound/(foot^3))", "(us_gallon/minute)", "byte", "(bit/second)", "one", "count", "currency_unit"
            }
        };

        Assert.That(systems.Length, Is.EqualTo(expectedPreferredUnitIds.Length));
        int systemIndex = 0;
        while (systemIndex < systems.Length)
        {
            Assert.That(systems[systemIndex].GetPreferredQuantityKindIds(), Is.EqualTo(expectedQuantityKindIds));
            Assert.That(
                systems[systemIndex].GetPreferredUnitIds(),
                Is.EqualTo(expectedPreferredUnitIds[systemIndex]),
                "preferred units for " + systems[systemIndex].GetId());

            int kindIndex = 0;
            while (kindIndex < expectedQuantityKindIds.Length)
            {
                Assert.That(systems[systemIndex].HasPreferredUnitFor(expectedQuantityKindIds[kindIndex]), Is.True);
                Assert.That(
                    systems[systemIndex].FindPreferredUnitId(expectedQuantityKindIds[kindIndex]),
                    Is.EqualTo(expectedPreferredUnitIds[systemIndex][kindIndex]));
                kindIndex = kindIndex + 1;
            }

            systemIndex = systemIndex + 1;
        }
    }

    [Test]
    public void StandardUnitSystemFactories_ReturnIndependentEquivalentDefinitions()
    {
        UnitSystemDefinition first = new StandardUnitSystems().Si();
        UnitSystemDefinition second = new StandardUnitSystems().Si();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.GetId(), Is.EqualTo(second.GetId()));
        Assert.That(first.GetBaseUnitIds(), Is.EqualTo(second.GetBaseUnitIds()));
        Assert.That(first.GetPreferredUnitIds(), Is.EqualTo(second.GetPreferredUnitIds()));
    }

    [Test]
    public void RegisterStandardUnitSystems_AddsDefinitionsToRegistry()
    {
        UnitRegistry registry = CreateRegistryWithStandardMetadata();

        OperationResult result = new StandardUnitSystems().RegisterStandardUnitSystems(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetSystems().Length, Is.EqualTo(4));
        AssertRegisteredSystem(registry.GetSystems()[0], "si", "meter");
        AssertRegisteredSystem(registry.GetSystems()[1], "cgs", "centimeter");
        AssertRegisteredSystem(registry.GetSystems()[2], "imperial", "foot");
        AssertRegisteredSystem(registry.GetSystems()[3], "us_customary", "foot");
    }

    [Test]
    public void RegisterStandardUnitSystems_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnitSystems().RegisterStandardUnitSystems(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetSystems(), Is.EqualTo(new UnitSystemDefinition[0]));
    }

    [Test]
    public void RegisterStandardUnitSystems_RejectsMissingRegistry()
    {
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnitSystems().RegisterStandardUnitSystems(null!));
    }

    static void AssertSystem(UnitSystemDefinition system, string id, string name, string[] baseUnitIds)
    {
        Assert.That(system.GetId(), Is.EqualTo(id));
        Assert.That(system.GetName(), Is.EqualTo(name));
        Assert.That(system.GetBaseUnitIds(), Is.EqualTo(baseUnitIds));
        Assert.That(system.GetPreferredQuantityKindIds().Length, Is.EqualTo(30));
        Assert.That(system.GetPreferredUnitIds().Length, Is.EqualTo(30));
    }

    static void AssertRegisteredSystem(UnitSystemDefinition system, string id, string lengthUnitId)
    {
        Assert.That(system.GetId(), Is.EqualTo(id));
        Assert.That(system.FindPreferredUnitId("length"), Is.EqualTo(lengthUnitId));
    }

    static UnitRegistry CreateRegistryWithStandardMetadata()
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
        new StandardQuantityKinds().RegisterCoreQuantityKinds(registry);
        return registry;
    }
}
