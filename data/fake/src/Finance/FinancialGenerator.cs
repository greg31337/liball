using System;

namespace fake;

internal static class FinancialGenerator
{
    const string Marker = "fake.synthetic-finance.v1";
    const string TransactionMask = "TXN-############";
    const string CheckMask = "CHK-########";
    const string InvoiceMask = "INV-########";
    static readonly string[] Payees = new string[]
    {
        "EXAMPLE PAYEE",
        "SAMPLE PAYEE",
        "REFERENCE PAYEE",
        "TEST PAYEE"
    };
    static readonly string[] Memos = new string[]
    {
        "Example payment",
        "Sample invoice",
        "Reference purchase",
        "Test expense"
    };
    static readonly FinancialTransactionType[] PaymentTypes = new FinancialTransactionType[]
    {
        FinancialTransactionType.Transfer,
        FinancialTransactionType.CardPayment,
        FinancialTransactionType.Check
    };

    internal static FinancialTransactionType TransactionType(FakeRandom random)
    {
        return (FinancialTransactionType)random.IntFromMaximumExclusive(8);
    }

    internal static string TransactionDescription(
        FinancialTransactionType type,
        int maximumOutputSize)
    {
        string result;
        switch (type)
        {
            case FinancialTransactionType.Deposit:
                result = "Synthetic deposit";
                break;
            case FinancialTransactionType.Withdrawal:
                result = "Synthetic withdrawal";
                break;
            case FinancialTransactionType.Transfer:
                result = "Synthetic transfer";
                break;
            case FinancialTransactionType.CardPayment:
                result = "Synthetic card payment";
                break;
            case FinancialTransactionType.Refund:
                result = "Synthetic refund";
                break;
            case FinancialTransactionType.Fee:
                result = "Synthetic fee";
                break;
            case FinancialTransactionType.Interest:
                result = "Synthetic interest";
                break;
            case FinancialTransactionType.Check:
                result = "Synthetic check payment";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    "type",
                    type,
                    "type is not supported.");
        }
        Guard.OutputSize(
            result.Length,
            maximumOutputSize,
            "transactionDescription");
        return result;
    }

    internal static string TransactionDescription(
        FakeRandom random,
        int maximumOutputSize)
    {
        return TransactionDescription(
            TransactionType(random),
            maximumOutputSize);
    }

    internal static string TransactionReference(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(12, context.CapturedOptions.MaximumCount, "transactionReference");
        return MaskFormatter.Format(
            random,
            TransactionMask,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string CheckNumber(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(8, context.CapturedOptions.MaximumCount, "checkNumber");
        return MaskFormatter.Format(
            random,
            CheckMask,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string InvoiceNumber(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.Count(8, context.CapturedOptions.MaximumCount, "invoiceNumber");
        return MaskFormatter.Format(
            random,
            InvoiceMask,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static decimal TaxRate(FakeRandom random)
    {
        return Rate(random, 0, 2500);
    }

    internal static decimal DiscountRate(FakeRandom random)
    {
        return Rate(random, 0, 3000);
    }

    internal static decimal InterestRate(FakeRandom random)
    {
        return Rate(random, 10, 2000);
    }

    internal static decimal Fee(FakeRandom random)
    {
        return MoneyGenerator.Amount(
            random,
            decimal.Zero,
            25m,
            2,
            MoneyRoundingMode.ToEven);
    }

    internal static decimal Fee(FakeRandom random, string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        return MoneyGenerator.Amount(
            random,
            decimal.Zero,
            25m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
    }

    internal static decimal Balance(FakeRandom random)
    {
        return MoneyGenerator.Amount(
            random,
            -10000m,
            10000m,
            2,
            MoneyRoundingMode.ToEven);
    }

    internal static decimal Balance(FakeRandom random, string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        return MoneyGenerator.Amount(
            random,
            -10000m,
            10000m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
    }

    internal static decimal TaxAmount(
        decimal amount,
        decimal rate,
        string currencyCode)
    {
        return PercentageAmount(amount, rate, currencyCode, "amount", "rate");
    }

    internal static decimal DiscountAmount(
        decimal amount,
        decimal rate,
        string currencyCode)
    {
        return PercentageAmount(amount, rate, currencyCode, "amount", "rate");
    }

    internal static decimal InterestAmount(
        decimal principal,
        decimal annualRate,
        int days,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        if (principal < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                "principal",
                principal,
                "principal cannot be negative.");
        }
        ValidateRate(annualRate, "annualRate");
        if (days < 0 || days > 3660)
        {
            throw new ArgumentOutOfRangeException(
                "days",
                days,
                "days must be between zero and 3660, inclusive.");
        }
        decimal raw = principal * annualRate * days / 365m;
        return Round(raw, currency);
    }

    internal static CheckRecord Check(
        GenerationContext context,
        FakeRandom random,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        Guard.Count(12, context.CapturedOptions.MaximumCount, "accountNumber");
        CheckRecord result = new CheckRecord();
        result.Number = CheckNumber(context, random);
        result.RoutingNumber = BankingGenerator.RoutingNumber(
            context,
            random,
            context.CapturedOptions.MaximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        result.AccountNumber = NumericStringGenerator.Digits(
            random,
            12,
            FakeLimits.DefaultMaximumOutputSize);
        result.Payee = Payees[random.IntFromMaximumExclusive(Payees.Length)];
        result.Memo = Memos[random.IntFromMaximumExclusive(Memos.Length)];
        result.Amount = MoneyGenerator.Amount(
            random,
            0.01m,
            1000m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        result.CurrencyCode = currency.Code;
        result.WrittenOn = context.ReferenceTime();
        result.IsRoutingChecksumValid = BankingGenerator.IsRoutingNumberValid(
            result.RoutingNumber,
            FakeLimits.DefaultMaximumCount);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.IsNegotiable = false;
        Guard.OutputSize(
            CheckSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "check");
        return result;
    }

    internal static InvoiceRecord Invoice(
        GenerationContext context,
        FakeRandom random,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        DateTimeOffset reference = context.ReferenceTime();
        if (reference > DateTimeOffset.MaxValue.AddDays(-30))
        {
            throw FakeException.Create(
                "No invoice due time is representable thirty days after the captured reference time.");
        }
        InvoiceRecord result = new InvoiceRecord();
        result.Number = InvoiceNumber(context, random);
        result.CurrencyCode = currency.Code;
        result.Subtotal = MoneyGenerator.Amount(
            random,
            10m,
            1000m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        result.TaxRate = TaxRate(random);
        result.TaxAmount = Round(result.Subtotal * result.TaxRate, currency);
        result.DiscountRate = Rate(random, 0, 2000);
        result.DiscountAmount = Round(result.Subtotal * result.DiscountRate, currency);
        result.FeeAmount = MoneyGenerator.Amount(
            random,
            decimal.Zero,
            25m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        result.Total = Round(
            result.Subtotal + result.TaxAmount + result.FeeAmount - result.DiscountAmount,
            currency);
        result.BalanceDue = result.Total;
        result.IssuedOn = reference;
        result.DueOn = reference.AddDays(30);
        result.IsPaid = false;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        Guard.OutputSize(
            InvoiceSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "invoice");
        return result;
    }

    internal static PaymentRecord Payment(
        GenerationContext context,
        FakeRandom random,
        string currencyCode)
    {
        InvoiceRecord invoice = Invoice(
            context,
            random,
            currencyCode);
        CurrencyRecord currency = Currency(invoice.CurrencyCode);
        PaymentRecord result = new PaymentRecord();
        result.Reference = TransactionReference(context, random);
        result.InvoiceNumber = invoice.Number;
        result.Type = PaymentTypes[random.IntFromMaximumExclusive(PaymentTypes.Length)];
        result.Description = TransactionDescription(
            result.Type,
            FakeLimits.DefaultMaximumOutputSize);
        result.CheckNumber = result.Type == FinancialTransactionType.Check
            ? CheckNumber(context, random)
            : string.Empty;
        result.CurrencyCode = invoice.CurrencyCode;
        result.Subtotal = invoice.Subtotal;
        result.TaxRate = invoice.TaxRate;
        result.TaxAmount = invoice.TaxAmount;
        result.DiscountRate = invoice.DiscountRate;
        result.DiscountAmount = invoice.DiscountAmount;
        result.FeeAmount = invoice.FeeAmount;
        result.Amount = invoice.Total;
        result.BalanceBefore = MoneyGenerator.Amount(
            random,
            result.Amount,
            result.Amount + 2000m,
            MoneyGenerator.Places(currency),
            MoneyRoundingMode.ToEven);
        result.BalanceAfter = Round(
            result.BalanceBefore - result.Amount,
            currency);
        result.OccurredOn = context.ReferenceTime();
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.IsAuthorized = false;
        result.IsSettled = false;
        Guard.OutputSize(
            PaymentSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "payment");
        return result;
    }

    static decimal PercentageAmount(
        decimal amount,
        decimal rate,
        string currencyCode,
        string amountParameter,
        string rateParameter)
    {
        CurrencyRecord currency = Currency(currencyCode);
        if (amount < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                amountParameter,
                amount,
                amountParameter + " cannot be negative.");
        }
        ValidateRate(rate, rateParameter);
        return Round(amount * rate, currency);
    }

    static void ValidateRate(decimal rate, string parameterName)
    {
        if (rate < decimal.Zero || rate > decimal.One)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                rate,
                parameterName + " must be between zero and one, inclusive.");
        }
    }

    static decimal Rate(FakeRandom random, int minimum, int maximum)
    {
        return random.IntFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, true) / 10000m;
    }

    static CurrencyRecord Currency(string currencyCode)
    {
        return CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
    }

    static decimal Round(decimal amount, CurrencyRecord currency)
    {
        return decimal.Round(
            amount,
            MoneyGenerator.Places(currency),
            MidpointRounding.ToEven);
    }

    static int CheckSize(CheckRecord value)
    {
        return value.Number.Length + value.RoutingNumber.Length +
            value.AccountNumber.Length + value.Payee.Length + value.Memo.Length +
            value.CurrencyCode.Length + value.TestMarker.Length;
    }

    static int InvoiceSize(InvoiceRecord value)
    {
        return value.Number.Length + value.CurrencyCode.Length +
            value.TestMarker.Length;
    }

    static int PaymentSize(PaymentRecord value)
    {
        return value.Reference.Length + value.InvoiceNumber.Length +
            value.Description.Length + value.CheckNumber.Length + value.CurrencyCode.Length +
            value.TestMarker.Length;
    }
}
