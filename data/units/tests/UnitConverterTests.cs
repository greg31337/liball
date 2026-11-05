using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitConverterTests
{
    [Test]
    public void Convert_ConvertsMetricLengthThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult kilometers = converter.Convert(1500.0d, Meter(), Kilometer());
        ConversionResult centimeters = converter.Convert(1.25d, Meter(), Centimeter());

        AssertSuccess(kilometers, 1.5d, Kilometer());
        AssertSuccess(centimeters, 125.0d, Centimeter());
    }

    [Test]
    public void Convert_ConvertsMetricMassThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult kilograms = converter.Convert(5000.0d, Gram(), Kilogram());
        ConversionResult grams = converter.Convert(2.5d, Kilogram(), Gram());

        AssertSuccess(kilograms, 5.0d, Kilogram());
        AssertSuccess(grams, 2500.0d, Gram());
    }

    [Test]
    public void Convert_ConvertsTimeThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult minutes = converter.Convert(180.0d, Second(), Minute());
        ConversionResult seconds = converter.Convert(2.5d, Minute(), Second());

        AssertSuccess(minutes, 3.0d, Minute());
        AssertSuccess(seconds, 150.0d, Second());
    }

    [Test]
    public void Convert_ConvertsAreaThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult hectares = converter.Convert(25000.0d, SquareMeter(), Hectare());
        ConversionResult squareMeters = converter.Convert(1.2d, Hectare(), SquareMeter());

        AssertSuccess(hectares, 2.5d, Hectare());
        AssertSuccess(squareMeters, 12000.0d, SquareMeter());
    }

    [Test]
    public void Convert_ConvertsVolumeThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult liters = converter.Convert(0.75d, CubicMeter(), Liter());
        ConversionResult cubicMeters = converter.Convert(250.0d, Liter(), CubicMeter());

        AssertSuccess(liters, 750.0d, Liter());
        AssertSuccess(cubicMeters, 0.25d, CubicMeter());
    }

    [Test]
    public void Convert_ConvertsSpeedThroughSystemUnit()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult kilometersPerHour = converter.Convert(10.0d, MeterPerSecond(), KilometerPerHour());
        ConversionResult metersPerSecond = converter.Convert(72.0d, KilometerPerHour(), MeterPerSecond());

        AssertSuccess(kilometersPerHour, 36.0d, KilometerPerHour());
        AssertSuccess(metersPerSecond, 20.0d, MeterPerSecond());
    }

    [Test]
    public void CanConvert_ReturnsTrueForSupportedScaleAndAffinePlans()
    {
        UnitConverter converter = new UnitConverter();

        Assert.That(converter.CanConvert(Meter(), Kilometer()), Is.True);
        Assert.That(converter.CanConvert(Celsius(), Kelvin()), Is.True);
        Assert.That(converter.CanConvert(Celsius(), CelsiusInterval()), Is.False);
        Assert.That(converter.CanConvert(null, Kilometer()), Is.False);
        Assert.That(converter.CanConvert(Meter(), null), Is.False);
    }

    [Test]
    public void Convert_ConvertsAffineTemperaturePoints()
    {
        UnitConverter converter = new UnitConverter();

        AssertSuccess(converter.Convert(0.0d, Celsius(), Kelvin()), 273.15d, Kelvin());
        AssertSuccess(converter.Convert(273.15d, Kelvin(), Celsius()), 0.0d, Celsius());
        AssertSuccess(converter.Convert(100.0d, Celsius(), Fahrenheit()), 212.0d, Fahrenheit());
        AssertSuccess(converter.Convert(32.0d, Fahrenheit(), Celsius()), 0.0d, Celsius());
        AssertSuccess(converter.Convert(300.0d, Kelvin(), Rankine()), 540.0d, Rankine());
        AssertSuccess(converter.Convert(540.0d, Rankine(), Kelvin()), 300.0d, Kelvin());
    }

    [Test]
    public void Convert_SeparatesTemperaturePointsFromTemperatureIntervals()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult pointToInterval = converter.Convert(0.0d, Celsius(), CelsiusInterval());
        ConversionResult intervalToPoint = converter.Convert(10.0d, CelsiusInterval(), Celsius());

        Assert.That(pointToInterval.IsFailure(), Is.True);
        Assert.That(pointToInterval.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionInvalidAffineOperation));
        Assert.That(pointToInterval.GetMessage(), Does.Contain("points"));
        Assert.That(intervalToPoint.IsFailure(), Is.True);
        Assert.That(intervalToPoint.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionInvalidAffineOperation));
    }

    [Test]
    public void Convert_ConvertsTemperatureIntervalsWithScaleOnlyMath()
    {
        UnitConverter converter = new UnitConverter();

        AssertSuccess(converter.Convert(10.0d, CelsiusInterval(), FahrenheitInterval()), 18.0d, FahrenheitInterval());
        AssertSuccess(converter.Convert(18.0d, FahrenheitInterval(), CelsiusInterval()), 10.0d, CelsiusInterval());
        AssertSuccess(converter.Convert(10.0d, KelvinInterval(), RankineInterval()), 18.0d, RankineInterval());
    }

    [Test]
    public void Convert_RejectsIncompatibleDimensions()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult result = converter.Convert(1.0d, Meter(), Second());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(result.GetMessage(), Does.Contain("incompatible"));
        Assert.Throws<System.InvalidOperationException>(() => result.GetValue());
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void CanConvert_ReturnsFalseForIncompatibleDimensions()
    {
        UnitConverter converter = new UnitConverter();

        Assert.That(converter.CanConvert(Meter(), Second()), Is.False);
        Assert.That(converter.CanConvert(Kilogram(), Liter()), Is.False);
    }

    [Test]
    public void GetPlan_ReturnsIncompatibleDimensionError()
    {
        UnitConverter converter = new UnitConverter();

        ConversionPlan plan = converter.GetPlan(Meter(), Second());

        Assert.That(plan.IsUnsupported(), Is.True);
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Unsupported));
        Assert.That(plan.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    [Test]
    public void GetPlan_ReturnsCachedConversionDetails()
    {
        UnitConverter converter = new UnitConverter();

        ConversionPlan plan = converter.GetPlan(Meter(), Kilometer());

        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale));
        Assert.That(plan.GetValueScale().EqualsValue(new RationalNumber(1, 1000)), Is.True);
        Assert.That(plan.GetValueOffset().EqualsValue(new RationalNumber(0, 1)), Is.True);
    }

    static void AssertSuccess(ConversionResult result, double expectedValue, UnitDefinition expectedUnit)
    {
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(expectedValue).Within(0.000000001d));
        Assert.That(result.GetUnit().IsEquivalentTo(expectedUnit), Is.True);
    }

    static UnitDefinition Meter()
    {
        return Unit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1));
    }

    static UnitDefinition Kilometer()
    {
        return Unit("kilometer", "kilometer", "kilometers", "km", DimensionVector.Length(), new RationalNumber(1000, 1));
    }

    static UnitDefinition Centimeter()
    {
        return Unit("centimeter", "centimeter", "centimeters", "cm", DimensionVector.Length(), new RationalNumber(1, 100));
    }

    static UnitDefinition Kilogram()
    {
        return Unit("kilogram", "kilogram", "kilograms", "kg", DimensionVector.Mass(), new RationalNumber(1, 1));
    }

    static UnitDefinition Gram()
    {
        return Unit("gram", "gram", "grams", "g", DimensionVector.Mass(), new RationalNumber(1, 1000));
    }

    static UnitDefinition Second()
    {
        return Unit("second", "second", "seconds", "s", DimensionVector.Time(), new RationalNumber(1, 1));
    }

    static UnitDefinition Minute()
    {
        return Unit("minute", "minute", "minutes", "min", DimensionVector.Time(), new RationalNumber(60, 1));
    }

    static UnitDefinition SquareMeter()
    {
        return Unit("square_meter", "square meter", "square meters", "m^2", DimensionVector.Length().Pow(2), new RationalNumber(1, 1));
    }

    static UnitDefinition Hectare()
    {
        return Unit("hectare", "hectare", "hectares", "ha", DimensionVector.Length().Pow(2), new RationalNumber(10000, 1));
    }

    static UnitDefinition CubicMeter()
    {
        return Unit("cubic_meter", "cubic meter", "cubic meters", "m^3", DimensionVector.Length().Pow(3), new RationalNumber(1, 1));
    }

    static UnitDefinition Liter()
    {
        return Unit("liter", "liter", "liters", "L", DimensionVector.Length().Pow(3), new RationalNumber(1, 1000));
    }

    static UnitDefinition MeterPerSecond()
    {
        return Unit("meter_per_second", "meter per second", "meters per second", "m/s", DimensionVector.Length().Divide(DimensionVector.Time()), new RationalNumber(1, 1));
    }

    static UnitDefinition KilometerPerHour()
    {
        return Unit("kilometer_per_hour", "kilometer per hour", "kilometers per hour", "km/h", DimensionVector.Length().Divide(DimensionVector.Time()), new RationalNumber(5, 18));
    }

    static UnitDefinition Kelvin()
    {
        return new UnitDefinition("kelvin", "kelvin", "kelvins", "K", new string[0], DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Affine);
    }

    static UnitDefinition Celsius()
    {
        return new UnitDefinition("degree_celsius", "degree Celsius", "degrees Celsius", "degC", new string[0], DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1), new RationalNumber(27315, 100), false, UnitConversionKind.Affine);
    }

    static UnitDefinition Fahrenheit()
    {
        return new UnitDefinition("degree_fahrenheit", "degree Fahrenheit", "degrees Fahrenheit", "degF", new string[0], DimensionVector.ThermodynamicTemperature(), new RationalNumber(5, 9), new RationalNumber(45967, 180), false, UnitConversionKind.Affine);
    }

    static UnitDefinition Rankine()
    {
        return new UnitDefinition("rankine", "degree Rankine", "degrees Rankine", "degR", new string[0], DimensionVector.ThermodynamicTemperature(), new RationalNumber(5, 9), new RationalNumber(0, 1), false, UnitConversionKind.Affine);
    }

    static UnitDefinition KelvinInterval()
    {
        return Unit("kelvin_interval", "kelvin interval", "kelvin intervals", "delta_K", DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1));
    }

    static UnitDefinition CelsiusInterval()
    {
        return Unit("celsius_interval", "Celsius interval", "Celsius intervals", "delta_degC", DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1));
    }

    static UnitDefinition FahrenheitInterval()
    {
        return Unit("fahrenheit_interval", "Fahrenheit interval", "Fahrenheit intervals", "delta_degF", DimensionVector.ThermodynamicTemperature(), new RationalNumber(5, 9));
    }

    static UnitDefinition RankineInterval()
    {
        return Unit("rankine_interval", "Rankine interval", "Rankine intervals", "delta_degR", DimensionVector.ThermodynamicTemperature(), new RationalNumber(5, 9));
    }

    static UnitDefinition Unit(string id, string name, string pluralName, string symbol, DimensionVector dimension, RationalNumber scale)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            new string[0],
            dimension,
            scale,
            new RationalNumber(0, 1),
            true,
            UnitConversionKind.Scale);
    }
}
