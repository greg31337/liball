using System;

namespace currencycodes.Tests;

public class CurrencySnapshotFactoryTests
{
    [Test]
    public void CurrencySnapshotCopiesEveryFieldAndNestedDate()
    {
        CurrencyRow row = CreateCurrencyRow("USD", CurrencyStatus.Active);

        CurrencyInfo snapshot = CurrencySnapshotFactory.CreateCurrencyInfo(row);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.AlphabeticCode, Is.EqualTo(row.AlphabeticCode));
            Assert.That(snapshot.HasNumericCode, Is.EqualTo(row.HasNumericCode));
            Assert.That(snapshot.NumericCode, Is.EqualTo(row.NumericCode));
            Assert.That(snapshot.NumericCodeValue, Is.EqualTo(row.NumericCodeValue));
            Assert.That(snapshot.OfficialName, Is.EqualTo(row.OfficialName));
            Assert.That(snapshot.HasIsoMinorUnit, Is.EqualTo(row.HasIsoMinorUnit));
            Assert.That(snapshot.IsoMinorUnitDigits, Is.EqualTo(row.IsoMinorUnitDigits));
            Assert.That(snapshot.Status, Is.EqualTo(row.Status));
            Assert.That(snapshot.Kind, Is.EqualTo(row.Kind));
            Assert.That(snapshot.IdentityFromIso, Is.EqualTo(row.IdentityFromIso));
            Assert.That(snapshot.HistoricalStatusFromIso, Is.EqualTo(row.HistoricalStatusFromIso));
            Assert.That(snapshot.FundClassificationFromIso, Is.EqualTo(row.FundClassificationFromIso));
            Assert.That(snapshot.EnrichedFromCldr, Is.EqualTo(row.EnrichedFromCldr));
            Assert.That(snapshot.IntroductionDate, Is.Not.SameAs(row.IntroductionDate));
            Assert.That(snapshot.WithdrawalDate, Is.Not.SameAs(row.WithdrawalDate));
            Assert.That(snapshot.IntroductionDate.Year, Is.EqualTo(row.IntroductionDate.Year));
            Assert.That(snapshot.WithdrawalDate.Year, Is.EqualTo(row.WithdrawalDate.Year));
        });

        snapshot.IntroductionDate.Year = 1900;
        snapshot.WithdrawalDate.IsKnown = false;

        Assert.Multiple(() =>
        {
            Assert.That(row.IntroductionDate.Year, Is.EqualTo(2000));
            Assert.That(row.WithdrawalDate.IsKnown, Is.True);
        });
    }

    [Test]
    public void TerritorySnapshotCopiesEveryRowField()
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

        TerritoryInfo snapshot = CurrencySnapshotFactory.CreateTerritoryInfo(row);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(snapshot.HasAlpha2Code, Is.True);
            Assert.That(snapshot.Alpha2Code, Is.EqualTo("BG"));
            Assert.That(snapshot.HasAlpha3Code, Is.True);
            Assert.That(snapshot.Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(snapshot.HasNumericCode, Is.True);
            Assert.That(snapshot.NumericCode, Is.EqualTo("100"));
            Assert.That(snapshot.NumericCodeValue, Is.EqualTo(100));
            Assert.That(snapshot.Kind, Is.EqualTo(TerritoryKind.Country));
        });
    }

    [Test]
    public void UsageSnapshotCopiesIntervalsProvenanceAndNestedDates()
    {
        PartialDate start = CreateDate(1999, 1, 1);
        PartialDate end = CreateDate(2002, 2, 28);
        CurrencyUsageRow row = new CurrencyUsageRow(
            "BE",
            "BEF",
            start,
            end,
            true,
            new DateOnly(1999, 1, 1),
            true,
            new DateOnly(2002, 2, 28),
            true,
            false,
            false,
            true,
            true);

        CurrencyUsage snapshot = CurrencySnapshotFactory.CreateCurrencyUsage(row);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.TerritoryKey, Is.EqualTo("BE"));
            Assert.That(snapshot.CurrencyCode, Is.EqualTo("BEF"));
            Assert.That(snapshot.SourceStartDate, Is.Not.SameAs(start));
            Assert.That(snapshot.SourceEndDate, Is.Not.SameAs(end));
            Assert.That(snapshot.SourceStartDate.Year, Is.EqualTo(1999));
            Assert.That(snapshot.SourceEndDate.Year, Is.EqualTo(2002));
            Assert.That(snapshot.HasInclusiveStartDate, Is.True);
            Assert.That(snapshot.InclusiveStartDate, Is.EqualTo(new DateOnly(1999, 1, 1)));
            Assert.That(snapshot.HasInclusiveEndDate, Is.True);
            Assert.That(snapshot.InclusiveEndDate, Is.EqualTo(new DateOnly(2002, 2, 28)));
            Assert.That(snapshot.IsTender, Is.True);
            Assert.That(snapshot.IsCurrent, Is.False);
            Assert.That(snapshot.FromIsoListOne, Is.False);
            Assert.That(snapshot.FromIsoListThree, Is.True);
            Assert.That(snapshot.FromCldr, Is.True);
        });
    }

    [Test]
    public void FormatSnapshotCopiesAllIndependentRuleFamilies()
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

        CurrencyFormatMetadata snapshot = CurrencySnapshotFactory.CreateCurrencyFormatMetadata(row);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(snapshot.HasIsoMinorUnit, Is.True);
            Assert.That(snapshot.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(snapshot.IsoMinorUnitFactor, Is.EqualTo(100m));
            Assert.That(snapshot.HasStandardFormat, Is.True);
            Assert.That(snapshot.StandardFormatDigits, Is.EqualTo(2));
            Assert.That(snapshot.StandardRoundingIncrement, Is.Zero);
            Assert.That(snapshot.StandardFormatFactor, Is.EqualTo(100m));
            Assert.That(snapshot.HasCash, Is.True);
            Assert.That(snapshot.CashDigits, Is.EqualTo(2));
            Assert.That(snapshot.CashRoundingIncrement, Is.EqualTo(5));
            Assert.That(snapshot.CashFactor, Is.EqualTo(100m));
            Assert.That(snapshot.SourceVersion, Is.EqualTo("48.2"));
        });
    }

    [Test]
    public void SnapshotArraysContainFreshObjectsOnEveryCall()
    {
        CurrencyRow row = CreateCurrencyRow("USD", CurrencyStatus.Active);
        CurrencyRow[] rows = new[] { row };

        CurrencyInfo[] first = CurrencySnapshotFactory.CreateCurrencyInfoArray(rows);
        CurrencyInfo[] second = CurrencySnapshotFactory.CreateCurrencyInfoArray(rows);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(first[0].IntroductionDate, Is.Not.SameAs(second[0].IntroductionDate));
        });

        first[0].AlphabeticCode = "XXX";
        first[0].IntroductionDate.Year = 1900;

        Assert.Multiple(() =>
        {
            Assert.That(second[0].AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(second[0].IntroductionDate.Year, Is.EqualTo(2000));
            Assert.That(row.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(row.IntroductionDate.Year, Is.EqualTo(2000));
        });
    }

    [Test]
    public void LookupSnapshotsDeepCopyPrimaryMatchesAndMatchArrays()
    {
        CurrencyInfo primary = CurrencySnapshotFactory.CreateCurrencyInfo(
            CreateCurrencyRow("USD", CurrencyStatus.Active));
        CurrencyInfo historical = CurrencySnapshotFactory.CreateCurrencyInfo(
            CreateCurrencyRow("USD", CurrencyStatus.Historical));
        CurrencyLookupResult currencySource = new CurrencyLookupResult
        {
            Found = true,
            Error = LookupError.None,
            NormalizedQuery = "USD",
            Match = primary,
            Matches = new[] { primary, historical }
        };
        TerritoryInfo territory = new TerritoryInfo
        {
            CanonicalName = "Bulgaria",
            Alpha2Code = "BG",
            HasAlpha2Code = true
        };
        TerritoryLookupResult territorySource = new TerritoryLookupResult
        {
            Found = true,
            Error = LookupError.None,
            NormalizedQuery = "BG",
            Match = territory,
            Matches = new[] { territory }
        };

        CurrencyLookupResult currencyCopy = CurrencySnapshotFactory.CopyCurrencyLookupResult(currencySource);
        TerritoryLookupResult territoryCopy = CurrencySnapshotFactory.CopyTerritoryLookupResult(territorySource);

        Assert.Multiple(() =>
        {
            Assert.That(currencyCopy.Found, Is.True);
            Assert.That(currencyCopy.NormalizedQuery, Is.EqualTo("USD"));
            Assert.That(currencyCopy.Match, Is.Not.SameAs(primary));
            Assert.That(currencyCopy.Matches, Is.Not.SameAs(currencySource.Matches));
            Assert.That(currencyCopy.Matches[0], Is.Not.SameAs(primary));
            Assert.That(currencyCopy.Matches[0], Is.Not.SameAs(currencyCopy.Match));
            Assert.That(currencyCopy.Matches[0].IntroductionDate, Is.Not.SameAs(primary.IntroductionDate));
            Assert.That(territoryCopy.Match, Is.Not.SameAs(territory));
            Assert.That(territoryCopy.Matches, Is.Not.SameAs(territorySource.Matches));
            Assert.That(territoryCopy.Matches[0], Is.Not.SameAs(territory));
        });
    }

    [Test]
    public void DataVersionSnapshotOwnsItsChecksumArray()
    {
        DataVersionInfo first = CurrencySnapshotFactory.CreateDataVersionInfo();
        DataVersionInfo second = CurrencySnapshotFactory.CreateDataVersionInfo();
        string expectedFirstChecksum = GeneratedDataVersion.SourceChecksums[0];

        Assert.Multiple(() =>
        {
            Assert.That(first.IsoPublicationDate, Is.EqualTo(GeneratedDataVersion.IsoPublicationDate));
            Assert.That(first.CldrVersion, Is.EqualTo(GeneratedDataVersion.CldrVersion));
            Assert.That(first.GeneratorSchemaVersion, Is.EqualTo(GeneratedDataVersion.GeneratorSchemaVersion));
            Assert.That(first.SnapshotIdentifier, Is.EqualTo(GeneratedDataVersion.SnapshotIdentifier));
            Assert.That(first.SourceChecksums, Is.EqualTo(GeneratedDataVersion.SourceChecksums));
            Assert.That(first.SourceChecksums, Is.Not.SameAs(GeneratedDataVersion.SourceChecksums));
            Assert.That(first.SourceChecksums, Is.Not.SameAs(second.SourceChecksums));
        });

        first.SourceChecksums[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.SourceChecksums[0], Is.EqualTo(expectedFirstChecksum));
            Assert.That(GeneratedDataVersion.SourceChecksums[0], Is.EqualTo(expectedFirstChecksum));
        });
    }

    private static CurrencyRow CreateCurrencyRow(string code, CurrencyStatus status)
    {
        return new CurrencyRow(
            code,
            true,
            "840",
            840,
            "US Dollar",
            true,
            2,
            status,
            CurrencyKind.MonetaryCurrency,
            true,
            status == CurrencyStatus.Historical,
            false,
            true,
            CreateDate(2000, 1, 1),
            CreateDate(2020, 12, 31));
    }

    private static PartialDate CreateDate(int year, int month, int day)
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
