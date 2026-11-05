using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceTransactionTests
{
    [Test]
    public void TransactionTypesDescriptionsAndReferencesAreDeterministicShapes()
    {
        const long seed = 9972;
        FinanceProvider first = Fake.Create(seed).Finance();
        FinanceProvider second = Fake.Create(seed).Finance();
        FinancialTransactionType type = Fake.Create(seed).Finance().TransactionType();
        string description = Fake.Create(seed).Finance().TransactionDescription();
        string reference = first.TransactionReference();

        Assert.Multiple(() =>
        {
            Assert.That(type, Is.EqualTo(Fake.Create(seed).Finance().TransactionType()));
            Assert.That(description,
                Is.EqualTo(Fake.Create(seed).Finance().TransactionDescriptionFromType(type)));
            Assert.That(reference, Is.EqualTo(second.TransactionReference()));
            Assert.That(reference, Does.Match("^TXN-[0-9]{12}$"));
            Assert.That(first.CheckNumber(), Does.Match("^CHK-[0-9]{8}$"));
            Assert.That(first.InvoiceNumber(), Does.Match("^INV-[0-9]{8}$"));
        });

        foreach (FinancialTransactionType value in Enum.GetValues<FinancialTransactionType>())
        {
            Assert.That(Fake.Create(seed).Finance().TransactionDescriptionFromType(value),
                Does.StartWith("Synthetic "));
        }
    }

    [Test]
    public void RateFeeAndBalanceFieldsUseBoundedDecimalValues()
    {
        FinanceProvider finance = Fake.Create(9973).Finance();
        decimal taxRate = Fake.Create(9973).Finance().TaxRate();
        decimal discountRate = Fake.Create(9973).Finance().DiscountRate();
        decimal interestRate = Fake.Create(9973).Finance().InterestRate();
        decimal fee = Fake.Create(9973).Finance().Fee();
        decimal yenFee = Fake.Create(9973).Finance().FeeFromCurrencyCode("JPY");
        decimal dinarFee = Fake.Create(9973).Finance().FeeFromCurrencyCode("KWD");
        decimal balance = Fake.Create(9973).Finance().Balance();
        decimal yenBalance = Fake.Create(9973).Finance().BalanceFromCurrencyCode("JPY");

        Assert.Multiple(() =>
        {
            Assert.That(taxRate, Is.InRange(0m, 0.25m));
            Assert.That(discountRate, Is.InRange(0m, 0.30m));
            Assert.That(interestRate, Is.InRange(0.001m, 0.20m));
            Assert.That(taxRate * 10000m, Is.EqualTo(decimal.Truncate(taxRate * 10000m)));
            Assert.That(fee, Is.InRange(0m, 25m));
            Assert.That(yenFee, Is.EqualTo(decimal.Truncate(yenFee)));
            Assert.That(dinarFee * 1000m, Is.EqualTo(decimal.Truncate(dinarFee * 1000m)));
            Assert.That(balance, Is.InRange(-10000m, 10000m));
            Assert.That(yenBalance, Is.EqualTo(decimal.Truncate(yenBalance)));
            Assert.That(finance.Fee(), Is.EqualTo(Fake.Create(9973).Finance().Fee()));
        });
    }

    [Test]
    public void TaxDiscountAndInterestCalculationsUseCurrencyMinorUnits()
    {
        FinanceProvider finance = Fake.Create(9974).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.TaxAmount(10.05m, 0.075m, "USD"), Is.EqualTo(0.75m));
            Assert.That(finance.TaxAmount(100m, 0.075m, "JPY"), Is.EqualTo(8m));
            Assert.That(finance.DiscountAmount(12.3456m, 0.10m, "KWD"), Is.EqualTo(1.235m));
            Assert.That(finance.InterestAmount(1000m, 0.12m, 30, "USD"), Is.EqualTo(9.86m));
            Assert.That(finance.InterestAmount(1000m, 0m, 365, "USD"), Is.Zero);
        });
    }

    [Test]
    public void CheckRecordsAreChecksumValidNonNegotiableFixtures()
    {
        FakeOptions options = FakeOptions.Create(9975);
        options.ReferenceTime = new DateTimeOffset(2028, 6, 15, 9, 30, 0, TimeSpan.Zero);
        CheckRecord first = Fake.Create(options).Finance().Check();
        CheckRecord second = Fake.Create(options).Finance().Check();

        Assert.Multiple(() =>
        {
            AssertCheckEquivalent(first, second);
            Assert.That(first.Number, Does.Match("^CHK-[0-9]{8}$"));
            Assert.That(first.RoutingNumber, Does.Match("^[0-9]{9}$"));
            Assert.That(int.Parse(first.RoutingNumber.Substring(0, 2)), Is.InRange(13, 20));
            Assert.That(first.AccountNumber, Does.Match("^[0-9]{12}$"));
            Assert.That(first.Payee, Does.EndWith("PAYEE"));
            Assert.That(first.Amount, Is.GreaterThanOrEqualTo(0.01m));
            Assert.That(first.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(first.WrittenOn, Is.EqualTo(options.ReferenceTime));
            Assert.That(first.IsRoutingChecksumValid, Is.True);
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.TestMarker, Is.EqualTo("fake.synthetic-finance.v1"));
            Assert.That(first.IsNegotiable, Is.False);
        });
    }

    [Test]
    public void InvoiceRecordsPreserveArithmeticDatesAndCurrencyPrecision()
    {
        FakeOptions options = FakeOptions.Create(9976);
        options.ReferenceTime = new DateTimeOffset(2029, 2, 10, 12, 0, 0, TimeSpan.FromHours(2));
        InvoiceRecord dollars = Fake.Create(options).Finance().InvoiceFromCurrencyCode("USD");
        InvoiceRecord dollarsAgain = Fake.Create(options).Finance().InvoiceFromCurrencyCode("USD");
        InvoiceRecord yen = Fake.Create(options).Finance().InvoiceFromCurrencyCode("JPY");
        InvoiceRecord dinar = Fake.Create(options).Finance().InvoiceFromCurrencyCode("KWD");

        Assert.Multiple(() =>
        {
            AssertInvoiceEquivalent(dollars, dollarsAgain);
            Assert.That(dollars.Number, Does.Match("^INV-[0-9]{8}$"));
            Assert.That(dollars.Total,
                Is.EqualTo(dollars.Subtotal + dollars.TaxAmount +
                    dollars.FeeAmount - dollars.DiscountAmount));
            Assert.That(dollars.BalanceDue, Is.EqualTo(dollars.Total));
            Assert.That(dollars.IssuedOn, Is.EqualTo(options.ReferenceTime));
            Assert.That(dollars.DueOn, Is.EqualTo(options.ReferenceTime.AddDays(30)));
            Assert.That(dollars.IsPaid, Is.False);
            Assert.That(dollars.IsSynthetic, Is.True);
            Assert.That(dollars.TestMarker, Is.EqualTo("fake.synthetic-finance.v1"));
            Assert.That(yen.Total, Is.EqualTo(decimal.Truncate(yen.Total)));
            Assert.That(yen.TaxAmount, Is.EqualTo(decimal.Truncate(yen.TaxAmount)));
            Assert.That(dinar.Total * 1000m, Is.EqualTo(decimal.Truncate(dinar.Total * 1000m)));
        });
    }

    [Test]
    public void PaymentRecordsCorrelateInvoiceArithmeticDescriptionsAndBalances()
    {
        FakeOptions options = FakeOptions.Create(9977);
        options.ReferenceTime = new DateTimeOffset(2030, 4, 20, 15, 45, 0, TimeSpan.Zero);
        FinanceProvider finance = Fake.Create(options).Finance();
        PaymentRecord first = finance.PaymentFromCurrencyCode("KWD");
        PaymentRecord second = Fake.Create(options).Finance().PaymentFromCurrencyCode("KWD");

        Assert.Multiple(() =>
        {
            AssertPaymentEquivalent(first, second);
            Assert.That(first.Reference, Does.Match("^TXN-[0-9]{12}$"));
            Assert.That(first.InvoiceNumber, Does.Match("^INV-[0-9]{8}$"));
            Assert.That(first.Description,
                Is.EqualTo(Fake.Create(options).Finance().TransactionDescriptionFromType(first.Type)));
            Assert.That(first.Amount,
                Is.EqualTo(first.Subtotal + first.TaxAmount +
                    first.FeeAmount - first.DiscountAmount));
            Assert.That(first.BalanceAfter, Is.EqualTo(first.BalanceBefore - first.Amount));
            Assert.That(first.BalanceAfter, Is.GreaterThanOrEqualTo(decimal.Zero));
            Assert.That(first.Amount * 1000m, Is.EqualTo(decimal.Truncate(first.Amount * 1000m)));
            Assert.That(first.OccurredOn, Is.EqualTo(options.ReferenceTime));
            Assert.That(first.CheckNumber,
                first.Type == FinancialTransactionType.Check
                    ? Does.Match("^CHK-[0-9]{8}$")
                    : Is.Empty);
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.TestMarker, Is.EqualTo("fake.synthetic-finance.v1"));
            Assert.That(first.IsAuthorized, Is.False);
            Assert.That(first.IsSettled, Is.False);
        });
    }

    [Test]
    public void PaymentCorrelationsRecomputeAcrossCurrencyMinorUnits()
    {
        AssertPaymentRecomputes(Fake.Create(9986).Finance(), "JPY", 1m);
        AssertPaymentRecomputes(Fake.Create(9987).Finance(), "KWD", 1000m);
        AssertPaymentRecomputes(Fake.Create(9988).Finance(), "CLF", 10000m);
    }

    [Test]
    public void PercentageCalculationsUseCurrencySpecificMidpointToEvenRounding()
    {
        FinanceProvider finance = Fake.Create(9989).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.TaxAmount(1m, 0.005m, "USD"), Is.EqualTo(0m));
            Assert.That(finance.TaxAmount(3m, 0.005m, "USD"), Is.EqualTo(0.02m));
            Assert.That(finance.TaxAmount(1m, 0.5m, "JPY"), Is.EqualTo(0m));
            Assert.That(finance.TaxAmount(3m, 0.5m, "JPY"), Is.EqualTo(2m));
            Assert.That(finance.DiscountAmount(1m, 0.0005m, "KWD"), Is.EqualTo(0m));
            Assert.That(finance.DiscountAmount(3m, 0.0005m, "KWD"), Is.EqualTo(0.002m));
            Assert.That(finance.DiscountAmount(1m, 0.00005m, "CLF"), Is.EqualTo(0m));
            Assert.That(finance.DiscountAmount(3m, 0.00005m, "CLF"), Is.EqualTo(0.0002m));
        });
    }

    [Test]
    public void FinancialApisRejectInvalidInputsAndHonorLimitsBeforeEntropy()
    {
        const long seed = 9978;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 11;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 10;
        FakeOptions dateOptions = FakeOptions.Create(seed);
        dateOptions.ReferenceTime = DateTimeOffset.MaxValue;

        Assert.That(
            () => inspected.TransactionDescriptionFromType((FinancialTransactionType)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("type"));
        Assert.That(() => inspected.InvoiceFromCurrencyCode("ZZZ"), Throws.TypeOf<ArgumentException>());
        Assert.That(inspected.Invoice().Number, Is.EqualTo(untouched.Invoice().Number));
        Assert.That(() => inspected.TaxAmount(-1m, 0.1m, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => inspected.DiscountAmount(10m, 1.1m, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => inspected.InterestAmount(10m, 0.1m, -1, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(countOptions).Finance().Check(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance().TransactionReference(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance().Check(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(dateOptions).Finance().Invoice(), Throws.TypeOf<FakeException>());
    }

    static void AssertCheckEquivalent(CheckRecord actual, CheckRecord expected)
    {
        Assert.That(actual.Number, Is.EqualTo(expected.Number));
        Assert.That(actual.RoutingNumber, Is.EqualTo(expected.RoutingNumber));
        Assert.That(actual.AccountNumber, Is.EqualTo(expected.AccountNumber));
        Assert.That(actual.Payee, Is.EqualTo(expected.Payee));
        Assert.That(actual.Memo, Is.EqualTo(expected.Memo));
        Assert.That(actual.Amount, Is.EqualTo(expected.Amount));
        Assert.That(actual.WrittenOn, Is.EqualTo(expected.WrittenOn));
    }

    static void AssertInvoiceEquivalent(InvoiceRecord actual, InvoiceRecord expected)
    {
        Assert.That(actual.Number, Is.EqualTo(expected.Number));
        Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode));
        Assert.That(actual.Subtotal, Is.EqualTo(expected.Subtotal));
        Assert.That(actual.TaxRate, Is.EqualTo(expected.TaxRate));
        Assert.That(actual.TaxAmount, Is.EqualTo(expected.TaxAmount));
        Assert.That(actual.DiscountRate, Is.EqualTo(expected.DiscountRate));
        Assert.That(actual.DiscountAmount, Is.EqualTo(expected.DiscountAmount));
        Assert.That(actual.FeeAmount, Is.EqualTo(expected.FeeAmount));
        Assert.That(actual.Total, Is.EqualTo(expected.Total));
        Assert.That(actual.DueOn, Is.EqualTo(expected.DueOn));
    }

    static void AssertPaymentEquivalent(PaymentRecord actual, PaymentRecord expected)
    {
        Assert.That(actual.Reference, Is.EqualTo(expected.Reference));
        Assert.That(actual.InvoiceNumber, Is.EqualTo(expected.InvoiceNumber));
        Assert.That(actual.Type, Is.EqualTo(expected.Type));
        Assert.That(actual.Description, Is.EqualTo(expected.Description));
        Assert.That(actual.CheckNumber, Is.EqualTo(expected.CheckNumber));
        Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode));
        Assert.That(actual.Subtotal, Is.EqualTo(expected.Subtotal));
        Assert.That(actual.TaxAmount, Is.EqualTo(expected.TaxAmount));
        Assert.That(actual.DiscountAmount, Is.EqualTo(expected.DiscountAmount));
        Assert.That(actual.FeeAmount, Is.EqualTo(expected.FeeAmount));
        Assert.That(actual.Amount, Is.EqualTo(expected.Amount));
        Assert.That(actual.BalanceBefore, Is.EqualTo(expected.BalanceBefore));
        Assert.That(actual.BalanceAfter, Is.EqualTo(expected.BalanceAfter));
    }

    static void AssertPaymentRecomputes(
        FinanceProvider finance,
        string currencyCode,
        decimal quantumMultiplier)
    {
        PaymentRecord payment = finance.PaymentFromCurrencyCode(currencyCode);
        decimal recomputedTotal = payment.Subtotal + payment.TaxAmount +
            payment.FeeAmount - payment.DiscountAmount;

        Assert.Multiple(() =>
        {
            Assert.That(payment.TaxAmount,
                Is.EqualTo(finance.TaxAmount(
                    payment.Subtotal,
                    payment.TaxRate,
                    currencyCode)));
            Assert.That(payment.DiscountAmount,
                Is.EqualTo(finance.DiscountAmount(
                    payment.Subtotal,
                    payment.DiscountRate,
                    currencyCode)));
            Assert.That(payment.Amount,
                Is.EqualTo(finance.MoneyFromAmountAndCurrencyCode(recomputedTotal, currencyCode).Amount));
            Assert.That(payment.BalanceAfter,
                Is.EqualTo(finance.MoneyFromAmountAndCurrencyCode(
                    payment.BalanceBefore - payment.Amount,
                    currencyCode).Amount));
            Assert.That(payment.Amount * quantumMultiplier,
                Is.EqualTo(decimal.Truncate(payment.Amount * quantumMultiplier)));
            Assert.That(payment.BalanceAfter, Is.GreaterThanOrEqualTo(decimal.Zero));
        });
    }
}
