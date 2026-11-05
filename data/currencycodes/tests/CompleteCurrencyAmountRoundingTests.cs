using System;

namespace currencycodes.Tests;

public class CompleteCurrencyAmountRoundingTests
{
    [Test]
    public void EveryMidpointModeRoundsPositiveAndNegativeMidpoints()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertMode(
            rules,
            2.5m,
            -2.5m,
            MidpointRounding.ToEven,
            2m,
            -2m);
        AssertMode(
            rules,
            2.5m,
            -2.5m,
            MidpointRounding.AwayFromZero,
            3m,
            -3m);
        AssertMode(
            rules,
            2.5m,
            -2.5m,
            MidpointRounding.ToZero,
            2m,
            -2m);
        AssertMode(
            rules,
            2.5m,
            -2.5m,
            MidpointRounding.ToNegativeInfinity,
            2m,
            -3m);
        AssertMode(
            rules,
            2.5m,
            -2.5m,
            MidpointRounding.ToPositiveInfinity,
            3m,
            -2m);
    }

    [Test]
    public void EveryMidpointModeRoundsPositiveAndNegativeNonMidpoints()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertMode(rules, 2.1m, -2.1m, MidpointRounding.ToEven, 2m, -2m);
        AssertMode(rules, 2.1m, -2.1m, MidpointRounding.AwayFromZero, 2m, -2m);
        AssertMode(rules, 2.1m, -2.1m, MidpointRounding.ToZero, 2m, -2m);
        AssertMode(rules, 2.1m, -2.1m, MidpointRounding.ToNegativeInfinity, 2m, -3m);
        AssertMode(rules, 2.1m, -2.1m, MidpointRounding.ToPositiveInfinity, 3m, -2m);
        AssertMode(rules, 2.6m, -2.6m, MidpointRounding.ToEven, 3m, -3m);
        AssertMode(rules, 2.6m, -2.6m, MidpointRounding.AwayFromZero, 3m, -3m);
        AssertMode(rules, 2.6m, -2.6m, MidpointRounding.ToZero, 2m, -2m);
        AssertMode(rules, 2.6m, -2.6m, MidpointRounding.ToNegativeInfinity, 2m, -3m);
        AssertMode(rules, 2.6m, -2.6m, MidpointRounding.ToPositiveInfinity, 3m, -2m);
    }

    [Test]
    public void ExactValuesAndZeroRemainUnchangedForEveryMode()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        foreach (MidpointRounding mode in GetModes())
        {
            Assert.Multiple(() =>
            {
                Assert.That(Round(rules, 2m, "JPY", AmountUsage.StandardFormat, mode), Is.EqualTo(2m));
                Assert.That(Round(rules, -2m, "JPY", AmountUsage.StandardFormat, mode), Is.EqualTo(-2m));
                Assert.That(Round(rules, 0m, "JPY", AmountUsage.StandardFormat, mode), Is.Zero);
                Assert.That(Round(rules, 1.05m, "CHF", AmountUsage.Cash, mode), Is.EqualTo(1.05m));
                Assert.That(Round(rules, -1.05m, "CHF", AmountUsage.Cash, mode), Is.EqualTo(-1.05m));
            });
        }
    }

    [Test]
    public void CashIncrementMidpointsHonorEveryModeAtEverySupportedIncrementScale()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertIncrementModes(rules, "CHF", 1.025m, 1.00m, 1.05m);
        AssertIncrementModes(rules, "DKK", 1.25m, 1.00m, 1.50m);
        AssertIncrementModes(rules, "HUF", 12.5m, 10m, 15m);
    }

    [Test]
    public void ThreeAndFourDigitMidpointsHonorEveryMode()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        AssertScaleModes(
            rules,
            "BHD",
            AmountUsage.IsoMinorUnit,
            1.2345m,
            1.234m,
            1.235m);
        AssertScaleModes(
            rules,
            "CLF",
            AmountUsage.StandardFormat,
            1.23445m,
            1.2344m,
            1.2345m);
    }

    [Test]
    public void MidpointRoundingCarriesAcrossTheIntegerBoundaryForBothSigns()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(rules, 9.995m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(10.00m));
            Assert.That(
                Round(rules, -9.995m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(-10.00m));
            Assert.That(
                Round(rules, 99.95m, "JPY", AmountUsage.StandardFormat, MidpointRounding.AwayFromZero),
                Is.EqualTo(100m));
            Assert.That(
                Round(rules, -99.95m, "JPY", AmountUsage.StandardFormat, MidpointRounding.AwayFromZero),
                Is.EqualTo(-100m));
        });
    }

    [Test]
    public void OverflowIsReportedOnlyWhenTheRoundedResultExceedsDecimalRange()
    {
        decimal positiveOverflowInput = decimal.MaxValue - 2m;
        decimal negativeOverflowInput = decimal.MinValue + 2m;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CurrencyAmountRoundingRules.RoundToIncrement(
                    positiveOverflowInput,
                    4m,
                    MidpointRounding.ToPositiveInfinity),
                Throws.TypeOf<OverflowException>()
                    .With.Message.EqualTo("The rounded amount is outside the decimal range."));
            Assert.That(
                () => CurrencyAmountRoundingRules.RoundToIncrement(
                    negativeOverflowInput,
                    4m,
                    MidpointRounding.ToNegativeInfinity),
                Throws.TypeOf<OverflowException>()
                    .With.Message.EqualTo("The rounded amount is outside the decimal range."));
            Assert.That(
                CurrencyAmountRoundingRules.RoundToIncrement(
                    decimal.MaxValue - 1m,
                    decimal.MaxValue,
                    MidpointRounding.ToEven),
                Is.EqualTo(decimal.MaxValue));
            Assert.That(
                CurrencyAmountRoundingRules.RoundToIncrement(
                    decimal.MinValue + 1m,
                    decimal.MaxValue,
                    MidpointRounding.ToEven),
                Is.EqualTo(decimal.MinValue));
        });
    }

    private static void AssertMode(
        ICurrencyAmountRules rules,
        decimal positive,
        decimal negative,
        MidpointRounding mode,
        decimal expectedPositive,
        decimal expectedNegative)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Round(rules, positive, "JPY", AmountUsage.StandardFormat, mode),
                Is.EqualTo(expectedPositive),
                mode.ToString());
            Assert.That(
                Round(rules, negative, "JPY", AmountUsage.StandardFormat, mode),
                Is.EqualTo(expectedNegative),
                mode.ToString());
        });
    }

    private static void AssertIncrementModes(
        ICurrencyAmountRules rules,
        string code,
        decimal midpoint,
        decimal towardZero,
        decimal awayFromZero)
    {
        AssertScaleModes(
            rules,
            code,
            AmountUsage.Cash,
            midpoint,
            towardZero,
            awayFromZero);
    }

    private static void AssertScaleModes(
        ICurrencyAmountRules rules,
        string code,
        AmountUsage usage,
        decimal midpoint,
        decimal towardZero,
        decimal awayFromZero)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Round(rules, midpoint, code, usage, MidpointRounding.ToEven),
                Is.EqualTo(towardZero),
                code);
            Assert.That(
                Round(rules, -midpoint, code, usage, MidpointRounding.ToEven),
                Is.EqualTo(-towardZero),
                code);
            Assert.That(
                Round(rules, midpoint, code, usage, MidpointRounding.AwayFromZero),
                Is.EqualTo(awayFromZero),
                code);
            Assert.That(
                Round(rules, -midpoint, code, usage, MidpointRounding.AwayFromZero),
                Is.EqualTo(-awayFromZero),
                code);
            Assert.That(
                Round(rules, midpoint, code, usage, MidpointRounding.ToZero),
                Is.EqualTo(towardZero),
                code);
            Assert.That(
                Round(rules, -midpoint, code, usage, MidpointRounding.ToZero),
                Is.EqualTo(-towardZero),
                code);
            Assert.That(
                Round(rules, midpoint, code, usage, MidpointRounding.ToNegativeInfinity),
                Is.EqualTo(towardZero),
                code);
            Assert.That(
                Round(rules, -midpoint, code, usage, MidpointRounding.ToNegativeInfinity),
                Is.EqualTo(-awayFromZero),
                code);
            Assert.That(
                Round(rules, midpoint, code, usage, MidpointRounding.ToPositiveInfinity),
                Is.EqualTo(awayFromZero),
                code);
            Assert.That(
                Round(rules, -midpoint, code, usage, MidpointRounding.ToPositiveInfinity),
                Is.EqualTo(-towardZero),
                code);
        });
    }

    private static MidpointRounding[] GetModes()
    {
        return new[]
        {
            MidpointRounding.ToEven,
            MidpointRounding.AwayFromZero,
            MidpointRounding.ToZero,
            MidpointRounding.ToNegativeInfinity,
            MidpointRounding.ToPositiveInfinity
        };
    }

    private static decimal Round(
        ICurrencyAmountRules rules,
        decimal amount,
        string code,
        AmountUsage usage,
        MidpointRounding mode)
    {
        return rules.RoundCurrencyAmount(amount, code, usage, mode);
    }
}
