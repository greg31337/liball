using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class LengthTests
{
    [Test]
    public void FromMeters_ConstructsMeterLength()
    {
        Length length = Length.FromMeters(3.5d);

        Assert.That(length.GetValue(), Is.EqualTo(3.5d));
        Assert.That(length.GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(length.Meters(), Is.EqualTo(3.5d));
    }

    [Test]
    public void Factories_ConstructCommonLengthUnits()
    {
        Length kilometers = Length.FromKilometers(2.0d);
        Length feet = Length.FromFeet(3.0d);
        Length inches = Length.FromInches(12.0d);
        Length miles = Length.FromMiles(1.0d);

        Assert.That(kilometers.GetUnit().GetId(), Is.EqualTo("kilometer"));
        Assert.That(kilometers.Meters(), Is.EqualTo(2000.0d));
        Assert.That(feet.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(feet.Meters(), Is.EqualTo(0.9144d).Within(0.000000000001d));
        Assert.That(inches.Feet(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(miles.Feet(), Is.EqualTo(5280.0d).Within(0.000000001d));
    }

    [Test]
    public void ToUnit_ConvertsToTargetLengthUnit()
    {
        Length length = Length.FromMeters(1.0d);

        Length feet = length.ToUnit(new StandardUnits().Foot());

        Assert.That(feet.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(feet.GetValue(), Is.EqualTo(3.280839895013123d).Within(0.000000000001d));
    }

    [Test]
    public void AddAndSubtract_ConvertRightOperandToLeftUnit()
    {
        Length meters = Length.FromMeters(1.0d);
        Length foot = Length.FromFeet(1.0d);
        Length threeFeet = Length.FromFeet(3.0d);
        Length twelveInches = Length.FromInches(12.0d);

        Length sum = meters.Add(foot);
        Length difference = threeFeet.Subtract(twelveInches);

        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(sum.GetValue(), Is.EqualTo(1.3048d).Within(0.000000000001d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(difference.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
    }

    [Test]
    public void ScaleNegateAndAbsoluteValue_PreserveUnit()
    {
        Length length = Length.FromFeet(-3.0d);

        Length scaled = length.Scale(2.0d);
        Length negated = length.Negate();
        Length absolute = length.AbsoluteValue();

        Assert.That(scaled.GetValue(), Is.EqualTo(-6.0d));
        Assert.That(scaled.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(negated.GetValue(), Is.EqualTo(3.0d));
        Assert.That(negated.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(absolute.GetValue(), Is.EqualTo(3.0d));
        Assert.That(absolute.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(length.Abs().GetValue(), Is.EqualTo(3.0d));
    }

    [Test]
    public void ToQuantityValue_ReturnsDynamicQuantityBridge()
    {
        Length length = Length.FromMiles(2.0d);

        QuantityValue quantity = length.ToQuantityValue();

        Assert.That(quantity.GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("mile"));
        Assert.That(quantity, Is.Not.SameAs(length.ToQuantityValue()));
    }

    [Test]
    public void Constructor_RejectsMissingOrNonLengthUnit()
    {
        Assert.Throws<System.ArgumentNullException>(() => new Length(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Length(1.0d, new StandardUnits().Second()));
    }

    [Test]
    public void AddAndSubtract_RejectMissingLength()
    {
        Length length = Length.FromMeters(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => length.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => length.Subtract(null!));
    }
}
