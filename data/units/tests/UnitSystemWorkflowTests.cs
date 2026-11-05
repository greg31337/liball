using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitSystemWorkflowTests
{
    [Test]
    public void ConvertQuantityToPreferredUnit_ConvertsQuantityValueThroughSystemPreference()
    {
        IUnitSystemConverter converter = new UnitSystemConverter();
        QuantityValue quantity = new QuantityValue(12.0d, new StandardUnits().Inch());

        ConversionResult result = converter.ConvertQuantityToPreferredUnit(
            quantity,
            new StandardQuantityKinds().Length(),
            new StandardUnitSystems().Si(),
            new StandardRegistry().Create());

        AssertSuccess(result, 0.3048d, "meter");
        AssertQuantity(quantity, 12.0d, "inch");
    }

    [Test]
    public void DecomposeToBaseUnits_BuildsExactSiForceExpression()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        QuantityValue force = new QuantityValue(3.0d, new StandardUnits().Newton());

        ConversionResult result = converter.DecomposeToBaseUnits(force, new StandardUnitSystems().Si(), new StandardRegistry().Create());

        AssertSuccess(result, 3.0d, "((meter*kilogram)/(second^2))");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(force.GetUnit().GetDimension()), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void DecomposeToBaseUnits_ConvertsToCgsBaseExpressionWithExactScale()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        QuantityValue force = new QuantityValue(1.0d, new StandardUnits().Newton());

        ConversionResult result = converter.DecomposeToBaseUnits(force, new StandardUnitSystems().Cgs(), new StandardRegistry().Create());

        AssertSuccess(result, 100000.0d, "((centimeter*gram)/(second^2))");
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 100000)), Is.True);
    }

    [Test]
    public void DecomposeToBaseUnits_HandlesDenominatorOnlyAndDimensionlessExpressions()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue frequency = new QuantityValue(2.0d, new StandardUnits().Hertz());
        QuantityValue percent = new QuantityValue(50.0d, new StandardUnits().Percent());

        ConversionResult inverseTime = converter.DecomposeToBaseUnits(frequency, new StandardUnitSystems().Si(), registry);
        ConversionResult dimensionless = converter.DecomposeToBaseUnits(percent, new StandardUnitSystems().Si(), registry);

        AssertSuccess(inverseTime, 2.0d, "(1/second)");
        AssertSuccess(dimensionless, 0.5d, "one");
    }

    [Test]
    public void DecomposeToBaseUnits_PreservesTemperaturePointAndIntervalSemantics()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue point = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue interval = new QuantityValue(18.0d, new StandardUnits().FahrenheitInterval());

        ConversionResult pointResult = converter.DecomposeToBaseUnits(point, new StandardUnitSystems().Si(), registry);
        ConversionResult intervalResult = converter.DecomposeToBaseUnits(interval, new StandardUnitSystems().Si(), registry);

        AssertSuccess(pointResult, 293.15d, "kelvin");
        AssertSuccess(intervalResult, 10.0d, "kelvin_interval");
    }

    [Test]
    public void DecomposeToBaseUnits_ReturnsFailuresForMissingOrInvalidBaseMetadata()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue speed = new QuantityValue(1.0d, new StandardUnits().MeterPerSecond());
        QuantityValue force = new QuantityValue(1.0d, new StandardUnits().Newton());
        UnitSystemDefinition missingTime = new UnitSystemDefinition("length_only", "Length only", new string[] { "meter" }, new string[0], new string[0]);
        UnitSystemDefinition duplicateLength = new UnitSystemDefinition("duplicate_length", "Duplicate length", new string[] { "meter", "foot" }, new string[0], new string[0]);
        UnitSystemDefinition compoundBase = new UnitSystemDefinition("compound", "Compound", new string[] { "newton" }, new string[0], new string[0]);

        ConversionResult missing = converter.DecomposeToBaseUnits(speed, missingTime, registry);
        ConversionResult duplicate = converter.DecomposeToBaseUnits(force, duplicateLength, registry);
        ConversionResult invalid = converter.DecomposeToBaseUnits(force, compoundBase, registry);

        AssertFailure(missing, UnitErrorCode.LookupNotFound, "dimension slot");
        AssertFailure(duplicate, UnitErrorCode.RegistryInvalidMetadata, "more than one");
        AssertFailure(invalid, UnitErrorCode.RegistryInvalidMetadata, "one base dimension");
    }

    [Test]
    public void ComposeToNamedUnit_ComposesSiAndCgsBaseExpressions()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue siExpression = new QuantityValue(4.0d, new StandardUnits().Kilogram().Multiply(new StandardUnits().Meter()).Divide(new StandardUnits().Second().Pow(2)));
        ConversionResult cgsExpression = converter.DecomposeToBaseUnits(new QuantityValue(1.0d, new StandardUnits().Newton()), new StandardUnitSystems().Cgs(), registry);

        ConversionResult siNamed = converter.ComposeToNamedUnit(siExpression, new StandardUnitSystems().Si(), registry);
        ConversionResult cgsNamed = converter.ComposeToNamedUnit(new QuantityValue(cgsExpression.GetValue(), cgsExpression.GetUnit()), new StandardUnitSystems().Cgs(), registry);

        AssertSuccess(siNamed, 4.0d, "newton");
        AssertSuccess(cgsNamed, 100000.0d, "dyne");
    }

    [Test]
    public void ComposeToNamedUnit_ReturnsNotFoundForNonEquivalentOrUnnamedExpression()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        UnitDefinition scaledForce = new UnitDefinition("double_force_expression", "double force expression", "double force expressions", "dfx", new string[0], new StandardUnits().Newton().GetDimension(), new RationalNumber(2, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
        QuantityValue area = new QuantityValue(1.0d, new StandardUnits().SquareMeter());

        ConversionResult scaled = converter.ComposeToNamedUnit(new QuantityValue(1.0d, scaledForce), new StandardUnitSystems().Si(), registry);
        ConversionResult unnamed = converter.ComposeToNamedUnit(area, new StandardUnitSystems().Si(), registry);

        AssertFailure(scaled, UnitErrorCode.LookupNotFound, "named derived");
        AssertFailure(unnamed, UnitErrorCode.LookupNotFound, "named derived");
    }

    [Test]
    public void ComposeToNamedUnit_ReturnsAmbiguousFailureForEquivalentNamedPreferences()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition first = NamedForce("force_a", "fa");
        UnitDefinition second = NamedForce("force_b", "fb");
        registry.AddUnit(first);
        registry.AddUnit(second);
        UnitSystemDefinition system = new UnitSystemDefinition("custom", "Custom", new string[0], new string[] { "first_force", "second_force" }, new string[] { "force_a", "force_b" });
        QuantityValue expression = new QuantityValue(2.0d, new StandardUnits().Kilogram().Multiply(new StandardUnits().Meter()).Divide(new StandardUnits().Second().Pow(2)));

        ConversionResult result = converter.ComposeToNamedUnit(expression, system, registry);

        AssertFailure(result, UnitErrorCode.LookupAmbiguous, "More than one");
    }

    [Test]
    public void UnitSystemQuantityWorkflows_RejectMissingInputs()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());
        UnitSystemDefinition system = new StandardUnitSystems().Si();

        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertQuantityToPreferredUnit(null!, new StandardQuantityKinds().Length(), system, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertQuantityToPreferredUnit(quantity, null!, system, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertQuantityToPreferredUnit(quantity, new StandardQuantityKinds().Length(), null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertQuantityToPreferredUnit(quantity, new StandardQuantityKinds().Length(), system, null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.DecomposeToBaseUnits(null!, system, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.DecomposeToBaseUnits(quantity, null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.DecomposeToBaseUnits(quantity, system, null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.ComposeToNamedUnit(null!, system, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ComposeToNamedUnit(quantity, null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ComposeToNamedUnit(quantity, system, null!));
    }

    static UnitDefinition NamedForce(string id, string symbol)
    {
        return new UnitDefinition(id, id, id, symbol, new string[0], new StandardUnits().Newton().GetDimension(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
    }

    static void AssertSuccess(ConversionResult result, double expectedValue, string expectedUnitId)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(expectedValue).Within(0.000000001d));
        Assert.That(result.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertFailure(ConversionResult result, UnitErrorCode expectedErrorCode, string messageText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(expectedErrorCode));
        Assert.That(result.GetMessage(), Does.Contain(messageText));
        Assert.Throws<System.InvalidOperationException>(() => result.GetValue());
    }

    static void AssertQuantity(QuantityValue quantity, double expectedValue, string expectedUnitId)
    {
        Assert.That(quantity.GetValue(), Is.EqualTo(expectedValue).Within(0.000000001d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }
}
