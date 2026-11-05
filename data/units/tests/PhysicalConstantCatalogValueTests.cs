using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PhysicalConstantCatalogValueTests
{
    [Test]
    public void FundamentalConstants_UseExpectedUnitsAndDimensions()
    {
        AssertConstantUnit(new PhysicalConstants().Caesium133HyperfineTransitionFrequency(), "hertz", new StandardDimensions().Time().Pow(-1));
        AssertConstantUnit(new PhysicalConstants().SpeedOfLight(), "(meter/second)", new StandardDimensions().Length().Divide(new StandardDimensions().Time()));
        AssertConstantUnit(new PhysicalConstants().PlanckConstant(), "(joule*second)", new StandardUnits().Joule().GetDimension().Multiply(new StandardDimensions().Time()));
        AssertConstantUnit(new PhysicalConstants().ElementaryCharge(), "coulomb", new StandardUnits().Coulomb().GetDimension());
        AssertConstantUnit(new PhysicalConstants().BoltzmannConstant(), "(joule/kelvin_interval)", new StandardUnits().Joule().GetDimension().Divide(new StandardDimensions().ThermodynamicTemperature()));
        AssertConstantUnit(new PhysicalConstants().AvogadroConstant(), "(1/mole)", new StandardDimensions().AmountOfSubstance().Pow(-1));
        AssertConstantUnit(new PhysicalConstants().LuminousEfficacy(), "(lumen/watt)", new StandardUnits().Lumen().GetDimension().Divide(new StandardUnits().Watt().GetDimension()));
        AssertConstantUnit(new PhysicalConstants().StandardGravity(), "(meter/(second^2))", new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2)));
        AssertConstantUnit(new PhysicalConstants().GravitationalConstant(), "(((meter^3)/kilogram)/(second^2))", new StandardDimensions().Length().Pow(3).Divide(new StandardDimensions().Mass()).Divide(new StandardDimensions().Time().Pow(2)));
    }

    [Test]
    public void FundamentalConstants_HaveRepresentativeValuesAndExactnessLabels()
    {
        AssertConstantValue(new PhysicalConstants().Caesium133HyperfineTransitionFrequency(), "exact", 9192631770.0d, 0.0d);
        AssertConstantValue(new PhysicalConstants().SpeedOfLight(), "exact", 299792458.0d, 0.0d);
        AssertConstantValue(new PhysicalConstants().PlanckConstant(), "exact", 6.62607015E-34d, 0.0d);
        AssertConstantValue(new PhysicalConstants().ElementaryCharge(), "exact", 1.602176634E-19d, 0.0d);
        AssertConstantValue(new PhysicalConstants().BoltzmannConstant(), "exact", 1.380649E-23d, 0.0d);
        AssertConstantValue(new PhysicalConstants().AvogadroConstant(), "exact", 6.02214076E23d, 1000000000000.0d);
        AssertConstantValue(new PhysicalConstants().LuminousEfficacy(), "exact", 683.0d, 0.0d);
        AssertConstantValue(new PhysicalConstants().StandardGravity(), "exact", 9.80665d, 0.0d);
        AssertConstantValue(new PhysicalConstants().GravitationalConstant(), "measured", 6.67430E-11d, 0.000000000000001d);
    }

    [Test]
    public void FundamentalConstants_ConvertToRepresentativeCompatibleUnits()
    {
        QuantityValue speedOfLight = new PhysicalConstants().SpeedOfLight().GetQuantity();
        QuantityValue standardGravity = new PhysicalConstants().StandardGravity().GetQuantity();
        QuantityValue oneMoleCount = new QuantityValue(1.0d, new StandardUnits().Mole());

        QuantityValue speedKilometersPerSecond = speedOfLight.ToUnit(Kilometer().Divide(new StandardUnits().Second()));
        QuantityValue gravityFeetPerSecondSquared = standardGravity.ToUnit(new StandardUnits().Foot().Divide(new StandardUnits().Second().Pow(2)));
        QuantityValue avogadroCount = new QuantityMath().Multiply(new PhysicalConstants().AvogadroConstant().GetQuantity(), oneMoleCount);

        Assert.That(speedKilometersPerSecond.GetValue(), Is.EqualTo(299792.458d).Within(0.000000001d));
        Assert.That(speedKilometersPerSecond.GetUnit().GetId(), Is.EqualTo("(kilometer/second)"));
        Assert.That(gravityFeetPerSecondSquared.GetValue(), Is.EqualTo(32.17404855643044d).Within(0.000000000001d));
        Assert.That(gravityFeetPerSecondSquared.GetUnit().GetId(), Is.EqualTo("(foot/(second^2))"));
        Assert.That(avogadroCount.GetValue(), Is.EqualTo(6.02214076E23d).Within(1000000000000.0d));
        Assert.That(avogadroCount.IsDimensionless(), Is.True);
    }

    [Test]
    public void StandardRegistry_ExposesPhysicalConstantsByIdSymbolNameAndAlias()
    {
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.That(registry.FindPhysicalConstant("speed_of_light").GetPhysicalConstant().GetSymbol(), Is.EqualTo("c"));
        Assert.That(registry.FindPhysicalConstant("c").GetPhysicalConstant().GetId(), Is.EqualTo("speed_of_light"));
        Assert.That(registry.FindPhysicalConstant("Planck constant").GetPhysicalConstant().GetId(), Is.EqualTo("planck_constant"));
        Assert.That(registry.FindPhysicalConstant("avogadro").GetPhysicalConstant().GetId(), Is.EqualTo("avogadro_constant"));
        Assert.That(registry.FindPhysicalConstant("big_g").GetPhysicalConstant().GetExactnessLabel(), Is.EqualTo("measured"));
    }

    static void AssertConstantUnit(PhysicalConstantDefinition constant, string unitId, DimensionVector dimension)
    {
        UnitDefinition unit = constant.GetQuantity().GetUnit();

        Assert.That(unit.GetId(), Is.EqualTo(unitId));
        Assert.That(unit.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(unit.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    static void AssertConstantValue(PhysicalConstantDefinition constant, string exactnessLabel, double value, double tolerance)
    {
        Assert.That(constant.GetExactnessLabel(), Is.EqualTo(exactnessLabel));
        Assert.That(constant.GetQuantity().GetValue(), Is.EqualTo(value).Within(tolerance));
    }

    static UnitDefinition Kilometer()
    {
        return new UnitDefinition(
            "kilometer",
            "kilometer",
            "kilometers",
            "km",
            new string[] { "kilometre", "kilometres" },
            new StandardDimensions().Length(),
            new RationalNumber(1000, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
