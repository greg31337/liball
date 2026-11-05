namespace currencycodes.Tests;

public class PartialDateTests
{
    [Test]
    public void DefaultStateRepresentsAnUnknownDateExplicitly()
    {
        PartialDate date = new PartialDate();

        Assert.Multiple(() =>
        {
            Assert.That(date.IsKnown, Is.False);
            Assert.That(date.Precision, Is.EqualTo(PartialDatePrecision.Unknown));
            Assert.That(date.Year, Is.Zero);
            Assert.That(date.Month, Is.Zero);
            Assert.That(date.Day, Is.Zero);
        });
    }

    [TestCase(PartialDatePrecision.Year, 2026, 0, 0)]
    [TestCase(PartialDatePrecision.Month, 2026, 9, 0)]
    [TestCase(PartialDatePrecision.Day, 2026, 9, 15)]
    public void KnownDatePreservesOnlyTheSuppliedPrecision(
        PartialDatePrecision precision,
        int year,
        int month,
        int day)
    {
        PartialDate date = new PartialDate
        {
            IsKnown = true,
            Year = year,
            Month = month,
            Day = day,
            Precision = precision
        };

        Assert.Multiple(() =>
        {
            Assert.That(date.IsKnown, Is.True);
            Assert.That(date.Precision, Is.EqualTo(precision));
            Assert.That(date.Year, Is.EqualTo(year));
            Assert.That(date.Month, Is.EqualTo(month));
            Assert.That(date.Day, Is.EqualTo(day));
        });
    }
}
