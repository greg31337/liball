using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardRegistryTests
{
    [Test]
    public void Create_RegistersStandardDefinitionsAndFreezesRegistry()
    {
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.That(registry.IsFrozen(), Is.True);
        Assert.That(registry.GetUnits(), Has.Length.EqualTo(ExpectedUnitCount()));
        Assert.That(registry.GetPrefixes(), Has.Length.EqualTo(new StandardPrefixes().CreateCatalog().GetCount()));
        Assert.That(registry.GetQuantityKinds(), Has.Length.EqualTo(new StandardQuantityKinds().CreateCoreQuantityKinds().Length));
        Assert.That(registry.GetSystems(), Has.Length.EqualTo(new StandardUnitSystems().CreateStandardUnitSystems().Length));
        Assert.That(registry.GetEquivalencies(), Has.Length.EqualTo(new StandardEquivalencies().CreateStandardEquivalencies().Length));
        Assert.That(registry.GetConstants(), Has.Length.EqualTo(new PhysicalConstants().CreateFundamentalConstants().Length));
        Assert.That(registry.FindUnit("meter").GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(registry.FindPrefixBySymbol("k")!.GetId(), Is.EqualTo("kilo"));
        Assert.That(((QuantityKindDefinition)registry.FindQuantityKind("Length").GetQuantityKind()).GetId(), Is.EqualTo("length"));
        Assert.That(registry.FindSystem("si").GetUnitSystem().GetName(), Is.EqualTo("SI"));
        Assert.That(registry.FindEquivalency("mass_energy")!.GetName(), Is.EqualTo("Mass-energy equivalency"));
        Assert.That(registry.FindPhysicalConstant("c").GetPhysicalConstant().GetId(), Is.EqualTo("speed_of_light"));
    }

    [Test]
    public void Create_ReturnsFrozenRegistryWithValidValidationReport()
    {
        UnitRegistry registry = new StandardRegistry().Create();

        RegistryValidationReport report = registry.Validate();

        Assert.That(registry.IsFrozen(), Is.True);
        Assert.That(report.IsValid(), Is.True);
        Assert.That(report.GetIssueCount(), Is.EqualTo(0));
        Assert.That(report.GetIssues(), Is.Empty);
    }

    [Test]
    public void Create_ResolvesEveryQuantityKindDefaultAndAcceptedUnitId()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityKindDefinition[] values = registry.GetQuantityKinds();
        int kindIndex = 0;

        while (kindIndex < values.Length)
        {
            QuantityKindDefinition quantityKind = values[kindIndex];
            AssertResolvedCompatibleUnit(registry, quantityKind, quantityKind.GetDefaultUnitId());

            string[] acceptedUnitIds = quantityKind.GetAcceptedUnitIds();
            int unitIndex = 0;
            while (unitIndex < acceptedUnitIds.Length)
            {
                AssertResolvedCompatibleUnit(registry, quantityKind, acceptedUnitIds[unitIndex]);
                unitIndex = unitIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [Test]
    public void Create_ResolvesEveryUnitSystemBaseAndPreferredUnitId()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitSystemDefinition[] values = registry.GetSystems();
        int systemIndex = 0;

        while (systemIndex < values.Length)
        {
            UnitSystemDefinition system = values[systemIndex];
            AssertResolvedUnitIds(registry, system.GetBaseUnitIds(), system.GetId(), "base");

            string[] quantityKindIds = system.GetPreferredQuantityKindIds();
            string[] unitIds = system.GetPreferredUnitIds();
            int preferredIndex = 0;
            while (preferredIndex < unitIds.Length)
            {
                QuantityKindDefinition quantityKind = FindQuantityKind(registry, quantityKindIds[preferredIndex]);
                AssertResolvedCompatibleUnit(registry, quantityKind, unitIds[preferredIndex]);
                preferredIndex = preferredIndex + 1;
            }

            systemIndex = systemIndex + 1;
        }
    }

    [Test]
    public void Create_ResolvesEveryEquivalencyAndPhysicalConstantReference()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        EquivalencyDefinition[] equivalencies = registry.GetEquivalencies();
        int equivalencyIndex = 0;
        while (equivalencyIndex < equivalencies.Length)
        {
            EquivalencyDefinition equivalency = equivalencies[equivalencyIndex];
            QuantityKindDefinition source = FindQuantityKind(registry, equivalency.GetSourceQuantityKindId());
            QuantityKindDefinition target = FindQuantityKind(registry, equivalency.GetTargetQuantityKindId());
            Assert.That(
                source.GetDimension().EqualsDimension(equivalency.GetSourceDimension()),
                Is.True,
                equivalency.GetId() + " has an incompatible source quantity-kind reference.");
            Assert.That(
                target.GetDimension().EqualsDimension(equivalency.GetTargetDimension()),
                Is.True,
                equivalency.GetId() + " has an incompatible target quantity-kind reference.");
            equivalencyIndex = equivalencyIndex + 1;
        }

        UnitParser parser = new UnitParser();
        PhysicalConstantDefinition[] constants = registry.GetConstants();
        int constantIndex = 0;
        while (constantIndex < constants.Length)
        {
            PhysicalConstantDefinition constant = constants[constantIndex];
            UnitDefinition quantityUnit = constant.GetQuantity().GetUnit();
            UnitParseResult result = parser.Parse(quantityUnit.GetCanonicalExpression(), registry);
            Assert.That(result.IsSuccess(), Is.True, constant.GetId() + " has an unresolved quantity-unit expression.");
            Assert.That(result.GetUnit().IsEquivalentTo(quantityUnit), Is.True, constant.GetId() + " has incompatible quantity-unit metadata.");
            constantIndex = constantIndex + 1;
        }
    }

    [Test]
    public void Create_SupportsStandardParsingAndFormatting()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitParser parser = new UnitParser();
        UnitFormatter formatter = new UnitFormatter();

        UnitDefinition speed = parser.ParseOrThrow("km/s", registry);

        Assert.That(speed.GetId(), Is.EqualTo("(kilometer/second)"));
        Assert.That(formatter.FormatWithOptions(speed, UnitFormatOptions.CreateDecomposed()), Is.EqualTo("m/s"));
    }

    [Test]
    public void Create_ReturnsFrozenRegistryThatRejectsMutation()
    {
        UnitRegistry registry = new StandardRegistry().Create();

        OperationResult unitResult = registry.AddUnit(new StandardUnits().Meter());
        OperationResult prefixResult = registry.AddPrefix(new PrefixDefinition("test", "test", "x", new RationalNumber(1, 1)));
        OperationResult kindResult = registry.AddQuantityKind(new StandardQuantityKinds().Length());
        OperationResult systemResult = registry.AddSystem(new StandardUnitSystems().Si());
        OperationResult equivalencyResult = registry.AddEquivalency(new StandardEquivalencies().MassEnergy());

        Assert.That(unitResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(prefixResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(kindResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(systemResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.That(equivalencyResult.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
    }

    [Test]
    public void Create_ReturnsIndependentRegistryInstances()
    {
        UnitRegistry first = new StandardRegistry().Create();
        UnitRegistry second = new StandardRegistry().Create();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.GetUnits()[0], Is.Not.SameAs(second.GetUnits()[0]));
        Assert.That(first.GetPrefixes()[0], Is.Not.SameAs(second.GetPrefixes()[0]));
        Assert.That(first.GetQuantityKinds()[0], Is.Not.SameAs(second.GetQuantityKinds()[0]));
        Assert.That(first.GetSystems()[0], Is.Not.SameAs(second.GetSystems()[0]));
        Assert.That(first.GetConstants()[0], Is.Not.SameAs(second.GetConstants()[0]));
        Assert.That(first.GetEquivalencies()[0], Is.Not.SameAs(second.GetEquivalencies()[0]));
        Assert.That(first.GetUnits()[0].GetId(), Is.EqualTo(second.GetUnits()[0].GetId()));
        Assert.That(first.GetPrefixes()[0].GetId(), Is.EqualTo(second.GetPrefixes()[0].GetId()));
        Assert.That(first.GetQuantityKinds()[0].GetId(), Is.EqualTo(second.GetQuantityKinds()[0].GetId()));
        Assert.That(first.GetSystems()[0].GetId(), Is.EqualTo(second.GetSystems()[0].GetId()));
        Assert.That(first.GetConstants()[0].GetId(), Is.EqualTo(second.GetConstants()[0].GetId()));
        Assert.That(first.GetEquivalencies()[0].GetId(), Is.EqualTo(second.GetEquivalencies()[0].GetId()));
    }

    static int ExpectedUnitCount()
    {
        return new StandardUnits().CreateSiBaseUnits().Length
            + new StandardUnits().CreateSiDerivedUnits().Length
            + new StandardUnits().CreateMetricUnits().Length
            + new StandardUnits().CreateTemperatureUnits().Length
            + new StandardUnits().CreateDimensionlessUnits().Length
            + new StandardUnits().CreateCgsUnits().Length
            + new StandardUnits().CreateImperialAndUsCustomaryUnits().Length
            + new StandardUnits().CreateDataUnits().Length
            + new StandardUnits().CreateCompoundUnits().Length;
    }

    static void AssertResolvedCompatibleUnit(UnitRegistry registry, QuantityKindDefinition quantityKind, string unitId)
    {
        UnitLookupResult result = registry.FindUnitById(unitId);

        Assert.That(result.IsSuccess(), Is.True, quantityKind.GetId() + " references unresolved unit " + unitId + ".");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(quantityKind.GetDimension()), Is.True, quantityKind.GetId() + " references incompatible unit " + unitId + ".");
    }

    static void AssertResolvedUnitIds(UnitRegistry registry, string[] unitIds, string subjectId, string relationship)
    {
        int index = 0;
        while (index < unitIds.Length)
        {
            UnitLookupResult result = registry.FindUnitById(unitIds[index]);
            Assert.That(result.IsSuccess(), Is.True, subjectId + " has unresolved " + relationship + " unit " + unitIds[index] + ".");
            index = index + 1;
        }
    }

    static QuantityKindDefinition FindQuantityKind(UnitRegistry registry, string id)
    {
        QuantityKindLookupResult result = registry.FindQuantityKind(id);
        Assert.That(result.IsSuccess(), Is.True, "Quantity kind was not found: " + id);
        return result.GetQuantityKind();
    }

}
