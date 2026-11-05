using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class RegistryPublicBehaviorTests
{
    [Test]
    public void CustomRegistry_RegistersEveryCatalogAndValidatesRelationships()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition furlong = Furlong();
        UnitDefinition fortnight = Fortnight();
        PrefixDefinition doublePrefix = DoublePrefix();
        QuantityKindDefinition distance = new QuantityKindDefinition(
            "custom_distance",
            "Custom distance",
            DimensionVector.Length(),
            "furlong",
            new string[] { "journey distance" },
            new string[0]);
        QuantityKindDefinition duration = new QuantityKindDefinition(
            "custom_duration",
            "Custom duration",
            DimensionVector.Time(),
            "fortnight",
            new string[] { "journey duration" },
            new string[0]);
        UnitSystemDefinition system = new UnitSystemDefinition(
            "furlong_fortnight",
            "Furlong-fortnight system",
            new string[] { "furlong", "fortnight" },
            new string[] { "custom_distance", "custom_duration" },
            new string[] { "furlong", "fortnight" });
        PhysicalConstantDefinition constant = new PhysicalConstantDefinition(
            "reference_furlong",
            "reference furlong",
            "F0",
            "A custom exact reference length.",
            "exact",
            "test source",
            "test edition",
            0.0d,
            new QuantityValue(1.0d, furlong),
            new string[] { "journey reference" });
        EquivalencyDefinition equivalency = DistanceDurationEquivalency();

        Assert.That(registry.AddUnit(furlong).IsSuccess(), Is.True);
        Assert.That(registry.AddUnit(fortnight).IsSuccess(), Is.True);
        Assert.That(registry.AddAlias("furlong", "stadium length").IsSuccess(), Is.True);
        Assert.That(registry.AddPrefix(doublePrefix).IsSuccess(), Is.True);
        Assert.That(registry.AddQuantityKind(distance).IsSuccess(), Is.True);
        Assert.That(registry.AddQuantityKind(duration).IsSuccess(), Is.True);
        Assert.That(registry.AddSystem(system).IsSuccess(), Is.True);
        Assert.That(registry.AddConstant(constant).IsSuccess(), Is.True);
        Assert.That(registry.AddEquivalency(equivalency).IsSuccess(), Is.True);

        Assert.That(registry.FindUnit("stadium length").GetUnit(), Is.SameAs(furlong));
        Assert.That(registry.FindPrefixByName("double"), Is.SameAs(doublePrefix));
        Assert.That(registry.FindQuantityKind("journey distance").GetQuantityKind(), Is.SameAs(distance));
        Assert.That(registry.FindQuantityKindByUnit(fortnight).GetQuantityKind(), Is.SameAs(duration));
        Assert.That(registry.FindSystem("Furlong-fortnight system").GetUnitSystem(), Is.SameAs(system));
        Assert.That(registry.FindPhysicalConstant("journey reference").GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(registry.FindEquivalency("constant journey speed"), Is.SameAs(equivalency));
        Assert.That(registry.FindEquivalencyById("distance_duration"), Is.SameAs(equivalency));
        Assert.That(registry.Validate().IsValid(), Is.True);
    }

    [Test]
    public void AddPrefix_SupportsTypedLookupSnapshotsAndAllDuplicateKeys()
    {
        UnitRegistry registry = new UnitRegistry();
        PrefixDefinition prefix = DoublePrefix();

        OperationResult added = registry.AddPrefix(prefix);
        OperationResult duplicateId = registry.AddPrefix(new PrefixDefinition("double", "twice", "T", new RationalNumber(2, 1)));
        OperationResult duplicateName = registry.AddPrefix(new PrefixDefinition("other_id", "double", "O", new RationalNumber(3, 1)));
        OperationResult duplicateSymbol = registry.AddPrefix(new PrefixDefinition("another_id", "another", "D", new RationalNumber(4, 1)));
        PrefixDefinition[] snapshot = registry.GetPrefixes();
        snapshot[0] = new PrefixDefinition("replacement", "replacement", "R", new RationalNumber(5, 1));

        Assert.That(added.IsSuccess(), Is.True);
        AssertDuplicate(duplicateId);
        AssertDuplicate(duplicateName);
        AssertDuplicate(duplicateSymbol);
        Assert.That(registry.GetPrefixes(), Is.EqualTo(new PrefixDefinition[] { prefix }));
        Assert.That(registry.FindPrefixById("double"), Is.SameAs(prefix));
        Assert.That(registry.FindPrefixByName("double"), Is.SameAs(prefix));
        Assert.That(registry.FindPrefixBySymbol("D"), Is.SameAs(prefix));
        Assert.That(registry.FindPrefixById("missing"), Is.Null);
        Assert.That(registry.FindPrefixByName(null), Is.Null);
        Assert.That(registry.FindPrefixBySymbol(""), Is.Null);
    }

    [Test]
    public void GetEquivalencies_ReturnsSnapshotAndLookupUsesIdNameAndAlias()
    {
        UnitRegistry registry = new UnitRegistry();
        EquivalencyDefinition equivalency = DistanceDurationEquivalency();
        registry.AddEquivalency(equivalency);

        EquivalencyDefinition[] snapshot = registry.GetEquivalencies();
        snapshot[0] = new StandardEquivalencies().MassEnergy();

        Assert.That(registry.GetEquivalencies(), Is.EqualTo(new EquivalencyDefinition[] { equivalency }));
        Assert.That(registry.FindEquivalencyById("distance_duration"), Is.SameAs(equivalency));
        Assert.That(registry.FindEquivalency("Distance-duration equivalency"), Is.SameAs(equivalency));
        Assert.That(registry.FindEquivalency("constant journey speed"), Is.SameAs(equivalency));
        Assert.That(registry.FindEquivalency("missing"), Is.Null);
    }

    [Test]
    public void FindUnit_ReportsNamePluralAndAliasAmbiguityWithoutDuplicateCandidates()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition first = SharedLookupUnit("first", "a");
        UnitDefinition second = SharedLookupUnit("second", "b");
        registry.AddUnit(first);
        registry.AddUnit(second);
        registry.AddAlias("first", "first");

        AssertAmbiguous(registry.FindUnit("shared unit"), first, second);
        AssertAmbiguous(registry.FindUnit("shared units"), first, second);
        AssertAmbiguous(registry.FindUnit("common unit"), first, second);
        Assert.That(registry.FindUnits("first"), Is.EqualTo(new UnitDefinition[] { first }));
    }

    static void AssertDuplicate(OperationResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    static void AssertAmbiguous(UnitLookupResult result, UnitDefinition first, UnitDefinition second)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(result.GetUnits(), Is.EqualTo(new UnitDefinition[] { first, second }));
    }

    static UnitDefinition Furlong()
    {
        return new UnitDefinition(
            "furlong",
            "furlong",
            "furlongs",
            "fur",
            new string[] { "furrow length" },
            DimensionVector.Length(),
            new RationalNumber(25146, 125),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition Fortnight()
    {
        return new UnitDefinition(
            "fortnight",
            "fortnight",
            "fortnights",
            "fn",
            new string[0],
            DimensionVector.Time(),
            new RationalNumber(1209600, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static PrefixDefinition DoublePrefix()
    {
        return new PrefixDefinition("double", "double", "D", new RationalNumber(2, 1));
    }

    static EquivalencyDefinition DistanceDurationEquivalency()
    {
        return new EquivalencyDefinition(
            "distance_duration",
            "Distance-duration equivalency",
            "Relates distance and duration at a selected constant speed.",
            "custom_distance",
            DimensionVector.Length(),
            "custom_duration",
            DimensionVector.Time(),
            new RationalNumber(2, 1),
            1,
            new RationalNumber(1, 2),
            1,
            new string[] { "constant journey speed" });
    }

    static UnitDefinition SharedLookupUnit(string id, string symbol)
    {
        return new UnitDefinition(
            id,
            "shared unit",
            "shared units",
            symbol,
            new string[] { "common unit" },
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
