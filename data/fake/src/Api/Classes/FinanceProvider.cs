using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides finance and banking generation operations.
/// </summary>
public class FinanceProvider : IFinanceProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal FinanceProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("finance");
    }

    /// <summary>Generates a nonnegative decimal amount from zero through 1000 with two decimal places.</summary>
    /// <returns>A deterministic decimal value rounded to even.</returns>
    public decimal Amount()
    {
        return MoneyGenerator.DefaultAmount(RandomValue);
    }

    /// <summary>Generates a decimal amount within inclusive bounds using two decimal places.</summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic decimal value rounded to even.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is invalid or contains no two-decimal value.</exception>
    public decimal AmountFromMinimumAndMaximum(decimal minimum, decimal maximum)
    {
        return MoneyGenerator.Amount(RandomValue, minimum, maximum, 2, MoneyRoundingMode.ToEven);
    }

    /// <summary>Generates a decimal amount within inclusive bounds using explicit precision and rounding.</summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic decimal value representable at the requested precision.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument or representable interval is invalid.</exception>
    public decimal AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Amount(RandomValue, minimum, maximum, decimalPlaces, roundingMode);
    }

    /// <summary>Generates a nonnegative price from 0.01 through 1000 with two decimal places.</summary>
    /// <returns>A deterministic decimal price rounded to even.</returns>
    public decimal Price()
    {
        return MoneyGenerator.DefaultPrice(RandomValue);
    }

    /// <summary>Generates a nonnegative price within inclusive bounds using two decimal places.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic decimal price rounded to even.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is negative, invalid, or contains no two-decimal value.</exception>
    public decimal PriceFromMinimumAndMaximum(decimal minimum, decimal maximum)
    {
        return MoneyGenerator.Price(RandomValue, minimum, maximum, 2, MoneyRoundingMode.ToEven);
    }

    /// <summary>Generates a nonnegative price with explicit inclusive bounds, precision, and rounding.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic decimal price representable at the requested precision.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument or representable interval is invalid.</exception>
    public decimal PriceFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Price(RandomValue, minimum, maximum, decimalPlaces, roundingMode);
    }

    /// <summary>Generates a money record using a randomly selected current currency code.</summary>
    /// <returns>A numeric amount with separate locale-formatted display and currency metadata.</returns>
    public MoneyRecord Money()
    {
        return MoneyGenerator.RandomMoney(Context, RandomValue);
    }

    /// <summary>Generates a money record for a requested current currency code.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A numeric amount rounded to the currency's minor units with a separate display.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is empty, malformed, or absent.</exception>
    public MoneyRecord MoneyFromCurrencyCode(string currencyCode)
    {
        return MoneyGenerator.RandomMoney(Context, RandomValue, currencyCode);
    }

    /// <summary>Creates a money record from a supplied decimal amount using currency minor units and round-to-even.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    public MoneyRecord MoneyFromAmountAndCurrencyCode(decimal amount, string currencyCode)
    {
        return MoneyGenerator.Money(Context, amount, currencyCode, MoneyRoundingMode.ToEven);
    }

    /// <summary>Creates a money record using currency minor units and an explicit rounding rule.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    public MoneyRecord MoneyFromAmountAndCurrencyCodeAndRoundingMode(decimal amount, string currencyCode, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Money(Context, amount, currencyCode, roundingMode);
    }

    /// <summary>Creates a money record using explicit display precision and rounding.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision or rounding is invalid.</exception>
    public MoneyRecord MoneyFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(decimal amount, string currencyCode, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Money(Context, amount, currencyCode, decimalPlaces, roundingMode);
    }

    /// <summary>Formats a decimal amount using currency minor units, round-to-even, and the configured locale.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A locale-formatted display string.</returns>
    public string CurrencyDisplay(decimal amount, string currencyCode)
    {
        return MoneyGenerator.Display(Context, amount, currencyCode, MoneyRoundingMode.ToEven);
    }

    /// <summary>Formats a decimal amount using currency minor units and explicit rounding.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A locale-formatted display string.</returns>
    public string CurrencyDisplayFromAmountAndCurrencyCodeAndRoundingMode(decimal amount, string currencyCode, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Display(Context, amount, currencyCode, roundingMode);
    }

    /// <summary>Formats a decimal amount using explicit precision, rounding, and the configured locale.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A locale-formatted display string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision or rounding is invalid.</exception>
    public string CurrencyDisplayFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(decimal amount, string currencyCode, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return MoneyGenerator.Display(Context, amount, currencyCode, decimalPlaces, roundingMode);
    }

    /// <summary>Selects a current currency and returns its configured-locale symbol or alphabetic-code fallback.</summary>
    /// <returns>A locale symbol when the selected code is native to the locale; otherwise its alphabetic code.</returns>
    public string CurrencySymbol()
    {
        string code = CurrencyRegistryStore.Current().ChooseCode(RandomValue, FakeLimits.DefaultMaximumOutputSize);
        return CurrencySymbolFromCurrencyCode(code);
    }

    /// <summary>Returns a code's configured-locale symbol or its alphabetic-code fallback.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A locale symbol when the code is native to the locale; otherwise its alphabetic code.</returns>
    public string CurrencySymbolFromCurrencyCode(string currencyCode)
    {
        string result = MoneyGenerator.Symbol(Context.CapturedOptions.Locale, currencyCode);
        Guard.OutputSize(result.Length, Context.CapturedOptions.MaximumOutputSize, "currencySymbol");
        return result;
    }

    /// <summary>Selects one current currency or funds record from the bundled ISO 4217 snapshot.</summary>
    /// <remarks>Catalog presence is not legal-tender advice or evidence that a code is accepted for a transaction.</remarks>
    /// <returns>A correlated alphabetic code, numeric code, name, minor-unit rule, entity mappings, and registry version.</returns>
    public CurrencyRecord Currency()
    {
        return CurrencyRegistryStore.Current().Choose(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Looks up a current currency or funds record by alphabetic code.</summary>
    /// <param name="code">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The matching current registry record.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is empty, malformed, or absent.</exception>
    public CurrencyRecord CurrencyFromCode(string code)
    {
        return CurrencyRegistryStore.Current().Find(code, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Looks up a current currency or funds record by numeric code.</summary>
    /// <param name="numericCode">The three-digit ISO 4217 numeric code, including leading zeroes.</param>
    /// <returns>The matching current registry record.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the numeric code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the numeric code is empty, malformed, or absent.</exception>
    public CurrencyRecord CurrencyByNumericCode(string numericCode)
    {
        return CurrencyRegistryStore.Current().FindNumeric(numericCode, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns every current currency and funds record in alphabetic-code order.</summary>
    /// <returns>Independent copies of the bundled ISO 4217 catalog records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured collection or output limit is insufficient.</exception>
    public CurrencyRecord[] Currencies()
    {
        return CurrencyRegistryStore.Current().CopyAll(Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a current three-letter currency or funds code.</summary>
    /// <returns>An alphabetic ISO 4217 code.</returns>
    public string CurrencyCode()
    {
        return CurrencyRegistryStore.Current().ChooseCode(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects the English name of a current currency or funds code.</summary>
    /// <returns>A registry currency or funds name.</returns>
    public string CurrencyName()
    {
        return CurrencyRegistryStore.Current().ChooseName(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns source and update metadata for the bundled currency catalog.</summary>
    /// <returns>The source, publication date, retrieval date, and deduplicated record count.</returns>
    public CurrencyCatalogMetadata CurrencyMetadata()
    {
        return CurrencyRegistryStore.Current().Metadata(Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a BBAN matching the bundled format for a country or territory.</summary>
    /// <remarks>The generated value is synthetic and makes no claim that an account exists.</remarks>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>A synthetic BBAN matching the registry character pattern.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the country code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the country code is malformed or absent.</exception>
    public string Bban(string countryCode)
    {
        return IbanGenerator.Bban(RandomValue, countryCode, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic electronic IBAN using a randomly selected registry format.</summary>
    /// <returns>An uppercase, space-free IBAN with a valid MOD 97 checksum.</returns>
    public string Iban()
    {
        return IbanGenerator.Electronic(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic electronic IBAN for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>An uppercase, space-free IBAN with a valid MOD 97 checksum.</returns>
    public string IbanFromCountryCode(string countryCode)
    {
        return IbanGenerator.Electronic(RandomValue, countryCode, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic print-formatted IBAN using a randomly selected registry format.</summary>
    /// <returns>A valid IBAN grouped in blocks of four characters.</returns>
    public string IbanPrint()
    {
        return IbanGenerator.Print(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic print-formatted IBAN for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>A valid IBAN grouped in blocks of four characters.</returns>
    public string IbanPrintFromCountryCode(string countryCode)
    {
        return IbanGenerator.Print(RandomValue, countryCode, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a correlated synthetic IBAN record using a randomly selected registry format.</summary>
    /// <returns>Electronic, print, BBAN, component, registry, checksum, and safety fields.</returns>
    public IbanRecord IbanRecord()
    {
        return IbanGenerator.Record(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a correlated synthetic IBAN record for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>Electronic, print, BBAN, component, registry, checksum, and safety fields.</returns>
    public IbanRecord IbanRecordFromCountryCode(string countryCode)
    {
        return IbanGenerator.Record(RandomValue, countryCode, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Looks up one country or territory format from the bundled IBAN registry transcription.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>The matching format as an independent copy.</returns>
    public IbanFormat IbanFormat(string countryCode)
    {
        return IbanRegistryStore.Current().Find(countryCode, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns every bundled IBAN country and territory format in country-code order.</summary>
    /// <returns>Independent copies of all bundled format records.</returns>
    public IbanFormat[] IbanFormats()
    {
        return IbanRegistryStore.Current().CopyAll(Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Reports whether an IBAN has a known country format and a valid MOD 97 checksum.</summary>
    /// <remarks>Validation checks syntax and checksum only, not account existence, ownership, or payment reachability.</remarks>
    /// <param name="value">An electronic or space-grouped IBAN.</param>
    /// <returns><c>true</c> when the value matches its registry format and checksum; otherwise <c>false</c>.</returns>
    public bool IsIbanValid(string value)
    {
        return IbanGenerator.IsValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Normalizes a valid electronic or space-grouped IBAN to print form.</summary>
    /// <param name="value">A structurally valid IBAN with a valid MOD 97 checksum.</param>
    /// <returns>The uppercase IBAN grouped in blocks of four characters.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or checksum-invalid.</exception>
    public string FormatIban(string value)
    {
        return IbanGenerator.Format(value, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns source and update metadata for the bundled IBAN format catalog.</summary>
    /// <returns>The transcription source, upstream registry, retrieval date, and record count.</returns>
    public IbanCatalogMetadata IbanMetadata()
    {
        return IbanRegistryStore.Current().Metadata(Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an eight-character BIC shape using the configured locale's country.</summary>
    /// <remarks>The value is not checked against the BIC Directory and makes no registration or connectivity claim.</remarks>
    /// <returns>An uppercase ISO 9362-shaped business-party identifier.</returns>
    public string Bic()
    {
        return BankingGenerator.Bic(Context, RandomValue, false, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an eight-character BIC shape for a registered two-letter region code.</summary>
    /// <remarks>The value is not checked against the BIC Directory and makes no registration or connectivity claim.</remarks>
    /// <param name="countryCode">The case-insensitive two-letter region code.</param>
    /// <returns>An uppercase ISO 9362-shaped business-party identifier.</returns>
    public string BicFromCountryCode(string countryCode)
    {
        return BankingGenerator.Bic(RandomValue, countryCode, false, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an eleven-character BIC shape with a branch identifier.</summary>
    /// <returns>An uppercase ISO 9362-shaped business-party and branch identifier.</returns>
    public string BranchBic()
    {
        return BankingGenerator.Bic(Context, RandomValue, true, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an eleven-character BIC shape for a registered two-letter region code.</summary>
    /// <param name="countryCode">The case-insensitive two-letter region code.</param>
    /// <returns>An uppercase ISO 9362-shaped business-party and branch identifier.</returns>
    public string BranchBicFromCountryCode(string countryCode)
    {
        return BankingGenerator.Bic(RandomValue, countryCode, true, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Reports whether text has an eight- or eleven-character BIC structure and registered region code.</summary>
    /// <remarks>Structural validity is not evidence of BIC Directory registration, status, or network connectivity.</remarks>
    /// <param name="value">The uppercase BIC-shaped text to inspect.</param>
    /// <returns><c>true</c> for a supported BIC shape; otherwise <c>false</c>.</returns>
    public bool IsBicValid(string value)
    {
        return BankingGenerator.IsBicValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Generates a nine-digit ABA routing-number shape with a valid checksum.</summary>
    /// <remarks>Checksum validity is not evidence that a routing number is assigned, active, or suitable for payments.</remarks>
    /// <returns>A synthetic nine-digit routing-number shape.</returns>
    public string RoutingNumber()
    {
        return BankingGenerator.RoutingNumber(Context, RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a nine-digit ABA routing-number shape with a valid checksum.</summary>
    /// <remarks>This explicit alias has the same shape-only semantics as <see cref="RoutingNumber"/>.</remarks>
    /// <returns>A synthetic nine-digit routing-number shape.</returns>
    public string AbaRoutingNumber()
    {
        return RoutingNumber();
    }

    /// <summary>Reports whether text is nine decimal digits satisfying the ABA routing checksum.</summary>
    /// <remarks>The checksum does not establish assignment, institution identity, status, or payment reachability.</remarks>
    /// <param name="value">The routing-number-shaped text to inspect.</param>
    /// <returns><c>true</c> when the decimal shape and checksum are valid; otherwise <c>false</c>.</returns>
    public bool IsRoutingNumberValid(string value)
    {
        return BankingGenerator.IsRoutingNumberValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Reports whether text is nine decimal digits satisfying the ABA routing checksum.</summary>
    /// <param name="value">The routing-number-shaped text to inspect.</param>
    /// <returns><c>true</c> when the decimal shape and checksum are valid; otherwise <c>false</c>.</returns>
    public bool IsAbaRoutingNumberValid(string value)
    {
        return IsRoutingNumberValid(value);
    }

    /// <summary>Generates a synthetic domestic-account shape selected from the configured locale.</summary>
    /// <remarks>The generated value is not checked against an account directory and does not represent an active account.</remarks>
    /// <returns>A value generated from the locale-selected domestic mask.</returns>
    public string DomesticAccountNumber()
    {
        return BankingGenerator.DomesticAccountNumber(Context, RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns the domestic-account mask selected from the configured locale.</summary>
    /// <returns>A mask using <c>#</c> for digits, <c>?</c> for letters, and <c>*</c> for uppercase alphanumeric characters.</returns>
    public string DomesticAccountMask()
    {
        return BankingGenerator.DomesticAccountMask(Context, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a synthetic account category.</summary>
    /// <returns>A deterministic account category.</returns>
    public BankAccountType AccountType()
    {
        return BankingGenerator.AccountType(RandomValue);
    }

    /// <summary>Selects the configured-locale display name of a synthetic account category.</summary>
    /// <returns>A localized account-category name.</returns>
    public string AccountTypeName()
    {
        return BankingGenerator.AccountTypeName(Context, RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns the configured-locale display name of an account category.</summary>
    /// <param name="type">The account category.</param>
    /// <returns>A localized account-category name.</returns>
    public string AccountTypeNameFromType(BankAccountType type)
    {
        return BankingGenerator.AccountTypeName(Context, type, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an explicitly synthetic account display name.</summary>
    /// <returns>A project-authored marker combined with a localized account category.</returns>
    public string AccountName()
    {
        return BankingGenerator.AccountName(Context, RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects an explicitly fictional bank name.</summary>
    /// <returns>A project-authored bank fixture name.</returns>
    public string BankName()
    {
        return BankingGenerator.BankName(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects an explicitly fictional branch name.</summary>
    /// <returns>A project-authored branch fixture name.</returns>
    public string BranchName()
    {
        return BankingGenerator.BranchName(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a locale-shaped synthetic domestic bank-account record.</summary>
    /// <returns>Correlated name, type, number, mask, locale, and safety fields.</returns>
    public BankAccountRecord BankAccount()
    {
        return BankingGenerator.Account(Context, RandomValue);
    }

    /// <summary>Generates a project-authored synthetic bank record.</summary>
    /// <returns>A fictional name with correlated BIC country, locale, and safety fields.</returns>
    public BankRecord Bank()
    {
        return BankingGenerator.Bank(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic bank-branch record.</summary>
    /// <returns>Fictional names with BIC, domestic routing, locale, and safety fields.</returns>
    public BankBranchRecord BankBranch()
    {
        return BankingGenerator.Branch(Context, RandomValue);
    }

    /// <summary>Selects a synthetic financial transaction category.</summary>
    /// <returns>A deterministic deposit, withdrawal, transfer, payment, refund, fee, interest, or check category.</returns>
    public FinancialTransactionType TransactionType()
    {
        return FinancialGenerator.TransactionType(RandomValue);
    }

    /// <summary>Selects a project-authored synthetic transaction description.</summary>
    /// <returns>A description correlated with a deterministically selected transaction category.</returns>
    public string TransactionDescription()
    {
        return FinancialGenerator.TransactionDescription(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns the project-authored description for a transaction category.</summary>
    /// <param name="type">The transaction category.</param>
    /// <returns>A synthetic description correlated with the category.</returns>
    public string TransactionDescriptionFromType(FinancialTransactionType type)
    {
        return FinancialGenerator.TransactionDescription(type, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic transaction-reference shape.</summary>
    /// <returns>A value beginning with <c>TXN-</c> followed by twelve digits.</returns>
    public string TransactionReference()
    {
        return FinancialGenerator.TransactionReference(Context, RandomValue);
    }

    /// <summary>Generates a synthetic check-number shape.</summary>
    /// <returns>A value beginning with <c>CHK-</c> followed by eight digits.</returns>
    public string CheckNumber()
    {
        return FinancialGenerator.CheckNumber(Context, RandomValue);
    }

    /// <summary>Generates a synthetic invoice-number shape.</summary>
    /// <returns>A value beginning with <c>INV-</c> followed by eight digits.</returns>
    public string InvoiceNumber()
    {
        return FinancialGenerator.InvoiceNumber(Context, RandomValue);
    }

    /// <summary>Generates a positive exchange-rate-shaped decimal through 10000 with six decimal places.</summary>
    /// <remarks>The value is synthetic and is not a current or historical market quote.</remarks>
    /// <returns>A deterministic positive decimal value.</returns>
    public decimal ExchangeRate()
    {
        return FinanceCompletionGenerator.ExchangeRate(RandomValue);
    }

    /// <summary>Generates a positive exchange-rate-shaped decimal within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic value with six decimal places of resolution.</returns>
    public decimal ExchangeRateFromMinimumAndMaximum(decimal minimum, decimal maximum)
    {
        return FinanceCompletionGenerator.ExchangeRateFromBounds(
            RandomValue,
            minimum,
            maximum,
            6,
            MoneyRoundingMode.ToEven);
    }

    /// <summary>Generates a positive exchange-rate-shaped decimal with explicit precision and rounding.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic value representable at the requested precision.</returns>
    public decimal ExchangeRateFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return FinanceCompletionGenerator.ExchangeRateFromBounds(
            RandomValue,
            minimum,
            maximum,
            decimalPlaces,
            roundingMode);
    }

    /// <summary>Generates a synthetic exchange-rate record for two distinct current currencies.</summary>
    /// <returns>An ordered currency pair and positive rate with shape-only metadata.</returns>
    public ExchangeRateRecord ExchangeRateRecord()
    {
        return FinanceCompletionGenerator.RandomExchangeRateRecord(
            RandomValue,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic exchange-rate record for an ordered currency pair.</summary>
    /// <param name="baseCurrencyCode">The ISO 4217 code representing one base unit.</param>
    /// <param name="quoteCurrencyCode">The distinct ISO 4217 quoted-currency code.</param>
    /// <returns>The requested pair and a positive six-place rate.</returns>
    public ExchangeRateRecord ExchangeRateRecordFromBaseAndQuoteCurrencyCodes(string baseCurrencyCode, string quoteCurrencyCode)
    {
        return FinanceCompletionGenerator.ExchangeRateRecordFromCodes(
            RandomValue,
            baseCurrencyCode,
            quoteCurrencyCode,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Creates a synthetic exchange-rate record from a supplied value.</summary>
    /// <param name="baseCurrencyCode">The ISO 4217 code representing one base unit.</param>
    /// <param name="quoteCurrencyCode">The distinct ISO 4217 quoted-currency code.</param>
    /// <param name="rate">The positive quoted units per base unit.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The requested pair and rounded synthetic rate.</returns>
    public ExchangeRateRecord ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(string baseCurrencyCode, string quoteCurrencyCode, decimal rate, int decimalPlaces, MoneyRoundingMode roundingMode)
    {
        return FinanceCompletionGenerator.ExchangeRateRecordFromValues(
            baseCurrencyCode,
            quoteCurrencyCode,
            rate,
            decimalPlaces,
            roundingMode,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a positive quantity from one through one hundred.</summary>
    /// <returns>A deterministic integer quantity.</returns>
    public int Quantity()
    {
        return FinanceCompletionGenerator.Quantity(
            RandomValue,
            1,
            100,
            Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Generates a positive quantity within inclusive configured bounds.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum, limited by the configured maximum count.</param>
    /// <returns>A deterministic integer quantity.</returns>
    public int QuantityFromMinimumAndMaximum(int minimum, int maximum)
    {
        return FinanceCompletionGenerator.Quantity(
            RandomValue,
            minimum,
            maximum,
            Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Creates a thirty-day inclusive ledger interval ending at the captured reference time.</summary>
    /// <returns>An ordered interval and its duration.</returns>
    public LedgerIntervalRecord LedgerInterval()
    {
        return FinanceCompletionGenerator.LedgerInterval(Context);
    }

    /// <summary>Creates an inclusive ledger interval from supplied ordered bounds.</summary>
    /// <param name="start">The inclusive interval beginning.</param>
    /// <param name="end">The inclusive interval end, not earlier than <c>start</c>.</param>
    /// <returns>The supplied bounds and their elapsed duration.</returns>
    public LedgerIntervalRecord LedgerIntervalFromStartAndEnd(DateTimeOffset start, DateTimeOffset end)
    {
        return FinanceCompletionGenerator.LedgerIntervalFromBounds(start, end);
    }

    /// <summary>Generates a decimal tax-rate field from zero through twenty-five percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    public decimal TaxRate()
    {
        return FinancialGenerator.TaxRate(RandomValue);
    }

    /// <summary>Generates a decimal discount-rate field from zero through thirty percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    public decimal DiscountRate()
    {
        return FinancialGenerator.DiscountRate(RandomValue);
    }

    /// <summary>Generates a positive annual interest-rate field through twenty percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    public decimal InterestRate()
    {
        return FinancialGenerator.InterestRate(RandomValue);
    }

    /// <summary>Generates a nonnegative fee field through 25 with two decimal places.</summary>
    /// <returns>A deterministic decimal fee.</returns>
    public decimal Fee()
    {
        return FinancialGenerator.Fee(RandomValue);
    }

    /// <summary>Generates a nonnegative fee using a currency's ISO minor units.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A deterministic decimal fee rounded to the currency's minor units.</returns>
    public decimal FeeFromCurrencyCode(string currencyCode)
    {
        return FinancialGenerator.Fee(RandomValue, currencyCode);
    }

    /// <summary>Generates a balance from negative 10000 through positive 10000 with two decimal places.</summary>
    /// <returns>A deterministic decimal balance.</returns>
    public decimal Balance()
    {
        return FinancialGenerator.Balance(RandomValue);
    }

    /// <summary>Generates a balance using a currency's ISO minor units.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A deterministic decimal balance rounded to the currency's minor units.</returns>
    public decimal BalanceFromCurrencyCode(string currencyCode)
    {
        return FinancialGenerator.Balance(RandomValue, currencyCode);
    }

    /// <summary>Calculates and rounds a tax amount using currency minor units.</summary>
    /// <param name="amount">The nonnegative amount to which the rate applies.</param>
    /// <param name="rate">The decimal rate from zero through one.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The amount multiplied by the rate and rounded to ISO minor units.</returns>
    public decimal TaxAmount(decimal amount, decimal rate, string currencyCode)
    {
        return FinancialGenerator.TaxAmount(amount, rate, currencyCode);
    }

    /// <summary>Calculates and rounds a discount amount using currency minor units.</summary>
    /// <param name="amount">The nonnegative amount to which the rate applies.</param>
    /// <param name="rate">The decimal rate from zero through one.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The amount multiplied by the rate and rounded to ISO minor units.</returns>
    public decimal DiscountAmount(decimal amount, decimal rate, string currencyCode)
    {
        return FinancialGenerator.DiscountAmount(amount, rate, currencyCode);
    }

    /// <summary>Calculates simple annual interest for a bounded day count using currency minor units.</summary>
    /// <param name="principal">The nonnegative principal.</param>
    /// <param name="annualRate">The decimal annual rate from zero through one.</param>
    /// <param name="days">The day count from zero through 3660.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>Simple interest calculated on a 365-day denominator and rounded to ISO minor units.</returns>
    public decimal InterestAmount(decimal principal, decimal annualRate, int days, string currencyCode)
    {
        return FinancialGenerator.InterestAmount(principal, annualRate, days, currencyCode);
    }

    /// <summary>Generates an explicitly synthetic, non-negotiable U.S. check fixture in USD.</summary>
    /// <returns>A correlated check number, ABA routing shape, account shape, payee, memo, amount, date, and safety fields.</returns>
    public CheckRecord Check()
    {
        return FinancialGenerator.Check(Context, RandomValue, "USD");
    }

    /// <summary>Generates an explicitly synthetic, non-negotiable U.S. check fixture.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code for the numeric amount.</param>
    /// <returns>A correlated check fixture rounded to the currency's minor units.</returns>
    public CheckRecord CheckFromCurrencyCode(string currencyCode)
    {
        return FinancialGenerator.Check(Context, RandomValue, currencyCode);
    }

    /// <summary>Generates an arithmetically correlated synthetic invoice fixture in USD.</summary>
    /// <returns>An unpaid invoice with subtotal, tax, discount, fee, total, balance, and dates.</returns>
    public InvoiceRecord Invoice()
    {
        return FinancialGenerator.Invoice(Context, RandomValue, "USD");
    }

    /// <summary>Generates an arithmetically correlated synthetic invoice fixture.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>An unpaid invoice rounded to the currency's minor units.</returns>
    public InvoiceRecord InvoiceFromCurrencyCode(string currencyCode)
    {
        return FinancialGenerator.Invoice(Context, RandomValue, currencyCode);
    }

    /// <summary>Generates a correlated synthetic payment calculation in USD.</summary>
    /// <returns>A payment-shaped record with invoice arithmetic, reference, type, description, and balances.</returns>
    public PaymentRecord Payment()
    {
        return FinancialGenerator.Payment(Context, RandomValue, "USD");
    }

    /// <summary>Generates a correlated synthetic payment calculation.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A payment-shaped record rounded to the currency's minor units.</returns>
    public PaymentRecord PaymentFromCurrencyCode(string currencyCode)
    {
        return FinancialGenerator.Payment(Context, RandomValue, currencyCode);
    }

    /// <summary>Generates an arithmetically correlated synthetic purchase order in USD.</summary>
    /// <returns>A quantity, numeric amounts, separate displays, dates, and safety fields.</returns>
    public PurchaseOrderRecord PurchaseOrder()
    {
        int quantity = FinanceCompletionGenerator.Quantity(
            RandomValue,
            1,
            25,
            Context.CapturedOptions.MaximumCount);
        return FinanceCompletionGenerator.PurchaseOrder(
            Context,
            RandomValue,
            quantity,
            "USD");
    }

    /// <summary>Generates an arithmetically correlated synthetic purchase order.</summary>
    /// <param name="quantity">The positive quantity, limited by the configured maximum count.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A purchase order using the requested quantity and currency.</returns>
    public PurchaseOrderRecord PurchaseOrderFromQuantityAndCurrencyCode(int quantity, string currencyCode)
    {
        return FinanceCompletionGenerator.PurchaseOrder(
            Context,
            RandomValue,
            quantity,
            currencyCode);
    }

    /// <summary>Generates a correlated synthetic account ledger with ten USD entries.</summary>
    /// <returns>An account, interval, balances, entries, separate displays, and safety fields.</returns>
    public AccountLedgerRecord AccountLedger()
    {
        decimal openingBalance = MoneyGenerator.Amount(
            RandomValue,
            100m,
            10000m,
            2,
            MoneyRoundingMode.ToEven);
        return FinanceCompletionGenerator.AccountLedger(
            Context,
            RandomValue,
            openingBalance,
            10,
            "USD");
    }

    /// <summary>Generates a correlated synthetic account ledger with explicit inputs.</summary>
    /// <param name="openingBalance">The opening numeric balance.</param>
    /// <param name="entryCount">The nonnegative entry count, limited by configured count and output bounds.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code shared by every entry.</param>
    /// <returns>An account ledger whose chronologically ordered entries reconcile to its closing balance.</returns>
    public AccountLedgerRecord AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(decimal openingBalance, int entryCount, string currencyCode)
    {
        return FinanceCompletionGenerator.AccountLedger(
            Context,
            RandomValue,
            openingBalance,
            entryCount,
            currencyCode);
    }

    /// <summary>Generates a Bitcoin P2PKH Base58Check address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic mainnet-shaped address with a valid structural checksum.</returns>
    public string BitcoinAddress()
    {
        return CryptocurrencyGenerator.BitcoinAddress(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a Litecoin P2PKH Base58Check address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic mainnet-shaped address with a valid structural checksum.</returns>
    public string LitecoinAddress()
    {
        return CryptocurrencyGenerator.LitecoinAddress(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an Ethereum EIP-55 hexadecimal address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic address shape with valid mixed-case checksum presentation.</returns>
    public string EthereumAddress()
    {
        return CryptocurrencyGenerator.EthereumAddress(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an address shape for a supported cryptocurrency.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <param name="cryptocurrency">The requested address family.</param>
    /// <returns>A deterministic address shape with a valid structural checksum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    public string CryptocurrencyAddress(Cryptocurrency cryptocurrency)
    {
        return CryptocurrencyGenerator.Address(RandomValue, cryptocurrency, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Validates a Bitcoin P2PKH Base58Check address shape.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the version, length, alphabet, and checksum are structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool IsBitcoinAddressValid(string value)
    {
        return CryptocurrencyGenerator.IsBitcoinAddressValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Validates a Litecoin P2PKH Base58Check address shape.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the version, length, alphabet, and checksum are structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool IsLitecoinAddressValid(string value)
    {
        return CryptocurrencyGenerator.IsLitecoinAddressValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Validates an Ethereum hexadecimal address shape and its mixed-case checksum when present.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the value is structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool IsEthereumAddressValid(string value)
    {
        return CryptocurrencyGenerator.IsEthereumAddressValid(value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Validates an address shape for a supported cryptocurrency.</summary>
    /// <param name="cryptocurrency">The address family to validate.</param>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the value is structurally valid for the selected family.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    public bool IsCryptocurrencyAddressValid(Cryptocurrency cryptocurrency, string value)
    {
        return CryptocurrencyGenerator.IsAddressValid(cryptocurrency, value, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Generates an explicitly synthetic cryptocurrency address fixture.</summary>
    /// <remarks>The fixture contains no private key and never claims ownership, activity, balance, or spendability.</remarks>
    /// <returns>A deterministic address fixture for a randomly selected supported family.</returns>
    public CryptoAddressRecord CryptoAddress()
    {
        return CryptocurrencyGenerator.Record(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates an explicitly synthetic cryptocurrency address fixture for a selected family.</summary>
    /// <remarks>The fixture contains no private key and never claims ownership, activity, balance, or spendability.</remarks>
    /// <param name="cryptocurrency">The requested address family.</param>
    /// <returns>A deterministic address fixture.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    public CryptoAddressRecord CryptoAddressFromCryptocurrency(Cryptocurrency cryptocurrency)
    {
        return CryptocurrencyGenerator.Record(RandomValue, cryptocurrency, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a generic 256-bit lowercase hexadecimal hash shape.</summary>
    /// <remarks>The value is random fixture data and is not a transaction or block identifier from a cryptocurrency network.</remarks>
    /// <returns>A deterministic 64-character hexadecimal value.</returns>
    public string CryptoHash()
    {
        return CryptocurrencyGenerator.Hash(RandomValue, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a 256-bit hash shape using a supported cryptocurrency's conventional presentation.</summary>
    /// <remarks>The value is random fixture data and is not a transaction or block identifier from a cryptocurrency network.</remarks>
    /// <param name="cryptocurrency">The requested presentation family.</param>
    /// <returns>A deterministic hexadecimal hash shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    public string CryptoHashFromCryptocurrency(Cryptocurrency cryptocurrency)
    {
        return CryptocurrencyGenerator.Hash(RandomValue, cryptocurrency, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a supported payment-card issuer shape.</summary>
    /// <returns>A deterministic issuer selection.</returns>
    public CardIssuer CardIssuer()
    {
        return CardGenerator.Issuer(RandomValue);
    }

    /// <summary>Identifies the supported issuer shape of a PAN.</summary>
    /// <param name="number">A decimal PAN with a supported prefix and length.</param>
    /// <returns>The matching issuer shape without making an account or authorization claim.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the number is empty, malformed, or unsupported.</exception>
    public CardIssuer CardIssuerFromNumber(string number)
    {
        return CardGenerator.Issuer(number);
    }

    /// <summary>Returns PAN-prefix, length, CVV, and safe test-fixture metadata for an issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>An independent issuer-pattern record.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public CardIssuerPattern CardPattern(CardIssuer issuer)
    {
        return CardGenerator.Pattern(issuer, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns every supported payment-card issuer pattern.</summary>
    /// <returns>Independent pattern records in issuer order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public CardIssuerPattern[] CardPatterns()
    {
        return CardGenerator.Patterns(Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a Luhn-valid documented test PAN from a supported issuer shape.</summary>
    /// <remarks>The value is test-only and does not represent an active or authorized account.</remarks>
    /// <returns>A deterministic Luhn-valid test PAN.</returns>
    public string CardNumber()
    {
        return CardNumberFromIssuer(CardGenerator.Issuer(RandomValue));
    }

    /// <summary>Returns the Luhn-valid documented test PAN for an issuer shape.</summary>
    /// <remarks>The value is test-only and does not represent an active or authorized account.</remarks>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>The issuer's Luhn-valid test PAN.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public string CardNumberFromIssuer(CardIssuer issuer)
    {
        return CardGenerator.Number(issuer, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Selects a checksum-invalid mutation of a supported test PAN.</summary>
    /// <returns>A PAN-shaped value retaining its issuer shape but failing Luhn validation.</returns>
    public string InvalidCardNumber()
    {
        return InvalidCardNumberFromIssuer(CardGenerator.Issuer(RandomValue));
    }

    /// <summary>Returns a checksum-invalid mutation of an issuer's test PAN.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>A PAN-shaped value retaining its issuer shape but failing Luhn validation.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public string InvalidCardNumberFromIssuer(CardIssuer issuer)
    {
        return CardGenerator.InvalidNumber(issuer, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Reports whether a PAN has a supported issuer shape and valid Luhn digit.</summary>
    /// <remarks>Validation checks structure and checksum only, not account state, ownership, authorization, or acceptance.</remarks>
    /// <param name="number">The PAN text to inspect.</param>
    /// <returns><c>true</c> for a supported decimal shape with a valid Luhn digit; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    public bool IsCardNumberValid(string number)
    {
        return CardGenerator.IsValid(number, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Selects and masks a supported test PAN, retaining its final four digits.</summary>
    /// <returns>A masked test PAN.</returns>
    public string MaskedCard()
    {
        return MaskedCardFromIssuer(CardGenerator.Issuer(RandomValue));
    }

    /// <summary>Masks an issuer's supported test PAN, retaining its final four digits.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>A masked test PAN.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public string MaskedCardFromIssuer(CardIssuer issuer)
    {
        return CardGenerator.Mask(CardGenerator.Number(issuer, FakeLimits.DefaultMaximumOutputSize), Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Masks a supported PAN shape, retaining its final four digits.</summary>
    /// <param name="number">A decimal PAN with a supported prefix and length.</param>
    /// <returns>A same-length string containing asterisks and the final four digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the number is empty, malformed, or unsupported.</exception>
    public string MaskedCardFromNumber(string number)
    {
        return CardGenerator.Mask(number, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a synthetic CVV shape for a randomly selected supported issuer.</summary>
    /// <returns>Three digits, or four digits when American Express is selected.</returns>
    public string Cvv()
    {
        return CvvFromIssuer(CardGenerator.Issuer(RandomValue));
    }

    /// <summary>Generates a synthetic CVV shape for a requested issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>Three or four decimal digits according to the issuer shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public string CvvFromIssuer(CardIssuer issuer)
    {
        return CardGenerator.Cvv(RandomValue, issuer, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a four-digit synthetic payment-card PIN.</summary>
    /// <returns>Exactly four decimal digits, retaining leading zeroes.</returns>
    public string Pin()
    {
        return CardGenerator.Pin(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a future synthetic card expiry in <c>MM/yy</c> form.</summary>
    /// <returns>An expiry from one through 60 months after the captured reference time.</returns>
    /// <exception cref="FakeException">Thrown when no future month is representable.</exception>
    public string Expiry()
    {
        return CardGenerator.Expiry(Context, RandomValue);
    }

    /// <summary>Selects an explicitly synthetic uppercase cardholder label.</summary>
    /// <returns>An example, sample, or test cardholder label.</returns>
    public string Cardholder()
    {
        return CardGenerator.Cardholder(RandomValue, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Generates a correlated, explicitly synthetic test-card record.</summary>
    /// <returns>Issuer, PAN, mask, CVV, PIN, expiry, cardholder, and test-safety fields.</returns>
    public CardRecord Card()
    {
        return CardGenerator.Card(Context, RandomValue);
    }

    /// <summary>Generates a correlated, explicitly synthetic test-card record for an issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>Issuer, PAN, mask, CVV, PIN, expiry, cardholder, and test-safety fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    public CardRecord CardFromIssuer(CardIssuer issuer)
    {
        return CardGenerator.Card(Context, RandomValue, issuer);
    }
}
