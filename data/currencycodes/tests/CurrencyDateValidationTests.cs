using System;

namespace currencycodes.Tests;

public class CurrencyDateValidationTests
{
    [Test]
    public void KnownActiveIntroductionDefinesAnInclusiveOpenInterval()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult before = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " usd ",
            new DateOnly(1791, 12, 31));
        CurrencyValidationResult start = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " usd ",
            new DateOnly(1792, 1, 1));
        CurrencyValidationResult later = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " usd ",
            new DateOnly(2026, 1, 1));

        Assert.Multiple(() =>
        {
            AssertInvalid(before, "USD");
            AssertValid(start, "USD", CurrencyStatus.Active);
            AssertValid(later, "USD", CurrencyStatus.Active);
        });
    }

    [Test]
    public void FullyKnownHistoricalIntervalIncludesBothBoundaries()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult before = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "ARL",
            new DateOnly(1969, 12, 31));
        CurrencyValidationResult start = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "ARL",
            new DateOnly(1970, 1, 1));
        CurrencyValidationResult end = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "ARL",
            new DateOnly(1983, 6, 1));
        CurrencyValidationResult after = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "ARL",
            new DateOnly(1983, 6, 2));

        Assert.Multiple(() =>
        {
            AssertInvalid(before, "ARL");
            AssertValid(start, "ARL", CurrencyStatus.Historical);
            AssertValid(end, "ARL", CurrencyStatus.Historical);
            AssertInvalid(after, "ARL");
        });
    }

    [Test]
    public void UnknownHistoricalIntroductionIsIndeterminateUntilKnownWithdrawalEnd()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult lastKnownMonth = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " bgn ",
            new DateOnly(2026, 1, 31));
        CurrencyValidationResult after = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " bgn ",
            new DateOnly(2026, 2, 1));

        Assert.Multiple(() =>
        {
            AssertIndeterminate(lastKnownMonth, "BGN", CurrencyStatus.Historical);
            AssertInvalid(after, "BGN");
        });
    }

    [Test]
    public void MissingBothSourceDatesProducesIndeterminateResult()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult result = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "xau",
            new DateOnly(2026, 1, 1));

        AssertIndeterminate(result, "XAU", CurrencyStatus.Active);
    }

    [Test]
    public void DuplicateDesignationsAggregateValidIndeterminateAndInvalidEvidence()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult unresolvedEuro = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "EUR",
            new DateOnly(1990, 1, 1));
        CurrencyValidationResult validEuro = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "EUR",
            new DateOnly(1999, 1, 1));
        CurrencyValidationResult unresolvedRuble = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "RUR",
            new DateOnly(2004, 1, 31));
        CurrencyValidationResult invalidRuble = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "RUR",
            new DateOnly(2004, 2, 1));

        Assert.Multiple(() =>
        {
            AssertIndeterminate(unresolvedEuro, "EUR", CurrencyStatus.Historical);
            AssertValid(validEuro, "EUR", CurrencyStatus.Active);
            AssertIndeterminate(unresolvedRuble, "RUR", CurrencyStatus.Historical);
            AssertInvalid(invalidRuble, "RUR");
        });
    }

    [Test]
    public void MalformedAndUnassignedCodesRetainAssignmentErrorSemantics()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult malformed = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            null,
            new DateOnly(2026, 1, 1));
        CurrencyValidationResult unassigned = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            " zzz ",
            new DateOnly(2026, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsValid, Is.False);
            Assert.That(malformed.IsIndeterminate, Is.False);
            Assert.That(malformed.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(malformed.NormalizedText, Is.Empty);
            Assert.That(malformed.Reason, Is.EqualTo("The currency alphabetic code is malformed."));
            Assert.That(malformed.Currency, Is.Null);

            Assert.That(unassigned.IsValid, Is.False);
            Assert.That(unassigned.IsIndeterminate, Is.False);
            Assert.That(unassigned.Error, Is.EqualTo(CurrencyValidationError.Unassigned));
            Assert.That(unassigned.NormalizedText, Is.EqualTo("ZZZ"));
            Assert.That(
                unassigned.Reason,
                Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(unassigned.Currency, Is.Null);
        });
    }

    [Test]
    public void ReturnedCurrencyAndNestedDatesAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyValidationResult first = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "BGN",
            new DateOnly(2026, 1, 31));

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.WithdrawalDate.Year = 1;

        CurrencyValidationResult second = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "BGN",
            new DateOnly(2026, 1, 31));

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.WithdrawalDate, Is.Not.SameAs(first.Currency.WithdrawalDate));
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

    private static void AssertInvalid(CurrencyValidationResult result, string normalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidDate));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
            Assert.That(result.Reason, Is.EqualTo("The currency is not valid on the requested date."));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalized));
        });
    }

    private static void AssertIndeterminate(
        CurrencyValidationResult result,
        string normalized,
        CurrencyStatus status)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.True);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.Indeterminate));
            Assert.That(result.NormalizedText, Is.EqualTo(normalized));
            Assert.That(
                result.Reason,
                Is.EqualTo("The available source dates cannot determine currency validity."));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalized));
            Assert.That(result.Currency?.Status, Is.EqualTo(status));
        });
    }
}
