using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class HistoricalTransitionTraversalTests
{
    [Test]
    public void SuccessorResolutionHandlesRenameMergerAndSplit()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        AssertCurrentSuccessor(
            catalog.ResolveHistoricalSuccessors("bu"),
            "MM");
        AssertCurrentSuccessor(
            catalog.ResolveHistoricalSuccessors("DDDE"),
            "DE");
        AssertCurrentCandidates(
            catalog.ResolveHistoricalSuccessors("an"),
            new[] { "BQ", "CW", "SX" });
    }

    [Test]
    public void SuccessorResolutionFollowsMultipleStepsAndDeduplicatesCandidates()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        HistoricalLookupResult alpha4 =
            catalog.ResolveHistoricalSuccessors("yucs");
        AssertCurrentCandidates(alpha4, new[] { "ME", "RS" });
        alpha4.CountryCandidates[0].Alpha2 = "changed";

        AssertCurrentCandidates(
            catalog.ResolveHistoricalSuccessors("YU"),
            new[] { "ME", "RS" });
        AssertCurrentCandidates(
            catalog.ResolveHistoricalSuccessors("891"),
            new[] { "ME", "RS" });
    }

    [Test]
    public void SuccessorResolutionDistinguishesInvalidUnknownAndCurrentInputs()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors(null!),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors(string.Empty),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("A"),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("A1"),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("12A"),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("AB1D"),
            LookupStatus.InvalidInput);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("ZZ"),
            LookupStatus.NotFound);
        AssertEmptyResult(
            catalog.ResolveHistoricalSuccessors("RS"),
            LookupStatus.NotFound);
    }

    [Test]
    public void ChangeHistoryTraversesPredecessorsAndSuccessorsChronologically()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        AssertChangeHistory(catalog.GetCountryChangeHistory("YUCS"));
        AssertChangeHistory(catalog.GetCountryChangeHistory("CSXX"));
        AssertChangeHistory(catalog.GetCountryChangeHistory("RS"));
        AssertChangeHistory(catalog.GetCountryChangeHistory("srb"));
        AssertChangeHistory(catalog.GetCountryChangeHistory("688"));
        AssertChangeHistory(catalog.GetCountryChangeHistory("891"));
    }

    [Test]
    public void ChangeHistoryIncludesEveryDirectPredecessorWithoutCrossingSplitSiblings()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        CountryChange[] belarus = catalog.GetCountryChangeHistory("BY");
        Assert.That(
            belarus.Select(value => value.Alpha4),
            Is.EqualTo(new[] { "BYAA", "SUHH" }));

        CountryChange[] russia = catalog.GetCountryChangeHistory("RU");
        Assert.That(
            russia.Select(value => value.Alpha4),
            Is.EqualTo(new[] { "SUHH" }));
    }

    [Test]
    public void ChangeHistoryReturnsDetachedDataAndEmptyForInvalidOrUnknownInput()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        CountryChange[] first = catalog.GetCountryChangeHistory("DD");
        Assert.That(first, Has.Length.EqualTo(1));
        Assert.That(first[0].Alpha4, Is.EqualTo("DDDE"));
        first[0].Alpha4 = "changed";
        first[0].SuccessorIdentifiers[0] = "changed";

        CountryChange[] second = catalog.GetCountryChangeHistory("DD");
        Assert.That(second[0].Alpha4, Is.EqualTo("DDDE"));
        Assert.That(second[0].SuccessorIdentifiers, Is.EqualTo(new[] { "DE" }));
        Assert.That(catalog.GetCountryChangeHistory(null!), Is.Empty);
        Assert.That(catalog.GetCountryChangeHistory("A1"), Is.Empty);
        Assert.That(catalog.GetCountryChangeHistory("ZZ"), Is.Empty);
    }

    [Test]
    public void ChangesCanBeEnumeratedByKindInStableAlpha4Order()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        CountryChange[] significant =
            catalog.GetCountryChangesByKind(CountryChangeKind.SignificantChange);
        Assert.That(significant, Is.Not.Empty);
        Assert.That(significant.All(value => value.Kind == CountryChangeKind.SignificantChange));
        string[] significantCodes = significant.Select(value => value.Alpha4).ToArray();
        string[] sortedCodes = (string[])significantCodes.Clone();
        Array.Sort(sortedCodes, StringComparer.Ordinal);
        Assert.That(significantCodes, Is.EqualTo(sortedCodes));

        CountryChange[] mergers =
            catalog.GetCountryChangesByKind(CountryChangeKind.Merge);
        Assert.That(mergers, Is.Not.Empty);
        Assert.That(mergers.All(value => value.Kind == CountryChangeKind.Merge));

        CountryChange[] splits =
            catalog.GetCountryChangesByKind(CountryChangeKind.Split);
        Assert.That(splits, Is.Not.Empty);
        Assert.That(splits.All(value => value.Kind == CountryChangeKind.Split));
        Assert.That(
            catalog.GetCountryChangesByKind(CountryChangeKind.OtherOrUnknown),
            Is.Empty);

        mergers[0].Alpha4 = "changed";
        Assert.That(
            catalog.GetCountryChangesByKind(CountryChangeKind.Merge)[0].Alpha4,
            Is.Not.EqualTo("changed"));
    }

    private static void AssertChangeHistory(CountryChange[] changes)
    {
        Assert.That(
            changes.Select(value => value.Alpha4),
            Is.EqualTo(new[] { "YUCS", "CSXX" }));
        Assert.That(
            changes.Select(value => value.EffectiveDate),
            Is.EqualTo(new[] { "2003-07-23", "2006-09-26" }));
    }

    private static void AssertCurrentSuccessor(
        HistoricalLookupResult result,
        string expectedAlpha2)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Country, Is.Not.Null);
        Assert.That(result.Country!.Alpha2, Is.EqualTo(expectedAlpha2));
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(result.CountryChange, Is.Null);
        Assert.That(result.CountryCandidates, Is.Empty);
        Assert.That(result.HistoricalCountryCandidates, Is.Empty);
        Assert.That(result.CountryChangeCandidates, Is.Empty);
    }

    private static void AssertCurrentCandidates(
        HistoricalLookupResult result,
        string[] expectedAlpha2)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(result.CountryChange, Is.Null);
        Assert.That(
            result.CountryCandidates.Select(value => value.Alpha2),
            Is.EqualTo(expectedAlpha2));
        Assert.That(result.HistoricalCountryCandidates, Is.Empty);
        Assert.That(result.CountryChangeCandidates, Is.Empty);
    }

    private static void AssertEmptyResult(
        HistoricalLookupResult result,
        LookupStatus expectedStatus)
    {
        Assert.That(result.Status, Is.EqualTo(expectedStatus));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(result.CountryChange, Is.Null);
        Assert.That(result.CountryCandidates, Is.Empty);
        Assert.That(result.HistoricalCountryCandidates, Is.Empty);
        Assert.That(result.CountryChangeCandidates, Is.Empty);
    }
}
