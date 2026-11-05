namespace currencycodes.Tests;

public class CurrencyAssignmentValidationTests
{
    [Test]
    public void ActiveHistoricalAndAllScopesReturnValidMatchedSnapshots()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult active = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            " \u2003usd\u00A0 ",
            CurrencySet.ActiveOnly);
        CurrencyValidationResult historical = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "bgn",
            CurrencySet.HistoricalOnly);
        CurrencyValidationResult all = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "eur",
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            AssertValid(active, "USD", CurrencyStatus.Active);
            AssertValid(historical, "BGN", CurrencyStatus.Historical);
            AssertValid(all, "EUR", CurrencyStatus.Active);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \u2003 ")]
    [TestCase("US")]
    [TestCase("USDD")]
    [TestCase("US1")]
    [TestCase("U D")]
    [TestCase("US\u00C9")]
    public void MalformedCodesReturnMalformedInputWithoutAMatch(string? code)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult result = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            code,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(result.NormalizedText, Is.Empty);
            Assert.That(result.Reason, Is.EqualTo("The currency alphabetic code is malformed."));
            Assert.That(result.Currency, Is.Null);
        });
    }

    [Test]
    public void WellFormedUnassignedCodePreservesNormalizedInput()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult syntax = CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax("zzz");
        CurrencyValidationResult assignment = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "zzz",
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(syntax.IsValid, Is.True);
            Assert.That(assignment.IsValid, Is.False);
            Assert.That(assignment.IsIndeterminate, Is.False);
            Assert.That(assignment.Error, Is.EqualTo(CurrencyValidationError.Unassigned));
            Assert.That(assignment.NormalizedText, Is.EqualTo("ZZZ"));
            Assert.That(
                assignment.Reason,
                Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(assignment.Currency, Is.Null);
        });
    }

    [Test]
    public void OutOfScopeCodesReturnInvalidStatusAndTheirCatalogMatch()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult historical = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "BGN",
            CurrencySet.ActiveOnly);
        CurrencyValidationResult active = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "USD",
            CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            AssertInvalidStatus(historical, "BGN", CurrencyStatus.Historical);
            AssertInvalidStatus(active, "USD", CurrencyStatus.Active);
        });
    }

    [Test]
    public void InvalidScopeIsRejectedBeforeInputValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => CurrencyValidationRules.ValidateCurrencyAssignment(
                store,
                null,
                (CurrencySet)99),
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void MatchedSnapshotsAndNestedDatesAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyValidationResult first = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "USD",
            CurrencySet.ActiveOnly);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.AlphabeticCode = "XXX";
        first.Currency.IntroductionDate.Year = 1;

        CurrencyValidationResult second = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "USD",
            CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(
                second.Currency?.IntroductionDate,
                Is.Not.SameAs(first.Currency.IntroductionDate));
            Assert.That(second.Currency?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(second.Currency?.IntroductionDate.Year, Is.EqualTo(1792));
        });
    }

    [Test]
    public void OutOfScopeSnapshotsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyValidationResult first = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "BGN",
            CurrencySet.ActiveOnly);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.WithdrawalDate.Year = 1;

        CurrencyValidationResult second = CurrencyValidationRules.ValidateCurrencyAssignment(
            store,
            "BGN",
            CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Bulgarian Lev"));
            Assert.That(second.Currency?.WithdrawalDate.Year, Is.EqualTo(2026));
        });
    }

    private static void AssertValid(
        CurrencyValidationResult result,
        string normalized,
        CurrencyStatus status)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalized));
            Assert.That(result.Currency?.Status, Is.EqualTo(status));
        });
    }

    private static void AssertInvalidStatus(
        CurrencyValidationResult result,
        string normalized,
        CurrencyStatus matchedStatus)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidStatus));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
            Assert.That(
                result.Reason,
                Is.EqualTo(
                    "Currency alphabetic code '"
                    + normalized
                    + "' is outside the requested scope."));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalized));
            Assert.That(result.Currency?.Status, Is.EqualTo(matchedStatus));
        });
    }
}
