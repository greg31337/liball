using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitSystemQuantityKindIntegrationTests
{
    [Test]
    public void StandardRegistry_ResolvesEveryApplicableUnitToItsDeclaredQuantityKind()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityKindDefinition[] quantityKinds = registry.GetQuantityKinds();

        int kindIndex = 0;
        while (kindIndex < quantityKinds.Length)
        {
            QuantityKindDefinition quantityKind = quantityKinds[kindIndex];
            string[] acceptedUnitIds = quantityKind.GetAcceptedUnitIds();
            int unitIndex = 0;
            while (unitIndex < acceptedUnitIds.Length)
            {
                UnitLookupResult unitLookup = registry.FindUnitById(acceptedUnitIds[unitIndex]);
                Assert.That(unitLookup.IsSuccess(), Is.True, acceptedUnitIds[unitIndex]);

                QuantityKindLookupResult kindLookup = registry.FindQuantityKindByUnit(unitLookup.GetUnit());
                Assert.That(kindLookup.IsSuccess(), Is.True, acceptedUnitIds[unitIndex]);
                Assert.That(kindLookup.GetQuantityKind().GetId(), Is.EqualTo(quantityKind.GetId()), acceptedUnitIds[unitIndex]);
                unitIndex = unitIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [Test]
    public void StandardRegistry_DistinguishesTemperaturePointsAndIntervalsByApplicableUnit()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitDefinition kelvin = registry.FindUnitById("kelvin").GetUnit();
        UnitDefinition kelvinInterval = registry.FindUnitById("kelvin_interval").GetUnit();
        QuantityKindDefinition point = registry.FindQuantityKindByUnit(kelvin).GetQuantityKind();
        QuantityKindDefinition interval = registry.FindQuantityKindByUnit(kelvinInterval).GetQuantityKind();

        Assert.That(kelvin.GetDimension().EqualsDimension(kelvinInterval.GetDimension()), Is.True);
        Assert.That(point.GetDimension().EqualsDimension(interval.GetDimension()), Is.True);
        Assert.That(point.GetId(), Is.EqualTo("temperature"));
        Assert.That(interval.GetId(), Is.EqualTo("temperature_interval"));
        Assert.That(point.AcceptsUnit(kelvinInterval), Is.False);
        Assert.That(interval.AcceptsUnit(kelvin), Is.False);
    }

    [Test]
    public void PreferredUnitSelection_ConvertsEveryStandardQuantityKindInEveryStandardSystem()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitSystemDefinition[] systems = registry.GetSystems();

        int systemIndex = 0;
        while (systemIndex < systems.Length)
        {
            UnitSystemDefinition system = systems[systemIndex];
            string[] quantityKindIds = system.GetPreferredQuantityKindIds();
            int kindIndex = 0;
            while (kindIndex < quantityKindIds.Length)
            {
                QuantityKindDefinition quantityKind = registry.FindQuantityKind(quantityKindIds[kindIndex]).GetQuantityKind();
                UnitDefinition source = registry.FindUnitById(quantityKind.GetDefaultUnitId()).GetUnit();
                string preferredUnitId = system.FindPreferredUnitId(quantityKind.GetId())!;

                ConversionResult result = converter.ConvertToPreferredUnit(1.0d, source, quantityKind, system, registry);

                Assert.That(result.IsSuccess(), Is.True, system.GetId() + ":" + quantityKind.GetId() + ":" + result.GetMessage());
                Assert.That(result.GetUnit().GetId(), Is.EqualTo(preferredUnitId), system.GetId() + ":" + quantityKind.GetId());
                kindIndex = kindIndex + 1;
            }

            systemIndex = systemIndex + 1;
        }
    }

    [Test]
    public void CustomQuantityKindAndSystem_SupportApplicableLookupAndPreferredSelection()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition pace = ScaleLengthUnit("pace", "pc", new RationalNumber(2, 1));
        UnitDefinition stride = ScaleLengthUnit("stride", "st", new RationalNumber(1, 1));
        QuantityKindDefinition journey = new QuantityKindDefinition(
            "journey_length",
            "Journey length",
            new StandardDimensions().Length(),
            "pace",
            new string[] { "journey distance" },
            new string[] { "stride" });
        UnitSystemDefinition system = new UnitSystemDefinition(
            "stride_system",
            "Stride system",
            new string[] { "stride" },
            new string[] { "journey_length" },
            new string[] { "stride" });

        Assert.That(registry.AddUnit(pace).IsSuccess(), Is.True);
        Assert.That(registry.AddUnit(stride).IsSuccess(), Is.True);
        Assert.That(registry.AddQuantityKind(journey).IsSuccess(), Is.True);
        Assert.That(registry.AddSystem(system).IsSuccess(), Is.True);

        QuantityKindLookupResult lookup = registry.FindQuantityKindByUnit(stride);
        ConversionResult conversion = new UnitSystemConverter().ConvertToPreferredUnit(3.0d, pace, journey, system, registry);

        Assert.That(lookup.IsSuccess(), Is.True);
        Assert.That(lookup.GetQuantityKind(), Is.SameAs(journey));
        Assert.That(registry.FindQuantityKind("journey distance").GetQuantityKind(), Is.SameAs(journey));
        Assert.That(registry.FindSystem("Stride system").GetUnitSystem(), Is.SameAs(system));
        Assert.That(conversion.IsSuccess(), Is.True, conversion.GetMessage());
        Assert.That(conversion.GetValue(), Is.EqualTo(6.0d).Within(0.000000000001d));
        Assert.That(conversion.GetUnit(), Is.SameAs(stride));
    }

    [Test]
    public void CustomMetadataRegistration_RejectsUnresolvedAndIncompatibleMappingsWithoutMutation()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = ScaleLengthUnit("meter", "m", new RationalNumber(1, 1));
        UnitDefinition second = new UnitDefinition(
            "second", "second", "seconds", "s", new string[0], new StandardDimensions().Time(),
            new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        registry.AddUnit(meter);
        registry.AddUnit(second);
        QuantityKindDefinition length = new QuantityKindDefinition(
            "custom_length", "Custom length", new StandardDimensions().Length(), "meter", new string[0], new string[0]);
        registry.AddQuantityKind(length);

        OperationResult missingBase = registry.AddSystem(new UnitSystemDefinition(
            "missing_base", "Missing base", new string[] { "unresolved_base" }, new string[0], new string[0]));
        OperationResult missingKind = registry.AddSystem(new UnitSystemDefinition(
            "missing_kind", "Missing kind", new string[0], new string[] { "unresolved_kind" }, new string[] { "meter" }));
        OperationResult missingPreferredUnit = registry.AddSystem(new UnitSystemDefinition(
            "missing_preferred", "Missing preferred", new string[0], new string[] { "custom_length" }, new string[] { "unresolved_unit" }));
        OperationResult incompatiblePreferredUnit = registry.AddSystem(new UnitSystemDefinition(
            "incompatible", "Incompatible", new string[0], new string[] { "custom_length" }, new string[] { "second" }));
        QuantityKindDefinition unresolvedKind = new QuantityKindDefinition(
            "unresolved_length", "Unresolved length", new StandardDimensions().Length(), "unresolved_unit", new string[0], new string[0]);
        OperationResult missingKindUnit = registry.AddQuantityKind(unresolvedKind);

        AssertInvalidMapping(missingBase, "unresolved_base");
        AssertInvalidMapping(missingKind, "unresolved_kind");
        AssertInvalidMapping(missingPreferredUnit, "unresolved_unit");
        AssertInvalidMapping(incompatiblePreferredUnit, "second");
        AssertInvalidMapping(missingKindUnit, "unresolved_unit");
        Assert.That(registry.GetSystems(), Is.Empty);
        Assert.That(registry.GetQuantityKinds(), Is.EqualTo(new QuantityKindDefinition[] { length }));
    }

    static UnitDefinition ScaleLengthUnit(string id, string symbol, RationalNumber scale)
    {
        return new UnitDefinition(
            id,
            id,
            id + "s",
            symbol,
            new string[0],
            new StandardDimensions().Length(),
            scale,
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }

    static void AssertInvalidMapping(OperationResult result, string detail)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(result.GetMessage(), Does.Contain(detail));
    }
}
