using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityValueTypedBridgeTests
{
    [Test]
    public void ReverseBridges_PreserveValueAndUnitForEveryTypedFacade()
    {
        StandardUnits units = new StandardUnits();

        AssertBridge(new QuantityValue(1.0d, units.Meter()).ToLength(), 1.0d, "meter");
        AssertBridge(new QuantityValue(2.0d, units.Kilogram()).ToMass(), 2.0d, "kilogram");
        AssertBridge(new QuantityValue(3.0d, units.Second()).ToDuration(), 3.0d, "second");
        AssertBridge(new QuantityValue(4.0d, units.SquareMeter()).ToArea(), 4.0d, "(meter^2)");
        AssertBridge(new QuantityValue(5.0d, units.CubicMeter()).ToVolume(), 5.0d, "(meter^3)");
        AssertBridge(new QuantityValue(6.0d, units.MeterPerSecond()).ToSpeed(), 6.0d, "(meter/second)");
        AssertBridge(new QuantityValue(7.0d, units.MeterPerSecondSquared()).ToAcceleration(), 7.0d, "(meter/(second^2))");
        AssertBridge(new QuantityValue(8.0d, units.Newton()).ToForce(), 8.0d, "newton");
        AssertBridge(new QuantityValue(9.0d, units.Pascal()).ToPressure(), 9.0d, "pascal");
        AssertBridge(new QuantityValue(10.0d, units.Joule()).ToEnergy(), 10.0d, "joule");
        AssertBridge(new QuantityValue(11.0d, units.Watt()).ToPower(), 11.0d, "watt");
        AssertBridge(new QuantityValue(12.0d, units.Hertz()).ToFrequency(), 12.0d, "hertz");
        AssertBridge(new QuantityValue(13.0d, units.Radian()).ToAngle(), 13.0d, "radian");
        AssertBridge(new QuantityValue(14.0d, units.DegreeCelsius()).ToTemperature(), 14.0d, "degree_celsius");
        AssertBridge(new QuantityValue(15.0d, units.KelvinInterval()).ToTemperatureInterval(), 15.0d, "kelvin_interval");
        AssertBridge(new QuantityValue(16.0d, units.Byte()).ToDataSize(), 16.0d, "byte");
        AssertBridge(new QuantityValue(17.0d, units.BytePerSecond()).ToDataRate(), 17.0d, "(byte/second)");
    }

    [Test]
    public void ReverseBridges_RejectIncompatibleDimensions()
    {
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentException>(() => length.ToMass());
        Assert.Throws<System.ArgumentException>(() => length.ToDuration());
        Assert.Throws<System.ArgumentException>(() => length.ToArea());
        Assert.Throws<System.ArgumentException>(() => length.ToVolume());
        Assert.Throws<System.ArgumentException>(() => length.ToSpeed());
        Assert.Throws<System.ArgumentException>(() => length.ToAcceleration());
        Assert.Throws<System.ArgumentException>(() => length.ToForce());
        Assert.Throws<System.ArgumentException>(() => length.ToPressure());
        Assert.Throws<System.ArgumentException>(() => length.ToEnergy());
        Assert.Throws<System.ArgumentException>(() => length.ToPower());
        Assert.Throws<System.ArgumentException>(() => length.ToFrequency());
        Assert.Throws<System.ArgumentException>(() => length.ToAngle());
        Assert.Throws<System.ArgumentException>(() => length.ToTemperature());
        Assert.Throws<System.ArgumentException>(() => length.ToTemperatureInterval());
        Assert.Throws<System.ArgumentException>(() => length.ToDataSize());
        Assert.Throws<System.ArgumentException>(() => length.ToDataRate());
    }

    [Test]
    public void TemperatureReverseBridges_DistinguishAffinePointsFromScaleIntervals()
    {
        QuantityValue point = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue interval = new QuantityValue(10.0d, new StandardUnits().CelsiusInterval());

        Assert.That(point.ToTemperature().Celsius(), Is.EqualTo(20.0d));
        Assert.That(interval.ToTemperatureInterval().Celsius(), Is.EqualTo(10.0d));
        Assert.Throws<System.ArgumentException>(() => point.ToTemperatureInterval());
        Assert.Throws<System.ArgumentException>(() => interval.ToTemperature());
    }

    static void AssertBridge(Length value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Mass value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Duration value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Area value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Volume value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Speed value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Acceleration value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Force value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Pressure value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Energy value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Power value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Frequency value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Angle value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(Temperature value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(TemperatureInterval value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(DataSize value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static void AssertBridge(DataRate value, double expectedValue, string expectedUnitId)
    {
        Assert.That(value.GetValue(), Is.EqualTo(expectedValue));
        Assert.That(value.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }
}
