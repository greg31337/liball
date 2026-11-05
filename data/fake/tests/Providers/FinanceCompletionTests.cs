using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceCompletionTests
{
    [Test]
    public void ExchangeRatesArePositivePreciseDeterministicAndPairShaped()
    {
        const long seed = 9990;
        FinanceProvider first = Fake.Create(seed).Finance();
        FinanceProvider second = Fake.Create(seed).Finance();

        decimal rate = first.ExchangeRateFromMinimumAndMaximum(0.5m, 2m);
        decimal repeated = second.ExchangeRateFromMinimumAndMaximum(0.5m, 2m);
        ExchangeRateRecord pair = first.ExchangeRateRecordFromBaseAndQuoteCurrencyCodes(
            "usd",
            "eur");
        ExchangeRateRecord supplied = first.ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(
            "USD",
            "JPY",
            1.2345m,
            3,
            MoneyRoundingMode.ToEven);

        Assert.Multiple(() =>
        {
            Assert.That(rate, Is.EqualTo(repeated));
            Assert.That(rate, Is.InRange(0.5m, 2m));
            Assert.That(rate * 1000000m, Is.EqualTo(decimal.Truncate(rate * 1000000m)));
            Assert.That(pair.BaseCurrencyCode, Is.EqualTo("USD"));
            Assert.That(pair.QuoteCurrencyCode, Is.EqualTo("EUR"));
            Assert.That(pair.Rate, Is.GreaterThan(decimal.Zero));
            Assert.That(pair.RepresentsMarketQuote, Is.False);
            Assert.That(supplied.Rate, Is.EqualTo(1.234m));
            Assert.That(supplied.DecimalPlaces, Is.EqualTo(3));
            Assert.That(supplied.IsSynthetic, Is.True);
            Assert.That(supplied.TestMarker, Is.EqualTo("fake.synthetic-finance.v1"));
        });
    }

    [Test]
    public void QuantitiesAndLedgerIntervalsHonorInclusiveInputs()
    {
        FakeOptions options = FakeOptions.Create(9991);
        options.ReferenceTime = new DateTimeOffset(2032, 5, 10, 8, 0, 0, TimeSpan.Zero);
        FinanceProvider finance = Fake.Create(options).Finance();
        int quantity = finance.QuantityFromMinimumAndMaximum(7, 7);
        LedgerIntervalRecord generated = finance.LedgerInterval();
        DateTimeOffset start = options.ReferenceTime.AddHours(-5);
        LedgerIntervalRecord supplied = finance.LedgerIntervalFromStartAndEnd(
            start,
            options.ReferenceTime);

        Assert.Multiple(() =>
        {
            Assert.That(quantity, Is.EqualTo(7));
            Assert.That(generated.Start, Is.EqualTo(options.ReferenceTime.AddDays(-30)));
            Assert.That(generated.End, Is.EqualTo(options.ReferenceTime));
            Assert.That(generated.Duration, Is.EqualTo(TimeSpan.FromDays(30)));
            Assert.That(generated.EndInclusive, Is.True);
            Assert.That(supplied.Start, Is.EqualTo(start));
            Assert.That(supplied.End, Is.EqualTo(options.ReferenceTime));
            Assert.That(supplied.Duration, Is.EqualTo(TimeSpan.FromHours(5)));
        });
    }

    [Test]
    public void PurchaseOrdersCorrelateQuantityAmountsDatesAndSeparateDisplays()
    {
        FakeOptions options = FakeOptions.Create("en-US", 9992);
        options.ReferenceTime = new DateTimeOffset(2033, 1, 15, 12, 30, 0, TimeSpan.Zero);
        FinanceProvider finance = Fake.Create(options).Finance();
        PurchaseOrderRecord order = finance.PurchaseOrderFromQuantityAndCurrencyCode(4, "USD");
        PurchaseOrderRecord repeated = Fake.Create(options).Finance()
            .PurchaseOrderFromQuantityAndCurrencyCode(4, "USD");

        Assert.Multiple(() =>
        {
            Assert.That(order.Number, Does.Match("^PO-[0-9]{8}$"));
            Assert.That(order.Number, Is.EqualTo(repeated.Number));
            Assert.That(order.Total, Is.EqualTo(repeated.Total));
            Assert.That(order.Quantity, Is.EqualTo(4));
            Assert.That(order.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(order.Subtotal, Is.EqualTo(order.UnitPrice * order.Quantity));
            Assert.That(order.Total,
                Is.EqualTo(order.Subtotal + order.TaxAmount + order.FeeAmount - order.DiscountAmount));
            Assert.That(order.UnitPriceDisplay, Is.EqualTo(finance.CurrencyDisplay(order.UnitPrice, "USD")));
            Assert.That(order.SubtotalDisplay, Is.EqualTo(finance.CurrencyDisplay(order.Subtotal, "USD")));
            Assert.That(order.TotalDisplay, Is.EqualTo(finance.CurrencyDisplay(order.Total, "USD")));
            Assert.That(order.CreatedOn, Is.EqualTo(options.ReferenceTime));
            Assert.That(order.ExpectedOn, Is.EqualTo(options.ReferenceTime.AddDays(14)));
            Assert.That(order.IsSynthetic, Is.True);
            Assert.That(order.RepresentsApprovedPurchase, Is.False);
        });
    }

    [Test]
    public void AccountLedgersCorrelateAccountsTimesCurrenciesAndBalanceArithmetic()
    {
        FakeOptions options = FakeOptions.Create("en-US", 9993);
        options.ReferenceTime = new DateTimeOffset(2034, 7, 20, 9, 0, 0, TimeSpan.Zero);
        FinanceProvider finance = Fake.Create(options).Finance();
        AccountLedgerRecord ledger = finance.AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(
            500m,
            6,
            "USD");
        AccountLedgerRecord repeated = Fake.Create(options).Finance()
            .AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(500m, 6, "USD");

        decimal balance = ledger.OpeningBalance;
        DateTimeOffset previous = ledger.Interval.Start;
        int index = 0;
        while (index < ledger.Entries.Length)
        {
            LedgerEntryRecord entry = ledger.Entries[index];
            Assert.Multiple(() =>
            {
                Assert.That(entry.AccountNumber, Is.EqualTo(ledger.Account.Number));
                Assert.That(entry.CurrencyCode, Is.EqualTo(ledger.CurrencyCode));
                Assert.That(entry.OccurredOn, Is.GreaterThan(previous));
                Assert.That(entry.OccurredOn, Is.LessThan(ledger.Interval.End));
                Assert.That(entry.BalanceBefore, Is.EqualTo(balance));
                Assert.That(entry.BalanceAfter,
                    Is.EqualTo(entry.BalanceBefore - entry.Debit + entry.Credit));
                Assert.That((entry.Debit == decimal.Zero) != (entry.Credit == decimal.Zero), Is.True);
                Assert.That(entry.BalanceAfterDisplay,
                    Is.EqualTo(finance.CurrencyDisplay(entry.BalanceAfter, ledger.CurrencyCode)));
                Assert.That(entry.IsSynthetic, Is.True);
                Assert.That(entry.RepresentsPostedTransaction, Is.False);
            });
            balance = entry.BalanceAfter;
            previous = entry.OccurredOn;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(ledger.Entries, Has.Length.EqualTo(6));
            Assert.That(ledger.Account.Number, Is.EqualTo(repeated.Account.Number));
            Assert.That(ledger.Entries[0].Reference, Is.EqualTo(repeated.Entries[0].Reference));
            Assert.That(ledger.ClosingBalance, Is.EqualTo(repeated.ClosingBalance));
            Assert.That(ledger.ClosingBalance, Is.EqualTo(balance));
            Assert.That(ledger.OpeningBalanceDisplay, Is.EqualTo("$500.00"));
            Assert.That(ledger.ClosingBalanceDisplay,
                Is.EqualTo(finance.CurrencyDisplay(ledger.ClosingBalance, "USD")));
            Assert.That(ledger.IsSynthetic, Is.True);
            Assert.That(ledger.RepresentsPostedLedger, Is.False);
        });
    }

    [Test]
    public void NewFinanceInputsRejectContradictionsBeforeConsumingEntropy()
    {
        const long seed = 9994;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        DateTimeOffset start = new DateTimeOffset(2035, 1, 2, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddDays(-1);

        Assert.That(
            () => inspected.ExchangeRateRecordFromBaseAndQuoteCurrencyCodes("USD", "usd"),
            Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("quoteCurrencyCode"));
        Assert.That(() => inspected.QuantityFromMinimumAndMaximum(0, 10),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => inspected.LedgerIntervalFromStartAndEnd(start, end),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => inspected.PurchaseOrderFromQuantityAndCurrencyCode(0, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(inspected.ExchangeRate(), Is.EqualTo(untouched.ExchangeRate()));
    }
}
