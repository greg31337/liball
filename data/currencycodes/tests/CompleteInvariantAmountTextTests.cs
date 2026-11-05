using System.Collections.Generic;
using System.Globalization;

namespace currencycodes.Tests;

public class CompleteInvariantAmountTextTests
{
    [Test]
    public void EveryActiveCurrencyRuleFormatsAndParsesAnExactRoundTrip()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> metadata =
            PinnedCurrencyFormatFixture.Load();
        CurrencyCodeService service = new CurrencyCodeService();
        ICurrencyCatalog catalog = service;
        ICurrencyAmountRules rules = service;
        CurrencyInfo[] currencies = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        HashSet<int> testedScales = new HashSet<int>();
        int availableCount = 0;
        int unavailableCount = 0;

        foreach (CurrencyInfo currency in currencies)
        {
            ExpectedCurrencyFormatMetadata format = metadata[currency.AlphabeticCode];
            foreach (AmountUsage usage in GetUsages())
            {
                ExpectedTextRule rule = SelectRule(format, usage);
                if (!rule.IsAvailable)
                {
                    unavailableCount++;
                    AssertUnavailableTextOperations(rules, currency.AlphabeticCode, usage);
                    continue;
                }

                availableCount++;
                testedScales.Add(rule.Digits);
                int unitCount = rule.Increment == 0 ? 1 : rule.Increment;
                decimal smallestAlignedAmount = unitCount / rule.Factor;
                AssertTextRoundTrip(
                    rules,
                    currency.AlphabeticCode,
                    usage,
                    smallestAlignedAmount,
                    rule.Digits);
                AssertTextRoundTrip(
                    rules,
                    currency.AlphabeticCode,
                    usage,
                    -smallestAlignedAmount,
                    rule.Digits);
                AssertTextRoundTrip(
                    rules,
                    currency.AlphabeticCode,
                    usage,
                    0m,
                    rule.Digits);
                AssertTextRoundTrip(
                    rules,
                    currency.AlphabeticCode,
                    usage,
                    100m,
                    rule.Digits);
                AssertTextRoundTrip(
                    rules,
                    currency.AlphabeticCode,
                    usage,
                    -100m,
                    rule.Digits);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(currencies, Has.Length.EqualTo(179));
            Assert.That(availableCount, Is.EqualTo(523));
            Assert.That(unavailableCount, Is.EqualTo(14));
            Assert.That(testedScales, Is.EquivalentTo(new[] { 0, 2, 3, 4 }));
        });
    }

    [Test]
    public void FormatterAppliesEveryExplicitRoundingModeForBothSigns()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertFormattedPair(rules, MidpointRounding.ToEven, 1.00m, -1.00m);
        AssertFormattedPair(rules, MidpointRounding.AwayFromZero, 1.05m, -1.05m);
        AssertFormattedPair(rules, MidpointRounding.ToZero, 1.00m, -1.00m);
        AssertFormattedPair(rules, MidpointRounding.ToNegativeInfinity, 1.00m, -1.05m);
        AssertFormattedPair(rules, MidpointRounding.ToPositiveInfinity, 1.05m, -1.00m);
    }

    [Test]
    public void ParserRejectsEveryNonAsciiAndNonCanonicalGrammarFamily()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();
        string?[] invalidTexts = new string?[]
        {
            null,
            string.Empty,
            "USD",
            "USD ",
            " USD 1.00",
            "USD\t1.00",
            "USD\u00A01.00",
            "USD  1.00",
            "USD 1.00 ",
            "USD +1.00",
            "USD -",
            "USD .50",
            "USD -.50",
            "USD 1.",
            "USD 1.2.3",
            "USD 1,00",
            "USD 1_000.00",
            "USD 1e2",
            "USD NaN",
            "USD Infinity",
            "USD \u22121.00",
            "USD \uFF0B1.00",
            "USD \u0661.00",
            "USD \uFF11.00",
            "USD 1\u066B00",
            "USD 1\uFF0E00",
            "USD 1.00\n",
            "USD 1.00\r",
            "USD 1.00\t"
        };

        foreach (string? text in invalidTexts)
        {
            AmountTextResult result = rules.ParseInvariantCurrencyAmount(
                text,
                AmountUsage.StandardFormat);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, text);
                Assert.That(result.Error, Is.EqualTo(AmountValidationError.InvalidFormat), text);
                Assert.That(
                    result.Reason,
                    Is.EqualTo("The invariant currency amount text has an invalid format."),
                    text);
                Assert.That(result.Amount, Is.Zero, text);
                Assert.That(result.InvariantText, Is.Empty, text);
            });
        }
    }

    [Test]
    public void ParserAcceptsOnlyTheExactAsciiSeparatorAndPreservesValidDigits()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();
        string longTrailingZeroText = "USD 1."
            + new string('0', 100);

        AmountTextResult lowercase = rules.ParseInvariantCurrencyAmount(
            "usd -001.2300",
            AmountUsage.StandardFormat);
        AmountTextResult negativeZero = rules.ParseInvariantCurrencyAmount(
            "USD -0.00",
            AmountUsage.StandardFormat);
        AmountTextResult trailingZeroes = rules.ParseInvariantCurrencyAmount(
            longTrailingZeroText,
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            AssertTextSuccess(lowercase, "USD", -1.23m, "USD -001.2300");
            AssertTextSuccess(negativeZero, "USD", 0m, "USD -0.00");
            AssertTextSuccess(trailingZeroes, "USD", 1m, longTrailingZeroText);
        });
    }

    [Test]
    public void MalformedUnknownHistoricalAndUnavailableCurrenciesReturnStructuredFailures()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertCurrencyFailure(
            rules.FormatInvariantCurrencyAmount(
                1.25m,
                "US1",
                AmountUsage.StandardFormat,
                MidpointRounding.ToEven),
            1.25m,
            AmountValidationError.InvalidCurrency,
            null,
            "The currency alphabetic code is malformed.");
        AssertCurrencyFailure(
            rules.ParseInvariantCurrencyAmount("ZZZ 1.25", AmountUsage.StandardFormat),
            0m,
            AmountValidationError.InvalidCurrency,
            null,
            "Currency alphabetic code 'ZZZ' is not assigned.");
        AssertCurrencyFailure(
            rules.FormatInvariantCurrencyAmount(
                -2.5m,
                "BGN",
                AmountUsage.StandardFormat,
                MidpointRounding.ToEven),
            -2.5m,
            AmountValidationError.InvalidCurrency,
            "BGN",
            "Currency alphabetic code 'BGN' is not active.");
        AssertCurrencyFailure(
            rules.ParseInvariantCurrencyAmount("XAU 1.00", AmountUsage.IsoMinorUnit),
            0m,
            AmountValidationError.MetadataUnavailable,
            "XAU",
            "ISO minor-unit metadata is unavailable for currency 'XAU'.");
    }

    [Test]
    public void DecimalLimitsRoundTripAtEverySupportedScaleAndOverflowIsStructured()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertExtremeRoundTrip(rules, "JPY", AmountUsage.StandardFormat, decimal.MaxValue);
        AssertExtremeRoundTrip(rules, "JPY", AmountUsage.StandardFormat, decimal.MinValue);
        AssertExtremeRoundTrip(rules, "USD", AmountUsage.StandardFormat, decimal.MaxValue);
        AssertExtremeRoundTrip(rules, "USD", AmountUsage.StandardFormat, decimal.MinValue);
        AssertExtremeRoundTrip(rules, "BHD", AmountUsage.IsoMinorUnit, decimal.MaxValue);
        AssertExtremeRoundTrip(rules, "BHD", AmountUsage.IsoMinorUnit, decimal.MinValue);
        AssertExtremeRoundTrip(rules, "CLF", AmountUsage.StandardFormat, decimal.MaxValue);
        AssertExtremeRoundTrip(rules, "CLF", AmountUsage.StandardFormat, decimal.MinValue);

        foreach (string text in new[]
        {
            "USD 79228162514264337593543950336",
            "USD -79228162514264337593543950336",
            "USD 79228162514264337593543950335.1",
            "USD -79228162514264337593543950335.1",
            "USD 0.00000000000000000000000000001",
            "USD -0.00000000000000000000000000001"
        })
        {
            AmountTextResult result = rules.ParseInvariantCurrencyAmount(
                text,
                AmountUsage.StandardFormat);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, text);
                Assert.That(result.Error, Is.EqualTo(AmountValidationError.OutOfRange), text);
                Assert.That(
                    result.Reason,
                    Is.EqualTo("The invariant amount is outside the decimal range."),
                    text);
                Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo("USD"), text);
                Assert.That(result.Amount, Is.Zero, text);
                Assert.That(result.InvariantText, Is.Empty, text);
            });
        }
    }

    private static void AssertTextRoundTrip(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage,
        decimal amount,
        int digits)
    {
        string amountText = amount.ToString(
            "F" + digits.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);
        string expectedText = code + " " + amountText;
        AmountTextResult formatted = rules.FormatInvariantCurrencyAmount(
            amount,
            "\t" + code.ToLowerInvariant() + "\u00A0",
            usage,
            MidpointRounding.ToEven);
        AssertTextSuccess(formatted, code, amount, expectedText);

        AmountTextResult parsed = rules.ParseInvariantCurrencyAmount(
            formatted.InvariantText,
            usage);
        AssertTextSuccess(parsed, code, amount, expectedText);
    }

    private static void AssertFormattedPair(
        ICurrencyAmountRules rules,
        MidpointRounding mode,
        decimal expectedPositive,
        decimal expectedNegative)
    {
        AmountTextResult positive = rules.FormatInvariantCurrencyAmount(
            1.025m,
            "CHF",
            AmountUsage.Cash,
            mode);
        AmountTextResult negative = rules.FormatInvariantCurrencyAmount(
            -1.025m,
            "CHF",
            AmountUsage.Cash,
            mode);

        Assert.Multiple(() =>
        {
            AssertTextSuccess(
                positive,
                "CHF",
                expectedPositive,
                "CHF " + expectedPositive.ToString("F2", CultureInfo.InvariantCulture));
            AssertTextSuccess(
                negative,
                "CHF",
                expectedNegative,
                "CHF " + expectedNegative.ToString("F2", CultureInfo.InvariantCulture));
        });
    }

    private static void AssertExtremeRoundTrip(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage,
        decimal amount)
    {
        AmountTextResult formatted = rules.FormatInvariantCurrencyAmount(
            amount,
            code,
            usage,
            MidpointRounding.ToEven);
        Assert.That(formatted.Success, Is.True, code);

        AmountTextResult parsed = rules.ParseInvariantCurrencyAmount(
            formatted.InvariantText,
            usage);
        AssertTextSuccess(parsed, code, amount, formatted.InvariantText);
    }

    private static void AssertUnavailableTextOperations(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage)
    {
        AmountTextResult formatted = rules.FormatInvariantCurrencyAmount(
            decimal.MaxValue,
            code,
            usage,
            MidpointRounding.ToEven);
        AmountTextResult parsed = rules.ParseInvariantCurrencyAmount(
            code + " 1.00",
            usage);
        string reason = "ISO minor-unit metadata is unavailable for currency '" + code + "'.";

        Assert.Multiple(() =>
        {
            AssertCurrencyFailure(
                formatted,
                decimal.MaxValue,
                AmountValidationError.MetadataUnavailable,
                code,
                reason);
            AssertCurrencyFailure(
                parsed,
                0m,
                AmountValidationError.MetadataUnavailable,
                code,
                reason);
        });
    }

    private static void AssertTextSuccess(
        AmountTextResult result,
        string code,
        decimal amount,
        string text)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, code);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None), code);
            Assert.That(result.Reason, Is.Empty, code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.Amount, Is.EqualTo(amount), code);
            Assert.That(result.InvariantText, Is.EqualTo(text), code);
        });
    }

    private static void AssertCurrencyFailure(
        AmountTextResult result,
        decimal amount,
        AmountValidationError error,
        string? code,
        string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, code);
            Assert.That(result.Error, Is.EqualTo(error), code);
            Assert.That(result.Reason, Is.EqualTo(reason), code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Amount, Is.EqualTo(amount), code);
            Assert.That(result.InvariantText, Is.Empty, code);
        });
    }

    private static ExpectedTextRule SelectRule(
        ExpectedCurrencyFormatMetadata metadata,
        AmountUsage usage)
    {
        if (usage == AmountUsage.IsoMinorUnit)
        {
            return new ExpectedTextRule(
                metadata.HasIsoMinorUnit,
                metadata.IsoMinorUnitDigits,
                0,
                metadata.IsoMinorUnitFactor);
        }

        if (usage == AmountUsage.StandardFormat)
        {
            return new ExpectedTextRule(
                true,
                metadata.StandardFormatDigits,
                metadata.StandardRoundingIncrement,
                metadata.StandardFormatFactor);
        }

        return new ExpectedTextRule(
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

    private class ExpectedTextRule
    {
        internal bool IsAvailable;
        internal int Digits;
        internal int Increment;
        internal decimal Factor;

        internal ExpectedTextRule(
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
