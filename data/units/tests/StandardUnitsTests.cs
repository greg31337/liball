using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardUnitsTests
{
    [Test]
    public void CreateSiBaseUnits_ReturnsSevenBaseUnitsInStableOrder()
    {
        UnitDefinition[] units = new StandardUnits().CreateSiBaseUnits();

        Assert.That(units.Length, Is.EqualTo(7));
        AssertUnit(units[0], "meter", "m", new StandardDimensions().Length(), UnitConversionKind.Scale, true);
        AssertUnit(units[1], "kilogram", "kg", new StandardDimensions().Mass(), UnitConversionKind.Scale, false);
        AssertUnit(units[2], "second", "s", new StandardDimensions().Time(), UnitConversionKind.Scale, true);
        AssertUnit(units[3], "ampere", "A", new StandardDimensions().ElectricCurrent(), UnitConversionKind.Scale, true);
        AssertUnit(units[4], "kelvin", "K", new StandardDimensions().ThermodynamicTemperature(), UnitConversionKind.Affine, true);
        AssertUnit(units[5], "mole", "mol", new StandardDimensions().AmountOfSubstance(), UnitConversionKind.Scale, true);
        AssertUnit(units[6], "candela", "cd", new StandardDimensions().LuminousIntensity(), UnitConversionKind.Scale, true);
    }

    [Test]
    public void BaseUnitFactories_ReturnIndependentEquivalentUnits()
    {
        UnitDefinition first = new StandardUnits().Meter();
        UnitDefinition second = new StandardUnits().Meter();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.IsEquivalentTo(second), Is.True);
        Assert.That(first.GetAliases(), Is.EqualTo(new string[] { "metre" }));
    }

    [Test]
    public void CreateSiDerivedUnits_ReturnsCommonDerivedUnitsInStableOrder()
    {
        UnitDefinition[] units = new StandardUnits().CreateSiDerivedUnits();

        Assert.That(units.Length, Is.EqualTo(14));
        AssertUnit(units[0], "newton", "N", new DimensionVector(1, 1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[1], "pascal", "Pa", new DimensionVector(-1, 1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[2], "joule", "J", new DimensionVector(2, 1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[3], "watt", "W", new DimensionVector(2, 1, -3, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[4], "coulomb", "C", new DimensionVector(0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[5], "volt", "V", new DimensionVector(2, 1, -3, -1, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[6], "ohm", "\u03a9", new DimensionVector(2, 1, -3, -2, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[7], "farad", "F", new DimensionVector(-2, -1, 4, 2, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[8], "tesla", "T", new DimensionVector(0, 1, -2, -1, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[9], "hertz", "Hz", new DimensionVector(0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[10], "radian", "rad", new StandardDimensions().PlaneAngle(), UnitConversionKind.Scale, true);
        AssertUnit(units[11], "steradian", "sr", new StandardDimensions().SolidAngle(), UnitConversionKind.Scale, true);
        AssertUnit(units[12], "lumen", "lm", new DimensionVector(0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0), UnitConversionKind.Scale, true);
        AssertUnit(units[13], "lux", "lx", new DimensionVector(-2, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0), UnitConversionKind.Scale, true);
    }

    [Test]
    public void DerivedUnitFactories_ReturnIndependentEquivalentUnits()
    {
        UnitDefinition first = new StandardUnits().Newton();
        UnitDefinition second = new StandardUnits().Newton();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.IsEquivalentTo(second), Is.True);
    }

    [Test]
    public void CreateMetricUnits_ReturnsCommonMetricUnitsInStableOrder()
    {
        UnitDefinition[] units = new StandardUnits().CreateMetricUnits();

        Assert.That(units.Length, Is.EqualTo(15));
        AssertUnitWithScale(units[0], "gram", "g", new StandardDimensions().Mass(), new RationalNumber(1, 1000), UnitConversionKind.Scale, true);
        AssertUnitWithScale(units[1], "liter", "L", new StandardDimensions().Length().Pow(3), new RationalNumber(1, 1000), UnitConversionKind.Scale, true);
        AssertUnitWithScale(units[2], "tonne", "t", new StandardDimensions().Mass(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[3], "hectare", "ha", new StandardDimensions().Length().Pow(2), new RationalNumber(10000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[4], "minute", "min", new StandardDimensions().Time(), new RationalNumber(60, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[5], "hour", "h", new StandardDimensions().Time(), new RationalNumber(3600, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[6], "day", "d", new StandardDimensions().Time(), new RationalNumber(86400, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[7], "electronvolt", "eV", new StandardUnits().Joule().GetDimension(), ElectronvoltScale(), UnitConversionKind.Scale, true);
        AssertUnitWithScale(units[8], "kilometer", "km", new StandardDimensions().Length(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[9], "kilo_newton", "kN", new StandardUnits().Newton().GetDimension(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[10], "kilo_pascal", "kPa", new StandardUnits().Pascal().GetDimension(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[11], "kilo_joule", "kJ", new StandardUnits().Joule().GetDimension(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[12], "kilo_watt", "kW", new StandardUnits().Watt().GetDimension(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[13], "kilo_hertz", "kHz", new StandardUnits().Hertz().GetDimension(), new RationalNumber(1000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[14], "mega_hertz", "MHz", new StandardUnits().Hertz().GetDimension(), new RationalNumber(1000000, 1), UnitConversionKind.Scale, false);
        Assert.That(units[0].GetAliases(), Is.EqualTo(new string[] { "gramme" }));
        Assert.That(units[1].GetAliases(), Is.EqualTo(new string[] { "litre" }));
        Assert.That(units[2].GetAliases(), Is.EqualTo(new string[] { "metric ton" }));
        Assert.That(units[7].GetAliases(), Is.EqualTo(new string[] { "electron volt" }));
    }

    [Test]
    public void MetricUnits_ConvertThroughSystemUnitsWithExactScales()
    {
        UnitConverter converter = new UnitConverter();

        AssertConversion(converter.Convert(2.0d, new StandardUnits().Kilogram(), new StandardUnits().Gram()), 2000.0d, new StandardUnits().Gram());
        AssertConversion(converter.Convert(3.0d, new StandardUnits().Liter(), new StandardUnits().Meter().Pow(3)), 0.003d, new StandardUnits().Meter().Pow(3));
        AssertConversion(converter.Convert(2.0d, new StandardUnits().Day(), new StandardUnits().Hour()), 48.0d, new StandardUnits().Hour());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Electronvolt(), new StandardUnits().Joule()), 1.602176634E-19d, new StandardUnits().Joule());
    }

    [Test]
    public void CreateImperialAndUsCustomaryUnits_ReturnsCommonUnitsInStableOrder()
    {
        UnitDefinition[] units = new StandardUnits().CreateImperialAndUsCustomaryUnits();

        Assert.That(units.Length, Is.EqualTo(20));
        AssertUnitWithScale(units[0], "inch", "in", new StandardDimensions().Length(), InchScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[1], "foot", "ft", new StandardDimensions().Length(), FootScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[2], "yard", "yd", new StandardDimensions().Length(), new RationalNumber(1143, 1250), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[3], "mile", "mi", new StandardDimensions().Length(), new RationalNumber(201168, 125), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[4], "pound", "lb", new StandardDimensions().Mass(), PoundScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[5], "ounce", "oz", new StandardDimensions().Mass(), OunceScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[6], "pound_force", "lbf", new StandardUnits().Newton().GetDimension(), PoundForceScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[7], "us_gallon", "gal", new StandardDimensions().Length().Pow(3), GallonScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[8], "us_quart", "qt", new StandardDimensions().Length().Pow(3), QuartScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[9], "us_pint", "pt", new StandardDimensions().Length().Pow(3), PintScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[10], "us_cup", "cup", new StandardDimensions().Length().Pow(3), CupScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[11], "us_fluid_ounce", "fl_oz_us", new StandardDimensions().Length().Pow(3), GallonScale().Divide(new RationalNumber(128, 1)), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[12], "imperial_gallon", "gal_imp", new StandardDimensions().Length().Pow(3), ImperialGallonScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[13], "imperial_quart", "qt_imp", new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(4, 1)), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[14], "imperial_pint", "pt_imp", new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(8, 1)), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[15], "imperial_fluid_ounce", "fl_oz_imp", new StandardDimensions().Length().Pow(3), ImperialGallonScale().Divide(new RationalNumber(160, 1)), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[16], "acre", "ac", new StandardDimensions().Length().Pow(2), AcreScale(), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[17], "psi", "psi", new StandardUnits().Pascal().GetDimension(), PoundForceScale().Divide(InchScale().Multiply(InchScale())), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[18], "horsepower", "hp", new StandardUnits().Watt().GetDimension(), FootScale().Multiply(PoundForceScale()).Multiply(new RationalNumber(550, 1)), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[19], "british_thermal_unit", "BTU", new StandardUnits().Joule().GetDimension(), new RationalNumber(52752792631, 50000000), UnitConversionKind.Scale, false);
        Assert.That(units[7].GetAliases(), Does.Contain("gallon"));
        Assert.That(units[17].GetAliases(), Is.EqualTo(new string[] { "pound-force per square inch" }));
        Assert.That(units[18].GetAliases(), Is.EqualTo(new string[] { "mechanical horsepower" }));
    }

    [Test]
    public void ImperialAndUsCustomaryUnits_ConvertThroughSystemUnitsWithExactScales()
    {
        UnitConverter converter = new UnitConverter();

        AssertConversion(converter.Convert(12.0d, new StandardUnits().Inch(), new StandardUnits().Foot()), 1.0d, new StandardUnits().Foot());
        AssertConversion(converter.Convert(5280.0d, new StandardUnits().Foot(), new StandardUnits().Mile()), 1.0d, new StandardUnits().Mile());
        AssertConversion(converter.Convert(16.0d, new StandardUnits().Ounce(), new StandardUnits().Pound()), 1.0d, new StandardUnits().Pound());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Gallon(), new StandardUnits().Quart()), 4.0d, new StandardUnits().Quart());
        AssertConversion(converter.Convert(640.0d, new StandardUnits().Acre(), new StandardUnits().Mile().Pow(2)), 1.0d, new StandardUnits().Mile().Pow(2));
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Psi(), new StandardUnits().Pascal()), 6894.757293168361d, new StandardUnits().Pascal(), 0.000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Horsepower(), new StandardUnits().Watt()), 745.6998715822702d, new StandardUnits().Watt(), 0.000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Btu(), new StandardUnits().Joule()), 1055.05585262d, new StandardUnits().Joule(), 0.000000001d);
    }

    [Test]
    public void ImperialAndUsLiquidVolumes_RemainDistinctAtEveryNamedSize()
    {
        StandardUnits units = new StandardUnits();

        Assert.That(units.ImperialGallon().IsEquivalentTo(units.UsGallon()), Is.False);
        Assert.That(units.ImperialQuart().IsEquivalentTo(units.UsQuart()), Is.False);
        Assert.That(units.ImperialPint().IsEquivalentTo(units.UsPint()), Is.False);
        Assert.That(units.ImperialFluidOunce().IsEquivalentTo(units.UsFluidOunce()), Is.False);
        Assert.That(units.ImperialGallon().GetScaleToSystemUnit().EqualsValue(new RationalNumber(454609, 100000000)), Is.True);
        Assert.That(units.UsGallon().GetScaleToSystemUnit().EqualsValue(new RationalNumber(473176473, 125000000000)), Is.True);
        Assert.That(units.ImperialFluidOunce().GetScaleToSystemUnit().EqualsValue(new RationalNumber(454609, 16000000000)), Is.True);
        Assert.That(units.UsFluidOunce().GetScaleToSystemUnit().EqualsValue(new RationalNumber(473176473, 16000000000000)), Is.True);
    }

    [Test]
    public void TemperatureUnits_PreserveExactPointOffsetsAndIntervalScales()
    {
        IStandardUnits catalog = new StandardUnits();
        UnitDefinition[] units = catalog.CreateTemperatureUnits();

        Assert.That(units, Has.Length.EqualTo(7));
        AssertAffineUnit(units[0], "degree_celsius", "degC", new RationalNumber(1, 1), new RationalNumber(27315, 100));
        AssertAffineUnit(units[1], "degree_fahrenheit", "degF", new RationalNumber(5, 9), new RationalNumber(45967, 180));
        AssertAffineUnit(units[2], "rankine", "degR", new RationalNumber(5, 9), new RationalNumber(0, 1));
        AssertUnitWithScale(units[3], "kelvin_interval", "delta_K", new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(1, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[4], "celsius_interval", "delta_degC", new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(1, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[5], "fahrenheit_interval", "delta_degF", new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(5, 9), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[6], "rankine_interval", "delta_degR", new StandardDimensions().ThermodynamicTemperature(), new RationalNumber(5, 9), UnitConversionKind.Scale, false);
    }

    [Test]
    public void DimensionlessUnits_CoverRatioCountAndCurrencyDimensions()
    {
        UnitDefinition[] units = new StandardUnits().CreateDimensionlessUnits();

        Assert.That(units, Has.Length.EqualTo(4));
        AssertUnitWithScale(units[0], "one", "1", new StandardDimensions().Dimensionless(), new RationalNumber(1, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[1], "percent", "%", new StandardDimensions().Dimensionless(), new RationalNumber(1, 100), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[2], "count", "count", new StandardDimensions().Count(), new RationalNumber(1, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[3], "currency_unit", "currency", new StandardDimensions().Currency(), new RationalNumber(1, 1), UnitConversionKind.Scale, false);
    }

    [Test]
    public void CgsUnits_UseExactMechanicalAndElectrostaticScales()
    {
        UnitDefinition[] units = new StandardUnits().CreateCgsUnits();
        RationalNumber statcoulomb = new RationalNumber(1, 1).Divide(new RationalNumber(2997924580, 1));
        RationalNumber statvolt = new RationalNumber(299792458, 1000000);

        Assert.That(units, Has.Length.EqualTo(9));
        AssertUnitWithScale(units[0], "centimeter", "cm", new StandardDimensions().Length(), new RationalNumber(1, 100), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[1], "dyne", "dyn", new StandardUnits().Newton().GetDimension(), new RationalNumber(1, 100000), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[2], "barye", "Ba", new StandardUnits().Pascal().GetDimension(), new RationalNumber(1, 10), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[3], "erg", "erg", new StandardUnits().Joule().GetDimension(), new RationalNumber(1, 10000000), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[4], "statcoulomb", "statC", new StandardUnits().Coulomb().GetDimension(), statcoulomb, UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[5], "statampere", "statA", new StandardDimensions().ElectricCurrent(), statcoulomb, UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[6], "statvolt", "statV", new StandardUnits().Volt().GetDimension(), statvolt, UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[7], "statohm", "statOhm", new StandardUnits().Ohm().GetDimension(), statvolt.Divide(statcoulomb), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[8], "statfarad", "statF", new StandardUnits().Farad().GetDimension(), statcoulomb.Divide(statvolt), UnitConversionKind.Scale, false);
    }

    [Test]
    public void CompoundUnits_CoverEveryMetadataAndTypedFacadeReference()
    {
        UnitDefinition[] units = new StandardUnits().CreateCompoundUnits();

        Assert.That(UnitIds(units), Is.EqualTo(new string[]
        {
            "(meter^2)", "(meter^3)", "(meter/second)", "(kilometer/hour)", "(mile/hour)",
            "(meter/(second^2))", "(foot/(second^2))", "(radian/second)", "(kilogram/(meter^3))", "((meter^3)/second)",
            "(centimeter^2)", "(centimeter^3)", "(centimeter/second)", "(centimeter/(second^2))", "(erg/second)",
            "(gram/(centimeter^3))", "((centimeter^3)/second)", "(foot^2)", "(foot^3)", "(foot/second)",
            "(pound/(foot^3))", "((foot^3)/second)", "(us_gallon/minute)", "(kilo_watt*hour)", "(bit/second)",
            "(byte/second)", "(kilobyte/second)", "(kibibyte/second)", "(megabyte/second)", "(mebibyte/second)"
        }));

        RationalNumber one = new RationalNumber(1, 1);
        RationalNumber centimeter = new RationalNumber(1, 100);
        RationalNumber foot = FootScale();
        RationalNumber pound = PoundScale();
        RationalNumber gallon = GallonScale();
        RationalNumber[] expectedScales = new RationalNumber[]
        {
            one,
            one,
            one,
            new RationalNumber(1000, 3600),
            new RationalNumber(201168, 125).Divide(new RationalNumber(3600, 1)),
            one,
            foot,
            one,
            one,
            one,
            centimeter.Multiply(centimeter),
            centimeter.Multiply(centimeter).Multiply(centimeter),
            centimeter,
            centimeter,
            new RationalNumber(1, 10000000),
            new RationalNumber(1, 1000).Divide(centimeter.Multiply(centimeter).Multiply(centimeter)),
            centimeter.Multiply(centimeter).Multiply(centimeter),
            foot.Multiply(foot),
            foot.Multiply(foot).Multiply(foot),
            foot,
            pound.Divide(foot.Multiply(foot).Multiply(foot)),
            foot.Multiply(foot).Multiply(foot),
            gallon.Divide(new RationalNumber(60, 1)),
            new RationalNumber(3600000, 1),
            one,
            new RationalNumber(8, 1),
            new RationalNumber(8000, 1),
            new RationalNumber(8192, 1),
            new RationalNumber(8000000, 1),
            new RationalNumber(8388608, 1)
        };
        DimensionVector length = new StandardDimensions().Length();
        DimensionVector time = new StandardDimensions().Time();
        DimensionVector informationRate = new StandardDimensions().Information().Divide(time);
        DimensionVector[] expectedDimensions = new DimensionVector[]
        {
            length.Pow(2),
            length.Pow(3),
            length.Divide(time),
            length.Divide(time),
            length.Divide(time),
            length.Divide(time.Pow(2)),
            length.Divide(time.Pow(2)),
            new StandardDimensions().PlaneAngle().Divide(time),
            new StandardDimensions().Mass().Divide(length.Pow(3)),
            length.Pow(3).Divide(time),
            length.Pow(2),
            length.Pow(3),
            length.Divide(time),
            length.Divide(time.Pow(2)),
            new StandardUnits().Watt().GetDimension(),
            new StandardDimensions().Mass().Divide(length.Pow(3)),
            length.Pow(3).Divide(time),
            length.Pow(2),
            length.Pow(3),
            length.Divide(time),
            new StandardDimensions().Mass().Divide(length.Pow(3)),
            length.Pow(3).Divide(time),
            length.Pow(3).Divide(time),
            new StandardUnits().Joule().GetDimension(),
            informationRate,
            informationRate,
            informationRate,
            informationRate,
            informationRate,
            informationRate
        };

        int index = 0;
        while (index < units.Length)
        {
            AssertCompoundMetadata(units[index], expectedDimensions[index], expectedScales[index]);
            index = index + 1;
        }
    }

    [Test]
    public void UsVolumeConvenienceFactories_AreExactAliasesForExplicitUsDefinitions()
    {
        StandardUnits units = new StandardUnits();

        Assert.That(units.Gallon().IsEquivalentTo(units.UsGallon()), Is.True);
        Assert.That(units.Quart().IsEquivalentTo(units.UsQuart()), Is.True);
        Assert.That(units.Pint().IsEquivalentTo(units.UsPint()), Is.True);
        Assert.That(units.Cup().IsEquivalentTo(units.UsCup()), Is.True);
        Assert.That(units.Gallon().GetId(), Is.EqualTo("us_gallon"));
        Assert.That(units.Quart().GetId(), Is.EqualTo("us_quart"));
        Assert.That(units.Pint().GetId(), Is.EqualTo("us_pint"));
        Assert.That(units.Cup().GetId(), Is.EqualTo("us_cup"));
    }

    [Test]
    public void NewCatalogRegistrationMethods_AddDefinitionsAndHonorRegistryState()
    {
        StandardUnits catalog = new StandardUnits();
        UnitRegistry registry = new UnitRegistry();

        AssertOperationSuccess(catalog.RegisterTemperatureUnits(registry));
        AssertOperationSuccess(catalog.RegisterDimensionlessUnits(registry));
        AssertOperationSuccess(catalog.RegisterCgsUnits(registry));
        AssertOperationSuccess(catalog.RegisterCompoundUnits(registry));
        Assert.That(registry.GetUnits(), Has.Length.EqualTo(50));
        AssertLookup(registry, "delta_degF", "fahrenheit_interval");
        AssertLookup(registry, "dyn", "dyne");
        AssertLookup(registry, "m/s", "(meter/second)");

        UnitRegistry frozen = new UnitRegistry();
        frozen.Freeze();
        Assert.That(catalog.RegisterTemperatureUnits(frozen).GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(catalog.RegisterDimensionlessUnits(frozen).GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(catalog.RegisterCgsUnits(frozen).GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(catalog.RegisterCompoundUnits(frozen).GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.Throws<System.ArgumentNullException>(() => catalog.RegisterTemperatureUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => catalog.RegisterDimensionlessUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => catalog.RegisterCgsUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => catalog.RegisterCompoundUnits(null!));
    }

    [Test]
    public void CreateDataUnits_ReturnsCommonDataUnitsInStableOrder()
    {
        UnitDefinition[] units = new StandardUnits().CreateDataUnits();

        Assert.That(units.Length, Is.EqualTo(6));
        AssertUnitWithScale(units[0], "bit", "bit", new StandardDimensions().Information(), new RationalNumber(1, 1), UnitConversionKind.Scale, true);
        AssertUnitWithScale(units[1], "byte", "B", new StandardDimensions().Information(), new RationalNumber(8, 1), UnitConversionKind.Scale, true);
        AssertUnitWithScale(units[2], "kilobyte", "kB", new StandardDimensions().Information(), new RationalNumber(8000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[3], "kibibyte", "KiB", new StandardDimensions().Information(), new RationalNumber(8192, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[4], "megabyte", "MB", new StandardDimensions().Information(), new RationalNumber(8000000, 1), UnitConversionKind.Scale, false);
        AssertUnitWithScale(units[5], "mebibyte", "MiB", new StandardDimensions().Information(), new RationalNumber(8388608, 1), UnitConversionKind.Scale, false);
        Assert.That(units[1].GetAliases(), Is.EqualTo(new string[] { "octet" }));
    }

    [Test]
    public void DataUnits_ConvertThroughInformationSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        AssertConversion(converter.Convert(1.0d, new StandardUnits().Byte(), new StandardUnits().Bit()), 8.0d, new StandardUnits().Bit());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Kilobyte(), new StandardUnits().Byte()), 1000.0d, new StandardUnits().Byte());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Kibibyte(), new StandardUnits().Byte()), 1024.0d, new StandardUnits().Byte());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Megabyte(), new StandardUnits().Kilobyte()), 1000.0d, new StandardUnits().Kilobyte());
        AssertConversion(converter.Convert(1.0d, new StandardUnits().Mebibyte(), new StandardUnits().Kibibyte()), 1024.0d, new StandardUnits().Kibibyte());
    }

    [Test]
    public void StandardCatalogs_RegisterTogetherWithoutAmbiguousSymbols()
    {
        UnitRegistry registry = new UnitRegistry();

        AssertOperationSuccess(new StandardUnits().RegisterSiBaseUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterSiDerivedUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterMetricUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterTemperatureUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterDimensionlessUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterCgsUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterDataUnits(registry));
        AssertOperationSuccess(new StandardUnits().RegisterCompoundUnits(registry));

        Assert.That(registry.GetUnits().Length, Is.EqualTo(112));
        AssertLookup(registry, "m", "meter");
        AssertLookup(registry, "N", "newton");
        AssertLookup(registry, "litre", "liter");
        AssertLookup(registry, "gallon", "us_gallon");
        AssertLookup(registry, "mechanical horsepower", "horsepower");
        AssertLookup(registry, "octet", "byte");
        AssertLookup(registry, "MiB", "mebibyte");
        AssertLookup(registry, "degC", "degree_celsius");
        AssertLookup(registry, "dyn", "dyne");
        AssertLookup(registry, "gal_imp", "imperial_gallon");
        AssertLookup(registry, "m/s", "(meter/second)");
    }

    [Test]
    public void StandardCatalogKnownConversions_CoverCrossCatalogValues()
    {
        UnitConverter converter = new UnitConverter();

        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Liter(), new StandardUnits().Gallon()), 0.2641720523581484d, new StandardUnits().Gallon(), 0.000000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Tonne(), new StandardUnits().Pound()), 2204.6226218487757d, new StandardUnits().Pound(), 0.000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Acre(), new StandardUnits().Hectare()), 0.40468564224d, new StandardUnits().Hectare(), 0.000000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Megabyte(), new StandardUnits().Mebibyte()), 0.95367431640625d, new StandardUnits().Mebibyte(), 0.000000000001d);
        AssertConversionWithin(converter.Convert(1.0d, new StandardUnits().Horsepower(), new StandardUnits().Btu().Divide(new StandardUnits().Hour())), 2544.433577644024d, new StandardUnits().Btu().Divide(new StandardUnits().Hour()), 0.000000001d);
    }

    [Test]
    public void RegisterSiBaseUnits_AddsUnitsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardUnits().RegisterSiBaseUnits(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits().Length, Is.EqualTo(7));
        Assert.That(registry.FindUnitById("meter").GetUnit().GetSymbol(), Is.EqualTo("m"));
        Assert.That(registry.FindUnit("amp").GetUnit().GetId(), Is.EqualTo("ampere"));
        Assert.That(registry.FindUnit("mol").GetUnit().GetId(), Is.EqualTo("mole"));
    }

    [Test]
    public void RegisterSiDerivedUnits_AddsUnitsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardUnits().RegisterSiDerivedUnits(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits().Length, Is.EqualTo(14));
        Assert.That(registry.FindUnitById("newton").GetUnit().GetSymbol(), Is.EqualTo("N"));
        Assert.That(registry.FindUnit("\u03a9").GetUnit().GetId(), Is.EqualTo("ohm"));
        Assert.That(registry.FindUnit("sr").GetUnit().GetId(), Is.EqualTo("steradian"));
    }

    [Test]
    public void RegisterMetricUnits_AddsUnitsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardUnits().RegisterMetricUnits(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits().Length, Is.EqualTo(15));
        Assert.That(registry.FindUnitById("gram").GetUnit().GetSymbol(), Is.EqualTo("g"));
        Assert.That(registry.FindUnit("litre").GetUnit().GetId(), Is.EqualTo("liter"));
        Assert.That(registry.FindUnit("metric ton").GetUnit().GetId(), Is.EqualTo("tonne"));
        Assert.That(registry.FindUnit("eV").GetUnit().GetId(), Is.EqualTo("electronvolt"));
    }

    [Test]
    public void RegisterImperialAndUsCustomaryUnits_AddsUnitsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits().Length, Is.EqualTo(20));
        Assert.That(registry.FindUnitById("inch").GetUnit().GetSymbol(), Is.EqualTo("in"));
        Assert.That(registry.FindUnit("gallon").GetUnit().GetId(), Is.EqualTo("us_gallon"));
        Assert.That(registry.FindUnit("mechanical horsepower").GetUnit().GetId(), Is.EqualTo("horsepower"));
        Assert.That(registry.FindUnit("BTU").GetUnit().GetId(), Is.EqualTo("british_thermal_unit"));
    }

    [Test]
    public void RegisterDataUnits_AddsUnitsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardUnits().RegisterDataUnits(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits().Length, Is.EqualTo(6));
        Assert.That(registry.FindUnitById("bit").GetUnit().GetSymbol(), Is.EqualTo("bit"));
        Assert.That(registry.FindUnit("octet").GetUnit().GetId(), Is.EqualTo("byte"));
        Assert.That(registry.FindUnit("KiB").GetUnit().GetId(), Is.EqualTo("kibibyte"));
        Assert.That(registry.FindUnit("MB").GetUnit().GetId(), Is.EqualTo("megabyte"));
    }

    [Test]
    public void RegisterSiBaseUnits_ReturnsDuplicateFailureWhenAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardUnits().RegisterSiBaseUnits(registry);

        OperationResult result = new StandardUnits().RegisterSiBaseUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void RegisterSiDerivedUnits_ReturnsDuplicateFailureWhenAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardUnits().RegisterSiDerivedUnits(registry);

        OperationResult result = new StandardUnits().RegisterSiDerivedUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void RegisterMetricUnits_ReturnsDuplicateFailureWhenAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardUnits().RegisterMetricUnits(registry);

        OperationResult result = new StandardUnits().RegisterMetricUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void RegisterImperialAndUsCustomaryUnits_ReturnsDuplicateFailureWhenAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);

        OperationResult result = new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void RegisterDataUnits_ReturnsDuplicateFailureWhenAlreadyRegistered()
    {
        UnitRegistry registry = new UnitRegistry();
        new StandardUnits().RegisterDataUnits(registry);

        OperationResult result = new StandardUnits().RegisterDataUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    [Test]
    public void RegisterSiBaseUnits_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnits().RegisterSiBaseUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[0]));
    }

    [Test]
    public void RegisterSiDerivedUnits_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnits().RegisterSiDerivedUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[0]));
    }

    [Test]
    public void RegisterMetricUnits_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnits().RegisterMetricUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[0]));
    }

    [Test]
    public void RegisterImperialAndUsCustomaryUnits_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[0]));
    }

    [Test]
    public void RegisterDataUnits_ReturnsFrozenFailureWhenRegistryIsFrozen()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new StandardUnits().RegisterDataUnits(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[0]));
    }

    [Test]
    public void RegisterSiBaseUnits_RejectsMissingRegistry()
    {
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnits().RegisterSiBaseUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnits().RegisterSiDerivedUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnits().RegisterMetricUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnits().RegisterImperialAndUsCustomaryUnits(null!));
        Assert.Throws<System.ArgumentNullException>(() => new StandardUnits().RegisterDataUnits(null!));
    }

    static void AssertUnit(UnitDefinition unit, string id, string symbol, DimensionVector dimension, UnitConversionKind conversionKind, bool prefixable)
    {
        AssertUnitWithScale(unit, id, symbol, dimension, new RationalNumber(1, 1), conversionKind, prefixable);
    }

    static void AssertUnitWithScale(UnitDefinition unit, string id, string symbol, DimensionVector dimension, RationalNumber scale, UnitConversionKind conversionKind, bool prefixable)
    {
        Assert.That(unit.GetId(), Is.EqualTo(id));
        Assert.That(unit.GetSymbol(), Is.EqualTo(symbol));
        Assert.That(unit.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(unit.GetScaleToSystemUnit().EqualsValue(scale), Is.True);
        Assert.That(unit.GetOffsetToSystemUnit().EqualsValue(new RationalNumber(0, 1)), Is.True);
        Assert.That(unit.GetConversionKind(), Is.EqualTo(conversionKind));
        Assert.That(unit.IsPrefixable(), Is.EqualTo(prefixable));
    }

    static void AssertAffineUnit(UnitDefinition unit, string id, string symbol, RationalNumber scale, RationalNumber offset)
    {
        Assert.That(unit.GetId(), Is.EqualTo(id));
        Assert.That(unit.GetSymbol(), Is.EqualTo(symbol));
        Assert.That(unit.GetDimension().EqualsDimension(new StandardDimensions().ThermodynamicTemperature()), Is.True);
        Assert.That(unit.GetScaleToSystemUnit().EqualsValue(scale), Is.True);
        Assert.That(unit.GetOffsetToSystemUnit().EqualsValue(offset), Is.True);
        Assert.That(unit.GetConversionKind(), Is.EqualTo(UnitConversionKind.Affine));
    }

    static void AssertCompoundMetadata(UnitDefinition unit, DimensionVector dimension, RationalNumber scale)
    {
        Assert.That(unit.GetDimension().EqualsDimension(dimension), Is.True, "dimension for " + unit.GetId());
        Assert.That(unit.GetScaleToSystemUnit().EqualsValue(scale), Is.True, "scale for " + unit.GetId());
        Assert.That(unit.GetOffsetToSystemUnit().IsZero(), Is.True, "offset for " + unit.GetId());
        Assert.That(unit.GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale), "conversion kind for " + unit.GetId());
        Assert.That(unit.IsPrefixable(), Is.False, "prefixability for " + unit.GetId());
    }

    static string[] UnitIds(UnitDefinition[] units)
    {
        string[] ids = new string[units.Length];
        int index = 0;
        while (index < units.Length)
        {
            ids[index] = units[index].GetId();
            index = index + 1;
        }

        return ids;
    }

    static void AssertConversion(ConversionResult result, double expectedValue, UnitDefinition expectedUnit)
    {
        AssertConversionWithin(result, expectedValue, expectedUnit, 0.000000000001d);
    }

    static void AssertConversionWithin(ConversionResult result, double expectedValue, UnitDefinition expectedUnit, double tolerance)
    {
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(expectedValue).Within(tolerance));
        Assert.That(result.GetUnit().IsEquivalentTo(expectedUnit), Is.True);
    }

    static void AssertOperationSuccess(OperationResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
    }

    static void AssertLookup(UnitRegistry registry, string text, string expectedId)
    {
        UnitLookupResult result = registry.FindUnit(text);

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetUnit().GetId(), Is.EqualTo(expectedId));
    }

    static RationalNumber ElectronvoltScale()
    {
        return RationalNumber.FromBigIntegers(new System.Numerics.BigInteger(1602176634), System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 28));
    }

    static RationalNumber InchScale()
    {
        return new RationalNumber(127, 5000);
    }

    static RationalNumber FootScale()
    {
        return new RationalNumber(381, 1250);
    }

    static RationalNumber PoundScale()
    {
        return new RationalNumber(45359237, 100000000);
    }

    static RationalNumber OunceScale()
    {
        return new RationalNumber(45359237, 1600000000);
    }

    static RationalNumber GallonScale()
    {
        return new RationalNumber(473176473, 125000000000);
    }

    static RationalNumber ImperialGallonScale()
    {
        return new RationalNumber(454609, 100000000);
    }

    static RationalNumber QuartScale()
    {
        return new RationalNumber(473176473, 500000000000);
    }

    static RationalNumber PintScale()
    {
        return new RationalNumber(473176473, 1000000000000);
    }

    static RationalNumber CupScale()
    {
        return new RationalNumber(473176473, 2000000000000);
    }

    static RationalNumber AcreScale()
    {
        return new RationalNumber(316160658, 78125);
    }

    static RationalNumber PoundForceScale()
    {
        return PoundScale().Multiply(new RationalNumber(196133, 20000));
    }
}
