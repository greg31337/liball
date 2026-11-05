using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceFrequencyTests
{
    [Test]
    public void Values_DefineSupportedFrequenciesInAscendingGranularityOrder()
    {
        Assert.That((int)RecurrenceFrequency.Secondly, Is.EqualTo(1));
        Assert.That((int)RecurrenceFrequency.Minutely, Is.EqualTo(2));
        Assert.That((int)RecurrenceFrequency.Hourly, Is.EqualTo(3));
        Assert.That((int)RecurrenceFrequency.Daily, Is.EqualTo(4));
        Assert.That((int)RecurrenceFrequency.Weekly, Is.EqualTo(5));
        Assert.That((int)RecurrenceFrequency.Monthly, Is.EqualTo(6));
        Assert.That((int)RecurrenceFrequency.Yearly, Is.EqualTo(7));
    }

    [Test]
    public void Values_ExposeExactlySupportedFrequencies()
    {
        RecurrenceFrequency[] values = (RecurrenceFrequency[])Enum.GetValues(typeof(RecurrenceFrequency));

        Assert.That(values.Length, Is.EqualTo(7));
        Assert.That(values[0], Is.EqualTo(RecurrenceFrequency.Secondly));
        Assert.That(values[1], Is.EqualTo(RecurrenceFrequency.Minutely));
        Assert.That(values[2], Is.EqualTo(RecurrenceFrequency.Hourly));
        Assert.That(values[3], Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.That(values[4], Is.EqualTo(RecurrenceFrequency.Weekly));
        Assert.That(values[5], Is.EqualTo(RecurrenceFrequency.Monthly));
        Assert.That(values[6], Is.EqualTo(RecurrenceFrequency.Yearly));
    }

    [TestCase(RecurrenceFrequency.Secondly, "Secondly")]
    [TestCase(RecurrenceFrequency.Minutely, "Minutely")]
    [TestCase(RecurrenceFrequency.Hourly, "Hourly")]
    [TestCase(RecurrenceFrequency.Daily, "Daily")]
    [TestCase(RecurrenceFrequency.Weekly, "Weekly")]
    [TestCase(RecurrenceFrequency.Monthly, "Monthly")]
    [TestCase(RecurrenceFrequency.Yearly, "Yearly")]
    public void ToString_ReturnsPublicFrequencyName(RecurrenceFrequency frequency, string expected)
    {
        Assert.That(frequency.ToString(), Is.EqualTo(expected));
    }
}
