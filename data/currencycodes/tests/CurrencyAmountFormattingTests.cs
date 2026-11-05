using System;
using System.Globalization;

namespace currencycodes.Tests;

public class CurrencyAmountFormattingTests
{
    [Test]
    public void FormatsCanonicalCodeSignDigitsAndFixedScaleWithoutGrouping()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult positive = Format(
            store,
            1234.5m,
            " usd ",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult negative = Format(
            store,
            -1234.5m,
            "USD",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult zero = Format(
            store,
            0m,
            "USD",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);

        Assert.Multiple(() =>
        {
            AssertSuccess(positive, 1234.50m, "USD 1234.50", "USD");
            AssertSuccess(negative, -1234.50m, "USD -1234.50", "USD");
            AssertSuccess(zero, 0m, "USD 0.00", "USD");
        });
    }

    [Test]
    public void SelectedUsageControlsDigitsAndRounding()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult iso = Format(
            store,
            1.25m,
            "AFN",
            AmountUsage.IsoMinorUnit,
            MidpointRounding.ToEven);
        AmountTextResult standard = Format(
            store,
            1.25m,
            "AFN",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult cash = Format(
            store,
            1.03m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.AwayFromZero);

        Assert.Multiple(() =>
        {
            AssertSuccess(iso, 1.25m, "AFN 1.25", "AFN");
            AssertSuccess(standard, 1m, "AFN 1", "AFN");
            AssertSuccess(cash, 1.05m, "CHF 1.05", "CHF");
        });
    }

    [Test]
    public void ExplicitMidpointModeControlsFormattedAmount()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult even = Format(
            store,
            1.025m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.ToEven);
        AmountTextResult away = Format(
            store,
            1.025m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.AwayFromZero);
        AmountTextResult positiveInfinity = Format(
            store,
            -1.01m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.ToPositiveInfinity);

        Assert.Multiple(() =>
        {
            AssertSuccess(even, 1.00m, "CHF 1.00", "CHF");
            AssertSuccess(away, 1.05m, "CHF 1.05", "CHF");
            AssertSuccess(positiveInfinity, -1.00m, "CHF -1.00", "CHF");
        });
    }

    [Test]
    public void ZeroThreeAndFourDigitRulesProduceTheirExactFixedScale()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult zeroDigits = Format(
            store,
            12.5m,
            "JPY",
            AmountUsage.IsoMinorUnit,
            MidpointRounding.ToEven);
        AmountTextResult threeDigits = Format(
            store,
            1.2345m,
            "BHD",
            AmountUsage.IsoMinorUnit,
            MidpointRounding.AwayFromZero);
        AmountTextResult fourDigits = Format(
            store,
            -1.2m,
            "CLF",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);

        Assert.Multiple(() =>
        {
            AssertSuccess(zeroDigits, 12m, "JPY 12", "JPY");
            AssertSuccess(threeDigits, 1.235m, "BHD 1.235", "BHD");
            AssertSuccess(fourDigits, -1.2m, "CLF -1.2000", "CLF");
        });
    }

    [Test]
    public void DecimalBoundaryFormattingUsesFixedPointWithoutGroupingOrExponent()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult maximum = Format(
            store,
            decimal.MaxValue,
            "USD",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult minimum = Format(
            store,
            decimal.MinValue,
            "JPY",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);

        Assert.Multiple(() =>
        {
            AssertSuccess(
                maximum,
                decimal.MaxValue,
                "USD 79228162514264337593543950335.00",
                "USD");
            AssertSuccess(
                minimum,
                decimal.MinValue,
                "JPY -79228162514264337593543950335",
                "JPY");
        });
    }

    [Test]
    public void FormattingIsIndependentOfCurrentCulture()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            AmountTextResult result = Format(
                store,
                -1234.5m,
                " usd ",
                AmountUsage.StandardFormat,
                MidpointRounding.ToEven);

            AssertSuccess(result, -1234.50m, "USD -1234.50", "USD");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void CurrencyAndMetadataFailuresReturnStructuredTextResults()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult malformed = Format(
            store,
            1.23m,
            "US1",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult historical = Format(
            store,
            2.34m,
            "BGN",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult unavailable = Format(
            store,
            3.45m,
            "XAU",
            AmountUsage.IsoMinorUnit,
            MidpointRounding.ToEven);

        Assert.Multiple(() =>
        {
            AssertFailure(
                malformed,
                1.23m,
                AmountValidationError.InvalidCurrency,
                null,
                "The currency alphabetic code is malformed.");
            AssertFailure(
                historical,
                2.34m,
                AmountValidationError.InvalidCurrency,
                "BGN",
                "Currency alphabetic code 'BGN' is not active.");
            AssertFailure(
                unavailable,
                3.45m,
                AmountValidationError.MetadataUnavailable,
                "XAU",
                "ISO minor-unit metadata is unavailable for currency 'XAU'.");
        });
    }

    [Test]
    public void RoundingOverflowHasANonThrowingOutcomeForStructuredCallers()
    {
        DecimalRoundingResult result = CurrencyAmountRoundingRules.GetRoundingResult(
            decimal.MaxValue,
            6m,
            MidpointRounding.AwayFromZero);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RoundedAmount, Is.Zero);
        });
    }

    [Test]
    public void InvalidSelectorsAreRejectedBeforeCurrencyValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Format(store, 1m, null, (AmountUsage)99, MidpointRounding.ToEven),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The amount usage is invalid."));
            Assert.That(
                () => Format(store, 1m, null, AmountUsage.StandardFormat, (MidpointRounding)99),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The midpoint rounding mode is invalid."));
        });
    }

    [Test]
    public void ReturnedCurrencySnapshotsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        AmountTextResult first = Format(
            store,
            1.03m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.ToEven);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.IntroductionDate.Year = 1;

        AmountTextResult second = Format(
            store,
            1.03m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.ToEven);

        Assert.Multiple(() =>
        {
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.IntroductionDate, Is.Not.SameAs(first.Currency.IntroductionDate));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Swiss Franc"));
            Assert.That(second.Currency?.IntroductionDate.Year, Is.EqualTo(1799));
        });
    }

    private static AmountTextResult Format(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage,
        MidpointRounding midpointRounding)
    {
        return CurrencyAmountTextRules.FormatInvariantCurrencyAmount(
            store,
            amount,
            code,
            usage,
            midpointRounding);
    }

    private static void AssertSuccess(
        AmountTextResult result,
        decimal amount,
        string invariantText,
        string currencyCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(currencyCode));
            Assert.That(result.Amount, Is.EqualTo(amount));
            Assert.That(result.InvariantText, Is.EqualTo(invariantText));
        });
    }

    private static void AssertFailure(
        AmountTextResult result,
        decimal amount,
        AmountValidationError error,
        string? currencyCode,
        string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(currencyCode));
            Assert.That(result.Amount, Is.EqualTo(amount));
            Assert.That(result.InvariantText, Is.Empty);
        });
    }
}
