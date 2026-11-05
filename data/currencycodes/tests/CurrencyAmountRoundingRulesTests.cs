using System;

namespace currencycodes.Tests;

public class CurrencyAmountRoundingRulesTests
{
    [Test]
    public void NearestModesRoundPositiveAndNegativeMidpoints()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 1.225m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(1.22m));
            Assert.That(
                Round(store, 1.235m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(1.24m));
            Assert.That(
                Round(store, -1.225m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(-1.22m));
            Assert.That(
                Round(store, -1.235m, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(-1.24m));
            Assert.That(
                Round(store, 1.225m, "USD", AmountUsage.StandardFormat, MidpointRounding.AwayFromZero),
                Is.EqualTo(1.23m));
            Assert.That(
                Round(store, -1.225m, "USD", AmountUsage.StandardFormat, MidpointRounding.AwayFromZero),
                Is.EqualTo(-1.23m));
        });
    }

    [Test]
    public void DirectedModesApplyToMidpointAndNonMidpointValues()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 2.5m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToZero),
                Is.EqualTo(2m));
            Assert.That(
                Round(store, -2.5m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToZero),
                Is.EqualTo(-2m));
            Assert.That(
                Round(store, 2.1m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToNegativeInfinity),
                Is.EqualTo(2m));
            Assert.That(
                Round(store, -2.1m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToNegativeInfinity),
                Is.EqualTo(-3m));
            Assert.That(
                Round(store, 2.1m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToPositiveInfinity),
                Is.EqualTo(3m));
            Assert.That(
                Round(store, -2.1m, "JPY", AmountUsage.StandardFormat, MidpointRounding.ToPositiveInfinity),
                Is.EqualTo(-2m));
        });
    }

    [Test]
    public void CashRoundingUsesConfiguredFiveAndFiftyUnitIncrements()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 1.025m, "CHF", AmountUsage.Cash, MidpointRounding.ToEven),
                Is.EqualTo(1.00m));
            Assert.That(
                Round(store, 1.075m, "CHF", AmountUsage.Cash, MidpointRounding.ToEven),
                Is.EqualTo(1.10m));
            Assert.That(
                Round(store, -1.025m, "CHF", AmountUsage.Cash, MidpointRounding.AwayFromZero),
                Is.EqualTo(-1.05m));
            Assert.That(
                Round(store, 1.24m, "DKK", AmountUsage.Cash, MidpointRounding.ToEven),
                Is.EqualTo(1.00m));
            Assert.That(
                Round(store, 1.25m, "DKK", AmountUsage.Cash, MidpointRounding.AwayFromZero),
                Is.EqualTo(1.50m));
        });
    }

    [Test]
    public void IntegerCashIncrementRoundsWithoutScaleConversion()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 12.5m, "HUF", AmountUsage.Cash, MidpointRounding.ToEven),
                Is.EqualTo(10m));
            Assert.That(
                Round(store, 12.5m, "HUF", AmountUsage.Cash, MidpointRounding.AwayFromZero),
                Is.EqualTo(15m));
            Assert.That(
                Round(store, -12.1m, "HUF", AmountUsage.Cash, MidpointRounding.ToNegativeInfinity),
                Is.EqualTo(-15m));
        });
    }

    [Test]
    public void ThreeAndFourDigitRulesRoundAtTheirSelectedScale()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 1.2345m, "BHD", AmountUsage.IsoMinorUnit, MidpointRounding.AwayFromZero),
                Is.EqualTo(1.235m));
            Assert.That(
                Round(store, 1.23445m, "CLF", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(1.2344m));
            Assert.That(
                Round(store, -1.23445m, "CLF", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(-1.2344m));
        });
    }

    [Test]
    public void ExactValuesAndDecimalBoundariesAreReturnedWithoutScaling()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                Round(store, 1.05m, "CHF", AmountUsage.Cash, MidpointRounding.ToEven),
                Is.EqualTo(1.05m));
            Assert.That(
                Round(store, 0m, "CHF", AmountUsage.Cash, MidpointRounding.ToPositiveInfinity),
                Is.Zero);
            Assert.That(
                Round(store, decimal.MaxValue, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(decimal.MaxValue));
            Assert.That(
                Round(store, decimal.MinValue, "USD", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Is.EqualTo(decimal.MinValue));
        });
    }

    [Test]
    public void CheckedDecimalAdditionReportsOverflow()
    {
        decimal value = decimal.MaxValue - 2m;

        Assert.Multiple(() =>
        {
            Assert.That(
                CurrencyAmountRoundingRules.RoundToIncrement(
                    decimal.MaxValue,
                    6m,
                    MidpointRounding.ToEven),
                Is.EqualTo(decimal.MaxValue - 3m));
            Assert.That(
                () => CurrencyAmountRoundingRules.RoundToIncrement(
                    decimal.MaxValue,
                    6m,
                    MidpointRounding.AwayFromZero),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => CurrencyAmountRoundingRules.RoundToIncrement(
                    value,
                    4m,
                    MidpointRounding.ToPositiveInfinity),
                Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void InvalidCurrencyAndUnavailableMetadataUseDedicatedExceptions()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Round(store, 1m, "US1", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => Round(store, 1m, "BGN", AmountUsage.StandardFormat, MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'BGN' is outside the requested scope."));
            Assert.That(
                () => Round(store, 1m, "XAU", AmountUsage.IsoMinorUnit, MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyAmountException>()
                    .With.Message.EqualTo(
                        "The selected amount metadata is unavailable for currency 'XAU'."));
        });
    }

    [Test]
    public void InvalidSelectorsAndIncrementAreRejectedBeforeArithmetic()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Round(store, 1m, null, (AmountUsage)99, MidpointRounding.ToEven),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The amount usage is invalid."));
            Assert.That(
                () => Round(store, 1m, null, AmountUsage.StandardFormat, (MidpointRounding)99),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The midpoint rounding mode is invalid."));
            Assert.That(
                () => CurrencyAmountRoundingRules.RoundToIncrement(
                    1m,
                    0m,
                    MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyAmountException>()
                    .With.Message.EqualTo("The rounding increment must be positive."));
        });
    }

    private static decimal Round(
        CurrencyDataStore store,
        decimal amount,
        string? code,
        AmountUsage usage,
        MidpointRounding midpointRounding)
    {
        return CurrencyAmountRoundingRules.RoundCurrencyAmount(
            store,
            amount,
            code,
            usage,
            midpointRounding);
    }
}
