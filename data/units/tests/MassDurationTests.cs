using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class MassDurationTests
{
    [Test]
    public void MassFactories_ConstructCommonMassUnits()
    {
        Mass kilograms = Mass.FromKilograms(2.0d);
        Mass grams = Mass.FromGrams(500.0d);
        Mass tonnes = Mass.FromTonnes(1.0d);
        Mass pounds = Mass.FromPounds(1.0d);
        Mass ounces = Mass.FromOunces(16.0d);

        Assert.That(kilograms.GetUnit().GetId(), Is.EqualTo("kilogram"));
        Assert.That(kilograms.Grams(), Is.EqualTo(2000.0d));
        Assert.That(grams.Kilograms(), Is.EqualTo(0.5d));
        Assert.That(tonnes.Kilograms(), Is.EqualTo(1000.0d));
        Assert.That(pounds.Kilograms(), Is.EqualTo(0.45359237d).Within(0.000000000001d));
        Assert.That(ounces.Pounds(), Is.EqualTo(1.0d).Within(0.000000000001d));
    }

    [Test]
    public void MassToUnitAndArithmetic_ConvertAcrossMassUnits()
    {
        Mass kilograms = Mass.FromKilograms(1.0d);
        Mass grams = Mass.FromGrams(500.0d);

        Mass pounds = kilograms.ToUnit(new StandardUnits().Pound());
        Mass sum = kilograms.Add(grams);
        Mass difference = Mass.FromPounds(2.0d).Subtract(Mass.FromOunces(16.0d));

        Assert.That(pounds.GetUnit().GetId(), Is.EqualTo("pound"));
        Assert.That(pounds.GetValue(), Is.EqualTo(2.2046226218487757d).Within(0.000000000001d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("kilogram"));
        Assert.That(sum.GetValue(), Is.EqualTo(1.5d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("pound"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.0d).Within(0.000000000001d));
    }

    [Test]
    public void MassScaleNegateAbsAndBridge_PreserveUnit()
    {
        Mass mass = Mass.FromPounds(-3.0d);

        Mass scaled = mass.Scale(2.0d);
        Mass negated = mass.Negate();
        Mass absolute = mass.AbsoluteValue();
        QuantityValue quantity = mass.ToQuantityValue();

        Assert.That(scaled.GetValue(), Is.EqualTo(-6.0d));
        Assert.That(scaled.GetUnit().GetId(), Is.EqualTo("pound"));
        Assert.That(negated.GetValue(), Is.EqualTo(3.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(3.0d));
        Assert.That(mass.Abs().GetValue(), Is.EqualTo(3.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-3.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("pound"));
    }

    [Test]
    public void Mass_RejectsMissingOrNonMassInputs()
    {
        Mass mass = Mass.FromKilograms(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Mass(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Mass(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => mass.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => mass.Subtract(null!));
    }

    [Test]
    public void DurationFactories_ConstructCommonTimeUnits()
    {
        Duration seconds = Duration.FromSeconds(120.0d);
        Duration minutes = Duration.FromMinutes(2.0d);
        Duration hours = Duration.FromHours(1.5d);
        Duration days = Duration.FromDays(1.0d);

        Assert.That(seconds.GetUnit().GetId(), Is.EqualTo("second"));
        Assert.That(seconds.Minutes(), Is.EqualTo(2.0d));
        Assert.That(minutes.Seconds(), Is.EqualTo(120.0d));
        Assert.That(hours.Minutes(), Is.EqualTo(90.0d));
        Assert.That(days.Hours(), Is.EqualTo(24.0d));
    }

    [Test]
    public void DurationToUnitAndArithmetic_ConvertAcrossTimeUnits()
    {
        Duration seconds = Duration.FromSeconds(30.0d);
        Duration minute = Duration.FromMinutes(1.0d);

        Duration hours = Duration.FromMinutes(90.0d).ToUnit(new StandardUnits().Hour());
        Duration sum = seconds.Add(minute);
        Duration difference = Duration.FromHours(2.0d).Subtract(Duration.FromMinutes(30.0d));

        Assert.That(hours.GetUnit().GetId(), Is.EqualTo("hour"));
        Assert.That(hours.GetValue(), Is.EqualTo(1.5d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("second"));
        Assert.That(sum.GetValue(), Is.EqualTo(90.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("hour"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.5d));
    }

    [Test]
    public void DurationScaleNegateAbsAndBridge_PreserveUnit()
    {
        Duration duration = Duration.FromHours(-2.0d);

        Duration scaled = duration.Scale(3.0d);
        Duration negated = duration.Negate();
        Duration absolute = duration.AbsoluteValue();
        QuantityValue quantity = duration.ToQuantityValue();

        Assert.That(scaled.GetValue(), Is.EqualTo(-6.0d));
        Assert.That(scaled.GetUnit().GetId(), Is.EqualTo("hour"));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(duration.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("hour"));
    }

    [Test]
    public void Duration_RejectsMissingOrNonDurationInputs()
    {
        Duration duration = Duration.FromSeconds(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new Duration(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new Duration(1.0d, new StandardUnits().Meter()));
        Assert.Throws<System.ArgumentNullException>(() => duration.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => duration.Subtract(null!));
    }
}
