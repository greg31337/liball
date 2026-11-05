using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class EquivalencyDefinitionTests
{
    [Test]
    public void Constructor_StoresEquivalencyMetadata()
    {
        EquivalencyDefinition definition = new EquivalencyDefinition(
            "mass_energy",
            "Mass-energy equivalency",
            "Energy equals mass times a constant squared.",
            "mass",
            new StandardDimensions().Mass(),
            "energy",
            new StandardUnits().Joule().GetDimension(),
            new RationalNumber(9, 1),
            1,
            new RationalNumber(1, 9),
            1,
            new string[] { "einstein" });

        Assert.That(definition.GetId(), Is.EqualTo("mass_energy"));
        Assert.That(definition.GetName(), Is.EqualTo("Mass-energy equivalency"));
        Assert.That(definition.GetDescription(), Is.EqualTo("Energy equals mass times a constant squared."));
        Assert.That(definition.GetSourceQuantityKindId(), Is.EqualTo("mass"));
        Assert.That(definition.GetSourceDimension().EqualsDimension(new StandardDimensions().Mass()), Is.True);
        Assert.That(definition.GetTargetQuantityKindId(), Is.EqualTo("energy"));
        Assert.That(definition.GetTargetDimension().EqualsDimension(new StandardUnits().Joule().GetDimension()), Is.True);
        Assert.That(definition.GetForwardScale().EqualsValue(new RationalNumber(9, 1)), Is.True);
        Assert.That(definition.GetForwardPower(), Is.EqualTo(1));
        Assert.That(definition.GetReverseScale().EqualsValue(new RationalNumber(1, 9)), Is.True);
        Assert.That(definition.GetReversePower(), Is.EqualTo(1));
        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "einstein" }));
    }

    [Test]
    public void Constructor_CopiesAliasesDefensively()
    {
        string[] aliases = new string[] { "photon" };

        EquivalencyDefinition definition = CreateReciprocalEquivalency(aliases);

        aliases[0] = "changed";
        string[] returned = definition.GetAliases();
        returned[0] = "changed_again";

        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "photon" }));
    }

    [Test]
    public void MatchesText_ReturnsTrueForIdNameAndAliases()
    {
        EquivalencyDefinition definition = CreateReciprocalEquivalency(new string[] { "photon" });

        Assert.That(definition.MatchesText("wavelength_frequency"), Is.True);
        Assert.That(definition.MatchesText("Wavelength-frequency equivalency"), Is.True);
        Assert.That(definition.MatchesText("photon"), Is.True);
        Assert.That(definition.MatchesText("Photon"), Is.False);
        Assert.That(definition.MatchesText(""), Is.False);
        Assert.That(definition.MatchesText(null), Is.False);
    }

    [Test]
    public void CanConvert_RecognizesForwardAndReverseDimensions()
    {
        EquivalencyDefinition definition = CreateReciprocalEquivalency(new string[0]);

        Assert.That(definition.CanConvertForward(new StandardUnits().Meter(), new StandardUnits().Hertz()), Is.True);
        Assert.That(definition.CanConvertReverse(new StandardUnits().Hertz(), new StandardUnits().Meter()), Is.True);
        Assert.That(definition.CanConvert(new StandardUnits().Meter(), new StandardUnits().Hertz()), Is.True);
        Assert.That(definition.CanConvert(new StandardUnits().Hertz(), new StandardUnits().Meter()), Is.True);
        Assert.That(definition.CanConvert(new StandardUnits().Meter(), new StandardUnits().Second()), Is.False);
        Assert.That(definition.CanConvert(null, new StandardUnits().Hertz()), Is.False);
        Assert.That(definition.CanConvert(new StandardUnits().Meter(), null), Is.False);
    }

    [Test]
    public void ConvertSystemValue_AppliesLinearAndReciprocalFormulas()
    {
        EquivalencyDefinition linear = new EquivalencyDefinition(
            "mass_energy",
            "Mass-energy equivalency",
            "",
            "mass",
            new StandardDimensions().Mass(),
            "energy",
            new StandardUnits().Joule().GetDimension(),
            new RationalNumber(9, 1),
            1,
            new RationalNumber(1, 9),
            1,
            new string[0]);
        EquivalencyDefinition reciprocal = CreateReciprocalEquivalency(new string[0]);

        Assert.That(linear.ConvertForwardSystemValue(2.0d), Is.EqualTo(18.0d));
        Assert.That(linear.ConvertReverseSystemValue(18.0d), Is.EqualTo(2.0d));
        Assert.That(reciprocal.ConvertForwardSystemValue(3.0d), Is.EqualTo(4.0d).Within(0.000000000001d));
        Assert.That(reciprocal.ConvertReverseSystemValue(4.0d), Is.EqualTo(3.0d).Within(0.000000000001d));
        Assert.That(reciprocal.ConvertSystemValue(3.0d, new StandardUnits().Meter(), new StandardUnits().Hertz()), Is.EqualTo(4.0d).Within(0.000000000001d));
        Assert.That(reciprocal.ConvertSystemValue(4.0d, new StandardUnits().Hertz(), new StandardUnits().Meter()), Is.EqualTo(3.0d).Within(0.000000000001d));
    }

    [Test]
    public void ConvertSystemValue_RejectsUnsupportedDimensionsAndZeroReciprocalInput()
    {
        EquivalencyDefinition definition = CreateReciprocalEquivalency(new string[0]);

        Assert.Throws<System.InvalidOperationException>(() => definition.ConvertSystemValue(1.0d, new StandardUnits().Meter(), new StandardUnits().Second()));
        Assert.Throws<System.ArgumentException>(() => definition.ConvertForwardSystemValue(0.0d));
    }

    [Test]
    public void Constructor_RejectsMissingOrInvalidValues()
    {
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", " ", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(0, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 0, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(0, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 0, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[] { "" }));
        Assert.Throws<System.ArgumentNullException>(() => new EquivalencyDefinition("id", "Name", "", "source", null!, "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentNullException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", null!, new RationalNumber(1, 1), 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentNullException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), null!, 1, new RationalNumber(1, 1), 1, new string[0]));
        Assert.Throws<System.ArgumentNullException>(() => new EquivalencyDefinition("id", "Name", "", "source", new StandardDimensions().Length(), "target", new StandardDimensions().Time(), new RationalNumber(1, 1), 1, null!, 1, new string[0]));
    }

    [Test]
    public void UnitRegistry_AddsFindsAndFreezesEquivalencies()
    {
        UnitRegistry registry = new UnitRegistry();
        EquivalencyDefinition definition = CreateReciprocalEquivalency(new string[] { "photon" });

        OperationResult added = registry.AddEquivalency(definition);
        OperationResult duplicate = registry.AddEquivalency(CreateReciprocalEquivalency(new string[0]));

        Assert.That(added.IsSuccess(), Is.True);
        Assert.That(duplicate.IsFailure(), Is.True);
        Assert.That(duplicate.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
        Assert.That(registry.GetEquivalencies(), Is.EqualTo(new EquivalencyDefinition[] { definition }));
        Assert.That(registry.FindEquivalency("wavelength_frequency"), Is.SameAs(definition));
        Assert.That(registry.FindEquivalency("Wavelength-frequency equivalency"), Is.SameAs(definition));
        Assert.That(registry.FindEquivalency("photon"), Is.SameAs(definition));
        Assert.That(registry.FindEquivalency("missing"), Is.Null);

        registry.Freeze();

        OperationResult frozen = registry.AddEquivalency(new EquivalencyDefinition(
            "mass_energy",
            "Mass-energy equivalency",
            "",
            "mass",
            new StandardDimensions().Mass(),
            "energy",
            new StandardUnits().Joule().GetDimension(),
            new RationalNumber(9, 1),
            1,
            new RationalNumber(1, 9),
            1,
            new string[0]));

        Assert.That(frozen.IsFailure(), Is.True);
        Assert.That(frozen.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddEquivalency(null!));
    }

    static EquivalencyDefinition CreateReciprocalEquivalency(string[] aliases)
    {
        return new EquivalencyDefinition(
            "wavelength_frequency",
            "Wavelength-frequency equivalency",
            "Frequency equals wave speed divided by wavelength.",
            "length",
            new StandardDimensions().Length(),
            "frequency",
            new StandardUnits().Hertz().GetDimension(),
            new RationalNumber(12, 1),
            -1,
            new RationalNumber(12, 1),
            -1,
            aliases);
    }
}
