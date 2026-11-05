using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class AreaVolumeRateTests
{
    [Test]
    public void AreaFactories_ConstructCommonAreaUnits()
    {
        Area squareMeters = Area.FromSquareMeters(10000.0d);
        Area hectares = Area.FromHectares(1.0d);
        Area acres = Area.FromAcres(1.0d);

        Assert.That(squareMeters.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(squareMeters.Hectares(), Is.EqualTo(1.0d));
        Assert.That(hectares.SquareMeters(), Is.EqualTo(10000.0d));
        Assert.That(acres.SquareMeters(), Is.EqualTo(4046.8564224d).Within(0.000000000001d));
    }

    [Test]
    public void AreaToUnitArithmeticAndBridge_ConvertAcrossAreaUnits()
    {
        Area squareMeters = Area.FromSquareMeters(1000.0d);
        Area hectare = Area.FromHectares(1.0d);
        Area acres = Area.FromAcres(-2.0d);

        Area converted = hectare.ToUnit(new StandardUnits().Acre());
        Area sum = squareMeters.Add(hectare);
        Area difference = Area.FromAcres(2.0d).Subtract(Area.FromAcres(1.0d));
        Area scaled = acres.Scale(2.0d);
        Area negated = acres.Negate();
        Area absolute = acres.AbsoluteValue();
        QuantityValue quantity = acres.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("acre"));
        Assert.That(converted.GetValue(), Is.EqualTo(2.471053814671653d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(sum.GetValue(), Is.EqualTo(11000.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("acre"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.0d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(acres.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("acre"));
    }

    [Test]
    public void Area_RejectsMissingOrNonAreaInputs()
    {
        Area area = Area.FromSquareMeters(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Area(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Area(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => area.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => area.Subtract(null!));
    }

    [Test]
    public void VolumeFactories_ConstructCommonVolumeUnits()
    {
        Volume cubicMeters = Volume.FromCubicMeters(1.0d);
        Volume liters = Volume.FromLiters(1000.0d);
        Volume gallons = Volume.FromGallons(1.0d);
        Volume quarts = Volume.FromQuarts(4.0d);
        Volume pints = Volume.FromPints(2.0d);
        Volume cups = Volume.FromCups(16.0d);

        Assert.That(cubicMeters.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(3)), Is.True);
        Assert.That(liters.CubicMeters(), Is.EqualTo(1.0d));
        Assert.That(gallons.Liters(), Is.EqualTo(3.785411784d).Within(0.000000000001d));
        Assert.That(quarts.Gallons(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(pints.Quarts(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(cups.Gallons(), Is.EqualTo(1.0d).Within(0.000000000001d));
    }

    [Test]
    public void VolumeToUnitArithmeticAndBridge_ConvertAcrossVolumeUnits()
    {
        Volume liters = Volume.FromLiters(500.0d);
        Volume cubicMeter = Volume.FromCubicMeters(1.0d);
        Volume gallons = Volume.FromGallons(-3.0d);

        Volume converted = cubicMeter.ToUnit(new StandardUnits().Gallon());
        Volume sum = liters.Add(cubicMeter);
        Volume difference = Volume.FromGallons(2.0d).Subtract(Volume.FromQuarts(4.0d));
        Volume scaled = gallons.Scale(2.0d);
        Volume negated = gallons.Negate();
        Volume absolute = gallons.AbsoluteValue();
        QuantityValue quantity = gallons.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("us_gallon"));
        Assert.That(converted.GetValue(), Is.EqualTo(264.1720523581484d).Within(0.000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("liter"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d).Within(0.000000000001d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("us_gallon"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-6.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(3.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(3.0d));
        Assert.That(gallons.Abs().GetValue(), Is.EqualTo(3.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-3.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("us_gallon"));
    }

    [Test]
    public void Volume_RejectsMissingOrNonVolumeInputs()
    {
        Volume volume = Volume.FromLiters(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Volume(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Volume(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => volume.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => volume.Subtract(null!));
    }

    [Test]
    public void SpeedFactories_ConstructCommonSpeedUnits()
    {
        Speed metersPerSecond = Speed.FromMetersPerSecond(10.0d);
        Speed kilometersPerHour = Speed.FromKilometersPerHour(36.0d);
        Speed milesPerHour = Speed.FromMilesPerHour(60.0d);

        Assert.That(metersPerSecond.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        Assert.That(metersPerSecond.KilometersPerHour(), Is.EqualTo(36.0d).Within(0.000000000001d));
        Assert.That(kilometersPerHour.MetersPerSecond(), Is.EqualTo(10.0d));
        Assert.That(milesPerHour.MetersPerSecond(), Is.EqualTo(26.8224d).Within(0.000000000001d));
    }

    [Test]
    public void SpeedToUnitArithmeticAndBridge_ConvertAcrossSpeedUnits()
    {
        Speed metersPerSecond = Speed.FromMetersPerSecond(10.0d);
        Speed kilometersPerHour = Speed.FromKilometersPerHour(36.0d);
        Speed milesPerHour = Speed.FromMilesPerHour(-60.0d);

        Speed converted = metersPerSecond.ToUnit(new StandardUnits().Mile().Divide(new StandardUnits().Hour()));
        Speed sum = metersPerSecond.Add(kilometersPerHour);
        Speed difference = Speed.FromMilesPerHour(60.0d).Subtract(Speed.FromMilesPerHour(30.0d));
        Speed scaled = milesPerHour.Scale(2.0d);
        Speed negated = milesPerHour.Negate();
        Speed absolute = milesPerHour.AbsoluteValue();
        QuantityValue quantity = milesPerHour.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("(mile/hour)"));
        Assert.That(converted.GetValue(), Is.EqualTo(22.369362920544024d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("(meter/second)"));
        Assert.That(sum.GetValue(), Is.EqualTo(20.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("(mile/hour)"));
        Assert.That(difference.GetValue(), Is.EqualTo(30.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-120.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(60.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(60.0d));
        Assert.That(milesPerHour.Abs().GetValue(), Is.EqualTo(60.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-60.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("(mile/hour)"));
    }

    [Test]
    public void Speed_RejectsMissingOrNonSpeedInputs()
    {
        Speed speed = Speed.FromMetersPerSecond(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Speed(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Speed(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => speed.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => speed.Subtract(null!));
    }

    [Test]
    public void AccelerationFactories_ConstructCommonAccelerationUnits()
    {
        Acceleration metersPerSecondSquared = Acceleration.FromMetersPerSecondSquared(9.80665d);
        Acceleration feetPerSecondSquared = Acceleration.FromFeetPerSecondSquared(32.17404855643044d);

        Assert.That(metersPerSecondSquared.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))), Is.True);
        Assert.That(metersPerSecondSquared.FeetPerSecondSquared(), Is.EqualTo(32.17404855643044d).Within(0.000000000001d));
        Assert.That(feetPerSecondSquared.MetersPerSecondSquared(), Is.EqualTo(9.80665d).Within(0.000000000001d));
    }

    [Test]
    public void AccelerationToUnitArithmeticAndBridge_ConvertAcrossAccelerationUnits()
    {
        Acceleration metersPerSecondSquared = Acceleration.FromMetersPerSecondSquared(1.0d);
        Acceleration feetPerSecondSquared = Acceleration.FromFeetPerSecondSquared(3.280839895013123d);
        Acceleration negativeFeetPerSecondSquared = Acceleration.FromFeetPerSecondSquared(-32.0d);

        Acceleration converted = metersPerSecondSquared.ToUnit(new StandardUnits().Foot().Divide(new StandardUnits().Second().Pow(2)));
        Acceleration sum = metersPerSecondSquared.Add(feetPerSecondSquared);
        Acceleration difference = Acceleration.FromFeetPerSecondSquared(64.0d).Subtract(Acceleration.FromFeetPerSecondSquared(32.0d));
        Acceleration scaled = negativeFeetPerSecondSquared.Scale(2.0d);
        Acceleration negated = negativeFeetPerSecondSquared.Negate();
        Acceleration absolute = negativeFeetPerSecondSquared.AbsoluteValue();
        QuantityValue quantity = negativeFeetPerSecondSquared.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("(foot/(second^2))"));
        Assert.That(converted.GetValue(), Is.EqualTo(3.280839895013123d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("(meter/(second^2))"));
        Assert.That(sum.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("(foot/(second^2))"));
        Assert.That(difference.GetValue(), Is.EqualTo(32.0d).Within(0.000000000001d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-64.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(32.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(32.0d));
        Assert.That(negativeFeetPerSecondSquared.Abs().GetValue(), Is.EqualTo(32.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-32.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("(foot/(second^2))"));
    }

    [Test]
    public void Acceleration_RejectsMissingOrNonAccelerationInputs()
    {
        Acceleration acceleration = Acceleration.FromMetersPerSecondSquared(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Acceleration(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Acceleration(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => acceleration.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => acceleration.Subtract(null!));
    }
}
