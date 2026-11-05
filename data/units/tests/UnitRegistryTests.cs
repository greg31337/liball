using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitRegistryTests
{
    [Test]
    public void AddUnit_StoresUnitsAndReturnsSnapshots()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition second = Second();

        OperationResult first = registry.AddUnit(meter);
        OperationResult secondResult = registry.AddUnit(second);
        UnitDefinition[] units = registry.GetUnits();
        units[0] = second;

        Assert.That(first.IsSuccess(), Is.True);
        Assert.That(secondResult.IsSuccess(), Is.True);
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter, second }));
    }

    [Test]
    public void FindUnits_FindsByIdNamePluralNameSymbolAndDefinitionAlias()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        registry.AddUnit(meter);

        Assert.That(registry.FindUnitsById("meter"), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnitsByName("meter"), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnitsByName("meters"), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnitsBySymbol("m"), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnitsByAlias("metre"), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnits("m"), Is.EqualTo(new UnitDefinition[] { meter }));
    }

    [Test]
    public void FindUnit_ReturnsSuccessForSingleUnitLookup()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        registry.AddUnit(meter);

        UnitLookupResult bySymbol = registry.FindUnit("m");
        UnitLookupResult byId = registry.FindUnitById("meter");

        Assert.That(bySymbol.IsSuccess(), Is.True);
        Assert.That(bySymbol.IsAmbiguous(), Is.False);
        Assert.That(bySymbol.GetUnit(), Is.SameAs(meter));
        Assert.That(bySymbol.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(byId.GetUnit(), Is.SameAs(meter));
    }

    [Test]
    public void FindUnit_ReturnsNotFoundResult()
    {
        UnitRegistry registry = new UnitRegistry();

        UnitLookupResult result = registry.FindUnit("missing");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.False);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void AddAlias_StoresRegistryAliasForExistingUnit()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        registry.AddUnit(meter);

        OperationResult result = registry.AddAlias("meter", "metres");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.FindUnitsByAlias("metres"), Is.EqualTo(new UnitDefinition[] { meter }));
    }

    [Test]
    public void AddAlias_ReturnsFailureForMissingUnitId()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = registry.AddAlias("missing", "alias");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
    }

    [Test]
    public void AddUnit_RejectsDuplicateUnitIds()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition duplicateMeter = new UnitDefinition(
            "meter",
            "metre",
            "metres",
            "m",
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);

        registry.AddUnit(meter);

        OperationResult result = registry.AddUnit(duplicateMeter);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter }));
    }

    [Test]
    public void AddUnit_RejectsAmbiguousSymbolReuseForNonEquivalentUnit()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition minute = Minute();
        registry.AddUnit(meter);

        OperationResult result = registry.AddUnit(minute);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryAmbiguousSymbol));
        Assert.That(registry.FindUnitsBySymbol("m"), Is.EqualTo(new UnitDefinition[] { meter }));
    }

    [Test]
    public void AddUnit_AllowsSharedSymbolForEquivalentUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition metre = AlternateMeter();
        registry.AddUnit(meter);

        OperationResult result = registry.AddUnit(metre);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.FindUnitsBySymbol("m"), Is.EqualTo(new UnitDefinition[] { meter, metre }));
    }

    [Test]
    public void FindUnit_ReturnsAmbiguousResultWithCandidates()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition metre = AlternateMeter();
        registry.AddUnit(meter);
        registry.AddUnit(metre);

        UnitLookupResult result = registry.FindUnit("m");
        UnitDefinition[] candidates = result.GetUnits();
        candidates[0] = Second();

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(result.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter, metre }));
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void Registry_StoresQuantityKindsSystemsAndConstants()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        QuantityKindDefinition quantityKind = LengthKind("length", "Length", new string[0]);
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
        PhysicalConstantDefinition constant = new PhysicalConstantDefinition("reference_length", "reference length", "r", "", "exact", "test source", "test edition", 0.0d, new QuantityValue(1.0d, meter), new string[0]);

        registry.AddUnit(meter);
        Assert.That(registry.AddQuantityKind(quantityKind).IsSuccess(), Is.True);
        Assert.That(registry.AddSystem(system).IsSuccess(), Is.True);
        Assert.That(registry.AddConstant(constant).IsSuccess(), Is.True);
        Assert.That(registry.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { quantityKind }));
        Assert.That(registry.GetSystems(), Is.EqualTo(new UnitSystemDefinition[] { system }));
        Assert.That(registry.GetConstants(), Is.EqualTo(new PhysicalConstantDefinition[] { constant }));
    }

    [Test]
    public void FindQuantityKind_ReturnsSuccessAndNotFoundResults()
    {
        UnitRegistry registry = new UnitRegistry();
        QuantityKindDefinition length = LengthKind("length", "Length", new string[0]);
        registry.AddUnit(Meter());
        registry.AddQuantityKind(length);

        QuantityKindLookupResult found = registry.FindQuantityKind("length");
        QuantityKindLookupResult missing = registry.FindQuantityKind("mass");

        Assert.That(found.IsSuccess(), Is.True);
        Assert.That(found.GetQuantityKind(), Is.SameAs(length));
        Assert.That(missing.IsFailure(), Is.True);
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.Throws<System.InvalidOperationException>(() => missing.GetQuantityKind());
    }

    [Test]
    public void FindQuantityKind_MatchesDefinitionIdNameAndAliases()
    {
        UnitRegistry registry = new UnitRegistry();
        QuantityKindDefinition length = LengthKind("length", "Length", new string[] { "distance" });
        registry.AddUnit(Meter());
        registry.AddQuantityKind(length);

        QuantityKindLookupResult byId = registry.FindQuantityKind("length");
        QuantityKindLookupResult byName = registry.FindQuantityKind("Length");
        QuantityKindLookupResult byAlias = registry.FindQuantityKind("distance");

        Assert.That(byId.IsSuccess(), Is.True);
        Assert.That(byId.GetQuantityKind(), Is.SameAs(length));
        Assert.That(byName.IsSuccess(), Is.True);
        Assert.That(byName.GetQuantityKind(), Is.SameAs(length));
        Assert.That(byAlias.IsSuccess(), Is.True);
        Assert.That(byAlias.GetQuantityKind(), Is.SameAs(length));
    }

    [Test]
    public void FindQuantityKind_DistinguishesSameDimensionDefinitions()
    {
        UnitRegistry registry = new UnitRegistry();
        QuantityKindDefinition temperature = new StandardQuantityKinds().Temperature();
        QuantityKindDefinition temperatureInterval = new StandardQuantityKinds().TemperatureInterval();
        new StandardUnits().RegisterSiBaseUnits(registry);
        new StandardUnits().RegisterTemperatureUnits(registry);
        registry.AddQuantityKind(temperature);
        registry.AddQuantityKind(temperatureInterval);

        QuantityKindLookupResult point = registry.FindQuantityKind("temperature");
        QuantityKindLookupResult interval = registry.FindQuantityKind("temperature difference");

        Assert.That(temperature.GetDimension().EqualsDimension(temperatureInterval.GetDimension()), Is.True);
        Assert.That(point.IsSuccess(), Is.True);
        Assert.That(point.GetQuantityKind(), Is.SameAs(temperature));
        Assert.That(interval.IsSuccess(), Is.True);
        Assert.That(interval.GetQuantityKind(), Is.SameAs(temperatureInterval));
    }

    [Test]
    public void FindQuantityKind_ReturnsAmbiguousWhenDefinitionsShareLookupText()
    {
        UnitRegistry registry = new UnitRegistry();
        QuantityKindDefinition length = LengthKind("length", "Length", new string[] { "distance" });
        QuantityKindDefinition alternateLength = new QuantityKindDefinition(
            "path_length",
            "Path length",
            DimensionVector.Length(),
            "meter",
            new string[] { "distance" },
            new string[0]);
        registry.AddUnit(Meter());
        registry.AddQuantityKind(length);
        registry.AddQuantityKind(alternateLength);

        QuantityKindLookupResult result = registry.FindQuantityKind("distance");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(result.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { length, alternateLength }));
        QuantityKindDefinition[] returned = result.GetQuantityKinds();
        returned[0] = alternateLength;
        Assert.That(result.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { length, alternateLength }));
    }

    [Test]
    public void TypedMetadataCatalogs_ReturnTypedSnapshotsAndLookupResults()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        QuantityKindDefinition length = LengthKind("length", "Length", new string[] { "distance" });
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
        PhysicalConstantDefinition constant = new PhysicalConstantDefinition("reference_length", "reference length", "r", "", "exact", "test source", "test edition", 0.0d, new QuantityValue(1.0d, meter), new string[0]);
        registry.AddUnit(meter);
        registry.AddQuantityKind(length);
        registry.AddSystem(system);
        registry.AddConstant(constant);

        QuantityKindDefinition[] quantityKinds = registry.GetQuantityKinds();
        UnitSystemDefinition[] systems = registry.GetSystems();
        PhysicalConstantDefinition[] constants = registry.GetConstants();
        quantityKinds[0] = LengthKind("other", "Other", new string[0]);
        systems[0] = new UnitSystemDefinition("other", "Other", new string[0], new string[0], new string[0]);
        constants[0] = new PhysicalConstantDefinition("other", "other", "o", "", "exact", "test source", "test edition", 0.0d, new QuantityValue(2.0d, meter), new string[0]);

        ILookupResultFactory factory = new LookupResultFactory();
        QuantityKindLookupResult result = factory.CreateQuantityKindResult(new QuantityKindDefinition[] { length }, "length");

        Assert.That(registry.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { length }));
        Assert.That(registry.GetSystems(), Is.EqualTo(new UnitSystemDefinition[] { system }));
        Assert.That(registry.GetConstants(), Is.EqualTo(new PhysicalConstantDefinition[] { constant }));
        Assert.That(result.GetQuantityKind(), Is.SameAs(length));
    }

    [Test]
    public void TypedMetadataCatalogs_RejectDuplicateIds()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        QuantityKindDefinition length = LengthKind("length", "Length", new string[0]);
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
        PhysicalConstantDefinition constant = new PhysicalConstantDefinition("reference_length", "reference length", "r", "", "exact", "test source", "test edition", 0.0d, new QuantityValue(1.0d, meter), new string[0]);
        registry.AddUnit(meter);
        registry.AddQuantityKind(length);
        registry.AddSystem(system);
        registry.AddConstant(constant);

        OperationResult quantityKindResult = registry.AddQuantityKind(LengthKind("length", "Other length", new string[0]));
        OperationResult systemResult = registry.AddSystem(new UnitSystemDefinition("metric", "Other metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" }));
        OperationResult constantResult = registry.AddConstant(new PhysicalConstantDefinition("reference_length", "other reference", "x", "", "measured", "test source", "test edition", 0.1d, new QuantityValue(2.0d, meter), new string[0]));

        AssertDuplicateId(quantityKindResult);
        AssertDuplicateId(systemResult);
        AssertDuplicateId(constantResult);
        Assert.That(registry.GetQuantityKinds(), Has.Length.EqualTo(1));
        Assert.That(registry.GetSystems(), Has.Length.EqualTo(1));
        Assert.That(registry.GetConstants(), Has.Length.EqualTo(1));
    }

    [Test]
    public void AddQuantityKind_RejectsMissingAcceptedUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(Meter());
        QuantityKindDefinition quantityKind = new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "meter", new string[0], new string[] { "foot" });

        OperationResult result = registry.AddQuantityKind(quantityKind);

        AssertInvalidMetadata(result, "foot");
        Assert.That(registry.GetQuantityKinds(), Is.Empty);
    }

    [Test]
    public void AddSystem_RejectsMissingAndIncompatibleRelationships()
    {
        UnitRegistry missingBaseRegistry = CreateLengthRegistry();
        UnitRegistry missingKindRegistry = CreateLengthRegistry();
        UnitRegistry missingPreferredRegistry = CreateLengthRegistry();
        UnitRegistry incompatiblePreferredRegistry = CreateLengthRegistry();
        incompatiblePreferredRegistry.AddUnit(Second());

        OperationResult missingBase = missingBaseRegistry.AddSystem(new UnitSystemDefinition("missing_base", "Missing base", new string[] { "foot" }, new string[] { "length" }, new string[] { "meter" }));
        OperationResult missingKind = missingKindRegistry.AddSystem(new UnitSystemDefinition("missing_kind", "Missing kind", new string[] { "meter" }, new string[] { "mass" }, new string[] { "meter" }));
        OperationResult missingPreferred = missingPreferredRegistry.AddSystem(new UnitSystemDefinition("missing_preferred", "Missing preferred", new string[] { "meter" }, new string[] { "length" }, new string[] { "foot" }));
        OperationResult incompatiblePreferred = incompatiblePreferredRegistry.AddSystem(new UnitSystemDefinition("incompatible", "Incompatible", new string[] { "meter" }, new string[] { "length" }, new string[] { "second" }));

        AssertInvalidMetadata(missingBase, "foot");
        AssertInvalidMetadata(missingKind, "mass");
        AssertInvalidMetadata(missingPreferred, "foot");
        AssertInvalidMetadata(incompatiblePreferred, "second");
        Assert.That(missingBaseRegistry.GetSystems(), Is.Empty);
        Assert.That(missingKindRegistry.GetSystems(), Is.Empty);
        Assert.That(missingPreferredRegistry.GetSystems(), Is.Empty);
        Assert.That(incompatiblePreferredRegistry.GetSystems(), Is.Empty);
    }

    [Test]
    public void Registry_RejectsMissingInputs()
    {
        UnitRegistry registry = new UnitRegistry();

        Assert.Throws<System.ArgumentNullException>(() => registry.AddUnit(null!));
        Assert.Throws<System.ArgumentException>(() => registry.AddAlias("", "alias"));
        Assert.Throws<System.ArgumentException>(() => registry.AddAlias("meter", ""));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddQuantityKind(null!));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddSystem(null!));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddConstant(null!));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddEquivalency(null!));
        Assert.Throws<System.ArgumentNullException>(() => registry.AddPrefix(null!));
        Assert.Throws<System.ArgumentException>(() => registry.FindUnits(""));
    }

    [Test]
    public void Freeze_BlocksLaterMutation()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        registry.AddUnit(meter);
        registry.Freeze();

        OperationResult addUnit = registry.AddUnit(Second());
        OperationResult addAlias = registry.AddAlias("meter", "metres");
        OperationResult addQuantityKind = registry.AddQuantityKind(LengthKind("length", "Length", new string[0]));
        OperationResult addSystem = registry.AddSystem(new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" }));
        OperationResult addConstant = registry.AddConstant(new PhysicalConstantDefinition("reference_length", "reference length", "r", "", "exact", "test source", "test edition", 0.0d, new QuantityValue(1.0d, meter), new string[0]));
        OperationResult addEquivalency = registry.AddEquivalency(new StandardEquivalencies().MassEnergy());
        OperationResult addPrefix = registry.AddPrefix(new PrefixDefinition("test", "test", "x", new RationalNumber(2, 1)));

        Assert.That(registry.IsFrozen(), Is.True);
        AssertFrozen(addUnit);
        AssertFrozen(addAlias);
        AssertFrozen(addQuantityKind);
        AssertFrozen(addSystem);
        AssertFrozen(addConstant);
        AssertFrozen(addEquivalency);
        AssertFrozen(addPrefix);
        Assert.That(registry.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter }));
        Assert.That(registry.FindUnitsByAlias("metres"), Is.EqualTo(new UnitDefinition[0]));
        Assert.That(registry.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[0]));
        Assert.That(registry.GetSystems(), Is.EqualTo(new UnitSystemDefinition[0]));
        Assert.That(registry.GetConstants(), Is.EqualTo(new PhysicalConstantDefinition[0]));
        Assert.That(registry.GetEquivalencies(), Is.EqualTo(new EquivalencyDefinition[0]));
        Assert.That(registry.GetPrefixes(), Is.EqualTo(new PrefixDefinition[0]));
    }

    [Test]
    public void Freeze_IsIdempotent()
    {
        UnitRegistry registry = new UnitRegistry();

        registry.Freeze();
        registry.Freeze();

        Assert.That(registry.IsFrozen(), Is.True);
    }

    static void AssertFrozen(OperationResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(result.GetMessage(), Does.Contain("frozen"));
    }

    static void AssertDuplicateId(OperationResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
    }

    static void AssertInvalidMetadata(OperationResult result, string referencedId)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(result.GetMessage(), Does.Contain(referencedId));
    }

    static UnitRegistry CreateLengthRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(Meter());
        registry.AddQuantityKind(LengthKind("length", "Length", new string[0]));
        return registry;
    }

    static QuantityKindDefinition LengthKind(string id, string name, string[] aliases)
    {
        return new QuantityKindDefinition(id, name, DimensionVector.Length(), "meter", aliases, new string[0]);
    }

    static UnitDefinition Meter()
    {
        return new UnitDefinition(
            "meter",
            "meter",
            "meters",
            "m",
            new string[] { "metre" },
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition Second()
    {
        return new UnitDefinition(
            "second",
            "second",
            "seconds",
            "s",
            new string[0],
            DimensionVector.Time(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static UnitDefinition AlternateMeter()
    {
        return new UnitDefinition(
            "metre",
            "metre",
            "metres",
            "m",
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static UnitDefinition Minute()
    {
        return new UnitDefinition(
            "minute",
            "minute",
            "minutes",
            "m",
            new string[0],
            DimensionVector.Time(),
            new RationalNumber(60, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
