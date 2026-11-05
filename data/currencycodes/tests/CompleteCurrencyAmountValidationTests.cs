using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyAmountValidationTests
{
    [Test]
    public void EveryActiveCurrencyRuleValidatesExactAndInexactAmounts()
    {
        Dictionary<string, ExpectedCurrencyFormatMetadata> metadata =
            PinnedCurrencyFormatFixture.Load();
        CurrencyCodeService service = new CurrencyCodeService();
        ICurrencyCatalog catalog = service;
        ICurrencyValidator validator = service;
        CurrencyInfo[] currencies = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        AmountUsage[] usages = new[]
        {
            AmountUsage.IsoMinorUnit,
            AmountUsage.StandardFormat,
            AmountUsage.Cash
        };
        HashSet<string> testedCombinations = new HashSet<string>(StringComparer.Ordinal);
        int availableRuleCount = 0;
        int unavailableRuleCount = 0;

        foreach (CurrencyInfo currency in currencies)
        {
            ExpectedCurrencyFormatMetadata expectedMetadata = metadata[currency.AlphabeticCode];
            foreach (AmountUsage usage in usages)
            {
                ExpectedAmountRule rule = SelectRule(expectedMetadata, usage);
                if (!rule.IsAvailable)
                {
                    unavailableRuleCount++;
                    AmountValidationResult unavailable = validator.ValidateCurrencyAmount(
                        1.25m,
                        currency.AlphabeticCode,
                        usage);
                    AssertUnavailable(unavailable, currency.AlphabeticCode, usage, 1.25m);
                    continue;
                }

                availableRuleCount++;
                testedCombinations.Add(
                    ((int)usage).ToString()
                        + "|"
                        + rule.Digits.ToString()
                        + "|"
                        + rule.Increment.ToString());
                ValidateAvailableRule(
                    validator,
                    currency.AlphabeticCode,
                    usage,
                    rule);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(currencies, Has.Length.EqualTo(179));
            Assert.That(availableRuleCount, Is.EqualTo(523));
            Assert.That(unavailableRuleCount, Is.EqualTo(14));
            Assert.That(testedCombinations, Has.Count.EqualTo(15));
            Assert.That(testedCombinations, Does.Contain("0|0|0"));
            Assert.That(testedCombinations, Does.Contain("0|2|0"));
            Assert.That(testedCombinations, Does.Contain("0|3|0"));
            Assert.That(testedCombinations, Does.Contain("0|4|0"));
            Assert.That(testedCombinations, Does.Contain("1|0|0"));
            Assert.That(testedCombinations, Does.Contain("1|2|0"));
            Assert.That(testedCombinations, Does.Contain("1|3|0"));
            Assert.That(testedCombinations, Does.Contain("1|4|0"));
            Assert.That(testedCombinations, Does.Contain("2|0|0"));
            Assert.That(testedCombinations, Does.Contain("2|0|5"));
            Assert.That(testedCombinations, Does.Contain("2|2|0"));
            Assert.That(testedCombinations, Does.Contain("2|2|5"));
            Assert.That(testedCombinations, Does.Contain("2|2|50"));
            Assert.That(testedCombinations, Does.Contain("2|3|0"));
            Assert.That(testedCombinations, Does.Contain("2|4|0"));
        });
    }

    [Test]
    public void WellFormedUnknownCurrencyPreservesTheAmountInItsStructuredFailure()
    {
        ICurrencyValidator validator = new CurrencyCodeService();

        foreach (AmountUsage usage in new[]
        {
            AmountUsage.IsoMinorUnit,
            AmountUsage.StandardFormat,
            AmountUsage.Cash
        })
        {
            AmountValidationResult result = validator.ValidateCurrencyAmount(
                decimal.MinValue,
                " zzz ",
                usage);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
                Assert.That(
                    result.Reason,
                    Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
                Assert.That(result.Currency, Is.Null);
                Assert.That(result.Amount, Is.EqualTo(decimal.MinValue));
                Assert.That(result.HasRequiredRule, Is.False);
                Assert.That(result.RequiredDigits, Is.Zero);
                Assert.That(result.RequiredIncrement, Is.Zero);
                Assert.That(result.HasRoundedCandidate, Is.False);
                Assert.That(result.RoundedCandidate, Is.Zero);
            });
        }
    }

    private static void ValidateAvailableRule(
        ICurrencyValidator validator,
        string code,
        AmountUsage usage,
        ExpectedAmountRule rule)
    {
        int unitCount = rule.Increment == 0 ? 1 : rule.Increment;
        decimal exact = unitCount / rule.Factor;
        decimal excessiveScale = 1m / (rule.Factor * 10m);
        decimal trailingPositive = CreateTrailingZeroAmount(unitCount, rule.Digits, false);
        decimal trailingNegative = CreateTrailingZeroAmount(unitCount, rule.Digits, true);
        decimal scaledZero = new decimal(0, 0, 0, false, 28);

        AssertValid(validator.ValidateCurrencyAmount(exact, code, usage), code, exact, rule);
        AssertValid(validator.ValidateCurrencyAmount(-exact, code, usage), code, -exact, rule);
        AssertValid(validator.ValidateCurrencyAmount(0m, code, usage), code, 0m, rule);
        AssertValid(validator.ValidateCurrencyAmount(scaledZero, code, usage), code, scaledZero, rule);
        AssertValid(
            validator.ValidateCurrencyAmount(trailingPositive, code, usage),
            code,
            trailingPositive,
            rule);
        AssertValid(
            validator.ValidateCurrencyAmount(trailingNegative, code, usage),
            code,
            trailingNegative,
            rule);
        AssertValid(
            validator.ValidateCurrencyAmount(decimal.MaxValue, code, usage),
            code,
            decimal.MaxValue,
            rule);
        AssertValid(
            validator.ValidateCurrencyAmount(decimal.MinValue, code, usage),
            code,
            decimal.MinValue,
            rule);
        AssertRuleFailure(
            validator.ValidateCurrencyAmount(excessiveScale, code, usage),
            code,
            excessiveScale,
            rule,
            AmountValidationError.InvalidScale);
        AssertRuleFailure(
            validator.ValidateCurrencyAmount(-excessiveScale, code, usage),
            code,
            -excessiveScale,
            rule,
            AmountValidationError.InvalidScale);

        if (rule.Increment > 1)
        {
            decimal wrongIncrement = 1m / rule.Factor;
            AssertRuleFailure(
                validator.ValidateCurrencyAmount(wrongIncrement, code, usage),
                code,
                wrongIncrement,
                rule,
                AmountValidationError.InvalidIncrement);
            AssertRuleFailure(
                validator.ValidateCurrencyAmount(-wrongIncrement, code, usage),
                code,
                -wrongIncrement,
                rule,
                AmountValidationError.InvalidIncrement);
        }
    }

    private static ExpectedAmountRule SelectRule(
        ExpectedCurrencyFormatMetadata metadata,
        AmountUsage usage)
    {
        if (usage == AmountUsage.IsoMinorUnit)
        {
            return new ExpectedAmountRule(
                metadata.HasIsoMinorUnit,
                metadata.IsoMinorUnitDigits,
                0,
                metadata.IsoMinorUnitFactor);
        }

        if (usage == AmountUsage.StandardFormat)
        {
            return new ExpectedAmountRule(
                true,
                metadata.StandardFormatDigits,
                metadata.StandardRoundingIncrement,
                metadata.StandardFormatFactor);
        }

        return new ExpectedAmountRule(
            true,
            metadata.CashDigits,
            metadata.CashRoundingIncrement,
            metadata.CashFactor);
    }

    private static decimal CreateTrailingZeroAmount(int unitCount, int digits, bool negative)
    {
        return new decimal(unitCount * 10, 0, 0, negative, (byte)(digits + 1));
    }

    private static void AssertValid(
        AmountValidationResult result,
        string code,
        decimal amount,
        ExpectedAmountRule rule)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, code);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None), code);
            Assert.That(result.Reason, Is.Empty, code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.Amount, Is.EqualTo(amount), code);
            Assert.That(result.HasRequiredRule, Is.True, code);
            Assert.That(result.RequiredDigits, Is.EqualTo(rule.Digits), code);
            Assert.That(result.RequiredIncrement, Is.EqualTo(rule.Increment), code);
            Assert.That(result.HasRoundedCandidate, Is.False, code);
            Assert.That(result.RoundedCandidate, Is.Zero, code);
        });
    }

    private static void AssertRuleFailure(
        AmountValidationResult result,
        string code,
        decimal amount,
        ExpectedAmountRule rule,
        AmountValidationError error)
    {
        string reason = error == AmountValidationError.InvalidScale
            ? "The amount has more fractional digits than the selected rule permits."
            : "The amount is not a multiple of the selected rounding increment.";

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False, code);
            Assert.That(result.Error, Is.EqualTo(error), code);
            Assert.That(result.Reason, Is.EqualTo(reason), code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.Amount, Is.EqualTo(amount), code);
            Assert.That(result.HasRequiredRule, Is.True, code);
            Assert.That(result.RequiredDigits, Is.EqualTo(rule.Digits), code);
            Assert.That(result.RequiredIncrement, Is.EqualTo(rule.Increment), code);
            Assert.That(result.HasRoundedCandidate, Is.False, code);
            Assert.That(result.RoundedCandidate, Is.Zero, code);
        });
    }

    private static void AssertUnavailable(
        AmountValidationResult result,
        string code,
        AmountUsage usage,
        decimal amount)
    {
        string sourceName = usage == AmountUsage.IsoMinorUnit
            ? "ISO minor-unit"
            : usage == AmountUsage.StandardFormat
                ? "Standard-format"
                : "Cash";

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False, code);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.MetadataUnavailable), code);
            Assert.That(
                result.Reason,
                Is.EqualTo(sourceName + " metadata is unavailable for currency '" + code + "'."),
                code);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(code), code);
            Assert.That(result.Currency?.Status, Is.EqualTo(CurrencyStatus.Active), code);
            Assert.That(result.Amount, Is.EqualTo(amount), code);
            Assert.That(result.HasRequiredRule, Is.False, code);
            Assert.That(result.RequiredDigits, Is.Zero, code);
            Assert.That(result.RequiredIncrement, Is.Zero, code);
            Assert.That(result.HasRoundedCandidate, Is.False, code);
            Assert.That(result.RoundedCandidate, Is.Zero, code);
        });
    }

    private class ExpectedAmountRule
    {
        internal bool IsAvailable;
        internal int Digits;
        internal int Increment;
        internal decimal Factor;

        internal ExpectedAmountRule(
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
