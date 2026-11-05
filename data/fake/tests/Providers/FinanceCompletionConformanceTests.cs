using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceCompletionConformanceTests
{
    [Test]
    public void DefaultCompletionValuesAndRecordsAreDeterministicAndBounded()
    {
        FinanceProvider first = Fake.Create(10009).Finance();
        FinanceProvider second = Fake.Create(10009).Finance();
        ExchangeRateRecord firstRate = first.ExchangeRateRecord();
        ExchangeRateRecord secondRate = second.ExchangeRateRecord();
        int firstQuantity = first.Quantity();
        int secondQuantity = second.Quantity();
        PurchaseOrderRecord firstOrder = first.PurchaseOrder();
        PurchaseOrderRecord secondOrder = second.PurchaseOrder();
        AccountLedgerRecord firstLedger = first.AccountLedger();
        AccountLedgerRecord secondLedger = second.AccountLedger();

        Assert.Multiple(() =>
        {
            Assert.That(firstRate.BaseCurrencyCode, Is.EqualTo(secondRate.BaseCurrencyCode));
            Assert.That(firstRate.QuoteCurrencyCode, Is.EqualTo(secondRate.QuoteCurrencyCode));
            Assert.That(firstRate.Rate, Is.EqualTo(secondRate.Rate));
            Assert.That(firstRate.BaseCurrencyCode, Is.Not.EqualTo(firstRate.QuoteCurrencyCode));
            Assert.That(firstRate.Rate, Is.GreaterThan(decimal.Zero));
            AssertQuantum(firstRate.Rate, 1000000m);
            Assert.That(firstQuantity, Is.EqualTo(secondQuantity));
            Assert.That(firstQuantity, Is.InRange(1, 100));
            Assert.That(firstOrder.Number, Is.EqualTo(secondOrder.Number));
            Assert.That(firstOrder.Quantity, Is.InRange(1, 25));
            Assert.That(firstOrder.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(firstLedger.Account.Number, Is.EqualTo(secondLedger.Account.Number));
            Assert.That(firstLedger.ClosingBalance, Is.EqualTo(secondLedger.ClosingBalance));
            Assert.That(firstLedger.Entries, Has.Length.EqualTo(10));
            Assert.That(firstLedger.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(firstLedger.OpeningBalance, Is.InRange(100m, 10000m));
        });
    }

    [Test]
    public void ExchangeRatesAndAmountsHonorRequestedPrecisionAndSeededOrder()
    {
        FinanceProvider first = Fake.Create(10010).Finance();
        FinanceProvider second = Fake.Create(10010).Finance();
        int[] places = new int[] { 0, 1, 2, 6 };

        int index = 0;
        while (index < places.Length)
        {
            int precision = places[index];
            decimal firstRate = first.ExchangeRateFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                1m,
                9m,
                precision,
                MoneyRoundingMode.ToEven);
            decimal secondRate = second.ExchangeRateFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                1m,
                9m,
                precision,
                MoneyRoundingMode.ToEven);
            decimal firstAmount = first.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                -9m,
                9m,
                precision,
                MoneyRoundingMode.ToEven);
            decimal secondAmount = second.AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                -9m,
                9m,
                precision,
                MoneyRoundingMode.ToEven);
            decimal multiplier = PowerOfTen(precision);

            Assert.Multiple(() =>
            {
                Assert.That(firstRate, Is.EqualTo(secondRate));
                Assert.That(firstAmount, Is.EqualTo(secondAmount));
                Assert.That(firstRate, Is.InRange(1m, 9m));
                Assert.That(firstAmount, Is.InRange(-9m, 9m));
                Assert.That(firstRate * multiplier,
                    Is.EqualTo(decimal.Truncate(firstRate * multiplier)));
                Assert.That(firstAmount * multiplier,
                    Is.EqualTo(decimal.Truncate(firstAmount * multiplier)));
            });
            index++;
        }
    }

    [Test]
    public void SuppliedExchangeRatesApplyEveryRoundingMode()
    {
        FinanceProvider finance = Fake.Create(10011).Finance();
        MoneyRoundingMode[] modes = new MoneyRoundingMode[]
        {
            MoneyRoundingMode.ToEven,
            MoneyRoundingMode.AwayFromZero,
            MoneyRoundingMode.ToZero,
            MoneyRoundingMode.ToNegativeInfinity,
            MoneyRoundingMode.ToPositiveInfinity
        };
        decimal[] expected = new decimal[]
        {
            2.34m,
            2.35m,
            2.34m,
            2.34m,
            2.35m
        };

        int index = 0;
        while (index < modes.Length)
        {
            ExchangeRateRecord rate = finance.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
                "USD",
                "EUR",
                2.345m,
                2,
                modes[index]);

            Assert.Multiple(() =>
            {
                Assert.That(rate.Rate, Is.EqualTo(expected[index]));
                Assert.That(rate.DecimalPlaces, Is.EqualTo(2));
                Assert.That(rate.RoundingMode, Is.EqualTo(modes[index]));
                Assert.That(rate.BaseCurrencyCode, Is.EqualTo("USD"));
                Assert.That(rate.QuoteCurrencyCode, Is.EqualTo("EUR"));
                Assert.That(rate.RepresentsMarketQuote, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void LedgerIntervalsPreserveOrderedInclusiveBoundsAndBoundaryDurations()
    {
        FinanceProvider finance = Fake.Create(10012).Finance();
        DateTimeOffset start = new DateTimeOffset(2036, 3, 1, 1, 30, 0, TimeSpan.FromHours(5));
        DateTimeOffset end = start.AddDays(2).AddMinutes(15);
        LedgerIntervalRecord interval = finance.LedgerIntervalFromStartAndEnd(start, end);
        LedgerIntervalRecord empty = finance.LedgerIntervalFromStartAndEnd(start, start);
        FakeOptions boundaryOptions = FakeOptions.Create(10012);
        boundaryOptions.ReferenceTime = DateTimeOffset.MinValue.AddDays(30);
        LedgerIntervalRecord boundary = Fake.Create(boundaryOptions).Finance().LedgerInterval();

        Assert.Multiple(() =>
        {
            Assert.That(interval.Start, Is.EqualTo(start));
            Assert.That(interval.End, Is.EqualTo(end));
            Assert.That(interval.Duration, Is.EqualTo(end - start));
            Assert.That(interval.EndInclusive, Is.True);
            Assert.That(empty.Start, Is.EqualTo(empty.End));
            Assert.That(empty.Duration, Is.EqualTo(TimeSpan.Zero));
            Assert.That(boundary.Start, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(boundary.End, Is.EqualTo(boundaryOptions.ReferenceTime));
            Assert.That(boundary.Duration, Is.EqualTo(TimeSpan.FromDays(30)));
        });
    }

    [Test]
    public void AccountLedgerEntriesReconcileDebitsCreditsAndClosingBalanceAcrossCurrencies()
    {
        string[] currencyCodes = new string[] { "USD", "JPY", "KWD", "CLF", "XAU" };
        decimal[] multipliers = new decimal[] { 100m, 1m, 1000m, 10000m, 100m };

        int currencyIndex = 0;
        while (currencyIndex < currencyCodes.Length)
        {
            string currencyCode = currencyCodes[currencyIndex];
            decimal multiplier = multipliers[currencyIndex];
            FinanceProvider finance = Fake.Create(10020 + currencyIndex).Finance();
            AccountLedgerRecord ledger = finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
                123.45678m,
                32,
                currencyCode);
            decimal balance = ledger.OpeningBalance;
            DateTimeOffset priorTime = ledger.Interval.Start;

            int entryIndex = 0;
            while (entryIndex < ledger.Entries.Length)
            {
                LedgerEntryRecord entry = ledger.Entries[entryIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(entry.Debit, Is.GreaterThanOrEqualTo(decimal.Zero));
                    Assert.That(entry.Credit, Is.GreaterThanOrEqualTo(decimal.Zero));
                    Assert.That((entry.Debit == decimal.Zero) != (entry.Credit == decimal.Zero), Is.True);
                    Assert.That(entry.BalanceBefore, Is.EqualTo(balance));
                    Assert.That(entry.BalanceAfter,
                        Is.EqualTo(entry.BalanceBefore - entry.Debit + entry.Credit));
                    Assert.That(entry.AccountNumber, Is.EqualTo(ledger.Account.Number));
                    Assert.That(entry.CurrencyCode, Is.EqualTo(currencyCode));
                    Assert.That(entry.OccurredOn, Is.GreaterThan(priorTime));
                    Assert.That(entry.OccurredOn, Is.LessThan(ledger.Interval.End));
                    AssertQuantum(entry.Debit, multiplier);
                    AssertQuantum(entry.Credit, multiplier);
                    AssertQuantum(entry.BalanceAfter, multiplier);
                });
                balance = entry.BalanceAfter;
                priorTime = entry.OccurredOn;
                entryIndex++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(ledger.CurrencyCode, Is.EqualTo(currencyCode));
                Assert.That(ledger.Entries, Has.Length.EqualTo(32));
                Assert.That(ledger.ClosingBalance, Is.EqualTo(balance));
                AssertQuantum(ledger.OpeningBalance, multiplier);
                AssertQuantum(ledger.ClosingBalance, multiplier);
            });
            currencyIndex++;
        }
    }

    [Test]
    public void EmptyAccountLedgerRoundsOpeningBalanceWithoutInventingEntries()
    {
        FinanceProvider finance = Fake.Create(10030).Finance();
        AccountLedgerRecord yen = finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
            124.5m,
            0,
            "JPY");
        AccountLedgerRecord gold = Fake.Create(10031).Finance()
            .AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(12.345m, 0, "XAU");

        Assert.Multiple(() =>
        {
            Assert.That(yen.Entries, Is.Empty);
            Assert.That(yen.OpeningBalance, Is.EqualTo(124m));
            Assert.That(yen.ClosingBalance, Is.EqualTo(yen.OpeningBalance));
            Assert.That(gold.Entries, Is.Empty);
            Assert.That(gold.OpeningBalance, Is.EqualTo(12.34m));
            Assert.That(gold.ClosingBalance, Is.EqualTo(gold.OpeningBalance));
        });
    }

    [Test]
    public void QuantityGenerationHonorsInclusiveAndConfiguredLimitsBeforeEntropy()
    {
        const long seed = 10032;
        FakeOptions options = FakeOptions.Create(seed);
        options.MaximumCount = 7;
        FinanceProvider inspected = Fake.Create(options).Finance();
        FinanceProvider untouched = Fake.Create(options).Finance();

        Assert.That(inspected.QuantityFromMinimumAndMaximum(7, 7), Is.EqualTo(7));
        Assert.That(() => inspected.QuantityFromMinimumAndMaximum(0, 7),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("minimum"));
        Assert.That(() => inspected.QuantityFromMinimumAndMaximum(5, 4),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("maximum"));
        Assert.That(() => inspected.QuantityFromMinimumAndMaximum(1, 8),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("maximum"));
        Assert.That(() => inspected.Quantity(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(inspected.QuantityFromMinimumAndMaximum(1, 7),
            Is.EqualTo(untouched.QuantityFromMinimumAndMaximum(1, 7)));
    }

    [Test]
    public void PurchaseOrdersRecomputeAtZeroThreeFourAndFallbackMinorUnits()
    {
        string[] currencyCodes = new string[] { "JPY", "KWD", "CLF", "XAU" };
        decimal[] multipliers = new decimal[] { 1m, 1000m, 10000m, 100m };

        int index = 0;
        while (index < currencyCodes.Length)
        {
            string currencyCode = currencyCodes[index];
            decimal multiplier = multipliers[index];
            FinanceProvider finance = Fake.Create(10040 + index).Finance();
            PurchaseOrderRecord order = finance.PurchaseOrderFromQuantityAndCurrencyCode(
                13,
                currencyCode);
            decimal recomputedTotal = order.Subtotal + order.TaxAmount +
                order.FeeAmount - order.DiscountAmount;

            Assert.Multiple(() =>
            {
                Assert.That(order.Quantity, Is.EqualTo(13));
                Assert.That(order.CurrencyCode, Is.EqualTo(currencyCode));
                Assert.That(order.Subtotal, Is.EqualTo(order.UnitPrice * order.Quantity));
                Assert.That(order.TaxAmount,
                    Is.EqualTo(finance.TaxAmount(order.Subtotal, order.TaxRate, currencyCode)));
                Assert.That(order.DiscountAmount,
                    Is.EqualTo(finance.DiscountAmount(
                        order.Subtotal,
                        order.DiscountRate,
                        currencyCode)));
                Assert.That(order.Total,
                    Is.EqualTo(finance.MoneyFromAmountAndCurrencyCode(
                        recomputedTotal,
                        currencyCode).Amount));
                Assert.That(order.UnitPriceDisplay,
                    Is.EqualTo(finance.CurrencyDisplay(order.UnitPrice, currencyCode)));
                Assert.That(order.SubtotalDisplay,
                    Is.EqualTo(finance.CurrencyDisplay(order.Subtotal, currencyCode)));
                Assert.That(order.TotalDisplay,
                    Is.EqualTo(finance.CurrencyDisplay(order.Total, currencyCode)));
                AssertQuantum(order.UnitPrice, multiplier);
                AssertQuantum(order.Subtotal, multiplier);
                AssertQuantum(order.TaxAmount, multiplier);
                AssertQuantum(order.DiscountAmount, multiplier);
                AssertQuantum(order.FeeAmount, multiplier);
                AssertQuantum(order.Total, multiplier);
            });
            index++;
        }
    }

    [Test]
    public void PurchaseOrderAmountsStayNumericWhileDisplaysFollowLocale()
    {
        FakeOptions englishOptions = FakeOptions.Create("en-US", 10050);
        FakeOptions germanOptions = FakeOptions.Create("de-DE", 10050);
        PurchaseOrderRecord english = Fake.Create(englishOptions).Finance()
            .PurchaseOrderFromQuantityAndCurrencyCode(3, "EUR");
        PurchaseOrderRecord german = Fake.Create(germanOptions).Finance()
            .PurchaseOrderFromQuantityAndCurrencyCode(3, "EUR");

        Assert.Multiple(() =>
        {
            Assert.That(english.UnitPrice, Is.EqualTo(german.UnitPrice));
            Assert.That(english.Subtotal, Is.EqualTo(german.Subtotal));
            Assert.That(english.Total, Is.EqualTo(german.Total));
            Assert.That(english.UnitPriceDisplay, Is.Not.EqualTo(german.UnitPriceDisplay));
            Assert.That(english.TotalDisplay, Is.Not.EqualTo(german.TotalDisplay));
            Assert.That(english.UnitPriceDisplay, Does.StartWith("EUR"));
            Assert.That(german.UnitPriceDisplay, Does.Contain("€"));
        });
    }

    [Test]
    public void DistinctCurrencyPairsAreAcceptedAndInvalidRateInputsAreRejected()
    {
        const long seed = 10051;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        ExchangeRateRecord pair = inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
            "usd",
            "jpy",
            150.125m,
            3,
            MoneyRoundingMode.ToEven);

        Assert.Multiple(() =>
        {
            Assert.That(pair.BaseCurrencyCode, Is.EqualTo("USD"));
            Assert.That(pair.QuoteCurrencyCode, Is.EqualTo("JPY"));
            Assert.That(pair.Rate, Is.EqualTo(150.125m));
            Assert.That(pair.IsSynthetic, Is.True);
            Assert.That(pair.RepresentsMarketQuote, Is.False);
        });
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodes("USD", "usd"),
            Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("quoteCurrencyCode"));
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodes("ZZZ", "EUR"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodes("USD", "ZZZ"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => inspected.ExchangeRateFromMinimumAndMaximum(0m, 1m),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("minimum"));
        Assert.That(
            () => inspected.ExchangeRateFromMinimumAndMaximum(2m, 1m),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("maximum"));
        Assert.That(
            () => inspected.ExchangeRateFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                0.1m,
                0.9m,
                0,
                MoneyRoundingMode.ToEven),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
                "USD",
                "EUR",
                0.4m,
                0,
                MoneyRoundingMode.ToZero),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("rate"));
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
                "USD",
                "EUR",
                1m,
                29,
                MoneyRoundingMode.ToEven),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("decimalPlaces"));
        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
                "USD",
                "EUR",
                1m,
                2,
                (MoneyRoundingMode)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("roundingMode"));
        Assert.That(inspected.ExchangeRate(), Is.EqualTo(untouched.ExchangeRate()));
    }

    [Test]
    public void PurchaseOrderAndLedgerInputsRejectInvalidLimitsCurrenciesAndDates()
    {
        const long seed = 10052;
        FinanceProvider finance = Fake.Create(seed).Finance();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 12;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 99;
        FakeOptions earlyOptions = FakeOptions.Create(seed);
        earlyOptions.ReferenceTime = DateTimeOffset.MinValue.AddDays(29);
        FakeOptions lateOptions = FakeOptions.Create(seed);
        lateOptions.ReferenceTime = DateTimeOffset.MaxValue.AddDays(-13);

        Assert.That(() => finance.PurchaseOrderFromQuantityAndCurrencyCode(0, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("quantity"));
        Assert.That(() => finance.PurchaseOrderFromQuantityAndCurrencyCode(1, "ZZZ"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
                0m,
                -1,
                "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("entryCount"));
        Assert.That(() => finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
                1000000000001m,
                0,
                "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("openingBalance"));
        Assert.That(() => finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
                0m,
                0,
                "ZZZ"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(() => finance.LedgerIntervalFromStartAndEnd(
                new DateTimeOffset(2040, 1, 2, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("end"));
        Assert.That(() => Fake.Create(countOptions).Finance()
                .PurchaseOrderFromQuantityAndCurrencyCode(13, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(countOptions).Finance()
                .AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(0m, 13, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance()
                .AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(0m, 1, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(earlyOptions).Finance().LedgerInterval(),
            Throws.TypeOf<FakeException>());
        Assert.That(() => Fake.Create(earlyOptions).Finance()
                .AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(0m, 0, "USD"),
            Throws.TypeOf<FakeException>());
        Assert.That(() => Fake.Create(lateOptions).Finance().PurchaseOrder(),
            Throws.TypeOf<FakeException>());
    }

    static decimal PowerOfTen(int places)
    {
        decimal result = decimal.One;
        int index = 0;
        while (index < places)
        {
            result = result * 10m;
            index++;
        }
        return result;
    }

    static void AssertQuantum(decimal value, decimal multiplier)
    {
        Assert.That(value * multiplier,
            Is.EqualTo(decimal.Truncate(value * multiplier)));
    }
}
