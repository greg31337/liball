using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyMinorUnitConversionTests
{
    [Test]
    public void EveryAvailableRuleConvertsSignedBoundariesAndExactRoundTrips()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> metadata =
            PinnedCurrencyFormatFixture.Load();
        CurrencyCodeService service = new CurrencyCodeService();
        ICurrencyCatalog catalog = service;
        ICurrencyAmountRules rules = service;
        CurrencyInfo[] currencies = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        HashSet<string> combinations = new HashSet<string>(StringComparer.Ordinal);
        int availableCount = 0;
        int unavailableCount = 0;

        foreach (CurrencyInfo currency in currencies)
        {
            ExpectedCurrencyFormatMetadata format = metadata[currency.AlphabeticCode];
            foreach (AmountUsage usage in GetUsages())
            {
                ExpectedConversionRule rule = SelectRule(format, usage);
                if (!rule.IsAvailable)
                {
                    unavailableCount++;
                    AssertUnavailableConversions(rules, currency.AlphabeticCode, usage);
                    continue;
                }

                availableCount++;
                combinations.Add(
                    ((int)usage).ToString()
                        + "|"
                        + rule.Digits.ToString()
                        + "|"
                        + rule.Increment.ToString());
                AssertRuleConversions(rules, currency.AlphabeticCode, usage, rule);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(currencies, Has.Length.EqualTo(179));
            Assert.That(availableCount, Is.EqualTo(523));
            Assert.That(unavailableCount, Is.EqualTo(14));
            Assert.That(combinations, Has.Count.EqualTo(15));
        });
    }

    [Test]
    public void EveryRuleRejectsExcessScaleAndCashRulesRejectMisalignedIncrements()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> metadata =
            PinnedCurrencyFormatFixture.Load();
        CurrencyCodeService service = new CurrencyCodeService();
        ICurrencyCatalog catalog = service;
        ICurrencyAmountRules rules = service;

        foreach (CurrencyInfo currency in catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly))
        {
            ExpectedCurrencyFormatMetadata format = metadata[currency.AlphabeticCode];
            foreach (AmountUsage usage in GetUsages())
            {
                ExpectedConversionRule rule = SelectRule(format, usage);
                if (!rule.IsAvailable)
                {
                    continue;
                }

                decimal excessiveScale = 1m / (rule.Factor * 10m);
                AssertMajorFailure(
                    rules.ConvertCurrencyMajorToMinor(
                        excessiveScale,
                        currency.AlphabeticCode,
                        usage),
                    currency.AlphabeticCode,
                    excessiveScale,
                    AmountValidationError.InvalidScale,
                    "The amount has more fractional digits than the selected rule permits.");
                AssertMajorFailure(
                    rules.ConvertCurrencyMajorToMinor(
                        -excessiveScale,
                        currency.AlphabeticCode,
                        usage),
                    currency.AlphabeticCode,
                    -excessiveScale,
                    AmountValidationError.InvalidScale,
                    "The amount has more fractional digits than the selected rule permits.");

                if (rule.Increment > 1)
                {
                    decimal misaligned = 1m / rule.Factor;
                    AssertMajorFailure(
                        rules.ConvertCurrencyMajorToMinor(
                            misaligned,
                            currency.AlphabeticCode,
                            usage),
                        currency.AlphabeticCode,
                        misaligned,
                        AmountValidationError.InvalidIncrement,
                        "The amount is not a multiple of the selected rounding increment.");
                    AssertMajorFailure(
                        rules.ConvertCurrencyMajorToMinor(
                            -misaligned,
                            currency.AlphabeticCode,
                            usage),
                        currency.AlphabeticCode,
                        -misaligned,
                        AmountValidationError.InvalidIncrement,
                        "The amount is not a multiple of the selected rounding increment.");

                    MinorUnitConversionResult represented = rules.ConvertCurrencyMinorToMajor(
                        1,
                        currency.AlphabeticCode,
                        usage);
                    AssertMinorSuccess(
                        represented,
                        currency.AlphabeticCode,
                        1m / rule.Factor,
                        1);
                }
            }
        }
    }

    [Test]
    public void UnknownCurrenciesPreserveInputsInBothConversionDirections()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        foreach (AmountUsage usage in GetUsages())
        {
            MinorUnitConversionResult major = rules.ConvertCurrencyMajorToMinor(
                decimal.MaxValue,
                " zzz ",
                usage);
            MinorUnitConversionResult minor = rules.ConvertCurrencyMinorToMajor(
                long.MinValue,
                " zzz ",
                usage);

            Assert.Multiple(() =>
            {
                Assert.That(major.Success, Is.False);
                Assert.That(major.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
                Assert.That(
                    major.Reason,
                    Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
                Assert.That(major.Currency, Is.Null);
                Assert.That(major.MajorAmount, Is.EqualTo(decimal.MaxValue));
                Assert.That(major.MinorUnitValue, Is.Zero);
                Assert.That(minor.Success, Is.False);
                Assert.That(minor.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
                Assert.That(
                    minor.Reason,
                    Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
                Assert.That(minor.Currency, Is.Null);
                Assert.That(minor.MajorAmount, Is.Zero);
                Assert.That(minor.MinorUnitValue, Is.EqualTo(long.MinValue));
            });
        }
    }

    [Test]
    public void DecimalExtremesReportSignedMinorRangeOverflow()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        MinorUnitConversionResult maximum = rules.ConvertCurrencyMajorToMinor(
            decimal.MaxValue,
            "JPY",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult minimum = rules.ConvertCurrencyMajorToMinor(
            decimal.MinValue,
            "JPY",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertMajorFailure(
                maximum,
                "JPY",
                decimal.MaxValue,
                AmountValidationError.OutOfRange,
                "The major-unit amount is outside the signed minor-unit range.");
            AssertMajorFailure(
                minimum,
                "JPY",
                decimal.MinValue,
                AmountValidationError.OutOfRange,
                "The major-unit amount is outside the signed minor-unit range.");
        });
    }

    private static void AssertRuleConversions(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage,
        ExpectedConversionRule rule)
    {
        long divisor = rule.Increment == 0 ? 1 : rule.Increment;
        long maximumAligned = long.MaxValue - (long.MaxValue % divisor);
        long minimumAligned = long.MinValue - (long.MinValue % divisor);
        decimal maximumMajor = maximumAligned / rule.Factor;
        decimal minimumMajor = minimumAligned / rule.Factor;
        decimal step = divisor / rule.Factor;

        AssertMinorSuccess(
            rules.ConvertCurrencyMinorToMajor(long.MaxValue, code, usage),
            code,
            (decimal)long.MaxValue / rule.Factor,
            long.MaxValue);
        AssertMinorSuccess(
            rules.ConvertCurrencyMinorToMajor(long.MinValue, code, usage),
            code,
            (decimal)long.MinValue / rule.Factor,
            long.MinValue);
        AssertRoundTrip(rules, code, usage, maximumMajor, maximumAligned);
        AssertRoundTrip(rules, code, usage, minimumMajor, minimumAligned);
        AssertRoundTrip(rules, code, usage, 0m, 0);
        AssertMajorFailure(
            rules.ConvertCurrencyMajorToMinor(maximumMajor + step, code, usage),
            code,
            maximumMajor + step,
            AmountValidationError.OutOfRange,
            "The major-unit amount is outside the signed minor-unit range.");
        AssertMajorFailure(
            rules.ConvertCurrencyMajorToMinor(minimumMajor - step, code, usage),
            code,
            minimumMajor - step,
            AmountValidationError.OutOfRange,
            "The major-unit amount is outside the signed minor-unit range.");
    }

    private static void AssertRoundTrip(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage,
        decimal majorAmount,
        long minorUnitValue)
    {
        MinorUnitConversionResult toMinor = rules.ConvertCurrencyMajorToMinor(
            majorAmount,
            code,
            usage);
        AssertMajorSuccess(toMinor, code, majorAmount, minorUnitValue);

        MinorUnitConversionResult toMajor = rules.ConvertCurrencyMinorToMajor(
            toMinor.MinorUnitValue,
            code,
            usage);
        AssertMinorSuccess(toMajor, code, majorAmount, minorUnitValue);
    }

    private static void AssertUnavailableConversions(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage)
    {
        MinorUnitConversionResult major = rules.ConvertCurrencyMajorToMinor(
            decimal.MinValue,
            code,
            usage);
        MinorUnitConversionResult minor = rules.ConvertCurrencyMinorToMajor(
            long.MaxValue,
            code,
            usage);
        string reason = "ISO minor-unit metadata is unavailable for currency '" + code + "'.";

        Assert.Multiple(() =>
        {
            Assert.That(major.Success, Is.False, code);
            Assert.That(major.Error, Is.EqualTo(AmountValidationError.MetadataUnavailable), code);
            Assert.That(major.Reason, Is.EqualTo(reason), code);
            Assert.That(major.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(major.MajorAmount, Is.EqualTo(decimal.MinValue), code);
            Assert.That(major.MinorUnitValue, Is.Zero, code);
            Assert.That(minor.Success, Is.False, code);
            Assert.That(minor.Error, Is.EqualTo(AmountValidationError.MetadataUnavailable), code);
            Assert.That(minor.Reason, Is.EqualTo(reason), code);
            Assert.That(minor.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(minor.MajorAmount, Is.Zero, code);
            Assert.That(minor.MinorUnitValue, Is.EqualTo(long.MaxValue), code);
        });
    }

    private static void AssertMajorSuccess(
        MinorUnitConversionResult result,
        string code,
        decimal majorAmount,
        long minorUnitValue)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, code);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None), code);
            Assert.That(result.Reason, Is.Empty, code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.MajorAmount, Is.EqualTo(majorAmount), code);
            Assert.That(result.MinorUnitValue, Is.EqualTo(minorUnitValue), code);
        });
    }

    private static void AssertMinorSuccess(
        MinorUnitConversionResult result,
        string code,
        decimal majorAmount,
        long minorUnitValue)
    {
        AssertMajorSuccess(result, code, majorAmount, minorUnitValue);
    }

    private static void AssertMajorFailure(
        MinorUnitConversionResult result,
        string code,
        decimal majorAmount,
        AmountValidationError error,
        string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, code);
            Assert.That(result.Error, Is.EqualTo(error), code);
            Assert.That(result.Reason, Is.EqualTo(reason), code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.MajorAmount, Is.EqualTo(majorAmount), code);
            Assert.That(result.MinorUnitValue, Is.Zero, code);
        });
    }

    private static ExpectedConversionRule SelectRule(
        ExpectedCurrencyFormatMetadata metadata,
        AmountUsage usage)
    {
        if (usage == AmountUsage.IsoMinorUnit)
        {
            return new ExpectedConversionRule(
                metadata.HasIsoMinorUnit,
                metadata.IsoMinorUnitDigits,
                0,
                metadata.IsoMinorUnitFactor);
        }

        if (usage == AmountUsage.StandardFormat)
        {
            return new ExpectedConversionRule(
                true,
                metadata.StandardFormatDigits,
                metadata.StandardRoundingIncrement,
                metadata.StandardFormatFactor);
        }

        return new ExpectedConversionRule(
            true,
            metadata.CashDigits,
            metadata.CashRoundingIncrement,
            metadata.CashFactor);
    }

    private static AmountUsage[] GetUsages()
    {
        return new[]
        {
            AmountUsage.IsoMinorUnit,
            AmountUsage.StandardFormat,
            AmountUsage.Cash
        };
    }

    private class ExpectedConversionRule
    {
        internal bool IsAvailable;
        internal int Digits;
        internal int Increment;
        internal decimal Factor;

        internal ExpectedConversionRule(
            bool isAvailable,
            int digits,
            int increment,
            decimal factor)
        {
            IsAvailable = isAvailable;
            Digits = digits;
            Increment = increment;
            Factor = factor;
        }
    }
}
