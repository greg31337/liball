using System.Globalization;

namespace currencycodes.Tests;

public class CurrencyAmountParsingTests
{
    [Test]
    public void ParsesPositiveNegativeAndZeroInvariantAmounts()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult positive = Parse(store, "USD 1234.50", AmountUsage.StandardFormat);
        AmountTextResult negative = Parse(store, "USD -1234.50", AmountUsage.StandardFormat);
        AmountTextResult zero = Parse(store, "USD 0.00", AmountUsage.StandardFormat);
        AmountTextResult normalizedCode = Parse(store, "usd 1.25", AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertSuccess(positive, 1234.50m, "USD 1234.50", "USD");
            AssertSuccess(negative, -1234.50m, "USD -1234.50", "USD");
            AssertSuccess(zero, 0m, "USD 0.00", "USD");
            AssertSuccess(normalizedCode, 1.25m, "USD 1.25", "USD");
        });
    }

    [Test]
    public void ParsesEveryGeneratedScaleAndPreservesValidTextDigits()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult zeroDigits = Parse(store, "JPY 123", AmountUsage.IsoMinorUnit);
        AmountTextResult twoDigits = Parse(store, "USD 1.2300", AmountUsage.IsoMinorUnit);
        AmountTextResult threeDigits = Parse(store, "BHD 1.234", AmountUsage.IsoMinorUnit);
        AmountTextResult fourDigits = Parse(store, "CLF -1.2345", AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertSuccess(zeroDigits, 123m, "JPY 123", "JPY");
            AssertSuccess(twoDigits, 1.23m, "USD 1.2300", "USD");
            AssertSuccess(threeDigits, 1.234m, "BHD 1.234", "BHD");
            AssertSuccess(fourDigits, -1.2345m, "CLF -1.2345", "CLF");
        });
    }

    [Test]
    public void FormatterOutputRoundTripsWithoutChangingAmountOrText()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        AmountTextResult formatted = CurrencyAmountTextRules.FormatInvariantCurrencyAmount(
            store,
            -1.025m,
            "CHF",
            AmountUsage.Cash,
            System.MidpointRounding.AwayFromZero);

        AmountTextResult parsed = Parse(store, formatted.InvariantText, AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Success, Is.True);
            AssertSuccess(parsed, -1.05m, "CHF -1.05", "CHF");
        });
    }

    [Test]
    public void ScaleAndIncrementFailuresPreserveParsedAmount()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult invalidScale = Parse(store, "USD 1.001", AmountUsage.StandardFormat);
        AmountTextResult invalidIncrement = Parse(store, "CHF 1.03", AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            AssertFailure(
                invalidScale,
                1.001m,
                AmountValidationError.InvalidScale,
                "USD",
                "The amount has more fractional digits than the selected rule permits.");
            AssertFailure(
                invalidIncrement,
                1.03m,
                AmountValidationError.InvalidIncrement,
                "CHF",
                "The amount is not a multiple of the selected rounding increment.");
        });
    }

    [TestCase("USD")]
    [TestCase("USD ")]
    [TestCase(" USD 1.00")]
    [TestCase("USD  1.00")]
    [TestCase("USD 1.00 ")]
    [TestCase("USD +1.00")]
    [TestCase("USD -")]
    [TestCase("USD .50")]
    [TestCase("USD 1.")]
    [TestCase("USD 1.2.3")]
    [TestCase("USD 1,00")]
    [TestCase("USD 1e2")]
    [TestCase("USD \u22121.00")]
    [TestCase("USD \u0661.00")]
    [TestCase("USD 1\u066B00")]
    public void RejectsNonCanonicalLayoutAndNonAsciiAmountGrammar(string text)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult result = Parse(store, text, AmountUsage.StandardFormat);

        AssertFailure(
            result,
            0m,
            AmountValidationError.InvalidFormat,
            text.Length >= 5 && text[3] == ' ' ? "USD" : null,
            "The invariant currency amount text has an invalid format.");
    }

    [Test]
    public void DecimalBoundariesAndInsignificantZeroesAreParsedExactly()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult maximum = Parse(
            store,
            "USD 79228162514264337593543950335.00",
            AmountUsage.StandardFormat);
        AmountTextResult minimum = Parse(
            store,
            "USD -79228162514264337593543950335.00",
            AmountUsage.StandardFormat);

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
                "USD -79228162514264337593543950335.00",
                "USD");
        });
    }

    [TestCase("USD 79228162514264337593543950336")]
    [TestCase("USD -79228162514264337593543950336")]
    [TestCase("USD 0.00000000000000000000000000001")]
    public void DecimalOverflowAndUnrepresentableScaleReturnOutOfRange(string text)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult result = Parse(store, text, AmountUsage.StandardFormat);

        AssertFailure(
            result,
            0m,
            AmountValidationError.OutOfRange,
            "USD",
            "The invariant amount is outside the decimal range.");
    }

    [Test]
    public void CurrencyAndMetadataFailuresAreReportedBeforeAmountParsing()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        AmountTextResult malformed = Parse(store, "US1 1.00", AmountUsage.StandardFormat);
        AmountTextResult unassigned = Parse(store, "ZZZ 1.00", AmountUsage.StandardFormat);
        AmountTextResult historical = Parse(store, "BGN 1.00", AmountUsage.StandardFormat);
        AmountTextResult unavailable = Parse(store, "XAU 1.00", AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertFailure(
                malformed,
                0m,
                AmountValidationError.InvalidCurrency,
                null,
                "The currency alphabetic code is malformed.");
            AssertFailure(
                unassigned,
                0m,
                AmountValidationError.InvalidCurrency,
                null,
                "Currency alphabetic code 'ZZZ' is not assigned.");
            AssertFailure(
                historical,
                0m,
                AmountValidationError.InvalidCurrency,
                "BGN",
                "Currency alphabetic code 'BGN' is not active.");
            AssertFailure(
                unavailable,
                0m,
                AmountValidationError.MetadataUnavailable,
                "XAU",
                "ISO minor-unit metadata is unavailable for currency 'XAU'.");
        });
    }

    [Test]
    public void ParsingIsIndependentOfCurrentCulture()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            AmountTextResult result = Parse(
                store,
                "usd -1234.50",
                AmountUsage.StandardFormat);

            AssertSuccess(result, -1234.50m, "USD -1234.50", "USD");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void InvalidUsageIsRejectedBeforeTextValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => Parse(store, null, (AmountUsage)99),
            Throws.TypeOf<System.ArgumentException>()
                .With.Message.EqualTo("The amount usage is invalid."));
    }

    [Test]
    public void ReturnedCurrencySnapshotsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        AmountTextResult first = Parse(store, "CHF 1.05", AmountUsage.Cash);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.IntroductionDate.Year = 1;

        AmountTextResult second = Parse(store, "CHF 1.05", AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.IntroductionDate, Is.Not.SameAs(first.Currency.IntroductionDate));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Swiss Franc"));
            Assert.That(second.Currency?.IntroductionDate.Year, Is.EqualTo(1799));
        });
    }

    private static AmountTextResult Parse(
        CurrencyDataStore store,
        string? text,
        AmountUsage usage)
    {
        return CurrencyAmountTextRules.ParseInvariantCurrencyAmount(store, text, usage);
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
