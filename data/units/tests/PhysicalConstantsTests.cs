using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PhysicalConstantsTests
{
    [Test]
    public void PhysicalConstantDefinition_StoresMetadataAndQuantity()
    {
        QuantityValue quantity = new QuantityValue(299792458.0d, new StandardUnits().Meter().Divide(new StandardUnits().Second()));

        PhysicalConstantDefinition definition = new PhysicalConstantDefinition(
            "speed_of_light",
            "speed of light in vacuum",
            "c",
            "Defined speed of light in vacuum.",
            "exact",
            "BIPM SI Brochure",
            "9th edition, version 4.01 (2026)",
            0.0d,
            quantity,
            new string[] { "light speed" });

        Assert.That(definition.GetId(), Is.EqualTo("speed_of_light"));
        Assert.That(definition.GetName(), Is.EqualTo("speed of light in vacuum"));
        Assert.That(definition.GetSymbol(), Is.EqualTo("c"));
        Assert.That(definition.GetDescription(), Is.EqualTo("Defined speed of light in vacuum."));
        Assert.That(definition.GetExactnessLabel(), Is.EqualTo("exact"));
        Assert.That(definition.IsExact(), Is.True);
        Assert.That(definition.GetSourceLabel(), Is.EqualTo("BIPM SI Brochure"));
        Assert.That(definition.GetAdjustmentOrEdition(), Is.EqualTo("9th edition, version 4.01 (2026)"));
        Assert.That(definition.GetStandardUncertainty(), Is.EqualTo(0.0d));
        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "light speed" }));
        Assert.That(definition.GetQuantity().GetValue(), Is.EqualTo(299792458.0d));
        Assert.That(definition.GetQuantity().GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        QuantityValue firstQuantity = definition.GetQuantity();
        QuantityValue secondQuantity = definition.GetQuantity();
        Assert.That(firstQuantity, Is.Not.SameAs(secondQuantity));
    }

    [Test]
    public void PhysicalConstantDefinition_CopiesAliasesAndQuantityDefensively()
    {
        string[] aliases = new string[] { "big_g" };
        QuantityValue quantity = new QuantityValue(6.67430E-11d, GravitationalConstantUnit());

        PhysicalConstantDefinition definition = new PhysicalConstantDefinition(
            "gravitational_constant",
            "Newtonian constant of gravitation",
            "G",
            null,
            "measured",
            "NIST Fundamental Physical Constants",
            "2022 CODATA adjustment",
            1.5E-15d,
            quantity,
            aliases);

        aliases[0] = "changed";
        string[] returned = definition.GetAliases();
        returned[0] = "changed_again";

        Assert.That(definition.GetDescription(), Is.EqualTo(""));
        Assert.That(definition.IsExact(), Is.False);
        Assert.That(definition.GetSourceLabel(), Is.EqualTo("NIST Fundamental Physical Constants"));
        Assert.That(definition.GetAdjustmentOrEdition(), Is.EqualTo("2022 CODATA adjustment"));
        Assert.That(definition.GetStandardUncertainty(), Is.EqualTo(1.5E-15d));
        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "big_g" }));
        Assert.That(definition.GetQuantity(), Is.Not.SameAs(quantity));
        Assert.That(definition.GetQuantity().GetValue(), Is.EqualTo(6.67430E-11d));
    }

    [Test]
    public void PhysicalConstantDefinition_MatchesIdNameSymbolAndAliases()
    {
        PhysicalConstantDefinition definition = new PhysicalConstants().SpeedOfLight();

        Assert.That(definition.MatchesText("speed_of_light"), Is.True);
        Assert.That(definition.MatchesText("speed of light in vacuum"), Is.True);
        Assert.That(definition.MatchesText("c"), Is.True);
        Assert.That(definition.MatchesText("light speed"), Is.True);
        Assert.That(definition.MatchesText("C"), Is.False);
        Assert.That(definition.MatchesText(""), Is.False);
        Assert.That(definition.MatchesText(null), Is.False);
    }

    [Test]
    public void PhysicalConstantDefinition_RejectsMissingRequiredValues()
    {
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("", "name", "x", "", "exact", "source", "edition", 0.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", " ", "x", "", "exact", "source", "edition", 0.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "", "source", "edition", 0.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "", "edition", 0.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", " ", 0.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", -1.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", double.NaN, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", double.PositiveInfinity, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", 1.0d, quantity, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", 0.0d, quantity, new string[] { "" }));
        Assert.Throws<System.ArgumentNullException>(() => new PhysicalConstantDefinition("id", "name", "x", "", "exact", "source", "edition", 0.0d, null!, new string[0]));
    }

    [Test]
    public void CreateFundamentalConstants_ReturnsStableConstantsWithExactnessLabels()
    {
        PhysicalConstantDefinition[] constants = new PhysicalConstants().CreateFundamentalConstants();

        Assert.That(constants.Length, Is.EqualTo(9));
        AssertConstant(constants[0], "caesium_133_hyperfine_transition_frequency", "Δν_Cs", "exact", 9192631770.0d, new StandardUnits().Hertz().GetDimension());
        AssertConstant(constants[1], "speed_of_light", "c", "exact", 299792458.0d, new StandardDimensions().Length().Divide(new StandardDimensions().Time()));
        AssertConstant(constants[2], "planck_constant", "h", "exact", 6.62607015E-34d, new StandardUnits().Joule().GetDimension().Multiply(new StandardDimensions().Time()));
        AssertConstant(constants[3], "elementary_charge", "e", "exact", 1.602176634E-19d, new StandardUnits().Coulomb().GetDimension());
        AssertConstant(constants[4], "boltzmann_constant", "k", "exact", 1.380649E-23d, new StandardUnits().Joule().GetDimension().Divide(new StandardDimensions().ThermodynamicTemperature()));
        AssertConstant(constants[5], "avogadro_constant", "N_A", "exact", 6.02214076E23d, new StandardDimensions().AmountOfSubstance().Pow(-1));
        AssertConstant(constants[6], "luminous_efficacy", "K_cd", "exact", 683.0d, new StandardUnits().Lumen().GetDimension().Divide(new StandardUnits().Watt().GetDimension()));
        AssertConstant(constants[7], "standard_gravity", "g0", "exact", 9.80665d, new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2)));
        AssertConstant(constants[8], "gravitational_constant", "G", "measured", 6.67430E-11d, GravitationalConstantUnit().GetDimension());
    }

    [Test]
    public void RegisterFundamentalConstants_AddsConstantsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new PhysicalConstants().RegisterFundamentalConstants(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetConstants(), Has.Length.EqualTo(9));
        Assert.That(registry.FindPhysicalConstant("speed_of_light").GetPhysicalConstant().GetSymbol(), Is.EqualTo("c"));
        Assert.That(registry.FindPhysicalConstant("light speed").GetPhysicalConstant().GetId(), Is.EqualTo("speed_of_light"));
        Assert.That(registry.FindPhysicalConstant("big_g").GetPhysicalConstant().GetId(), Is.EqualTo("gravitational_constant"));
        Assert.That(registry.FindPhysicalConstant("missing").GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
    }

    [Test]
    public void RegisterFundamentalConstants_RejectsMissingOrFrozenRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.Freeze();

        OperationResult result = new PhysicalConstants().RegisterFundamentalConstants(registry);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.Throws<System.ArgumentNullException>(() => new PhysicalConstants().RegisterFundamentalConstants(null!));
    }

    static void AssertConstant(PhysicalConstantDefinition definition, string id, string symbol, string exactnessLabel, double value, DimensionVector dimension)
    {
        Assert.That(definition.GetId(), Is.EqualTo(id));
        Assert.That(definition.GetSymbol(), Is.EqualTo(symbol));
        Assert.That(definition.GetExactnessLabel(), Is.EqualTo(exactnessLabel));
        Assert.That(definition.GetQuantity().GetValue(), Is.EqualTo(value).Within(System.Math.Abs(value) * 0.000000000001d + 0.000000000001d));
        Assert.That(definition.GetQuantity().GetUnit().GetDimension().EqualsDimension(dimension), Is.True);
    }

    static UnitDefinition GravitationalConstantUnit()
    {
        return new StandardUnits().Meter().Pow(3).Divide(new StandardUnits().Kilogram()).Divide(new StandardUnits().Second().Pow(2));
    }
}
