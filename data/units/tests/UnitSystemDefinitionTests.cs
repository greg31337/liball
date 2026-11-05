using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitSystemDefinitionTests
{
    [Test]
    public void Constructor_StoresSystemMetadata()
    {
        UnitSystemDefinition system = CreateSiSystem();

        Assert.That(system.GetId(), Is.EqualTo("si"));
        Assert.That(system.GetName(), Is.EqualTo("SI"));
        Assert.That(system.GetBaseUnitIds(), Is.EqualTo(new string[] { "meter", "kilogram", "second", "ampere", "kelvin", "mole", "candela" }));
        Assert.That(system.GetPreferredQuantityKindIds(), Is.EqualTo(new string[] { "length", "mass", "time", "pressure" }));
        Assert.That(system.GetPreferredUnitIds(), Is.EqualTo(new string[] { "meter", "kilogram", "second", "pascal" }));
    }

    [Test]
    public void Constructor_CopiesArraysDefensively()
    {
        string[] baseUnitIds = new string[] { "meter", "second" };
        string[] quantityKindIds = new string[] { "length", "time" };
        string[] unitIds = new string[] { "meter", "second" };

        UnitSystemDefinition system = new UnitSystemDefinition("custom", "Custom", baseUnitIds, quantityKindIds, unitIds);

        baseUnitIds[0] = "changed";
        quantityKindIds[0] = "changed";
        unitIds[0] = "changed";
        string[] returnedBaseUnitIds = system.GetBaseUnitIds();
        string[] returnedQuantityKindIds = system.GetPreferredQuantityKindIds();
        string[] returnedUnitIds = system.GetPreferredUnitIds();
        returnedBaseUnitIds[1] = "changed_again";
        returnedQuantityKindIds[1] = "changed_again";
        returnedUnitIds[1] = "changed_again";

        Assert.That(system.GetBaseUnitIds(), Is.EqualTo(new string[] { "meter", "second" }));
        Assert.That(system.GetPreferredQuantityKindIds(), Is.EqualTo(new string[] { "length", "time" }));
        Assert.That(system.GetPreferredUnitIds(), Is.EqualTo(new string[] { "meter", "second" }));
    }

    [Test]
    public void Constructor_AllowsNullArraysAsEmptyLists()
    {
        UnitSystemDefinition system = new UnitSystemDefinition("empty", "Empty", null, null, null);

        Assert.That(system.GetBaseUnitIds(), Is.EqualTo(new string[0]));
        Assert.That(system.GetPreferredQuantityKindIds(), Is.EqualTo(new string[0]));
        Assert.That(system.GetPreferredUnitIds(), Is.EqualTo(new string[0]));
    }

    [Test]
    public void UsesBaseUnitId_ReturnsTrueOnlyForRegisteredBaseUnitIds()
    {
        UnitSystemDefinition system = CreateSiSystem();

        Assert.That(system.UsesBaseUnitId("meter"), Is.True);
        Assert.That(system.UsesBaseUnitId("candela"), Is.True);
        Assert.That(system.UsesBaseUnitId("foot"), Is.False);
        Assert.That(system.UsesBaseUnitId(null), Is.False);
    }

    [Test]
    public void PreferredUnitLookup_ReturnsConfiguredPreferredUnits()
    {
        UnitSystemDefinition system = CreateSiSystem();

        Assert.That(system.HasPreferredUnitFor("length"), Is.True);
        Assert.That(system.FindPreferredUnitId("length"), Is.EqualTo("meter"));
        Assert.That(system.FindPreferredUnitId("pressure"), Is.EqualTo("pascal"));
        Assert.That(system.HasPreferredUnitFor("volume"), Is.False);
        Assert.That(system.FindPreferredUnitId("volume"), Is.Null);
        Assert.That(system.FindPreferredUnitId(null), Is.Null);
    }

    [Test]
    public void Constructor_RejectsMissingRequiredMetadata()
    {
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("", "SI", new string[0], new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", " ", new string[0], new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", "SI", new string[] { "" }, new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", "SI", new string[0], new string[] { "length" }, new string[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", "SI", new string[0], new string[0], new string[] { "meter" }));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", "SI", new string[0], new string[] { "" }, new string[] { "meter" }));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("si", "SI", new string[0], new string[] { "length" }, new string[] { "" }));
    }

    [Test]
    public void Constructor_RejectsDuplicateBaseAndPreferredQuantityKindIds()
    {
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("bad", "Bad", new string[] { "meter", "meter" }, new string[0], new string[0]));
        Assert.Throws<System.ArgumentException>(() => new UnitSystemDefinition("bad", "Bad", new string[0], new string[] { "length", "length" }, new string[] { "meter", "foot" }));
    }

    static UnitSystemDefinition CreateSiSystem()
    {
        return new UnitSystemDefinition(
            "si",
            "SI",
            new string[] { "meter", "kilogram", "second", "ampere", "kelvin", "mole", "candela" },
            new string[] { "length", "mass", "time", "pressure" },
            new string[] { "meter", "kilogram", "second", "pascal" });
    }
}
