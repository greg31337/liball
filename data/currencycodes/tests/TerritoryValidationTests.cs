using System;

namespace currencycodes.Tests;

public class TerritoryValidationTests
{
    [Test]
    public void TerritoryMembershipNormalizesAssignedAlpha2Code()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult result = CurrencyValidationRules.ValidateTerritoryAssignment(
            store,
            " \u2003bg\u00A0 ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(result.NormalizedText, Is.EqualTo("BG"));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("B")]
    [TestCase("B1")]
    [TestCase("BGG")]
    [TestCase("B\u011E")]
    public void MalformedTerritoryCodesReturnInvalidTerritory(string? territoryCode)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult result = CurrencyValidationRules.ValidateTerritoryAssignment(
            store,
            territoryCode);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidTerritory));
            Assert.That(result.NormalizedText, Is.Empty);
            Assert.That(result.Reason, Is.EqualTo("The territory alpha-2 code is malformed."));
            Assert.That(result.Currency, Is.Null);
        });
    }

    [Test]
    public void WellFormedUnassignedTerritoryPreservesNormalizedCode()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult result = CurrencyValidationRules.ValidateTerritoryAssignment(
            store,
            " aa ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidTerritory));
            Assert.That(result.NormalizedText, Is.EqualTo("AA"));
            Assert.That(result.Reason, Is.EqualTo("Territory alpha-2 code 'AA' is not assigned."));
            Assert.That(result.Currency, Is.Null);
        });
    }

    [Test]
    public void AssociationUsesInclusiveUsageBoundaries()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult levEnd =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                " bg ",
                " bgn ",
                new DateOnly(2026, 1, 31),
                true);
        CurrencyValidationResult levAfter =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "BG",
                "BGN",
                new DateOnly(2026, 2, 1),
                true);
        CurrencyValidationResult euroBefore =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "BG",
                "EUR",
                new DateOnly(2025, 12, 31),
                true);
        CurrencyValidationResult euroStart =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "BG",
                "EUR",
                new DateOnly(2026, 1, 1),
                true);

        Assert.Multiple(() =>
        {
            AssertValidAssociation(levEnd, "BGN", CurrencyStatus.Historical);
            AssertInvalidAssociation(levAfter, "BG", "BGN");
            AssertInvalidAssociation(euroBefore, "BG", "EUR");
            AssertValidAssociation(euroStart, "EUR", CurrencyStatus.Active);
        });
    }

    [Test]
    public void TenderOnlyExplicitlyExcludesNonTenderAssociation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult allUsage =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "CH",
                "CHE",
                new DateOnly(2026, 1, 1),
                false);
        CurrencyValidationResult tenderUsage =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "CH",
                "CHE",
                new DateOnly(2026, 1, 1),
                true);

        Assert.Multiple(() =>
        {
            AssertValidAssociation(allUsage, "CHE", CurrencyStatus.Active);
            AssertInvalidAssociation(tenderUsage, "CH", "CHE");
        });
    }

    [Test]
    public void InvalidTerritoryIsReportedBeforeCurrencyValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult malformed =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "B1",
                "US1",
                new DateOnly(2026, 1, 1),
                true);
        CurrencyValidationResult unassigned =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "AA",
                "ZZZ",
                new DateOnly(2026, 1, 1),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(CurrencyValidationError.InvalidTerritory));
            Assert.That(malformed.NormalizedText, Is.Empty);
            Assert.That(malformed.Currency, Is.Null);
            Assert.That(unassigned.Error, Is.EqualTo(CurrencyValidationError.InvalidTerritory));
            Assert.That(unassigned.NormalizedText, Is.EqualTo("AA"));
            Assert.That(unassigned.Currency, Is.Null);
        });
    }

    [Test]
    public void ValidTerritoryRetainsCurrencyInputErrors()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult malformed =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "US",
                "US1",
                new DateOnly(2026, 1, 1),
                true);
        CurrencyValidationResult unassigned =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "US",
                "ZZZ",
                new DateOnly(2026, 1, 1),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(malformed.NormalizedText, Is.Empty);
            Assert.That(malformed.Currency, Is.Null);
            Assert.That(unassigned.Error, Is.EqualTo(CurrencyValidationError.Unassigned));
            Assert.That(unassigned.NormalizedText, Is.EqualTo("ZZZ"));
            Assert.That(unassigned.Currency, Is.Null);
        });
    }

    [Test]
    public void AssociationResultsContainDetachedCurrencySnapshots()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyValidationResult first =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "BG",
                "BGN",
                new DateOnly(2026, 1, 31),
                true);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.WithdrawalDate.Year = 1;

        CurrencyValidationResult second =
            CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
                store,
                "BG",
                "BGN",
                new DateOnly(2026, 1, 31),
                true);

        Assert.Multiple(() =>
        {
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.WithdrawalDate, Is.Not.SameAs(first.Currency.WithdrawalDate));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Bulgarian Lev"));
            Assert.That(second.Currency?.WithdrawalDate.Year, Is.EqualTo(2026));
        });
    }

    private static void AssertValidAssociation(
        CurrencyValidationResult result,
        string normalizedCurrency,
        CurrencyStatus status)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(result.NormalizedText, Is.EqualTo(normalizedCurrency));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalizedCurrency));
            Assert.That(result.Currency?.Status, Is.EqualTo(status));
        });
    }

    private static void AssertInvalidAssociation(
        CurrencyValidationResult result,
        string normalizedTerritory,
        string normalizedCurrency)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidTerritory));
            Assert.That(result.NormalizedText, Is.EqualTo(normalizedCurrency));
            Assert.That(
                result.Reason,
                Is.EqualTo(
                    "Currency alphabetic code '"
                    + normalizedCurrency
                    + "' is not associated with territory '"
                    + normalizedTerritory
                    + "' on the requested date."));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(normalizedCurrency));
        });
    }
}
