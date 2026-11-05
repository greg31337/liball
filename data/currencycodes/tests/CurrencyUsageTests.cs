using System;

namespace currencycodes.Tests;

public class CurrencyUsageTests
{
    [Test]
    public void DefaultStateRepresentsAnOpenUnknownUsage()
    {
        CurrencyUsage usage = new CurrencyUsage();

        Assert.Multiple(() =>
        {
            Assert.That(usage.TerritoryKey, Is.Empty);
            Assert.That(usage.CurrencyCode, Is.Empty);
            Assert.That(usage.SourceStartDate, Is.Not.Null);
            Assert.That(usage.SourceStartDate.IsKnown, Is.False);
            Assert.That(usage.SourceEndDate, Is.Not.Null);
            Assert.That(usage.SourceEndDate.IsKnown, Is.False);
            Assert.That(usage.HasInclusiveStartDate, Is.False);
            Assert.That(usage.InclusiveStartDate, Is.EqualTo(default(DateOnly)));
            Assert.That(usage.HasInclusiveEndDate, Is.False);
            Assert.That(usage.InclusiveEndDate, Is.EqualTo(default(DateOnly)));
            Assert.That(usage.IsTender, Is.False);
            Assert.That(usage.IsCurrent, Is.False);
            Assert.That(usage.FromIsoListOne, Is.False);
            Assert.That(usage.FromIsoListThree, Is.False);
            Assert.That(usage.FromCldr, Is.False);
        });
    }

    [Test]
    public void FieldsPreserveHistoricalUsageIntervalAndProvenance()
    {
        PartialDate sourceStartDate = new PartialDate
        {
            IsKnown = true,
            Year = 1999,
            Precision = PartialDatePrecision.Year
        };
        PartialDate sourceEndDate = new PartialDate
        {
            IsKnown = true,
            Year = 2002,
            Month = 2,
            Day = 28,
            Precision = PartialDatePrecision.Day
        };
        CurrencyUsage usage = new CurrencyUsage
        {
            TerritoryKey = "BE",
            CurrencyCode = "BEF",
            SourceStartDate = sourceStartDate,
            SourceEndDate = sourceEndDate,
            HasInclusiveStartDate = true,
            InclusiveStartDate = new DateOnly(1999, 1, 1),
            HasInclusiveEndDate = true,
            InclusiveEndDate = new DateOnly(2002, 2, 28),
            IsTender = true,
            IsCurrent = false,
            FromIsoListOne = false,
            FromIsoListThree = true,
            FromCldr = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(usage.TerritoryKey, Is.EqualTo("BE"));
            Assert.That(usage.CurrencyCode, Is.EqualTo("BEF"));
            Assert.That(usage.SourceStartDate, Is.SameAs(sourceStartDate));
            Assert.That(usage.SourceEndDate, Is.SameAs(sourceEndDate));
            Assert.That(usage.HasInclusiveStartDate, Is.True);
            Assert.That(usage.InclusiveStartDate, Is.EqualTo(new DateOnly(1999, 1, 1)));
            Assert.That(usage.HasInclusiveEndDate, Is.True);
            Assert.That(usage.InclusiveEndDate, Is.EqualTo(new DateOnly(2002, 2, 28)));
            Assert.That(usage.IsTender, Is.True);
            Assert.That(usage.IsCurrent, Is.False);
            Assert.That(usage.FromIsoListOne, Is.False);
            Assert.That(usage.FromIsoListThree, Is.True);
            Assert.That(usage.FromCldr, Is.True);
        });
    }
}
