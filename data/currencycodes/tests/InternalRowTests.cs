using System;

namespace currencycodes.Tests;

public class InternalRowTests
{
    [Test]
    public void CurrencyRowPreservesIdentityClassificationAndDates()
    {
        PartialDate introduction = new PartialDate
        {
            IsKnown = true,
            Year = 1999,
            Precision = PartialDatePrecision.Year
        };
        PartialDate withdrawal = new PartialDate
        {
            IsKnown = true,
            Year = 2002,
            Month = 2,
            Day = 28,
            Precision = PartialDatePrecision.Day
        };
        CurrencyRow row = new CurrencyRow(
            "BEF",
            true,
            "056",
            56,
            "Belgian Franc",
            true,
            2,
            CurrencyStatus.Historical,
            CurrencyKind.MonetaryCurrency,
            true,
            true,
            false,
            true,
            introduction,
            withdrawal);

        Assert.Multiple(() =>
        {
            Assert.That(row.AlphabeticCode, Is.EqualTo("BEF"));
            Assert.That(row.HasNumericCode, Is.True);
            Assert.That(row.NumericCode, Is.EqualTo("056"));
            Assert.That(row.NumericCodeValue, Is.EqualTo(56));
            Assert.That(row.OfficialName, Is.EqualTo("Belgian Franc"));
            Assert.That(row.HasIsoMinorUnit, Is.True);
            Assert.That(row.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(row.Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(row.Kind, Is.EqualTo(CurrencyKind.MonetaryCurrency));
            Assert.That(row.IdentityFromIso, Is.True);
            Assert.That(row.HistoricalStatusFromIso, Is.True);
            Assert.That(row.FundClassificationFromIso, Is.False);
            Assert.That(row.EnrichedFromCldr, Is.True);
            Assert.That(row.IntroductionDate, Is.SameAs(introduction));
            Assert.That(row.WithdrawalDate, Is.SameAs(withdrawal));
        });
    }

    [Test]
    public void TerritoryRowPreservesStableKeyAndEveryCodeRepresentation()
    {
        TerritoryRow row = new TerritoryRow(
            "BG",
            "Bulgaria",
            true,
            "BG",
            true,
            "BGR",
            true,
            "100",
            100,
            TerritoryKind.Country);

        Assert.Multiple(() =>
        {
            Assert.That(row.TerritoryKey, Is.EqualTo("BG"));
            Assert.That(row.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(row.HasAlpha2Code, Is.True);
            Assert.That(row.Alpha2Code, Is.EqualTo("BG"));
            Assert.That(row.HasAlpha3Code, Is.True);
            Assert.That(row.Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(row.HasNumericCode, Is.True);
            Assert.That(row.NumericCode, Is.EqualTo("100"));
            Assert.That(row.NumericCodeValue, Is.EqualTo(100));
            Assert.That(row.Kind, Is.EqualTo(TerritoryKind.Country));
        });
    }

    [Test]
    public void CurrencyUsageRowPreservesNormalizedIntervalAndSources()
    {
        PartialDate sourceStart = new PartialDate
        {
            IsKnown = true,
            Year = 1999,
            Precision = PartialDatePrecision.Year
        };
        PartialDate sourceEnd = new PartialDate();
        CurrencyUsageRow row = new CurrencyUsageRow(
            "BG",
            "EUR",
            sourceStart,
            sourceEnd,
            true,
            new DateOnly(1999, 1, 1),
            false,
            default(DateOnly),
            true,
            true,
            true,
            false,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(row.TerritoryKey, Is.EqualTo("BG"));
            Assert.That(row.CurrencyCode, Is.EqualTo("EUR"));
            Assert.That(row.SourceStartDate, Is.SameAs(sourceStart));
            Assert.That(row.SourceEndDate, Is.SameAs(sourceEnd));
            Assert.That(row.HasInclusiveStartDate, Is.True);
            Assert.That(row.InclusiveStartDate, Is.EqualTo(new DateOnly(1999, 1, 1)));
            Assert.That(row.HasInclusiveEndDate, Is.False);
            Assert.That(row.InclusiveEndDate, Is.EqualTo(default(DateOnly)));
            Assert.That(row.IsTender, Is.True);
            Assert.That(row.IsCurrent, Is.True);
            Assert.That(row.FromIsoListOne, Is.True);
            Assert.That(row.FromIsoListThree, Is.False);
            Assert.That(row.FromCldr, Is.True);
        });
    }

    [Test]
    public void CurrencyFormatRowKeepsRuleFamiliesSeparate()
    {
        CurrencyFormatRow row = new CurrencyFormatRow(
            "CHF",
            true,
            2,
            100m,
            true,
            2,
            0,
            100m,
            true,
            2,
            5,
            100m,
            "48.2");

        Assert.Multiple(() =>
        {
            Assert.That(row.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(row.HasIsoMinorUnit, Is.True);
            Assert.That(row.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(row.IsoMinorUnitFactor, Is.EqualTo(100m));
            Assert.That(row.HasStandardFormat, Is.True);
            Assert.That(row.StandardFormatDigits, Is.EqualTo(2));
            Assert.That(row.StandardRoundingIncrement, Is.Zero);
            Assert.That(row.StandardFormatFactor, Is.EqualTo(100m));
            Assert.That(row.HasCash, Is.True);
            Assert.That(row.CashDigits, Is.EqualTo(2));
            Assert.That(row.CashRoundingIncrement, Is.EqualTo(5));
            Assert.That(row.CashFactor, Is.EqualTo(100m));
            Assert.That(row.SourceVersion, Is.EqualTo("48.2"));
        });
    }
}
