using System;

namespace currencycodes.Tests;

public class PartialDateRulesTests
{
    [Test]
    public void HasKnownPrecisionRequiresKnownStateAndCalendarPrecision()
    {
        PartialDate unknown = new PartialDate();
        PartialDate inconsistent = new PartialDate
        {
            IsKnown = true,
            Precision = PartialDatePrecision.Unknown
        };

        Assert.Multiple(() =>
        {
            Assert.That(PartialDateRules.HasKnownPrecision(unknown), Is.False);
            Assert.That(PartialDateRules.HasKnownPrecision(inconsistent), Is.False);
            Assert.That(PartialDateRules.HasKnownPrecision(CreateYear(2024)), Is.True);
            Assert.That(PartialDateRules.HasKnownPrecision(CreateMonth(2024, 2)), Is.True);
            Assert.That(PartialDateRules.HasKnownPrecision(CreateDay(2024, 2, 29)), Is.True);
        });
    }

    [Test]
    public void NormalizeInclusiveStartDateUsesFirstPossibleCalendarDay()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PartialDateRules.NormalizeInclusiveStartDate(CreateYear(2024)),
                Is.EqualTo(new DateOnly(2024, 1, 1)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveStartDate(CreateMonth(2024, 2)),
                Is.EqualTo(new DateOnly(2024, 2, 1)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveStartDate(CreateDay(2024, 2, 29)),
                Is.EqualTo(new DateOnly(2024, 2, 29)));
        });
    }

    [Test]
    public void NormalizeInclusiveEndDateUsesLastPossibleCalendarDay()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PartialDateRules.NormalizeInclusiveEndDate(CreateYear(2024)),
                Is.EqualTo(new DateOnly(2024, 12, 31)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveEndDate(CreateMonth(2024, 2)),
                Is.EqualTo(new DateOnly(2024, 2, 29)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveEndDate(CreateMonth(2023, 2)),
                Is.EqualTo(new DateOnly(2023, 2, 28)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveEndDate(CreateMonth(2024, 4)),
                Is.EqualTo(new DateOnly(2024, 4, 30)));
            Assert.That(
                PartialDateRules.NormalizeInclusiveEndDate(CreateDay(2024, 2, 29)),
                Is.EqualTo(new DateOnly(2024, 2, 29)));
        });
    }

    [Test]
    public void ComparisonUsesNormalizedInclusiveBoundaries()
    {
        PartialDate year = CreateYear(2024);
        PartialDate february = CreateMonth(2024, 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                PartialDateRules.CompareDateToInclusiveStart(new DateOnly(2023, 12, 31), year),
                Is.LessThan(0));
            Assert.That(
                PartialDateRules.CompareDateToInclusiveStart(new DateOnly(2024, 1, 1), year),
                Is.Zero);
            Assert.That(
                PartialDateRules.CompareDateToInclusiveStart(new DateOnly(2024, 1, 2), year),
                Is.GreaterThan(0));
            Assert.That(
                PartialDateRules.CompareDateToInclusiveEnd(new DateOnly(2024, 2, 28), february),
                Is.LessThan(0));
            Assert.That(
                PartialDateRules.CompareDateToInclusiveEnd(new DateOnly(2024, 2, 29), february),
                Is.Zero);
            Assert.That(
                PartialDateRules.CompareDateToInclusiveEnd(new DateOnly(2024, 3, 1), february),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void InclusiveBoundsSupportOpenAndClosedIntervals()
    {
        DateOnly start = new DateOnly(2024, 2, 1);
        DateOnly end = new DateOnly(2024, 2, 29);

        Assert.Multiple(() =>
        {
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(start, true, start, true, end),
                Is.True);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(end, true, start, true, end),
                Is.True);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(new DateOnly(2024, 1, 31), true, start, true, end),
                Is.False);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(new DateOnly(2024, 3, 1), true, start, true, end),
                Is.False);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(new DateOnly(1900, 1, 1), false, start, true, end),
                Is.True);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(new DateOnly(9999, 1, 1), true, start, false, end),
                Is.True);
            Assert.That(
                PartialDateRules.IsWithinInclusiveBounds(new DateOnly(2024, 6, 1), false, start, false, end),
                Is.True);
        });
    }

    [Test]
    public void NormalizationRejectsUnknownDates()
    {
        PartialDate unknown = new PartialDate();
        PartialDate inconsistent = new PartialDate
        {
            IsKnown = false,
            Year = 2024,
            Precision = PartialDatePrecision.Year
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => PartialDateRules.NormalizeInclusiveStartDate(unknown),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => PartialDateRules.NormalizeInclusiveEndDate(unknown),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => PartialDateRules.NormalizeInclusiveStartDate(inconsistent),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => PartialDateRules.NormalizeInclusiveEndDate(inconsistent),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void NormalizationPreservesOriginalSourcePrecisionAndComponents()
    {
        PartialDate source = CreateMonth(2024, 2);

        PartialDateRules.NormalizeInclusiveStartDate(source);
        PartialDateRules.NormalizeInclusiveEndDate(source);

        Assert.Multiple(() =>
        {
            Assert.That(source.IsKnown, Is.True);
            Assert.That(source.Year, Is.EqualTo(2024));
            Assert.That(source.Month, Is.EqualTo(2));
            Assert.That(source.Day, Is.Zero);
            Assert.That(source.Precision, Is.EqualTo(PartialDatePrecision.Month));
        });
    }

    [Test]
    public void GeneratedUsageBoundsFollowTheSharedPartialDateRules()
    {
        foreach (CurrencyUsageRow usage in GeneratedCurrencyUsageRows.Rows)
        {
            if (PartialDateRules.HasKnownPrecision(usage.SourceStartDate))
            {
                Assert.That(usage.HasInclusiveStartDate, Is.True, usage.TerritoryKey + ":" + usage.CurrencyCode);
                Assert.That(
                    usage.InclusiveStartDate,
                    Is.EqualTo(PartialDateRules.NormalizeInclusiveStartDate(usage.SourceStartDate)),
                    usage.TerritoryKey + ":" + usage.CurrencyCode);
            }
            else
            {
                Assert.That(usage.HasInclusiveStartDate, Is.False, usage.TerritoryKey + ":" + usage.CurrencyCode);
            }

            if (PartialDateRules.HasKnownPrecision(usage.SourceEndDate))
            {
                Assert.That(usage.HasInclusiveEndDate, Is.True, usage.TerritoryKey + ":" + usage.CurrencyCode);
                Assert.That(
                    usage.InclusiveEndDate,
                    Is.EqualTo(PartialDateRules.NormalizeInclusiveEndDate(usage.SourceEndDate)),
                    usage.TerritoryKey + ":" + usage.CurrencyCode);
            }
            else
            {
                Assert.That(usage.HasInclusiveEndDate, Is.False, usage.TerritoryKey + ":" + usage.CurrencyCode);
            }
        }
    }

    private static PartialDate CreateYear(int year)
    {
        return new PartialDate
        {
            IsKnown = true,
            Year = year,
            Precision = PartialDatePrecision.Year
        };
    }

    private static PartialDate CreateMonth(int year, int month)
    {
        return new PartialDate
        {
            IsKnown = true,
            Year = year,
            Month = month,
            Precision = PartialDatePrecision.Month
        };
    }

    private static PartialDate CreateDay(int year, int month, int day)
    {
        return new PartialDate
        {
            IsKnown = true,
            Year = year,
            Month = month,
            Day = day,
            Precision = PartialDatePrecision.Day
        };
    }
}
