using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityKindDefinitionTests
{
    [Test]
    public void Constructor_StoresQuantityKindMetadata()
    {
        DimensionVector dimension = DimensionVector.Length();

        QuantityKindDefinition definition = new QuantityKindDefinition(
            "length",
            "Length",
            dimension,
            "meter",
            new string[] { "distance", "width" },
            new string[] { "foot", "inch" });

        Assert.That(definition.GetId(), Is.EqualTo("length"));
        Assert.That(definition.GetName(), Is.EqualTo("Length"));
        Assert.That(definition.GetDimension().EqualsDimension(dimension), Is.True);
        Assert.That(definition.GetDefaultUnitId(), Is.EqualTo("meter"));
        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "distance", "width" }));
        Assert.That(definition.GetAcceptedUnitIds(), Is.EqualTo(new string[] { "foot", "inch", "meter" }));
    }

    [Test]
    public void Constructor_CopiesAliasesAndAcceptedUnitIdsDefensively()
    {
        string[] aliases = new string[] { "duration" };
        string[] acceptedUnitIds = new string[] { "second", "minute" };

        QuantityKindDefinition definition = new QuantityKindDefinition(
            "time",
            "Time",
            DimensionVector.Time(),
            "second",
            aliases,
            acceptedUnitIds);

        aliases[0] = "changed";
        acceptedUnitIds[1] = "changed";
        string[] returnedAliases = definition.GetAliases();
        string[] returnedAcceptedUnitIds = definition.GetAcceptedUnitIds();
        returnedAliases[0] = "changed_again";
        returnedAcceptedUnitIds[0] = "changed_again";

        Assert.That(definition.GetAliases(), Is.EqualTo(new string[] { "duration" }));
        Assert.That(definition.GetAcceptedUnitIds(), Is.EqualTo(new string[] { "second", "minute" }));
    }

    [Test]
    public void Constructor_AllowsNullAliasAndAcceptedUnitArrays()
    {
        QuantityKindDefinition definition = new QuantityKindDefinition(
            "mass",
            "Mass",
            DimensionVector.Mass(),
            "kilogram",
            null,
            null);

        Assert.That(definition.GetAliases(), Is.EqualTo(new string[0]));
        Assert.That(definition.GetAcceptedUnitIds(), Is.EqualTo(new string[] { "kilogram" }));
    }

    [Test]
    public void MatchesText_ReturnsTrueForIdNameAndAliases()
    {
        QuantityKindDefinition definition = new QuantityKindDefinition(
            "speed",
            "Speed",
            DimensionVector.Length().Divide(DimensionVector.Time()),
            "meter_per_second",
            new string[] { "velocity" },
            new string[] { "kilometer_per_hour" });

        Assert.That(definition.MatchesText("speed"), Is.True);
        Assert.That(definition.MatchesText("Speed"), Is.True);
        Assert.That(definition.MatchesText("velocity"), Is.True);
        Assert.That(definition.MatchesText("Velocity"), Is.False);
        Assert.That(definition.MatchesText(""), Is.False);
        Assert.That(definition.MatchesText(null), Is.False);
    }

    [Test]
    public void AcceptsUnitId_ReturnsTrueForDefaultAndAcceptedUnitIds()
    {
        QuantityKindDefinition definition = new QuantityKindDefinition(
            "area",
            "Area",
            DimensionVector.Length().Pow(2),
            "square_meter",
            new string[0],
            new string[] { "acre", "hectare" });

        Assert.That(definition.AcceptsUnitId("square_meter"), Is.True);
        Assert.That(definition.AcceptsUnitId("acre"), Is.True);
        Assert.That(definition.AcceptsUnitId("hectare"), Is.True);
        Assert.That(definition.AcceptsUnitId("meter"), Is.False);
        Assert.That(definition.AcceptsUnitId(null), Is.False);
    }

    [Test]
    public void AcceptsUnit_ReturnsTrueOnlyWhenIdAndDimensionMatch()
    {
        QuantityKindDefinition definition = new QuantityKindDefinition(
            "length",
            "Length",
            DimensionVector.Length(),
            "meter",
            new string[0],
            new string[] { "foot" });

        Assert.That(definition.AcceptsUnit(Unit("meter", DimensionVector.Length())), Is.True);
        Assert.That(definition.AcceptsUnit(Unit("foot", DimensionVector.Length())), Is.True);
        Assert.That(definition.AcceptsUnit(Unit("meter", DimensionVector.Time())), Is.False);
        Assert.That(definition.AcceptsUnit(Unit("second", DimensionVector.Time())), Is.False);
        Assert.That(definition.AcceptsUnit(null), Is.False);
    }

    [Test]
    public void Constructor_RejectsMissingRequiredMetadata()
    {
        Assert.Throws<System.ArgumentException>(() => new QuantityKindDefinition("", "Length", DimensionVector.Length(), "meter", new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityKindDefinition("length", " ", DimensionVector.Length(), "meter", new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "", new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "meter", new string[] { "" }, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "meter", new string[0], new string[] { " " }));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityKindDefinition("length", "Length", null!, "meter", new string[0], new string[0]));
    }

    static UnitDefinition Unit(string id, DimensionVector dimension)
    {
        return new UnitDefinition(
            id,
            id,
            id,
            id,
            new string[0],
            dimension,
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
