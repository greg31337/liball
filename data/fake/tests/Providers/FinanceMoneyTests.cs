using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceMoneyTests
{
    [Test]
    public void AmountsAndPricesAreDeterministicExactDecimalQuanta()
    {
        FinanceProvider first = Fake.Create(9810).Finance();
        FinanceProvider second = Fake.Create(9810).Finance();

        int index = 0;
        while (index < 256)
        {
            decimal firstAmount = first.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(-25m, 75m, 3, MoneyRoundingMode.ToEven);
            decimal secondAmount = second.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(-25m, 75m, 3, MoneyRoundingMode.ToEven);
            decimal firstPrice = first.Price();
            decimal secondPrice = second.Price();

            Assert.Multiple(() =>
            {
                Assert.That(firstAmount, Is.EqualTo(secondAmount));
                Assert.That(firstAmount, Is.InRange(-25m, 75m));
                Assert.That(firstAmount * 1000m,
                    Is.EqualTo(decimal.Truncate(firstAmount * 1000m)));
                Assert.That(firstPrice, Is.EqualTo(secondPrice));
                Assert.That(firstPrice, Is.InRange(0.01m, 1000m));
                Assert.That(firstPrice * 100m,
                    Is.EqualTo(decimal.Truncate(firstPrice * 100m)));
            });
            index++;
        }
    }

    [Test]
    public void GeneratedAmountsStayInsideTheRepresentableInclusiveInterval()
    {
        FinanceProvider finance = Fake.Create(9811).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(
                finance.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(1.001m, 1.019m, 2, MoneyRoundingMode.ToEven),
                Is.EqualTo(1.01m));
            Assert.That(finance.AmountFromMinimumAndMaximum(5m, 5m), Is.EqualTo(5m));
            Assert.That(finance.PriceFromMinimumAndMaximum(0m, 0m), Is.EqualTo(0m));
        });
    }

    [Test]
    public void MoneySupportsEveryExplicitRoundingRule()
    {
        FinanceProvider finance = Fake.Create(9812).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(
                finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.ToEven).Amount,
                Is.EqualTo(2.34m));
            Assert.That(
                finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.AwayFromZero).Amount,
                Is.EqualTo(2.35m));
            Assert.That(
                finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToZero).Amount,
                Is.EqualTo(-2.34m));
            Assert.That(
                finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToNegativeInfinity).Amount,
                Is.EqualTo(-2.35m));
            Assert.That(
                finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.341m, "USD", MoneyRoundingMode.ToPositiveInfinity).Amount,
                Is.EqualTo(2.35m));
        });
    }

    [Test]
    public void MidpointRoundingRulesHandlePositiveAndNegativeValuesSymmetrically()
    {
        FinanceProvider finance = Fake.Create(9819).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.ToEven).Amount,
                Is.EqualTo(2.34m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToEven).Amount,
                Is.EqualTo(-2.34m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.AwayFromZero).Amount,
                Is.EqualTo(2.35m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.AwayFromZero).Amount,
                Is.EqualTo(-2.35m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.ToZero).Amount,
                Is.EqualTo(2.34m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToZero).Amount,
                Is.EqualTo(-2.34m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.ToNegativeInfinity).Amount,
                Is.EqualTo(2.34m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToNegativeInfinity).Amount,
                Is.EqualTo(-2.35m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(2.345m, "USD", MoneyRoundingMode.ToPositiveInfinity).Amount,
                Is.EqualTo(2.35m));
            Assert.That(finance.MoneyFromAmountAndCurrencyCodeAndRoundingMode(-2.345m, "USD", MoneyRoundingMode.ToPositiveInfinity).Amount,
                Is.EqualTo(-2.34m));
        });
    }

    [Test]
    public void MoneyDefaultsToCurrencyMinorUnitsWithoutConflatingNotApplicable()
    {
        FinanceProvider finance = Fake.Create(9813).Finance();
        MoneyRecord yen = finance.MoneyFromAmountAndCurrencyCode(1234.5m, "JPY");
        MoneyRecord dinar = finance.MoneyFromAmountAndCurrencyCode(1.2345m, "KWD");
        MoneyRecord unit = finance.MoneyFromAmountAndCurrencyCode(1.23456m, "CLF");
        MoneyRecord gold = finance.MoneyFromAmountAndCurrencyCode(1.2345m, "XAU");
        MoneyRecord explicitGold = finance.MoneyFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(
            1.2345m,
            "XAU",
            4,
            MoneyRoundingMode.ToEven);

        Assert.Multiple(() =>
        {
            Assert.That(yen.Amount, Is.EqualTo(1234m));
            Assert.That(yen.DecimalPlaces, Is.EqualTo(0));
            Assert.That(yen.HasMinorUnits, Is.True);
            Assert.That(dinar.Amount, Is.EqualTo(1.234m));
            Assert.That(dinar.DecimalPlaces, Is.EqualTo(3));
            Assert.That(unit.Amount, Is.EqualTo(1.2346m));
            Assert.That(unit.DecimalPlaces, Is.EqualTo(4));
            Assert.That(gold.Amount, Is.EqualTo(1.23m));
            Assert.That(gold.DecimalPlaces, Is.EqualTo(2));
            Assert.That(gold.HasMinorUnits, Is.False);
            Assert.That(explicitGold.Amount, Is.EqualTo(1.2345m));
            Assert.That(explicitGold.DecimalPlaces, Is.EqualTo(4));
            Assert.That(explicitGold.HasMinorUnits, Is.False);
        });
    }

    [Test]
    public void MoneyKeepsNumericValuesSeparateFromLocaleDisplays()
    {
        FinanceProvider english = Fake.Create(FakeOptions.Create("en-US", 9814)).Finance();
        FinanceProvider german = Fake.Create(FakeOptions.Create("de-DE", 9814)).Finance();
        MoneyRecord dollars = english.MoneyFromAmountAndCurrencyCode(1234.565m, "USD");
        MoneyRecord euros = german.MoneyFromAmountAndCurrencyCode(1234.565m, "EUR");
        MoneyRecord foreign = english.MoneyFromAmountAndCurrencyCode(1234.565m, "EUR");

        Assert.Multiple(() =>
        {
            Assert.That(dollars.Amount, Is.EqualTo(1234.56m));
            Assert.That(dollars.Display, Is.EqualTo("$1,234.56"));
            Assert.That(dollars.CurrencySymbol, Is.EqualTo("$"));
            Assert.That(dollars.Locale, Is.EqualTo("en-US"));
            Assert.That(euros.Amount, Is.EqualTo(1234.56m));
            Assert.That(euros.Display, Does.Contain("1.234,56"));
            Assert.That(euros.Display, Does.Contain("€"));
            Assert.That(euros.CurrencySymbol, Is.EqualTo("€"));
            Assert.That(euros.Locale, Is.EqualTo("de-DE"));
            Assert.That(foreign.Amount, Is.EqualTo(euros.Amount));
            Assert.That(foreign.Display, Is.EqualTo("EUR1,234.56"));
            Assert.That(foreign.CurrencySymbol, Is.EqualTo("EUR"));
        });
    }

    [Test]
    public void CurrencyDisplayAndSymbolApisUseTheRequestedLocale()
    {
        FinanceProvider english = Fake.Create(FakeOptions.Create("en-US", 9815)).Finance();
        FinanceProvider german = Fake.Create(FakeOptions.Create("de-DE", 9815)).Finance();
        FinanceProvider first = Fake.Create(9816).Finance();
        FinanceProvider second = Fake.Create(9816).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(english.CurrencyDisplay(12.345m, "USD"), Is.EqualTo("$12.34"));
            Assert.That(english.CurrencySymbolFromCurrencyCode("USD"), Is.EqualTo("$"));
            Assert.That(english.CurrencySymbolFromCurrencyCode("EUR"), Is.EqualTo("EUR"));
            Assert.That(german.CurrencyDisplayFromAmountAndCurrencyCodeAndRoundingMode(
                12.345m,
                "EUR",
                MoneyRoundingMode.AwayFromZero), Does.Contain("12,35"));
            Assert.That(german.CurrencySymbolFromCurrencyCode("EUR"), Is.EqualTo("€"));
            Assert.That(first.CurrencySymbol(), Is.EqualTo(second.CurrencySymbol()));
        });
    }

    [Test]
    public void RandomMoneyIsDeterministicAndSuppliedMoneyConsumesNoEntropy()
    {
        const long seed = 9817;
        FinanceProvider first = Fake.Create(seed).Finance();
        FinanceProvider second = Fake.Create(seed).Finance();

        first.MoneyFromAmountAndCurrencyCode(99.995m, "USD");
        MoneyRecord actual = first.Money();
        MoneyRecord expected = second.Money();

        Assert.Multiple(() =>
        {
            AssertEquivalent(actual, expected);
            Assert.That(actual.Amount, Is.GreaterThanOrEqualTo(decimal.Zero));
            Assert.That(actual.Amount, Is.LessThanOrEqualTo(1000m));
            Assert.That(actual.Display, Is.Not.Empty);
        });
    }

    [Test]
    public void MoneyApisValidatePrecisionRangesRoundingAndOutputLimits()
    {
        const long seed = 9818;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        FakeOptions outputOptions = FakeOptions.Create("en-US", seed);
        outputOptions.MaximumOutputSize = 4;

        Assert.That(
            () => inspected.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(0m, 1m, 2, (MoneyRoundingMode)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("roundingMode"));
        Assert.That(inspected.Amount(), Is.EqualTo(untouched.Amount()));
        Assert.That(
            () => inspected.AmountFromMinimumAndMaximum(2m, 1m),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("maximum"));
        Assert.That(
            () => inspected.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(0.001m, 0.009m, 2, MoneyRoundingMode.ToEven),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => inspected.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(0m, 1m, 29, MoneyRoundingMode.ToEven),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("decimalPlaces"));
        Assert.That(
            () => inspected.PriceFromMinimumAndMaximum(-1m, 1m),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("minimum"));
        Assert.That(
            () => inspected.MoneyFromAmountAndCurrencyCode(1m, "ZZZ"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().CurrencyDisplay(1m, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().MoneyFromAmountAndCurrencyCode(1m, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static void AssertEquivalent(MoneyRecord actual, MoneyRecord expected)
    {
        Assert.That(actual.Amount, Is.EqualTo(expected.Amount));
        Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode));
        Assert.That(actual.CurrencyNumericCode, Is.EqualTo(expected.CurrencyNumericCode));
        Assert.That(actual.CurrencyName, Is.EqualTo(expected.CurrencyName));
        Assert.That(actual.CurrencySymbol, Is.EqualTo(expected.CurrencySymbol));
        Assert.That(actual.DecimalPlaces, Is.EqualTo(expected.DecimalPlaces));
        Assert.That(actual.HasMinorUnits, Is.EqualTo(expected.HasMinorUnits));
        Assert.That(actual.RoundingMode, Is.EqualTo(expected.RoundingMode));
        Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
        Assert.That(actual.Display, Is.EqualTo(expected.Display));
    }
}
