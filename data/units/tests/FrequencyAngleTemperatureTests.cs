using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class FrequencyAngleTemperatureTests
{
    [Test]
    public void FrequencyFactories_ConstructCommonFrequencyUnits()
    {
        Frequency hertz = Frequency.FromHertz(2000.0d);
        Frequency kilohertz = Frequency.FromKilohertz(2.0d);
        Frequency megahertz = Frequency.FromMegahertz(1.5d);

        Assert.That(hertz.GetUnit().GetId(), Is.EqualTo("hertz"));
        Assert.That(hertz.GetUnit().GetDimension().EqualsDimension(new StandardUnits().Hertz().GetDimension()), Is.True);
        Assert.That(hertz.Kilohertz(), Is.EqualTo(2.0d));
        Assert.That(kilohertz.Hertz(), Is.EqualTo(2000.0d));
        Assert.That(megahertz.Hertz(), Is.EqualTo(1500000.0d));
    }

    [Test]
    public void FrequencyToUnitArithmeticAndBridge_ConvertAcrossFrequencyUnits()
    {
        Frequency hertz = Frequency.FromHertz(500.0d);
        Frequency kilohertz = Frequency.FromKilohertz(1.0d);
        Frequency negativeMegahertz = Frequency.FromMegahertz(-2.0d);

        Frequency converted = kilohertz.ToUnit(new StandardUnits().Hertz());
        Frequency sum = hertz.Add(kilohertz);
        Frequency difference = Frequency.FromKilohertz(2.0d).Subtract(Frequency.FromHertz(500.0d));
        Frequency scaled = negativeMegahertz.Scale(2.0d);
        Frequency negated = negativeMegahertz.Negate();
        Frequency absolute = negativeMegahertz.AbsoluteValue();
        QuantityValue quantity = negativeMegahertz.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("hertz"));
        Assert.That(converted.GetValue(), Is.EqualTo(1000.0d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("hertz"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("kilo_hertz"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.5d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativeMegahertz.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("mega_hertz"));
    }

    [Test]
    public void Frequency_RejectsMissingOrNonFrequencyInputs()
    {
        Frequency frequency = Frequency.FromHertz(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Frequency(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Frequency(1.0d, new StandardUnits().Second()));
        Assert.Throws<System.ArgumentNullException>(() => frequency.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => frequency.Subtract(null!));
    }

    [Test]
    public void AngleFactoriesArithmeticAndBridge_UseRadians()
    {
        Angle radians = Angle.FromRadians(-1.5d);
        Angle converted = radians.ToUnit(new StandardUnits().Radian());
        Angle sum = Angle.FromRadians(1.0d).Add(Angle.FromRadians(2.0d));
        Angle difference = Angle.FromRadians(3.0d).Subtract(Angle.FromRadians(1.0d));
        Angle scaled = radians.Scale(2.0d);
        Angle negated = radians.Negate();
        Angle absolute = radians.AbsoluteValue();
        QuantityValue quantity = radians.ToQuantityValue();

        Assert.That(radians.GetUnit().GetId(), Is.EqualTo("radian"));
        Assert.That(radians.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().PlaneAngle()), Is.True);
        Assert.That(converted.Radians(), Is.EqualTo(-1.5d));
        Assert.That(sum.GetValue(), Is.EqualTo(3.0d));
        Assert.That(difference.GetValue(), Is.EqualTo(2.0d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-3.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(1.5d));
        Assert.That(absolute.GetValue(), Is.EqualTo(1.5d));
        Assert.That(radians.Abs().GetValue(), Is.EqualTo(1.5d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-1.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("radian"));
    }

    [Test]
    public void Angle_RejectsMissingOrNonAngleInputs()
    {
        Angle angle = Angle.FromRadians(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Angle(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Angle(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => angle.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => angle.Subtract(null!));
    }

    [Test]
    public void TemperatureFactories_ConvertAffineTemperaturePoints()
    {
        Temperature kelvins = Temperature.FromKelvins(300.0d);
        Temperature celsius = Temperature.FromCelsius(0.0d);
        Temperature fahrenheit = Temperature.FromFahrenheit(32.0d);
        Temperature rankine = Temperature.FromRankine(540.0d);

        Assert.That(kelvins.GetUnit().GetId(), Is.EqualTo("kelvin"));
        Assert.That(kelvins.GetUnit().GetConversionKind(), Is.EqualTo(UnitConversionKind.Affine));
        Assert.That(celsius.Kelvins(), Is.EqualTo(273.15d).Within(0.000000000001d));
        Assert.That(fahrenheit.Celsius(), Is.EqualTo(0.0d).Within(0.000000000001d));
        Assert.That(rankine.Kelvins(), Is.EqualTo(300.0d).Within(0.000000000001d));
        Assert.That(Temperature.FromCelsius(100.0d).Fahrenheit(), Is.EqualTo(212.0d).Within(0.000000000001d));
    }

    [Test]
    public void TemperatureIntervalArithmetic_AdjustsTemperaturePoints()
    {
        Temperature celsius = Temperature.FromCelsius(20.0d);
        TemperatureInterval tenCelsius = TemperatureInterval.FromCelsius(10.0d);
        Temperature fahrenheit = Temperature.FromFahrenheit(68.0d);

        Temperature increased = celsius.Add(tenCelsius);
        Temperature decreased = celsius.Subtract(tenCelsius);
        TemperatureInterval difference = Temperature.FromCelsius(30.0d).Difference(fahrenheit);
        QuantityValue quantity = celsius.ToQuantityValue();

        Assert.That(increased.GetUnit().GetId(), Is.EqualTo("degree_celsius"));
        Assert.That(increased.Celsius(), Is.EqualTo(30.0d).Within(0.000000000001d));
        Assert.That(decreased.Celsius(), Is.EqualTo(10.0d).Within(0.000000000001d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("kelvin_interval"));
        Assert.That(difference.Celsius(), Is.EqualTo(10.0d).Within(0.000000000001d));
        Assert.That(quantity.GetValue(), Is.EqualTo(20.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("degree_celsius"));
    }

    [Test]
    public void Temperature_RejectsMissingOrIntervalUnits()
    {
        Temperature temperature = Temperature.FromKelvins(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Temperature(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Temperature(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentException>(() => new Temperature(1.0d, TemperatureInterval.KelvinUnit()));
        Assert.Throws<System.ArgumentNullException>(() => temperature.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => temperature.Subtract(null!));
        Assert.Throws<System.ArgumentNullException>(() => temperature.Difference(null!));
    }

    [Test]
    public void TemperatureIntervalFactories_ConvertScaleOnlyIntervals()
    {
        TemperatureInterval kelvins = TemperatureInterval.FromKelvins(10.0d);
        TemperatureInterval celsius = TemperatureInterval.FromCelsius(10.0d);
        TemperatureInterval fahrenheit = TemperatureInterval.FromFahrenheit(18.0d);
        TemperatureInterval rankine = TemperatureInterval.FromRankine(18.0d);

        Assert.That(kelvins.GetUnit().GetId(), Is.EqualTo("kelvin_interval"));
        Assert.That(kelvins.GetUnit().GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale));
        Assert.That(kelvins.Celsius(), Is.EqualTo(10.0d));
        Assert.That(celsius.Fahrenheit(), Is.EqualTo(18.0d).Within(0.000000000001d));
        Assert.That(fahrenheit.Celsius(), Is.EqualTo(10.0d).Within(0.000000000001d));
        Assert.That(rankine.Kelvins(), Is.EqualTo(10.0d).Within(0.000000000001d));
    }

    [Test]
    public void TemperatureIntervalArithmeticAndBridge_PreserveScaleOnlyUnits()
    {
        TemperatureInterval celsius = TemperatureInterval.FromCelsius(-10.0d);
        TemperatureInterval fahrenheit = TemperatureInterval.FromFahrenheit(18.0d);

        TemperatureInterval sum = celsius.Add(fahrenheit);
        TemperatureInterval difference = TemperatureInterval.FromFahrenheit(36.0d).Subtract(TemperatureInterval.FromCelsius(10.0d));
        TemperatureInterval scaled = celsius.Scale(2.0d);
        TemperatureInterval negated = celsius.Negate();
        TemperatureInterval absolute = celsius.AbsoluteValue();
        QuantityValue quantity = celsius.ToQuantityValue();

        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("celsius_interval"));
        Assert.That(sum.GetValue(), Is.EqualTo(0.0d).Within(0.000000000001d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("fahrenheit_interval"));
        Assert.That(difference.GetValue(), Is.EqualTo(18.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-20.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(10.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(10.0d));
        Assert.That(celsius.Abs().GetValue(), Is.EqualTo(10.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-10.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("celsius_interval"));
    }

    [Test]
    public void TemperatureInterval_RejectsMissingOrAffinePointUnits()
    {
        TemperatureInterval interval = TemperatureInterval.FromKelvins(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new TemperatureInterval(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new TemperatureInterval(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentException>(() => new TemperatureInterval(1.0d, new StandardUnits().Kelvin()));
        Assert.Throws<System.ArgumentNullException>(() => interval.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => interval.Subtract(null!));
    }

    [Test]
    public void TemperatureFacades_RequireExactPointAndIntervalConversionKinds()
    {
        UnitDefinition scaleWithOffset = TemperatureUnit("scale_with_offset", UnitConversionKind.Scale, new RationalNumber(1, 1));
        UnitDefinition specialWithoutOffset = TemperatureUnit("special_without_offset", UnitConversionKind.Special, new RationalNumber(0, 1));
        Temperature point = Temperature.FromKelvins(1.0d);
        TemperatureInterval interval = TemperatureInterval.FromKelvins(1.0d);

        Assert.Throws<System.ArgumentException>(() => new Temperature(1.0d, scaleWithOffset));
        Assert.Throws<System.ArgumentException>(() => point.ToUnit(scaleWithOffset));
        Assert.Throws<System.ArgumentException>(() => new TemperatureInterval(1.0d, specialWithoutOffset));
        Assert.Throws<System.ArgumentException>(() => interval.ToUnit(specialWithoutOffset));
    }

    static UnitDefinition TemperatureUnit(string id, UnitConversionKind conversionKind, RationalNumber offset)
    {
        return new UnitDefinition(
            id,
            id,
            id,
            id,
            new string[0],
            new StandardDimensions().ThermodynamicTemperature(),
            new RationalNumber(1, 1),
            offset,
            false,
            conversionKind);
    }
}
