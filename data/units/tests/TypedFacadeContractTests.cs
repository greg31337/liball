using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class TypedFacadeContractTests
{
    [Test]
    public void DefaultFactories_UseExpectedUnitsForTypedFacades()
    {
        Assert.That(Length.FromMeters(1.0d).GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(Mass.FromKilograms(1.0d).GetUnit().GetId(), Is.EqualTo("kilogram"));
        Assert.That(Duration.FromSeconds(1.0d).GetUnit().GetId(), Is.EqualTo("second"));
        Assert.That(Area.FromSquareMeters(1.0d).GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(Volume.FromCubicMeters(1.0d).GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(3)), Is.True);
        Assert.That(Speed.FromMetersPerSecond(1.0d).GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        Assert.That(Acceleration.FromMetersPerSecondSquared(1.0d).GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))), Is.True);
        Assert.That(Force.FromNewtons(1.0d).GetUnit().GetId(), Is.EqualTo("newton"));
        Assert.That(Pressure.FromPascals(1.0d).GetUnit().GetId(), Is.EqualTo("pascal"));
        Assert.That(Energy.FromJoules(1.0d).GetUnit().GetId(), Is.EqualTo("joule"));
        Assert.That(Power.FromWatts(1.0d).GetUnit().GetId(), Is.EqualTo("watt"));
        Assert.That(Frequency.FromHertz(1.0d).GetUnit().GetId(), Is.EqualTo("hertz"));
        Assert.That(Angle.FromRadians(1.0d).GetUnit().GetId(), Is.EqualTo("radian"));
        Assert.That(Temperature.FromKelvins(1.0d).GetUnit().GetId(), Is.EqualTo("kelvin"));
        Assert.That(TemperatureInterval.FromKelvins(1.0d).GetUnit().GetId(), Is.EqualTo("kelvin_interval"));
        Assert.That(DataSize.FromBytes(1.0d).GetUnit().GetId(), Is.EqualTo("byte"));
        Assert.That(DataRate.FromBitsPerSecond(1.0d).GetUnit().GetId(), Is.EqualTo("(bit/second)"));
    }

    [Test]
    public void ConversionMethods_CoverRepresentativeTypedFacadeUnits()
    {
        Assert.That(Length.FromMiles(1.0d).Feet(), Is.EqualTo(5280.0d).Within(0.000000001d));
        Assert.That(Mass.FromOunces(16.0d).Pounds(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(Duration.FromDays(1.0d).Hours(), Is.EqualTo(24.0d));
        Assert.That(Area.FromAcres(1.0d).Hectares(), Is.EqualTo(0.40468564224d).Within(0.000000000001d));
        Assert.That(Volume.FromGallons(1.0d).Quarts(), Is.EqualTo(4.0d).Within(0.000000000001d));
        Assert.That(Speed.FromMilesPerHour(60.0d).MetersPerSecond(), Is.EqualTo(26.8224d).Within(0.000000000001d));
        Assert.That(Acceleration.FromFeetPerSecondSquared(3.280839895013123d).MetersPerSecondSquared(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(Force.FromKilonewtons(1.0d).Newtons(), Is.EqualTo(1000.0d));
        Assert.That(Pressure.FromPsi(1.0d).Pascals(), Is.EqualTo(6894.757293168361d).Within(0.000000001d));
        Assert.That(Energy.FromKilowattHours(1.0d).Joules(), Is.EqualTo(3600000.0d));
        Assert.That(Power.FromHorsepower(1.0d).Watts(), Is.EqualTo(745.6998715822702d).Within(0.000000001d));
        Assert.That(Frequency.FromMegahertz(1.0d).Hertz(), Is.EqualTo(1000000.0d));
        Assert.That(Angle.FromRadians(1.25d).Radians(), Is.EqualTo(1.25d));
        Assert.That(Temperature.FromFahrenheit(32.0d).Celsius(), Is.EqualTo(0.0d).Within(0.000000000001d));
        Assert.That(TemperatureInterval.FromFahrenheit(18.0d).Celsius(), Is.EqualTo(10.0d).Within(0.000000000001d));
        Assert.That(DataSize.FromMebibytes(1.0d).Kibibytes(), Is.EqualTo(1024.0d));
        Assert.That(DataRate.FromMebibytesPerSecond(1.0d).KibibytesPerSecond(), Is.EqualTo(1024.0d));
    }

    [Test]
    public void NamedConversionMethods_CoverEveryRemainingFacadeUnit()
    {
        Assert.That(Length.FromMiles(1.0d).Kilometers(), Is.EqualTo(1.609344d).Within(0.000000000001d));
        Assert.That(Length.FromMiles(1.0d).Inches(), Is.EqualTo(63360.0d).Within(0.000000001d));
        Assert.That(Mass.FromTonnes(1.0d).Tonnes(), Is.EqualTo(1.0d));
        Assert.That(Mass.FromPounds(1.0d).Ounces(), Is.EqualTo(16.0d).Within(0.000000000001d));
        Assert.That(Duration.FromDays(2.0d).Days(), Is.EqualTo(2.0d));
        Assert.That(Area.FromAcres(2.0d).Acres(), Is.EqualTo(2.0d));
        Assert.That(Volume.FromPints(2.0d).Pints(), Is.EqualTo(2.0d));
        Assert.That(Volume.FromCups(2.0d).Cups(), Is.EqualTo(2.0d));
        Assert.That(Force.FromNewtons(2500.0d).Kilonewtons(), Is.EqualTo(2.5d));
        Assert.That(Energy.FromJoules(3600000.0d).KilowattHours(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(Energy.FromBritishThermalUnits(2.0d).BritishThermalUnits(), Is.EqualTo(2.0d));
        Assert.That(Energy.FromElectronvolts(2.0d).Electronvolts(), Is.EqualTo(2.0d));
        Assert.That(Frequency.FromHertz(2500000.0d).Megahertz(), Is.EqualTo(2.5d));
        Assert.That(Temperature.FromCelsius(0.0d).Rankine(), Is.EqualTo(491.67d).Within(0.000000000001d));
        Assert.That(TemperatureInterval.FromCelsius(10.0d).Rankine(), Is.EqualTo(18.0d).Within(0.000000000001d));
        Assert.That(DataSize.FromKilobytes(2000.0d).Megabytes(), Is.EqualTo(2.0d));
        Assert.That(DataSize.FromKibibytes(2048.0d).Mebibytes(), Is.EqualTo(2.0d));
        Assert.That(DataRate.FromKilobytesPerSecond(2000.0d).MegabytesPerSecond(), Is.EqualTo(2.0d));
        Assert.That(DataRate.FromKibibytesPerSecond(2048.0d).MebibytesPerSecond(), Is.EqualTo(2.0d));
    }

    [Test]
    public void ToUnitMethods_RejectNullAndInvalidDimensionsForEveryFacade()
    {
        StandardUnits units = new StandardUnits();

        Assert.Throws<System.ArgumentNullException>(() => Length.FromMeters(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Length.FromMeters(1.0d).ToUnit(units.Second()));
        Assert.Throws<System.ArgumentNullException>(() => Mass.FromKilograms(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Mass.FromKilograms(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Duration.FromSeconds(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Duration.FromSeconds(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Area.FromSquareMeters(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Area.FromSquareMeters(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Volume.FromCubicMeters(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Volume.FromCubicMeters(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Speed.FromMetersPerSecond(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Speed.FromMetersPerSecond(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Acceleration.FromMetersPerSecondSquared(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Acceleration.FromMetersPerSecondSquared(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Force.FromNewtons(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Force.FromNewtons(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Pressure.FromPascals(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Pressure.FromPascals(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Energy.FromJoules(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Energy.FromJoules(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Power.FromWatts(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Power.FromWatts(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Frequency.FromHertz(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Frequency.FromHertz(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Angle.FromRadians(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Angle.FromRadians(1.0d).ToUnit(units.Meter()));
        Assert.Throws<System.ArgumentNullException>(() => Temperature.FromKelvins(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => Temperature.FromKelvins(1.0d).ToUnit(units.KelvinInterval()));
        Assert.Throws<System.ArgumentNullException>(() => TemperatureInterval.FromKelvins(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => TemperatureInterval.FromKelvins(1.0d).ToUnit(units.Kelvin()));
        Assert.Throws<System.ArgumentNullException>(() => DataSize.FromBytes(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => DataSize.FromBytes(1.0d).ToUnit(units.Second()));
        Assert.Throws<System.ArgumentNullException>(() => DataRate.FromBitsPerSecond(1.0d).ToUnit(null!));
        Assert.Throws<System.ArgumentException>(() => DataRate.FromBitsPerSecond(1.0d).ToUnit(units.Bit()));
    }

    [Test]
    public void ArithmeticMethods_PreserveLeftOperandUnits()
    {
        Assert.That(Length.FromFeet(3.0d).Add(Length.FromInches(12.0d)).GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(Mass.FromPounds(2.0d).Subtract(Mass.FromOunces(16.0d)).GetUnit().GetId(), Is.EqualTo("pound"));
        Assert.That(Duration.FromHours(2.0d).Subtract(Duration.FromMinutes(30.0d)).GetUnit().GetId(), Is.EqualTo("hour"));
        Assert.That(Area.FromAcres(2.0d).Subtract(Area.FromAcres(1.0d)).GetUnit().GetId(), Is.EqualTo("acre"));
        Assert.That(Volume.FromGallons(2.0d).Subtract(Volume.FromQuarts(4.0d)).GetUnit().GetId(), Is.EqualTo("us_gallon"));
        Assert.That(Speed.FromMilesPerHour(60.0d).Subtract(Speed.FromMilesPerHour(30.0d)).GetUnit().GetId(), Is.EqualTo("(mile/hour)"));
        Assert.That(Acceleration.FromFeetPerSecondSquared(64.0d).Subtract(Acceleration.FromFeetPerSecondSquared(32.0d)).GetUnit().GetId(), Is.EqualTo("(foot/(second^2))"));
        Assert.That(Force.FromKilonewtons(2.0d).Subtract(Force.FromNewtons(500.0d)).GetUnit().GetId(), Is.EqualTo("kilo_newton"));
        Assert.That(Pressure.FromPsi(3.0d).Subtract(Pressure.FromPsi(1.0d)).GetUnit().GetId(), Is.EqualTo("psi"));
        Assert.That(Energy.FromBritishThermalUnits(3.0d).Subtract(Energy.FromBritishThermalUnits(1.0d)).GetUnit().GetId(), Is.EqualTo("british_thermal_unit"));
        Assert.That(Power.FromHorsepower(3.0d).Subtract(Power.FromHorsepower(1.0d)).GetUnit().GetId(), Is.EqualTo("horsepower"));
        Assert.That(Frequency.FromKilohertz(2.0d).Subtract(Frequency.FromHertz(500.0d)).GetUnit().GetId(), Is.EqualTo("kilo_hertz"));
        Assert.That(Angle.FromRadians(2.0d).Add(Angle.FromRadians(1.0d)).GetUnit().GetId(), Is.EqualTo("radian"));
        Assert.That(Temperature.FromCelsius(20.0d).Add(TemperatureInterval.FromCelsius(10.0d)).GetUnit().GetId(), Is.EqualTo("degree_celsius"));
        Assert.That(TemperatureInterval.FromCelsius(10.0d).Add(TemperatureInterval.FromFahrenheit(18.0d)).GetUnit().GetId(), Is.EqualTo("celsius_interval"));
        Assert.That(DataSize.FromKibibytes(2.0d).Subtract(DataSize.FromBytes(1024.0d)).GetUnit().GetId(), Is.EqualTo("kibibyte"));
        Assert.That(DataRate.FromMegabytesPerSecond(2.0d).Subtract(DataRate.FromKilobytesPerSecond(500.0d)).GetUnit().GetId(), Is.EqualTo("(megabyte/second)"));
    }

    [Test]
    public void DynamicBridgeMethods_ReturnExpectedQuantityValues()
    {
        AssertBridge(Length.FromMeters(1.0d).ToQuantityValue(), 1.0d, "meter", new StandardDimensions().Length());
        AssertBridge(Mass.FromKilograms(1.0d).ToQuantityValue(), 1.0d, "kilogram", new StandardDimensions().Mass());
        AssertBridge(Duration.FromSeconds(1.0d).ToQuantityValue(), 1.0d, "second", new StandardDimensions().Time());
        AssertBridge(Area.FromHectares(1.0d).ToQuantityValue(), 1.0d, "hectare", new StandardDimensions().Length().Pow(2));
        AssertBridge(Volume.FromLiters(1.0d).ToQuantityValue(), 1.0d, "liter", new StandardDimensions().Length().Pow(3));
        AssertBridge(Speed.FromKilometersPerHour(1.0d).ToQuantityValue(), 1.0d, "(kilometer/hour)", new StandardDimensions().Length().Divide(new StandardDimensions().Time()));
        AssertBridge(Acceleration.FromFeetPerSecondSquared(1.0d).ToQuantityValue(), 1.0d, "(foot/(second^2))", new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2)));
        AssertBridge(Force.FromKilonewtons(1.0d).ToQuantityValue(), 1.0d, "kilo_newton", new StandardUnits().Newton().GetDimension());
        AssertBridge(Pressure.FromPsi(1.0d).ToQuantityValue(), 1.0d, "psi", new StandardUnits().Pascal().GetDimension());
        AssertBridge(Energy.FromKilowattHours(1.0d).ToQuantityValue(), 1.0d, "(kilo_watt*hour)", new StandardUnits().Joule().GetDimension());
        AssertBridge(Power.FromKilowatts(1.0d).ToQuantityValue(), 1.0d, "kilo_watt", new StandardUnits().Watt().GetDimension());
        AssertBridge(Frequency.FromMegahertz(1.0d).ToQuantityValue(), 1.0d, "mega_hertz", new StandardUnits().Hertz().GetDimension());
        AssertBridge(Angle.FromRadians(1.0d).ToQuantityValue(), 1.0d, "radian", new StandardDimensions().PlaneAngle());
        AssertBridge(Temperature.FromCelsius(1.0d).ToQuantityValue(), 1.0d, "degree_celsius", new StandardDimensions().ThermodynamicTemperature());
        AssertBridge(TemperatureInterval.FromFahrenheit(1.0d).ToQuantityValue(), 1.0d, "fahrenheit_interval", new StandardDimensions().ThermodynamicTemperature());
        AssertBridge(DataSize.FromMebibytes(1.0d).ToQuantityValue(), 1.0d, "mebibyte", new StandardDimensions().Information());
        AssertBridge(DataRate.FromMebibytesPerSecond(1.0d).ToQuantityValue(), 1.0d, "(mebibyte/second)", new StandardDimensions().Information().Divide(new StandardDimensions().Time()));
    }

    static void AssertBridge(QuantityValue quantity, double expectedValue, string expectedUnitId, DimensionVector expectedDimension)
    {
        Assert.That(quantity.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
        Assert.That(quantity.GetUnit().GetDimension().EqualsDimension(expectedDimension), Is.True);
    }
}
