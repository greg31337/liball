using System;

namespace currencycodes.Tests;

public class CurrencyDataStoreDefensiveTests
{
    [Test]
    public void EveryPositiveCollectionQueryReturnsFreshArraysAndObjects()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AssertFreshNonempty(
            store.FindCurrencyAlphabeticSnapshots("EUR"),
            store.FindCurrencyAlphabeticSnapshots("EUR"));
        AssertFreshNonempty(
            store.FindCurrencyNumericSnapshots("032"),
            store.FindCurrencyNumericSnapshots("032"));
        AssertFreshNonempty(
            store.FindCurrencyOfficialNameSnapshots("Euro"),
            store.FindCurrencyOfficialNameSnapshots("Euro"));
        AssertFreshNonempty(
            store.FindTerritoryKeySnapshots("BG"),
            store.FindTerritoryKeySnapshots("BG"));
        AssertFreshNonempty(
            store.FindTerritoryAlpha2Snapshots("BG"),
            store.FindTerritoryAlpha2Snapshots("BG"));
        AssertFreshNonempty(
            store.FindTerritoryAlpha3Snapshots("BGR"),
            store.FindTerritoryAlpha3Snapshots("BGR"));
        AssertFreshNonempty(
            store.FindTerritoryNumericSnapshots("100"),
            store.FindTerritoryNumericSnapshots("100"));
        AssertFreshNonempty(
            store.FindTerritoryOfficialNameSnapshots("Bulgaria"),
            store.FindTerritoryOfficialNameSnapshots("Bulgaria"));
        AssertFreshNonempty(
            store.FindUsageTerritorySnapshots("BG"),
            store.FindUsageTerritorySnapshots("BG"));
        AssertFreshNonempty(
            store.FindUsageCurrencySnapshots("EUR"),
            store.FindUsageCurrencySnapshots("EUR"));
        AssertFreshNonempty(
            store.FindFormatCurrencySnapshots("CHF"),
            store.FindFormatCurrencySnapshots("CHF"));
    }

    [Test]
    public void CurrencyMutationCannotChangeLaterStoreResults()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyInfo original = store.FindCurrencyAlphabeticSnapshots("USD")[0];
        int introductionYear = original.IntroductionDate.Year;
        bool withdrawalKnown = original.WithdrawalDate.IsKnown;

        original.AlphabeticCode = "XXX";
        original.NumericCode = "999";
        original.OfficialName = "changed";
        original.Status = CurrencyStatus.Historical;
        original.Kind = CurrencyKind.NoCurrency;
        original.IntroductionDate.Year = 1900;
        original.WithdrawalDate.IsKnown = !withdrawalKnown;

        CurrencyInfo later = store.FindCurrencyAlphabeticSnapshots("USD")[0];

        Assert.Multiple(() =>
        {
            Assert.That(later.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(later.NumericCode, Is.EqualTo("840"));
            Assert.That(later.OfficialName, Is.EqualTo("US Dollar"));
            Assert.That(later.Status, Is.EqualTo(CurrencyStatus.Active));
            Assert.That(later.Kind, Is.EqualTo(CurrencyKind.MonetaryCurrency));
            Assert.That(later.IntroductionDate.Year, Is.EqualTo(introductionYear));
            Assert.That(later.WithdrawalDate.IsKnown, Is.EqualTo(withdrawalKnown));
            Assert.That(later.IntroductionDate, Is.Not.SameAs(original.IntroductionDate));
            Assert.That(later.WithdrawalDate, Is.Not.SameAs(original.WithdrawalDate));
        });
    }

    [Test]
    public void TerritoryUsageAndFormatMutationCannotChangeLaterStoreResults()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        TerritoryInfo territory = store.FindTerritoryAlpha2Snapshots("BG")[0];
        CurrencyUsage usage = store.FindUsageTerritorySnapshots("BG")[0];
        CurrencyFormatMetadata format = store.FindFormatCurrencySnapshots("CHF")[0];
        string usageCurrency = usage.CurrencyCode;
        int usageStartYear = usage.SourceStartDate.Year;
        int usageEndYear = usage.SourceEndDate.Year;

        territory.CanonicalName = "changed";
        territory.Alpha2Code = "XX";
        usage.TerritoryKey = "XX";
        usage.CurrencyCode = "XXX";
        usage.SourceStartDate.Year = 1;
        usage.SourceEndDate.Year = 1;
        format.CurrencyCode = "XXX";
        format.CashRoundingIncrement = 99;

        TerritoryInfo laterTerritory = store.FindTerritoryAlpha2Snapshots("BG")[0];
        CurrencyUsage laterUsage = store.FindUsageTerritorySnapshots("BG")[0];
        CurrencyFormatMetadata laterFormat = store.FindFormatCurrencySnapshots("CHF")[0];

        Assert.Multiple(() =>
        {
            Assert.That(laterTerritory.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(laterTerritory.Alpha2Code, Is.EqualTo("BG"));
            Assert.That(laterUsage.TerritoryKey, Is.EqualTo("BG"));
            Assert.That(laterUsage.CurrencyCode, Is.EqualTo(usageCurrency));
            Assert.That(laterUsage.SourceStartDate.Year, Is.EqualTo(usageStartYear));
            Assert.That(laterUsage.SourceEndDate.Year, Is.EqualTo(usageEndYear));
            Assert.That(laterUsage.SourceStartDate, Is.Not.SameAs(usage.SourceStartDate));
            Assert.That(laterUsage.SourceEndDate, Is.Not.SameAs(usage.SourceEndDate));
            Assert.That(laterFormat.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(laterFormat.CashRoundingIncrement, Is.EqualTo(5));
        });
    }

    [Test]
    public void EveryZeroResultCollectionQueryReturnsANewEmptyArray()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AssertFreshEmpty(
            store.FindCurrencyAlphabeticSnapshots("missing"),
            store.FindCurrencyAlphabeticSnapshots("missing"));
        AssertFreshEmpty(
            store.FindCurrencyNumericSnapshots("missing"),
            store.FindCurrencyNumericSnapshots("missing"));
        AssertFreshEmpty(
            store.FindCurrencyOfficialNameSnapshots("missing"),
            store.FindCurrencyOfficialNameSnapshots("missing"));
        AssertFreshEmpty(
            store.FindTerritoryKeySnapshots("missing"),
            store.FindTerritoryKeySnapshots("missing"));
        AssertFreshEmpty(
            store.FindTerritoryAlpha2Snapshots("missing"),
            store.FindTerritoryAlpha2Snapshots("missing"));
        AssertFreshEmpty(
            store.FindTerritoryAlpha3Snapshots("missing"),
            store.FindTerritoryAlpha3Snapshots("missing"));
        AssertFreshEmpty(
            store.FindTerritoryNumericSnapshots("missing"),
            store.FindTerritoryNumericSnapshots("missing"));
        AssertFreshEmpty(
            store.FindTerritoryOfficialNameSnapshots("missing"),
            store.FindTerritoryOfficialNameSnapshots("missing"));
        AssertFreshEmpty(
            store.FindUsageTerritorySnapshots("missing"),
            store.FindUsageTerritorySnapshots("missing"));
        AssertFreshEmpty(
            store.FindUsageCurrencySnapshots("missing"),
            store.FindUsageCurrencySnapshots("missing"));
        AssertFreshEmpty(
            store.FindFormatCurrencySnapshots("missing"),
            store.FindFormatCurrencySnapshots("missing"));
    }

    private static void AssertFreshNonempty(Array first, Array second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first.Length, Is.GreaterThan(0));
            Assert.That(second.Length, Is.EqualTo(first.Length));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.GetValue(0), Is.Not.SameAs(second.GetValue(0)));
        });
    }

    private static void AssertFreshEmpty(Array first, Array second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first, Is.Empty);
            Assert.That(second, Is.Empty);
            Assert.That(first, Is.Not.SameAs(second));
        });
    }
}
