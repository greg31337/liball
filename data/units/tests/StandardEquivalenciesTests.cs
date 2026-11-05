using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardEquivalenciesTests
{
    [Test]
    public void CreateStandardEquivalencies_ReturnsInitialEquivalenciesInStableOrder()
    {
        EquivalencyDefinition[] equivalencies = new StandardEquivalencies().CreateStandardEquivalencies();

        Assert.That(equivalencies.Length, Is.EqualTo(2));
        Assert.That(equivalencies[0].GetId(), Is.EqualTo("wavelength_frequency"));
        Assert.That(equivalencies[0].GetSourceQuantityKindId(), Is.EqualTo("length"));
        Assert.That(equivalencies[0].GetTargetQuantityKindId(), Is.EqualTo("frequency"));
        Assert.That(equivalencies[0].GetForwardScale().EqualsValue(new RationalNumber(299792458, 1)), Is.True);
        Assert.That(equivalencies[0].GetForwardPower(), Is.EqualTo(-1));
        Assert.That(equivalencies[0].GetReversePower(), Is.EqualTo(-1));
        Assert.That(equivalencies[0].GetAliases(), Is.EqualTo(new string[] { "photon", "light" }));

        Assert.That(equivalencies[1].GetId(), Is.EqualTo("mass_energy"));
        Assert.That(equivalencies[1].GetSourceQuantityKindId(), Is.EqualTo("mass"));
        Assert.That(equivalencies[1].GetTargetQuantityKindId(), Is.EqualTo("energy"));
        Assert.That(equivalencies[1].GetForwardScale().EqualsValue(new RationalNumber(89875517873681764, 1)), Is.True);
        Assert.That(equivalencies[1].GetReverseScale().EqualsValue(new RationalNumber(1, 89875517873681764)), Is.True);
        Assert.That(equivalencies[1].GetAliases(), Is.EqualTo(new string[] { "einstein" }));
    }

    [Test]
    public void RegisterStandardEquivalencies_AddsDefinitionsToRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        OperationResult result = new StandardEquivalencies().RegisterStandardEquivalencies(registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(registry.GetEquivalencies(), Has.Length.EqualTo(2));
        Assert.That(registry.FindEquivalency("photon")!.GetId(), Is.EqualTo("wavelength_frequency"));
        Assert.That(registry.FindEquivalency("einstein")!.GetId(), Is.EqualTo("mass_energy"));
    }

    [Test]
    public void RegisterStandardEquivalencies_ReportsDuplicateOrFrozenRegistry()
    {
        UnitRegistry duplicateRegistry = new UnitRegistry();
        UnitRegistry frozenRegistry = new UnitRegistry();

        OperationResult first = new StandardEquivalencies().RegisterStandardEquivalencies(duplicateRegistry);
        OperationResult duplicate = new StandardEquivalencies().RegisterStandardEquivalencies(duplicateRegistry);
        frozenRegistry.Freeze();
        OperationResult frozen = new StandardEquivalencies().RegisterStandardEquivalencies(frozenRegistry);

        Assert.That(first.IsSuccess(), Is.True);
        Assert.That(duplicate.IsFailure(), Is.True);
        Assert.That(duplicate.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryDuplicateId));
        Assert.That(frozen.IsFailure(), Is.True);
        Assert.That(frozen.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryFrozen));
        Assert.Throws<System.ArgumentNullException>(() => new StandardEquivalencies().RegisterStandardEquivalencies(null!));
    }

    [Test]
    public void EquivalencyConverter_ConvertsWavelengthAndFrequencyWhenEquivalencyIsExplicit()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        EquivalencyDefinition equivalency = new StandardEquivalencies().WavelengthFrequency();
        UnitDefinition nanometer = Nanometer();

        ConversionResult frequency = converter.Convert(new QuantityValue(500.0d, nanometer), new StandardUnits().Hertz(), equivalency);
        ConversionResult wavelength = converter.Convert(new QuantityValue(599584916000000.0d, new StandardUnits().Hertz()), nanometer, equivalency);

        Assert.That(frequency.IsSuccess(), Is.True, frequency.GetMessage());
        Assert.That(frequency.GetValue(), Is.EqualTo(599584916000000.0d).Within(1.0d));
        Assert.That(frequency.GetUnit().GetId(), Is.EqualTo("hertz"));
        Assert.That(wavelength.IsSuccess(), Is.True, wavelength.GetMessage());
        Assert.That(wavelength.GetValue(), Is.EqualTo(500.0d).Within(0.000000001d));
        Assert.That(wavelength.GetUnit().GetId(), Is.EqualTo("nanometer"));
    }

    [Test]
    public void EquivalencyConverter_ConvertsMassAndEnergyWhenEquivalencyIsExplicit()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        EquivalencyDefinition equivalency = new StandardEquivalencies().MassEnergy();

        ConversionResult energy = converter.Convert(new QuantityValue(1.0d, new StandardUnits().Kilogram()), new StandardUnits().Joule(), equivalency);
        ConversionResult mass = converter.Convert(new QuantityValue(89875517873681764.0d, new StandardUnits().Joule()), new StandardUnits().Kilogram(), equivalency);

        Assert.That(energy.IsSuccess(), Is.True, energy.GetMessage());
        Assert.That(energy.GetValue(), Is.EqualTo(89875517873681764.0d).Within(1.0d));
        Assert.That(energy.GetUnit().GetId(), Is.EqualTo("joule"));
        Assert.That(mass.IsSuccess(), Is.True, mass.GetMessage());
        Assert.That(mass.GetValue(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(mass.GetUnit().GetId(), Is.EqualTo("kilogram"));
    }

    [Test]
    public void EquivalencyConverter_CanResolveEquivalencyFromRegistryById()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        EquivalencyConverter converter = new EquivalencyConverter();

        ConversionResult result = converter.ConvertById(new QuantityValue(1.0d, new StandardUnits().Kilogram()), new StandardUnits().Joule(), registry, "mass_energy");

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(89875517873681764.0d).Within(1.0d));
    }

    [Test]
    public void EquivalencyConverter_ReturnsFailuresForUnsupportedRequests()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        EquivalencyDefinition equivalency = new StandardEquivalencies().WavelengthFrequency();
        UnitRegistry registry = new StandardRegistry().Create();

        ConversionResult incompatible = converter.Convert(new QuantityValue(1.0d, new StandardUnits().Kilogram()), new StandardUnits().Hertz(), equivalency);
        ConversionResult zeroReciprocal = converter.Convert(new QuantityValue(0.0d, new StandardUnits().Meter()), new StandardUnits().Hertz(), equivalency);
        ConversionResult missing = converter.ConvertById(new QuantityValue(1.0d, new StandardUnits().Kilogram()), new StandardUnits().Joule(), registry, "missing");

        Assert.That(incompatible.IsFailure(), Is.True);
        Assert.That(incompatible.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(zeroReciprocal.IsFailure(), Is.True);
        Assert.That(zeroReciprocal.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(missing.IsFailure(), Is.True);
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
    }

    [Test]
    public void UnitConverter_DoesNotApplyStandardEquivalenciesImplicitly()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitConverter converter = new UnitConverter();

        ConversionResult massToEnergy = converter.Convert(1.0d, new StandardUnits().Kilogram(), new StandardUnits().Joule());
        ConversionResult lengthToFrequency = converter.Convert(500.0d, Nanometer(), new StandardUnits().Hertz());

        Assert.That(registry.FindEquivalency("mass_energy"), Is.Not.Null);
        Assert.That(registry.FindEquivalency("wavelength_frequency"), Is.Not.Null);
        Assert.That(massToEnergy.IsFailure(), Is.True);
        Assert.That(massToEnergy.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(lengthToFrequency.IsFailure(), Is.True);
        Assert.That(lengthToFrequency.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    [Test]
    public void QuantityValue_DoesNotApplyStandardEquivalenciesImplicitly()
    {
        QuantityValue mass = new QuantityValue(1.0d, new StandardUnits().Kilogram());
        QuantityValue wavelength = new QuantityValue(500.0d, Nanometer());

        ConversionResult massToEnergy = mass.ConvertTo(new StandardUnits().Joule());
        ConversionResult lengthToFrequency = wavelength.ConvertTo(new StandardUnits().Hertz());

        Assert.That(massToEnergy.IsFailure(), Is.True);
        Assert.That(massToEnergy.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(lengthToFrequency.IsFailure(), Is.True);
        Assert.That(lengthToFrequency.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.Throws<System.InvalidOperationException>(() => mass.ToUnit(new StandardUnits().Joule()));
        Assert.Throws<System.InvalidOperationException>(() => wavelength.ToUnit(new StandardUnits().Hertz()));
    }

    [Test]
    public void EquivalencyConverter_RejectsMissingInputs()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(null!, new StandardUnits().Hertz(), new StandardEquivalencies().WavelengthFrequency()));
        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(quantity, null!, new StandardEquivalencies().WavelengthFrequency()));
        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(quantity, new StandardUnits().Hertz(), null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertById(quantity, new StandardUnits().Hertz(), null!, "mass_energy"));
        Assert.Throws<System.ArgumentException>(() => converter.ConvertById(quantity, new StandardUnits().Hertz(), registry, ""));
    }

    static UnitDefinition Nanometer()
    {
        return new UnitDefinition(
            "nanometer",
            "nanometer",
            "nanometers",
            "nm",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(1, 1000000000),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
