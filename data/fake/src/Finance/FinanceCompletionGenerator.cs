using System;

namespace fake;

internal static class FinanceCompletionGenerator
{
    const string Marker = "fake.synthetic-finance.v1";
    const string PurchaseOrderMask = "PO-########";
    static readonly FinancialTransactionType[] DebitTypes = new FinancialTransactionType[]
    {
        FinancialTransactionType.Withdrawal,
        FinancialTransactionType.CardPayment,
        FinancialTransactionType.Fee,
        FinancialTransactionType.Check
    };
    static readonly FinancialTransactionType[] CreditTypes = new FinancialTransactionType[]
    {
        FinancialTransactionType.Deposit,
        FinancialTransactionType.Refund,
        FinancialTransactionType.Interest
    };

    internal static decimal ExchangeRate(FakeRandom random)
    {
        return MoneyGenerator.Amount(
            random,
            0.000001m,
            10000m,
            6,
            MoneyRoundingMode.ToEven);
    }

    internal static decimal ExchangeRateFromBounds(
        FakeRandom random,
        decimal minimum,
        decimal maximum,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        if (minimum <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                "minimum",
                minimum,
                "minimum exchange rate must be greater than zero.");
        }
        return MoneyGenerator.Amount(
            random,
            minimum,
            maximum,
            decimalPlaces,
            roundingMode);
    }

    internal static ExchangeRateRecord RandomExchangeRateRecord(
        FakeRandom random,
        int maximumOutputSize)
    {
        CurrencyRecord baseCurrency = CurrencyRegistryStore.Current().Choose(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        CurrencyRecord quoteCurrency = CurrencyRegistryStore.Current().Choose(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        int attempts = 1;
        while (quoteCurrency.Code == baseCurrency.Code && attempts < 100)
        {
            quoteCurrency = CurrencyRegistryStore.Current().Choose(
                random,
                FakeLimits.DefaultMaximumOutputSize);
            attempts = attempts + 1;
        }
        if (quoteCurrency.Code == baseCurrency.Code)
        {
            Guard.Exhausted("exchange currency pair", attempts);
        }
        return CreateGeneratedExchangeRateRecord(
            random,
            baseCurrency,
            quoteCurrency,
            maximumOutputSize);
    }

    internal static ExchangeRateRecord ExchangeRateRecordFromCodes(
        FakeRandom random,
        string baseCurrencyCode,
        string quoteCurrencyCode,
        int maximumOutputSize)
    {
        CurrencyRecord baseCurrency = Currency(baseCurrencyCode);
        CurrencyRecord quoteCurrency = Currency(quoteCurrencyCode);
        ValidatePair(baseCurrency, quoteCurrency);
        return CreateGeneratedExchangeRateRecord(
            random,
            baseCurrency,
            quoteCurrency,
            maximumOutputSize);
    }

    internal static ExchangeRateRecord ExchangeRateRecordFromValues(
        string baseCurrencyCode,
        string quoteCurrencyCode,
        decimal rate,
        int decimalPlaces,
        MoneyRoundingMode roundingMode,
        int maximumOutputSize)
    {
        CurrencyRecord baseCurrency = Currency(baseCurrencyCode);
        CurrencyRecord quoteCurrency = Currency(quoteCurrencyCode);
        ValidatePair(baseCurrency, quoteCurrency);
        if (rate <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                "rate",
                rate,
                "rate must be greater than zero.");
        }
        ExchangeRateRecord result = new ExchangeRateRecord();
        result.BaseCurrencyCode = baseCurrency.Code;
        result.QuoteCurrencyCode = quoteCurrency.Code;
        result.Rate = Round(rate, decimalPlaces, roundingMode);
        if (result.Rate <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                "rate",
                rate,
                "rate rounds to zero at the requested precision.");
        }
        result.DecimalPlaces = decimalPlaces;
        result.RoundingMode = roundingMode;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsMarketQuote = false;
        Guard.OutputSize(
            ExchangeRateRecordSize(result),
            maximumOutputSize,
            "exchangeRateRecord");
        return result;
    }

    internal static int Quantity(
        FakeRandom random,
        int minimum,
        int maximum,
        int maximumCount)
    {
        if (minimum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "minimum",
                minimum,
                "minimum quantity must be greater than zero.");
        }
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                "maximum",
                maximum,
                "maximum quantity must be greater than or equal to minimum.");
        }
        Guard.Count(maximum, maximumCount, "maximum");
        return random.IntFromMinimumAndMaximumAndMaximumInclusive(
            minimum,
            maximum,
            true);
    }

    internal static LedgerIntervalRecord LedgerInterval(
        GenerationContext context)
    {
        DateTimeOffset end = context.ReferenceTime();
        if (end < DateTimeOffset.MinValue.AddDays(30))
        {
            throw FakeException.Create(
                "No ledger start time is representable thirty days before the captured reference time.");
        }
        return LedgerIntervalFromBounds(end.AddDays(-30), end);
    }

    internal static LedgerIntervalRecord LedgerIntervalFromBounds(
        DateTimeOffset start,
        DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(
                "end",
                end,
                "end must be greater than or equal to start.");
        }
        LedgerIntervalRecord result = new LedgerIntervalRecord();
        result.Start = start;
        result.End = end;
        result.Duration = end - start;
        result.EndInclusive = true;
        return result;
    }

    internal static PurchaseOrderRecord PurchaseOrder(
        GenerationContext context,
        FakeRandom random,
        int quantity,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        ValidateQuantity(quantity, context.CapturedOptions.MaximumCount);
        Guard.Count(8, context.CapturedOptions.MaximumCount, "purchaseOrderNumber");
        DateTimeOffset reference = context.ReferenceTime();
        if (reference > DateTimeOffset.MaxValue.AddDays(-14))
        {
            throw FakeException.Create(
                "No purchase-order expected time is representable fourteen days after the captured reference time.");
        }
        int places = MoneyGenerator.Places(currency);
        PurchaseOrderRecord result = new PurchaseOrderRecord();
        result.Number = MaskFormatter.Format(
            random,
            PurchaseOrderMask,
            FakeLimits.DefaultMaximumOutputSize);
        result.Quantity = quantity;
        result.UnitPrice = MoneyGenerator.Amount(
            random,
            0.01m,
            1000m,
            places,
            MoneyRoundingMode.ToEven);
        result.CurrencyCode = currency.Code;
        result.Subtotal = RoundCurrency(result.UnitPrice * result.Quantity, currency);
        result.TaxRate = FinancialGenerator.TaxRate(random);
        result.TaxAmount = RoundCurrency(result.Subtotal * result.TaxRate, currency);
        result.DiscountRate = FinancialGenerator.DiscountRate(random);
        result.DiscountAmount = RoundCurrency(result.Subtotal * result.DiscountRate, currency);
        result.FeeAmount = MoneyGenerator.Amount(
            random,
            decimal.Zero,
            25m,
            places,
            MoneyRoundingMode.ToEven);
        result.Total = RoundCurrency(
            result.Subtotal + result.TaxAmount + result.FeeAmount - result.DiscountAmount,
            currency);
        result.UnitPriceDisplay = MoneyGenerator.Display(
            context,
            result.UnitPrice,
            currency.Code,
            MoneyRoundingMode.ToEven);
        result.SubtotalDisplay = MoneyGenerator.Display(
            context,
            result.Subtotal,
            currency.Code,
            MoneyRoundingMode.ToEven);
        result.TotalDisplay = MoneyGenerator.Display(
            context,
            result.Total,
            currency.Code,
            MoneyRoundingMode.ToEven);
        result.CreatedOn = reference;
        result.ExpectedOn = reference.AddDays(14);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsApprovedPurchase = false;
        Guard.OutputSize(
            PurchaseOrderSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "purchaseOrder");
        return result;
    }

    internal static AccountLedgerRecord AccountLedger(
        GenerationContext context,
        FakeRandom random,
        decimal openingBalance,
        int entryCount,
        string currencyCode)
    {
        CurrencyRecord currency = Currency(currencyCode);
        Guard.Count(entryCount, context.CapturedOptions.MaximumCount, "entryCount");
        Guard.OutputSize(
            entryCount,
            100,
            context.CapturedOptions.MaximumOutputSize,
            "entryCount",
            "estimatedEntrySize");
        if (openingBalance < -1000000000000m || openingBalance > 1000000000000m)
        {
            throw new ArgumentOutOfRangeException(
                "openingBalance",
                openingBalance,
                "openingBalance must be between -1000000000000 and 1000000000000, inclusive.");
        }
        LedgerIntervalRecord interval = LedgerInterval(context);
        BankAccountRecord account = BankingGenerator.Account(context, random);
        int places = MoneyGenerator.Places(currency);
        AccountLedgerRecord result = new AccountLedgerRecord();
        result.Account = account;
        result.Interval = interval;
        result.CurrencyCode = currency.Code;
        result.OpeningBalance = decimal.Round(openingBalance, places, MidpointRounding.ToEven);
        result.OpeningBalanceDisplay = MoneyGenerator.Display(
            context,
            result.OpeningBalance,
            currency.Code,
            MoneyRoundingMode.ToEven);
        result.Entries = new LedgerEntryRecord[entryCount];
        decimal balance = result.OpeningBalance;
        long intervalTicks = interval.Duration.Ticks;
        int index = 0;
        while (index < entryCount)
        {
            LedgerEntryRecord entry = new LedgerEntryRecord();
            bool credit = random.Bool();
            entry.Reference = FinancialGenerator.TransactionReference(context, random);
            entry.AccountNumber = account.Number;
            entry.Type = credit
                ? CreditTypes[random.IntFromMaximumExclusive(CreditTypes.Length)]
                : DebitTypes[random.IntFromMaximumExclusive(DebitTypes.Length)];
            entry.Description = FinancialGenerator.TransactionDescription(
                entry.Type,
                FakeLimits.DefaultMaximumOutputSize);
            entry.OccurredOn = interval.Start.AddTicks(
                intervalTicks * (index + 1) / (entryCount + 1));
            entry.CurrencyCode = currency.Code;
            decimal amount = MoneyGenerator.Amount(
                random,
                0.01m,
                250m,
                places,
                MoneyRoundingMode.ToEven);
            entry.Debit = credit ? decimal.Zero : amount;
            entry.Credit = credit ? amount : decimal.Zero;
            entry.BalanceBefore = balance;
            entry.BalanceAfter = RoundCurrency(
                balance - entry.Debit + entry.Credit,
                currency);
            entry.DebitDisplay = MoneyGenerator.Display(
                context,
                entry.Debit,
                currency.Code,
                MoneyRoundingMode.ToEven);
            entry.CreditDisplay = MoneyGenerator.Display(
                context,
                entry.Credit,
                currency.Code,
                MoneyRoundingMode.ToEven);
            entry.BalanceAfterDisplay = MoneyGenerator.Display(
                context,
                entry.BalanceAfter,
                currency.Code,
                MoneyRoundingMode.ToEven);
            entry.IsSynthetic = true;
            entry.TestMarker = Marker;
            entry.RepresentsPostedTransaction = false;
            result.Entries[index] = entry;
            balance = entry.BalanceAfter;
            index = index + 1;
        }
        result.ClosingBalance = balance;
        result.ClosingBalanceDisplay = MoneyGenerator.Display(
            context,
            result.ClosingBalance,
            currency.Code,
            MoneyRoundingMode.ToEven);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsPostedLedger = false;
        Guard.OutputSize(
            AccountLedgerSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "accountLedger");
        return result;
    }

    static ExchangeRateRecord CreateGeneratedExchangeRateRecord(
        FakeRandom random,
        CurrencyRecord baseCurrency,
        CurrencyRecord quoteCurrency,
        int maximumOutputSize)
    {
        ExchangeRateRecord result = new ExchangeRateRecord();
        result.BaseCurrencyCode = baseCurrency.Code;
        result.QuoteCurrencyCode = quoteCurrency.Code;
        result.Rate = ExchangeRate(random);
        result.DecimalPlaces = 6;
        result.RoundingMode = MoneyRoundingMode.ToEven;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsMarketQuote = false;
        Guard.OutputSize(
            ExchangeRateRecordSize(result),
            maximumOutputSize,
            "exchangeRateRecord");
        return result;
    }

    static void ValidatePair(
        CurrencyRecord baseCurrency,
        CurrencyRecord quoteCurrency)
    {
        if (baseCurrency.Code == quoteCurrency.Code)
        {
            throw new ArgumentException(
                "Base and quote currency codes must be different.",
                "quoteCurrencyCode");
        }
    }

    static void ValidateQuantity(int quantity, int maximumCount)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "quantity",
                quantity,
                "quantity must be greater than zero.");
        }
        Guard.Count(quantity, maximumCount, "quantity");
    }

    static CurrencyRecord Currency(string currencyCode)
    {
        return CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
    }

    static decimal Round(
        decimal value,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        if (decimalPlaces < 0 || decimalPlaces > 28)
        {
            throw new ArgumentOutOfRangeException(
                "decimalPlaces",
                decimalPlaces,
                "decimalPlaces must be between zero and 28, inclusive.");
        }
        MidpointRounding rounding;
        switch (roundingMode)
        {
            case MoneyRoundingMode.ToEven:
                rounding = MidpointRounding.ToEven;
                break;
            case MoneyRoundingMode.AwayFromZero:
                rounding = MidpointRounding.AwayFromZero;
                break;
            case MoneyRoundingMode.ToZero:
                rounding = MidpointRounding.ToZero;
                break;
            case MoneyRoundingMode.ToNegativeInfinity:
                rounding = MidpointRounding.ToNegativeInfinity;
                break;
            case MoneyRoundingMode.ToPositiveInfinity:
                rounding = MidpointRounding.ToPositiveInfinity;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    "roundingMode",
                    roundingMode,
                    "roundingMode is not supported.");
        }
        return decimal.Round(value, decimalPlaces, rounding);
    }

    static decimal RoundCurrency(decimal value, CurrencyRecord currency)
    {
        return decimal.Round(
            value,
            MoneyGenerator.Places(currency),
            MidpointRounding.ToEven);
    }

    static int PurchaseOrderSize(PurchaseOrderRecord value)
    {
        return value.Number.Length + value.CurrencyCode.Length +
            value.UnitPriceDisplay.Length + value.SubtotalDisplay.Length +
            value.TotalDisplay.Length + value.TestMarker.Length;
    }

    static int ExchangeRateRecordSize(ExchangeRateRecord value)
    {
        return value.BaseCurrencyCode.Length + value.QuoteCurrencyCode.Length +
            value.TestMarker.Length;
    }

    static long AccountLedgerSize(AccountLedgerRecord value)
    {
        long size = value.Account.Name.Length + value.Account.Number.Length +
            value.Account.MaskedNumber.Length + value.CurrencyCode.Length +
            value.OpeningBalanceDisplay.Length + value.ClosingBalanceDisplay.Length +
            value.TestMarker.Length;
        int index = 0;
        while (index < value.Entries.Length)
        {
            LedgerEntryRecord entry = value.Entries[index];
            size = size + entry.Reference.Length + entry.AccountNumber.Length +
                entry.Description.Length + entry.CurrencyCode.Length +
                entry.DebitDisplay.Length + entry.CreditDisplay.Length +
                entry.BalanceAfterDisplay.Length + entry.TestMarker.Length;
            index = index + 1;
        }
        return size;
    }
}
