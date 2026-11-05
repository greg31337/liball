namespace currencycodes.Tests;

public class CurrencyMajorToMinorConversionTests
{
    [Test]
    public void ConvertsPositiveNegativeAndZeroAmountsExactly()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult positive = Convert(
            store,
            123.45m,
            " usd ",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult negative = Convert(
            store,
            -123.45m,
            "USD",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult zero = Convert(
            store,
            0m,
            "USD",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            AssertSuccess(positive, 123.45m, 12345, "USD");
            AssertSuccess(negative, -123.45m, -12345, "USD");
            AssertSuccess(zero, 0m, 0, "USD");
        });
    }

    [Test]
    public void ConvertsZeroThreeAndFourDigitScales()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult zeroDigits = Convert(
            store,
            123m,
            "JPY",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult threeDigits = Convert(
            store,
            1.234m,
            "BHD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult fourDigits = Convert(
            store,
            -1.2345m,
            "CLF",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertSuccess(zeroDigits, 123m, 123, "JPY");
            AssertSuccess(threeDigits, 1.234m, 1234, "BHD");
            AssertSuccess(fourDigits, -1.2345m, -12345, "CLF");
        });
    }

    [Test]
    public void SelectedCashScaleAndIncrementAreEnforcedBeforeConversion()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult exact = Convert(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);
        MinorUnitConversionResult invalidIncrement = Convert(
            store,
            1.03m,
            "CHF",
            AmountUsage.Cash);
        MinorUnitConversionResult invalidScale = Convert(
            store,
            1.001m,
            "CHF",
            AmountUsage.Cash);
        MinorUnitConversionResult halfUnitCash = Convert(
            store,
            1.50m,
            "DKK",
            AmountUsage.Cash);
        MinorUnitConversionResult fiveUnitCash = Convert(
            store,
            15m,
            "HUF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, 1.05m, 105, "CHF");
            AssertFailure(
                invalidIncrement,
                1.03m,
                AmountValidationError.InvalidIncrement,
                "CHF",
                "The amount is not a multiple of the selected rounding increment.");
            AssertFailure(
                invalidScale,
                1.001m,
                AmountValidationError.InvalidScale,
                "CHF",
                "The amount has more fractional digits than the selected rule permits.");
            AssertSuccess(halfUnitCash, 1.50m, 150, "DKK");
            AssertSuccess(fiveUnitCash, 15m, 15, "HUF");
        });
    }

    [Test]
    public void ExactSignedLongBoundariesSucceedAtTwoDigitScale()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        decimal maximum = 92233720368547758.07m;
        decimal minimum = -92233720368547758.08m;

        MinorUnitConversionResult maximumResult = Convert(
            store,
            maximum,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult minimumResult = Convert(
            store,
            minimum,
            "USD",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertSuccess(maximumResult, maximum, long.MaxValue, "USD");
            AssertSuccess(minimumResult, minimum, long.MinValue, "USD");
        });
    }

    [Test]
    public void ValuesBeyondSignedLongBoundariesReturnOutOfRange()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        decimal aboveMaximum = 92233720368547758.08m;
        decimal belowMinimum = -92233720368547758.09m;

        MinorUnitConversionResult above = Convert(
            store,
            aboveMaximum,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult below = Convert(
            store,
            belowMinimum,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult decimalBoundary = Convert(
            store,
            decimal.MaxValue,
            "USD",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertFailure(
                above,
                aboveMaximum,
                AmountValidationError.OutOfRange,
                "USD",
                "The major-unit amount is outside the signed minor-unit range.");
            AssertFailure(
                below,
                belowMinimum,
                AmountValidationError.OutOfRange,
                "USD",
                "The major-unit amount is outside the signed minor-unit range.");
            AssertFailure(
                decimalBoundary,
                decimal.MaxValue,
                AmountValidationError.OutOfRange,
                "USD",
                "The major-unit amount is outside the signed minor-unit range.");
        });
    }

    [Test]
    public void FourDigitScaleUsesExactSignedLongBoundaries()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        decimal maximum = 922337203685477.5807m;
        decimal minimum = -922337203685477.5808m;

        MinorUnitConversionResult maximumResult = Convert(
            store,
            maximum,
            "CLF",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult minimumResult = Convert(
            store,
            minimum,
            "CLF",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult outOfRange = Convert(
            store,
            922337203685477.5808m,
            "CLF",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertSuccess(maximumResult, maximum, long.MaxValue, "CLF");
            AssertSuccess(minimumResult, minimum, long.MinValue, "CLF");
            AssertFailure(
                outOfRange,
                922337203685477.5808m,
                AmountValidationError.OutOfRange,
                "CLF",
                "The major-unit amount is outside the signed minor-unit range.");
        });
    }

    [Test]
    public void ValidationFailuresArePreservedWithoutAConversion()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult malformed = Convert(
            store,
            1m,
            "US1",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult historical = Convert(
            store,
            2m,
            "BGN",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult unavailable = Convert(
            store,
            3m,
            "XAU",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertFailure(
                malformed,
                1m,
                AmountValidationError.InvalidCurrency,
                null,
                "The currency alphabetic code is malformed.");
            AssertFailure(
                historical,
                2m,
                AmountValidationError.InvalidCurrency,
                "BGN",
                "Currency alphabetic code 'BGN' is not active.");
            AssertFailure(
                unavailable,
                3m,
                AmountValidationError.MetadataUnavailable,
                "XAU",
                "ISO minor-unit metadata is unavailable for currency 'XAU'.");
        });
    }

    [Test]
    public void InvalidUsageIsRejectedBeforeCurrencyValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => Convert(store, 1m, null, (AmountUsage)99),
            Throws.TypeOf<System.ArgumentException>()
                .With.Message.EqualTo("The amount usage is invalid."));
    }

    [Test]
    public void ReturnedCurrencySnapshotsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        MinorUnitConversionResult first = Convert(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.IntroductionDate.Year = 1;

        MinorUnitConversionResult second = Convert(
            store,
            1.05m,
            "CHF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(second.Currency, Is.Not.Null);
            Assert.That(second.Currency, Is.Not.SameAs(first.Currency));
            Assert.That(second.Currency?.IntroductionDate, Is.Not.SameAs(first.Currency.IntroductionDate));
            Assert.That(second.Currency?.OfficialName, Is.EqualTo("Swiss Franc"));
            Assert.That(second.Currency?.IntroductionDate.Year, Is.EqualTo(1799));
        });
    }

    private static MinorUnitConversionResult Convert(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage)
    {
        return CurrencyMinorUnitConversionRules.ConvertCurrencyMajorToMinor(
            store,
            amount,
            code,
            usage);
    }

    private static void AssertSuccess(
        MinorUnitConversionResult result,
        decimal majorAmount,
        long minorUnitValue,
        string currencyCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(currencyCode));
            Assert.That(result.MajorAmount, Is.EqualTo(majorAmount));
            Assert.That(result.MinorUnitValue, Is.EqualTo(minorUnitValue));
        });
    }

    private static void AssertFailure(
        MinorUnitConversionResult result,
        decimal majorAmount,
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
            Assert.That(result.MajorAmount, Is.EqualTo(majorAmount));
            Assert.That(result.MinorUnitValue, Is.Zero);
        });
    }
}
