using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class TypedCrossFacadeOperationTests
{
    [Test]
    public void LengthAndMotionOperations_ReturnTypedResults()
    {
        Speed speed = Length.FromMiles(120.0d).DivideByDuration(Duration.FromHours(2.0d));
        Length distance = speed.MultiplyByDuration(Duration.FromHours(2.0d));
        Acceleration acceleration = Speed.FromMetersPerSecond(10.0d).DivideByDuration(Duration.FromSeconds(2.0d));
        Speed acceleratedSpeed = acceleration.MultiplyByDuration(Duration.FromSeconds(2.0d));

        Assert.That(speed.MilesPerHour(), Is.EqualTo(60.0d).Within(0.000000000001d));
        Assert.That(distance.Miles(), Is.EqualTo(120.0d).Within(0.000000000001d));
        Assert.That(acceleration.MetersPerSecondSquared(), Is.EqualTo(5.0d).Within(0.000000000001d));
        Assert.That(acceleratedSpeed.MetersPerSecond(), Is.EqualTo(10.0d).Within(0.000000000001d));
    }

    [Test]
    public void LengthDerivedAreaAndVolumeOperations_ReturnTypedResults()
    {
        Area product = Length.FromMeters(3.0d).MultiplyByLength(Length.FromMeters(4.0d));
        Area square = Length.FromMeters(3.0d).Square();
        Volume cube = Length.FromMeters(2.0d).Cube();
        Volume prism = Area.FromSquareMeters(12.0d).MultiplyByLength(Length.FromMeters(2.0d));

        Assert.That(product.SquareMeters(), Is.EqualTo(12.0d));
        Assert.That(square.SquareMeters(), Is.EqualTo(9.0d));
        Assert.That(cube.CubicMeters(), Is.EqualTo(8.0d));
        Assert.That(prism.CubicMeters(), Is.EqualTo(24.0d));
    }

    [Test]
    public void MechanicalOperations_ReturnTypedResults()
    {
        Force force = Mass.FromKilograms(2.0d).MultiplyByAcceleration(Acceleration.FromMetersPerSecondSquared(3.0d));
        Energy energy = force.MultiplyByLength(Length.FromMeters(4.0d));
        Pressure pressure = force.DivideByArea(Area.FromSquareMeters(2.0d));
        Force reconstructedForce = pressure.MultiplyByArea(Area.FromSquareMeters(2.0d));
        Power power = energy.DivideByDuration(Duration.FromSeconds(4.0d));
        Energy reconstructedEnergy = power.MultiplyByDuration(Duration.FromSeconds(4.0d));

        Assert.That(force.Newtons(), Is.EqualTo(6.0d));
        Assert.That(energy.Joules(), Is.EqualTo(24.0d));
        Assert.That(pressure.Pascals(), Is.EqualTo(3.0d));
        Assert.That(reconstructedForce.Newtons(), Is.EqualTo(6.0d));
        Assert.That(power.Watts(), Is.EqualTo(6.0d));
        Assert.That(reconstructedEnergy.Joules(), Is.EqualTo(24.0d));
    }

    [Test]
    public void DataTransferOperations_ReturnTypedResults()
    {
        DataRate rate = DataSize.FromBytes(1000.0d).DivideByDuration(Duration.FromSeconds(2.0d));
        DataSize size = rate.MultiplyByDuration(Duration.FromSeconds(2.0d));

        Assert.That(rate.BytesPerSecond(), Is.EqualTo(500.0d));
        Assert.That(size.Bytes(), Is.EqualTo(1000.0d));
    }

    [Test]
    public void CrossFacadeOperations_RejectMissingOperands()
    {
        Assert.Throws<System.ArgumentNullException>(() => Length.FromMeters(1.0d).DivideByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Length.FromMeters(1.0d).MultiplyByLength(null!));
        Assert.Throws<System.ArgumentNullException>(() => Area.FromSquareMeters(1.0d).MultiplyByLength(null!));
        Assert.Throws<System.ArgumentNullException>(() => Speed.FromMetersPerSecond(1.0d).MultiplyByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Speed.FromMetersPerSecond(1.0d).DivideByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Acceleration.FromMetersPerSecondSquared(1.0d).MultiplyByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Mass.FromKilograms(1.0d).MultiplyByAcceleration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Force.FromNewtons(1.0d).MultiplyByLength(null!));
        Assert.Throws<System.ArgumentNullException>(() => Force.FromNewtons(1.0d).DivideByArea(null!));
        Assert.Throws<System.ArgumentNullException>(() => Pressure.FromPascals(1.0d).MultiplyByArea(null!));
        Assert.Throws<System.ArgumentNullException>(() => Energy.FromJoules(1.0d).DivideByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => Power.FromWatts(1.0d).MultiplyByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => DataSize.FromBytes(1.0d).DivideByDuration(null!));
        Assert.Throws<System.ArgumentNullException>(() => DataRate.FromBytesPerSecond(1.0d).MultiplyByDuration(null!));
    }

    [Test]
    public void CrossFacadeOperations_RejectAffineUnits()
    {
        Length affineLength = new Length(2.0d, AffineLengthUnit());

        Assert.Throws<System.InvalidOperationException>(() => affineLength.DivideByDuration(Duration.FromSeconds(1.0d)));
        Assert.Throws<System.InvalidOperationException>(() => affineLength.MultiplyByLength(Length.FromMeters(1.0d)));
        Assert.Throws<System.InvalidOperationException>(() => affineLength.Square());
        Assert.Throws<System.InvalidOperationException>(() => affineLength.Cube());
    }

    static UnitDefinition AffineLengthUnit()
    {
        return new UnitDefinition(
            "affine_length",
            "affine length",
            "affine lengths",
            "al",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(1, 1),
            new RationalNumber(1, 1),
            false,
            UnitConversionKind.Affine);
    }
}
