using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class MetadataLookupTests
{
    [Test]
    public void FindSystem_ReturnsTypedSuccessAndNotFoundResults()
    {
        IUnitRegistry registry = CreateLengthRegistry();
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
        registry.AddSystem(system);

        UnitSystemLookupResult byId = registry.FindSystem("metric");
        UnitSystemLookupResult byName = registry.FindSystem("Metric");
        UnitSystemLookupResult missing = registry.FindSystem("missing");

        Assert.That(byId.IsSuccess(), Is.True);
        Assert.That(byId.IsFailure(), Is.False);
        Assert.That(byId.IsAmbiguous(), Is.False);
        Assert.That(byId.GetUnitSystem(), Is.SameAs(system));
        Assert.That(byName.GetUnitSystem(), Is.SameAs(system));
        Assert.That(missing.IsFailure(), Is.True);
        Assert.That(missing.IsAmbiguous(), Is.False);
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(missing.GetMessage(), Does.Contain("missing"));
        Assert.Throws<System.InvalidOperationException>(() => missing.GetUnitSystem());
    }

    [Test]
    public void FindSystem_PreservesAmbiguousCandidatesAndArrayIsolation()
    {
        IUnitRegistry registry = new UnitRegistry();
        UnitSystemDefinition first = new UnitSystemDefinition("first", "Shared", new string[0], new string[0], new string[0]);
        UnitSystemDefinition second = new UnitSystemDefinition("second", "Shared", new string[0], new string[0], new string[0]);
        registry.AddSystem(first);
        registry.AddSystem(second);

        UnitSystemLookupResult result = registry.FindSystem("Shared");
        UnitSystemDefinition[] candidates = result.GetUnitSystems();
        candidates[0] = second;

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(result.GetUnitSystems(), Is.EqualTo(new UnitSystemDefinition[] { first, second }));
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnitSystem());
    }

    [Test]
    public void FindPhysicalConstant_MatchesTypedMetadataAndReportsNotFound()
    {
        IUnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        PhysicalConstantDefinition constant = Constant("reference_length", "reference length", "r", new string[] { "length reference" }, meter);
        registry.AddConstant(constant);

        PhysicalConstantLookupResult byId = registry.FindPhysicalConstant("reference_length");
        PhysicalConstantLookupResult byName = registry.FindPhysicalConstant("reference length");
        PhysicalConstantLookupResult bySymbol = registry.FindPhysicalConstant("r");
        PhysicalConstantLookupResult byAlias = registry.FindPhysicalConstant("length reference");
        PhysicalConstantLookupResult missing = registry.FindPhysicalConstant("missing");

        Assert.That(byId.GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(byName.GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(bySymbol.GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(byAlias.GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(missing.IsFailure(), Is.True);
        Assert.That(missing.IsAmbiguous(), Is.False);
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.Throws<System.InvalidOperationException>(() => missing.GetPhysicalConstant());
    }

    [Test]
    public void FindPhysicalConstant_PreservesAmbiguousCandidatesAndArrayIsolation()
    {
        IUnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        PhysicalConstantDefinition first = Constant("first", "First", "x", new string[0], meter);
        PhysicalConstantDefinition second = Constant("second", "Second", "x", new string[0], meter);
        registry.AddConstant(first);
        registry.AddConstant(second);

        PhysicalConstantLookupResult result = registry.FindPhysicalConstant("x");
        PhysicalConstantDefinition[] candidates = result.GetPhysicalConstants();
        candidates[0] = second;

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.IsAmbiguous(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(result.GetPhysicalConstants(), Is.EqualTo(new PhysicalConstantDefinition[] { first, second }));
        Assert.Throws<System.InvalidOperationException>(() => result.GetPhysicalConstant());
    }

    [Test]
    public void FindQuantityKindByUnit_ReturnsUniqueMissingAndAmbiguousResults()
    {
        IUnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Meter();
        UnitDefinition second = Second();
        QuantityKindDefinition length = LengthKind("length", "Length");
        QuantityKindDefinition distance = LengthKind("distance", "Distance");
        registry.AddUnit(meter);
        registry.AddUnit(second);
        registry.AddQuantityKind(length);

        QuantityKindLookupResult unique = registry.FindQuantityKindByUnit(meter);
        QuantityKindLookupResult missing = registry.FindQuantityKindByUnit(second);
        registry.AddQuantityKind(distance);
        QuantityKindLookupResult ambiguous = registry.FindQuantityKindByUnit(meter);

        Assert.That(unique.IsSuccess(), Is.True);
        Assert.That(unique.GetQuantityKind(), Is.SameAs(length));
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(ambiguous.IsAmbiguous(), Is.True);
        Assert.That(ambiguous.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(ambiguous.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { length, distance }));
        Assert.Throws<System.ArgumentNullException>(() => registry.FindQuantityKindByUnit(null!));
    }

    [Test]
    public void LookupResultFactory_CreatesTypedMetadataResultsWithDefensiveCopies()
    {
        UnitDefinition meter = Meter();
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[0], new string[0], new string[0]);
        PhysicalConstantDefinition constant = Constant("reference_length", "reference length", "r", new string[0], meter);
        UnitSystemDefinition[] systems = new UnitSystemDefinition[] { system };
        PhysicalConstantDefinition[] constants = new PhysicalConstantDefinition[] { constant };
        ILookupResultFactory factory = new LookupResultFactory();

        UnitSystemLookupResult systemResult = factory.CreateUnitSystemResult(systems, "metric");
        PhysicalConstantLookupResult constantResult = factory.CreatePhysicalConstantResult(constants, "reference_length");
        systems[0] = new UnitSystemDefinition("other", "Other", new string[0], new string[0], new string[0]);
        constants[0] = Constant("other", "Other", "o", new string[0], meter);

        Assert.That(systemResult.GetUnitSystem(), Is.SameAs(system));
        Assert.That(constantResult.GetPhysicalConstant(), Is.SameAs(constant));
        Assert.That(systemResult, Is.AssignableTo<IUnitSystemLookupResult>());
        Assert.That(constantResult, Is.AssignableTo<IPhysicalConstantLookupResult>());
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateUnitSystemResult(null!, "system"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreatePhysicalConstantResult(null!, "constant"));
    }

    [Test]
    public void MetadataEnumeration_IsTypedThroughRegistryContractAndReturnsSnapshots()
    {
        UnitRegistry concreteRegistry = CreateLengthRegistry();
        UnitSystemDefinition system = new UnitSystemDefinition("metric", "Metric", new string[] { "meter" }, new string[] { "length" }, new string[] { "meter" });
        PhysicalConstantDefinition constant = Constant("reference_length", "reference length", "r", new string[0], Meter());
        concreteRegistry.AddSystem(system);
        concreteRegistry.AddConstant(constant);
        IUnitRegistry registry = concreteRegistry;

        UnitSystemDefinition[] systems = registry.GetSystems();
        PhysicalConstantDefinition[] constants = registry.GetConstants();
        systems[0] = new UnitSystemDefinition("other", "Other", new string[0], new string[0], new string[0]);
        constants[0] = Constant("other", "Other", "o", new string[0], Meter());

        Assert.That(registry.GetSystems(), Is.EqualTo(new UnitSystemDefinition[] { system }));
        Assert.That(registry.GetConstants(), Is.EqualTo(new PhysicalConstantDefinition[] { constant }));
    }

    static UnitRegistry CreateLengthRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(Meter());
        registry.AddQuantityKind(LengthKind("length", "Length"));
        return registry;
    }

    static QuantityKindDefinition LengthKind(string id, string name)
    {
        return new QuantityKindDefinition(id, name, DimensionVector.Length(), "meter", new string[0], new string[0]);
    }

    static PhysicalConstantDefinition Constant(string id, string name, string symbol, string[] aliases, UnitDefinition unit)
    {
        return new PhysicalConstantDefinition(id, name, symbol, "", "exact", "test source", "test edition", 0.0d, new QuantityValue(1.0d, unit), aliases);
    }

    static UnitDefinition Meter()
    {
        return new UnitDefinition("meter", "meter", "meters", "m", new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }

    static UnitDefinition Second()
    {
        return new UnitDefinition("second", "second", "seconds", "s", new string[0], DimensionVector.Time(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }
}
