using System;

namespace currencycodes.Tests;

public class GeneratedRuntimeDataTests
{
    [Test]
    public void GeneratedTablesContainTheCompletePinnedSnapshot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedCurrencyRows.Rows, Has.Length.EqualTo(349));
            Assert.That(GeneratedTerritoryRows.Rows, Has.Length.EqualTo(287));
            Assert.That(GeneratedCurrencyUsageRows.Rows, Has.Length.EqualTo(552));
            Assert.That(GeneratedCurrencyFormatRows.Rows, Has.Length.EqualTo(326));
            Assert.That(GeneratedDataVersion.SourceChecksums, Has.Length.EqualTo(18));
        });
    }

    [Test]
    public void GeneratedCurrenciesPreserveIdentityStatusAndReviewedKinds()
    {
        CurrencyRow usd = FindCurrency("USD", CurrencyStatus.Active);
        CurrencyRow xau = FindCurrency("XAU", CurrencyStatus.Active);
        CurrencyRow xxx = FindCurrency("XXX", CurrencyStatus.Active);
        CurrencyRow bef = FindCurrency("BEF", CurrencyStatus.Historical);
        CurrencyRow arl = FindCurrency("ARL", CurrencyStatus.Historical);
        CurrencyRow cnh = FindCurrency("CNH", CurrencyStatus.Active);

        Assert.Multiple(() =>
        {
            Assert.That(usd.NumericCode, Is.EqualTo("840"));
            Assert.That(usd.NumericCodeValue, Is.EqualTo(840));
            Assert.That(usd.HasIsoMinorUnit, Is.True);
            Assert.That(usd.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(usd.Kind, Is.EqualTo(CurrencyKind.MonetaryCurrency));
            Assert.That(usd.IdentityFromIso, Is.True);
            Assert.That(usd.EnrichedFromCldr, Is.True);
            Assert.That(xau.Kind, Is.EqualTo(CurrencyKind.PreciousMetal));
            Assert.That(xxx.Kind, Is.EqualTo(CurrencyKind.NoCurrency));
            Assert.That(bef.HistoricalStatusFromIso, Is.True);
            Assert.That(bef.WithdrawalDate.IsKnown, Is.True);
            Assert.That(arl.HasNumericCode, Is.False);
            Assert.That(arl.OfficialName, Is.Empty);
            Assert.That(arl.Kind, Is.EqualTo(CurrencyKind.Unknown));
            Assert.That(arl.IdentityFromIso, Is.False);
            Assert.That(arl.IntroductionDate.IsKnown, Is.True);
            Assert.That(arl.WithdrawalDate.IsKnown, Is.True);
            Assert.That(cnh.IdentityFromIso, Is.False);
            Assert.That(cnh.EnrichedFromCldr, Is.True);
        });
    }

    [Test]
    public void GeneratedTerritoriesAndUsagesPreserveCodesIntervalsAndSources()
    {
        TerritoryRow bulgaria = FindTerritory("BG");
        TerritoryRow europeanUnion = FindTerritory("EU");
        CurrencyUsageRow lev = FindUsage("BG", "BGN");
        CurrencyUsageRow euro = FindUsage("BG", "EUR");

        Assert.Multiple(() =>
        {
            Assert.That(bulgaria.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(bulgaria.Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(bulgaria.NumericCode, Is.EqualTo("100"));
            Assert.That(europeanUnion.Kind, Is.EqualTo(TerritoryKind.Supranational));
            Assert.That(lev.HasInclusiveEndDate, Is.True);
            Assert.That(lev.InclusiveEndDate, Is.EqualTo(new DateOnly(2026, 1, 31)));
            Assert.That(lev.FromIsoListOne, Is.False);
            Assert.That(lev.FromCldr, Is.True);
            Assert.That(euro.InclusiveStartDate, Is.EqualTo(new DateOnly(2026, 1, 1)));
            Assert.That(euro.IsTender, Is.True);
            Assert.That(euro.IsCurrent, Is.True);
            Assert.That(euro.FromIsoListOne, Is.True);
        });
    }

    [Test]
    public void GeneratedFormatsKeepIsoStandardAndCashRulesSeparate()
    {
        CurrencyFormatRow swissFranc = FindFormat("CHF");

        Assert.Multiple(() =>
        {
            Assert.That(swissFranc.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(swissFranc.IsoMinorUnitFactor, Is.EqualTo(100m));
            Assert.That(swissFranc.StandardFormatDigits, Is.EqualTo(2));
            Assert.That(swissFranc.StandardRoundingIncrement, Is.Zero);
            Assert.That(swissFranc.CashDigits, Is.EqualTo(2));
            Assert.That(swissFranc.CashRoundingIncrement, Is.EqualTo(5));
            Assert.That(swissFranc.SourceVersion, Is.EqualTo("48.2"));
        });
    }

    [Test]
    public void GeneratedLookupIndexesCoverTheirRowsAndOptionalKeys()
    {
        AssertCompletePermutation(
            GeneratedLookupIndexes.CurrencyAlphabeticIndexes,
            GeneratedCurrencyRows.Rows.Length);
        AssertCompletePermutation(
            GeneratedLookupIndexes.CurrencyOfficialNameIndexes,
            GeneratedCurrencyRows.Rows.Length);
        AssertOptionalIndex(
            GeneratedLookupIndexes.CurrencyNumericIndexes,
            GeneratedCurrencyRows.Rows.Length,
            CountCurrenciesWithNumericCodes());
        AssertCompletePermutation(
            GeneratedLookupIndexes.TerritoryKeyIndexes,
            GeneratedTerritoryRows.Rows.Length);
        AssertCompletePermutation(
            GeneratedLookupIndexes.TerritoryOfficialNameIndexes,
            GeneratedTerritoryRows.Rows.Length);
        AssertOptionalIndex(
            GeneratedLookupIndexes.TerritoryAlpha2Indexes,
            GeneratedTerritoryRows.Rows.Length,
            CountTerritoriesWithAlpha2Codes());
        AssertOptionalIndex(
            GeneratedLookupIndexes.TerritoryAlpha3Indexes,
            GeneratedTerritoryRows.Rows.Length,
            CountTerritoriesWithAlpha3Codes());
        AssertOptionalIndex(
            GeneratedLookupIndexes.TerritoryNumericIndexes,
            GeneratedTerritoryRows.Rows.Length,
            CountTerritoriesWithNumericCodes());
        AssertCompletePermutation(
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            GeneratedCurrencyUsageRows.Rows.Length);
        AssertCompletePermutation(
            GeneratedLookupIndexes.UsageCurrencyIndexes,
            GeneratedCurrencyUsageRows.Rows.Length);
        AssertCompletePermutation(
            GeneratedLookupIndexes.FormatCurrencyIndexes,
            GeneratedCurrencyFormatRows.Rows.Length);
    }

    [Test]
    public void GeneratedUsageAndFormatRowsReferenceGeneratedIdentities()
    {
        foreach (CurrencyUsageRow usage in GeneratedCurrencyUsageRows.Rows)
        {
            Assert.That(HasTerritory(usage.TerritoryKey), Is.True, usage.TerritoryKey);
            Assert.That(HasCurrency(usage.CurrencyCode), Is.True, usage.CurrencyCode);
        }

        foreach (CurrencyFormatRow format in GeneratedCurrencyFormatRows.Rows)
        {
            Assert.That(HasCurrency(format.CurrencyCode), Is.True, format.CurrencyCode);
        }
    }

    [Test]
    public void GeneratedLookupIndexesAreOrdinallySortedByTheirLookupKeys()
    {
        AssertSorted(
            GeneratedLookupIndexes.CurrencyAlphabeticIndexes,
            index => GeneratedCurrencyRows.Rows[index].AlphabeticCode);
        AssertSorted(
            GeneratedLookupIndexes.CurrencyNumericIndexes,
            index => GeneratedCurrencyRows.Rows[index].NumericCode);
        AssertSorted(
            GeneratedLookupIndexes.CurrencyOfficialNameIndexes,
            index => GeneratedCurrencyRows.Rows[index].OfficialName);
        AssertSorted(
            GeneratedLookupIndexes.TerritoryKeyIndexes,
            index => GeneratedTerritoryRows.Rows[index].TerritoryKey);
        AssertSorted(
            GeneratedLookupIndexes.TerritoryAlpha2Indexes,
            index => GeneratedTerritoryRows.Rows[index].Alpha2Code);
        AssertSorted(
            GeneratedLookupIndexes.TerritoryAlpha3Indexes,
            index => GeneratedTerritoryRows.Rows[index].Alpha3Code);
        AssertSorted(
            GeneratedLookupIndexes.TerritoryNumericIndexes,
            index => GeneratedTerritoryRows.Rows[index].NumericCode);
        AssertSorted(
            GeneratedLookupIndexes.TerritoryOfficialNameIndexes,
            index => GeneratedTerritoryRows.Rows[index].CanonicalName);
        AssertSorted(
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            index => GeneratedCurrencyUsageRows.Rows[index].TerritoryKey);
        AssertSorted(
            GeneratedLookupIndexes.UsageCurrencyIndexes,
            index => GeneratedCurrencyUsageRows.Rows[index].CurrencyCode);
        AssertSorted(
            GeneratedLookupIndexes.FormatCurrencyIndexes,
            index => GeneratedCurrencyFormatRows.Rows[index].CurrencyCode);
    }

    [Test]
    public void GeneratedVersionMatchesThePinnedPublicMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedDataVersion.IsoPublicationDate, Is.EqualTo(CurrencyDataVersions.IsoPublicationDate));
            Assert.That(GeneratedDataVersion.CldrVersion, Is.EqualTo(CurrencyDataVersions.CldrVersion));
            Assert.That(GeneratedDataVersion.GeneratorSchemaVersion, Is.EqualTo(CurrencyDataVersions.GeneratorSchemaVersion));
            Assert.That(GeneratedDataVersion.SnapshotIdentifier, Is.EqualTo(CurrencyDataVersions.SnapshotIdentifier));
            Assert.That(GeneratedDataVersion.SourceChecksums[0], Does.StartWith("data/overrides/currency-kinds.csv\t"));
            Assert.That(GeneratedDataVersion.SourceChecksums[17], Does.StartWith("specs/unicode-tr35.html\t"));
        });
    }

    private static CurrencyRow FindCurrency(string code, CurrencyStatus status)
    {
        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            if (row.AlphabeticCode == code && row.Status == status)
            {
                return row;
            }
        }

        throw new AssertionException("The generated currency row was not found.");
    }

    private static TerritoryRow FindTerritory(string key)
    {
        foreach (TerritoryRow row in GeneratedTerritoryRows.Rows)
        {
            if (row.TerritoryKey == key)
            {
                return row;
            }
        }

        throw new AssertionException("The generated territory row was not found.");
    }

    private static CurrencyUsageRow FindUsage(string territoryKey, string currencyCode)
    {
        foreach (CurrencyUsageRow row in GeneratedCurrencyUsageRows.Rows)
        {
            if (row.TerritoryKey == territoryKey && row.CurrencyCode == currencyCode)
            {
                return row;
            }
        }

        throw new AssertionException("The generated currency-usage row was not found.");
    }

    private static CurrencyFormatRow FindFormat(string currencyCode)
    {
        foreach (CurrencyFormatRow row in GeneratedCurrencyFormatRows.Rows)
        {
            if (row.CurrencyCode == currencyCode)
            {
                return row;
            }
        }

        throw new AssertionException("The generated currency-format row was not found.");
    }

    private static bool HasCurrency(string code)
    {
        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            if (row.AlphabeticCode == code)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTerritory(string key)
    {
        foreach (TerritoryRow row in GeneratedTerritoryRows.Rows)
        {
            if (row.TerritoryKey == key)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertCompletePermutation(int[] indexes, int rowCount)
    {
        Assert.That(indexes, Has.Length.EqualTo(rowCount));
        AssertOptionalIndex(indexes, rowCount, rowCount);
    }

    private static void AssertOptionalIndex(int[] indexes, int rowCount, int expectedCount)
    {
        bool[] seen = new bool[rowCount];
        Assert.That(indexes, Has.Length.EqualTo(expectedCount));
        foreach (int index in indexes)
        {
            Assert.That(index, Is.InRange(0, rowCount - 1));
            Assert.That(seen[index], Is.False);
            seen[index] = true;
        }
    }

    private static void AssertSorted(int[] indexes, Func<int, string> keySelector)
    {
        for (int position = 1; position < indexes.Length; position++)
        {
            string previous = keySelector(indexes[position - 1]);
            string current = keySelector(indexes[position]);
            Assert.That(string.CompareOrdinal(previous, current), Is.LessThanOrEqualTo(0));
        }
    }

    private static int CountCurrenciesWithNumericCodes()
    {
        int count = 0;
        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            if (row.HasNumericCode)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountTerritoriesWithAlpha2Codes()
    {
        int count = 0;
        foreach (TerritoryRow row in GeneratedTerritoryRows.Rows)
        {
            if (row.HasAlpha2Code)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountTerritoriesWithAlpha3Codes()
    {
        int count = 0;
        foreach (TerritoryRow row in GeneratedTerritoryRows.Rows)
        {
            if (row.HasAlpha3Code)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountTerritoriesWithNumericCodes()
    {
        int count = 0;
        foreach (TerritoryRow row in GeneratedTerritoryRows.Rows)
        {
            if (row.HasNumericCode)
            {
                count++;
            }
        }

        return count;
    }
}
