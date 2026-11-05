namespace currencycodes.Tests;

public class CurrencyAmountValidationRulesTests
{
    [Test]
    public void IsoAndStandardValidationUseTheirIndependentDigitRules()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult iso = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.25m,
            " afn ",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult standard = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.25m,
            " afn ",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertValid(iso, 1.25m, "AFN", 2, 0);
            AssertRuleFailure(
                standard,
                1.25m,
                "AFN",
                AmountValidationError.InvalidScale,
                0,
                0,
                "The amount has more fractional digits than the selected rule permits.");
        });
    }

    [Test]
    public void CashValidationChecksScaleBeforeExactIncrement()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult standard = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.03m,
            "CHF",
            AmountUsage.StandardFormat);
        AmountValidationResult cashIncrement = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.03m,
            "CHF",
            AmountUsage.Cash);
        AmountValidationResult cashScale = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.001m,
            "CHF",
            AmountUsage.Cash);
        AmountValidationResult cashExact = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            AssertValid(standard, 1.03m, "CHF", 2, 0);
            AssertRuleFailure(
                cashIncrement,
                1.03m,
                "CHF",
                AmountValidationError.InvalidIncrement,
                2,
                5,
                "The amount is not a multiple of the selected rounding increment.");
            AssertRuleFailure(
                cashScale,
                1.001m,
                "CHF",
                AmountValidationError.InvalidScale,
                2,
                5,
                "The amount has more fractional digits than the selected rule permits.");
            AssertValid(cashExact, 1.05m, "CHF", 2, 5);
        });
    }

    [Test]
    public void NegativeZeroAndDecimalBoundaryAmountsRemainExact()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult negative = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            -1.05m,
            "CHF",
            AmountUsage.Cash);
        AmountValidationResult zero = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            0m,
            "CHF",
            AmountUsage.Cash);
        AmountValidationResult maximum = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            decimal.MaxValue,
            "USD",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult minimum = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            decimal.MinValue,
            "USD",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertValid(negative, -1.05m, "CHF", 2, 5);
            AssertValid(zero, 0m, "CHF", 2, 5);
            AssertValid(maximum, decimal.MaxValue, "USD", 2, 0);
            AssertValid(minimum, decimal.MinValue, "USD", 2, 0);
        });
    }

    [Test]
    public void TrailingZeroesAreAcceptedWhenValueFitsSelectedScale()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        decimal storedScaleTwo = new decimal(10000, 0, 0, false, 2);

        AmountValidationResult valid = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            storedScaleTwo,
            "JPY",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult invalid = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            100.1m,
            "JPY",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertValid(valid, storedScaleTwo, "JPY", 0, 0);
            AssertRuleFailure(
                invalid,
                100.1m,
                "JPY",
                AmountValidationError.InvalidScale,
                0,
                0,
                "The amount has more fractional digits than the selected rule permits.");
        });
    }

    [Test]
    public void ThreeAndFourDigitRulesValidateSmallestRepresentableUnits()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult threeDigits = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            0.001m,
            "BHD",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult fourDigits = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            -0.0001m,
            "CLF",
            AmountUsage.StandardFormat);
        AmountValidationResult tooPrecise = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            0.00001m,
            "CLF",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertValid(threeDigits, 0.001m, "BHD", 3, 0);
            AssertValid(fourDigits, -0.0001m, "CLF", 4, 0);
            AssertRuleFailure(
                tooPrecise,
                0.00001m,
                "CLF",
                AmountValidationError.InvalidScale,
                4,
                0,
                "The amount has more fractional digits than the selected rule permits.");
        });
    }

    [Test]
    public void UnavailableIsoMetadataDoesNotAssumeTwoDigits()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult iso = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.23m,
            "XAU",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult standard = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.23m,
            "XAU",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            Assert.That(iso.IsValid, Is.False);
            Assert.That(iso.Error, Is.EqualTo(AmountValidationError.MetadataUnavailable));
            Assert.That(
                iso.Reason,
                Is.EqualTo("ISO minor-unit metadata is unavailable for currency 'XAU'."));
            Assert.That(iso.Currency?.AlphabeticCode, Is.EqualTo("XAU"));
            Assert.That(iso.Amount, Is.EqualTo(1.23m));
            Assert.That(iso.HasRequiredRule, Is.False);
            Assert.That(iso.RequiredDigits, Is.Zero);
            Assert.That(iso.RequiredIncrement, Is.Zero);
            Assert.That(iso.HasRoundedCandidate, Is.False);
            Assert.That(iso.RoundedCandidate, Is.Zero);
            AssertValid(standard, 1.23m, "XAU", 2, 0);
        });
    }

    [Test]
    public void InvalidAndHistoricalCurrenciesReturnInvalidCurrency()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountValidationResult malformed = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.23m,
            "US1",
            AmountUsage.StandardFormat);
        AmountValidationResult unassigned = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            -4.56m,
            "zzz",
            AmountUsage.StandardFormat);
        AmountValidationResult historical = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            7.89m,
            "bgn",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertInvalidCurrency(
                malformed,
                1.23m,
                "The currency alphabetic code is malformed.",
                null);
            AssertInvalidCurrency(
                unassigned,
                -4.56m,
                "Currency alphabetic code 'ZZZ' is not assigned.",
                null);
            AssertInvalidCurrency(
                historical,
                7.89m,
                "Currency alphabetic code 'BGN' is not active.",
                "BGN");
            Assert.That(historical.Currency?.Status, Is.EqualTo(CurrencyStatus.Historical));
        });
    }

    [Test]
    public void InvalidAmountUsageIsRejectedBeforeCurrencyLookup()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => CurrencyAmountValidationRules.ValidateCurrencyAmount(
                store,
                1m,
                null,
                (AmountUsage)99),
            Throws.TypeOf<System.ArgumentException>()
                .With.Message.EqualTo("The amount usage is invalid."));
    }

    [Test]
    public void CurrencySnapshotsAreDetachedAcrossValidationCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        AmountValidationResult first = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.IntroductionDate.Year = 1;

        AmountValidationResult second = CurrencyAmountValidationRules.ValidateCurrencyAmount(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.IntroductionDate, Is.Not.SameAs(first.Currency.IntroductionDate));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Swiss Franc"));
            Assert.That(second.Currency?.IntroductionDate.Year, Is.EqualTo(1799));
        });
    }

    private static void AssertValid(
        AmountValidationResult result,
        decimal amount,
        string currencyCode,
        int digits,
        int increment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(currencyCode));
            Assert.That(result.Amount, Is.EqualTo(amount));
            Assert.That(result.HasRequiredRule, Is.True);
            Assert.That(result.RequiredDigits, Is.EqualTo(digits));
            Assert.That(result.RequiredIncrement, Is.EqualTo(increment));
            Assert.That(result.HasRoundedCandidate, Is.False);
            Assert.That(result.RoundedCandidate, Is.Zero);
        });
    }

    private static void AssertRuleFailure(
        AmountValidationResult result,
        decimal amount,
        string currencyCode,
        AmountValidationError error,
        int digits,
        int increment,
        string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(currencyCode));
            Assert.That(result.Amount, Is.EqualTo(amount));
            Assert.That(result.HasRequiredRule, Is.True);
            Assert.That(result.RequiredDigits, Is.EqualTo(digits));
            Assert.That(result.RequiredIncrement, Is.EqualTo(increment));
            Assert.That(result.HasRoundedCandidate, Is.False);
            Assert.That(result.RoundedCandidate, Is.Zero);
        });
    }

    private static void AssertInvalidCurrency(
        AmountValidationResult result,
        decimal amount,
        string reason,
        string? matchedCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(matchedCode));
            Assert.That(result.Amount, Is.EqualTo(amount));
            Assert.That(result.HasRequiredRule, Is.False);
            Assert.That(result.RequiredDigits, Is.Zero);
            Assert.That(result.RequiredIncrement, Is.Zero);
            Assert.That(result.HasRoundedCandidate, Is.False);
            Assert.That(result.RoundedCandidate, Is.Zero);
        });
    }
}
