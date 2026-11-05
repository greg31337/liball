namespace currencycodes.Tests;

public class CurrencyMinorToMajorConversionTests
{
    [Test]
    public void ConvertsPositiveNegativeAndZeroMinorUnitsExactly()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult positive = Convert(
            store,
            12345,
            " usd ",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult negative = Convert(
            store,
            -12345,
            "USD",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult zero = Convert(
            store,
            0,
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
    public void UsesSelectedZeroThreeAndFourDigitScales()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult zeroDigits = Convert(
            store,
            123,
            "JPY",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult threeDigits = Convert(
            store,
            1234,
            "BHD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult fourDigits = Convert(
            store,
            -12345,
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
    public void DifferentUsageSelectionsUseIndependentScales()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult iso = Convert(
            store,
            125,
            "AFN",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult standard = Convert(
            store,
            125,
            "AFN",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertSuccess(iso, 1.25m, 125, "AFN");
            AssertSuccess(standard, 125m, 125, "AFN");
        });
    }

    [Test]
    public void EverySignedMinorValueHasAnExactCashScaleRepresentation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult swissCent = Convert(
            store,
            1,
            "CHF",
            AmountUsage.Cash);
        MinorUnitConversionResult forintUnit = Convert(
            store,
            1,
            "HUF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            AssertSuccess(swissCent, 0.01m, 1, "CHF");
            AssertSuccess(forintUnit, 1m, 1, "HUF");
        });
    }

    [Test]
    public void SignedLongBoundariesRemainExactAtTwoAndFourDigits()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult twoDigitMaximum = Convert(
            store,
            long.MaxValue,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult twoDigitMinimum = Convert(
            store,
            long.MinValue,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult fourDigitMaximum = Convert(
            store,
            long.MaxValue,
            "CLF",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult fourDigitMinimum = Convert(
            store,
            long.MinValue,
            "CLF",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertSuccess(
                twoDigitMaximum,
                92233720368547758.07m,
                long.MaxValue,
                "USD");
            AssertSuccess(
                twoDigitMinimum,
                -92233720368547758.08m,
                long.MinValue,
                "USD");
            AssertSuccess(
                fourDigitMaximum,
                922337203685477.5807m,
                long.MaxValue,
                "CLF");
            AssertSuccess(
                fourDigitMinimum,
                -922337203685477.5808m,
                long.MinValue,
                "CLF");
        });
    }

    [Test]
    public void InvalidCurrencyAndUnavailableMetadataPreserveMinorValue()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        MinorUnitConversionResult malformed = Convert(
            store,
            11,
            "US1",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult historical = Convert(
            store,
            22,
            "BGN",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult unavailable = Convert(
            store,
            33,
            "XAU",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            AssertFailure(
                malformed,
                11,
                AmountValidationError.InvalidCurrency,
                null,
                "The currency alphabetic code is malformed.");
            AssertFailure(
                historical,
                22,
                AmountValidationError.InvalidCurrency,
                "BGN",
                "Currency alphabetic code 'BGN' is not active.");
            AssertFailure(
                unavailable,
                33,
                AmountValidationError.MetadataUnavailable,
                "XAU",
                "ISO minor-unit metadata is unavailable for currency 'XAU'.");
        });
    }

    [Test]
    public void RepresentableScaleRequiresAvailablePowerOfTenMetadata()
    {
        CurrencyAmountRule valid = new CurrencyAmountRule
        {
            IsAvailable = true,
            Digits = 28,
            Factor = 10000000000000000000000000000m
        };
        CurrencyAmountRule unavailable = new CurrencyAmountRule
        {
            IsAvailable = false,
            Digits = 2,
            Factor = 100m
        };
        CurrencyAmountRule negativeDigits = new CurrencyAmountRule
        {
            IsAvailable = true,
            Digits = -1,
            Factor = 1m
        };
        CurrencyAmountRule excessiveDigits = new CurrencyAmountRule
        {
            IsAvailable = true,
            Digits = 29,
            Factor = 10000000000000000000000000000m
        };
        CurrencyAmountRule mismatchedFactor = new CurrencyAmountRule
        {
            IsAvailable = true,
            Digits = 2,
            Factor = 10m
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(valid),
                Is.True);
            Assert.That(
                CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(unavailable),
                Is.False);
            Assert.That(
                CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(negativeDigits),
                Is.False);
            Assert.That(
                CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(excessiveDigits),
                Is.False);
            Assert.That(
                CurrencyAmountRuleSelector.HasRepresentableCurrencyAmountScale(mismatchedFactor),
                Is.False);
        });
    }

    [Test]
    public void InvalidUsageIsRejectedBeforeCurrencyValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => Convert(store, 1, null, (AmountUsage)99),
            Throws.TypeOf<System.ArgumentException>()
                .With.Message.EqualTo("The amount usage is invalid."));
    }

    [Test]
    public void ReturnedCurrencySnapshotsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        MinorUnitConversionResult first = Convert(
            store,
            105,
            "CHF",
            AmountUsage.Cash);

        Assert.That(first.Currency, Is.Not.Null);
        first.Currency!.OfficialName = "changed";
        first.Currency.IntroductionDate.Year = 1;

        MinorUnitConversionResult second = Convert(
            store,
            105,
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
        long minorUnitValue,
        string? code,
        AmountUsage usage)
    {
        return CurrencyMinorUnitConversionRules.ConvertCurrencyMinorToMajor(
            store,
            minorUnitValue,
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
        long minorUnitValue,
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
            Assert.That(result.MajorAmount, Is.Zero);
            Assert.That(result.MinorUnitValue, Is.EqualTo(minorUnitValue));
        });
    }
}
