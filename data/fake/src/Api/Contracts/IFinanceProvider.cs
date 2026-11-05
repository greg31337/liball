using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="FinanceProvider"/>.</summary>
public interface IFinanceProvider
{
    /// <summary>Generates a nonnegative decimal amount from zero through 1000 with two decimal places.</summary>
    /// <returns>A deterministic decimal value rounded to even.</returns>
    decimal Amount();
    /// <summary>Generates a decimal amount within inclusive bounds using two decimal places.</summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic decimal value rounded to even.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is invalid or contains no two-decimal value.</exception>
    decimal AmountFromMinimumAndMaximum(decimal minimum, decimal maximum);
    /// <summary>Generates a decimal amount within inclusive bounds using explicit precision and rounding.</summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic decimal value representable at the requested precision.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument or representable interval is invalid.</exception>
    decimal AmountFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Generates a nonnegative price from 0.01 through 1000 with two decimal places.</summary>
    /// <returns>A deterministic decimal price rounded to even.</returns>
    decimal Price();
    /// <summary>Generates a nonnegative price within inclusive bounds using two decimal places.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic decimal price rounded to even.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is negative, invalid, or contains no two-decimal value.</exception>
    decimal PriceFromMinimumAndMaximum(decimal minimum, decimal maximum);
    /// <summary>Generates a nonnegative price with explicit inclusive bounds, precision, and rounding.</summary>
    /// <param name="minimum">The inclusive nonnegative minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic decimal price representable at the requested precision.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument or representable interval is invalid.</exception>
    decimal PriceFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Generates a money record using a randomly selected current currency code.</summary>
    /// <returns>A numeric amount with separate locale-formatted display and currency metadata.</returns>
    MoneyRecord Money();
    /// <summary>Generates a money record for a requested current currency code.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A numeric amount rounded to the currency's minor units with a separate display.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is empty, malformed, or absent.</exception>
    MoneyRecord MoneyFromCurrencyCode(string currencyCode);
    /// <summary>Creates a money record from a supplied decimal amount using currency minor units and round-to-even.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    MoneyRecord MoneyFromAmountAndCurrencyCode(decimal amount, string currencyCode);
    /// <summary>Creates a money record using currency minor units and an explicit rounding rule.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    MoneyRecord MoneyFromAmountAndCurrencyCodeAndRoundingMode(decimal amount, string currencyCode, MoneyRoundingMode roundingMode);
    /// <summary>Creates a money record using explicit display precision and rounding.</summary>
    /// <param name="amount">The decimal amount to round.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The rounded numeric amount with a separate locale-formatted display.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision or rounding is invalid.</exception>
    MoneyRecord MoneyFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(decimal amount, string currencyCode, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Formats a decimal amount using currency minor units, round-to-even, and the configured locale.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A locale-formatted display string.</returns>
    string CurrencyDisplay(decimal amount, string currencyCode);
    /// <summary>Formats a decimal amount using currency minor units and explicit rounding.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A locale-formatted display string.</returns>
    string CurrencyDisplayFromAmountAndCurrencyCodeAndRoundingMode(decimal amount, string currencyCode, MoneyRoundingMode roundingMode);
    /// <summary>Formats a decimal amount using explicit precision, rounding, and the configured locale.</summary>
    /// <param name="amount">The decimal amount to round and format.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A locale-formatted display string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when precision or rounding is invalid.</exception>
    string CurrencyDisplayFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(decimal amount, string currencyCode, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Selects a current currency and returns its configured-locale symbol or alphabetic-code fallback.</summary>
    /// <returns>A locale symbol when the selected code is native to the locale; otherwise its alphabetic code.</returns>
    string CurrencySymbol();
    /// <summary>Returns a code's configured-locale symbol or its alphabetic-code fallback.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A locale symbol when the code is native to the locale; otherwise its alphabetic code.</returns>
    string CurrencySymbolFromCurrencyCode(string currencyCode);
    /// <summary>Selects one current currency or funds record from the bundled ISO 4217 snapshot.</summary>
    /// <remarks>Catalog presence is not legal-tender advice or evidence that a code is accepted for a transaction.</remarks>
    /// <returns>A correlated alphabetic code, numeric code, name, minor-unit rule, entity mappings, and registry version.</returns>
    CurrencyRecord Currency();
    /// <summary>Looks up a current currency or funds record by alphabetic code.</summary>
    /// <param name="code">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The matching current registry record.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the code is empty, malformed, or absent.</exception>
    CurrencyRecord CurrencyFromCode(string code);
    /// <summary>Looks up a current currency or funds record by numeric code.</summary>
    /// <param name="numericCode">The three-digit ISO 4217 numeric code, including leading zeroes.</param>
    /// <returns>The matching current registry record.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the numeric code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the numeric code is empty, malformed, or absent.</exception>
    CurrencyRecord CurrencyByNumericCode(string numericCode);
    /// <summary>Returns every current currency and funds record in alphabetic-code order.</summary>
    /// <returns>Independent copies of the bundled ISO 4217 catalog records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured collection or output limit is insufficient.</exception>
    CurrencyRecord[] Currencies();
    /// <summary>Selects a current three-letter currency or funds code.</summary>
    /// <returns>An alphabetic ISO 4217 code.</returns>
    string CurrencyCode();
    /// <summary>Selects the English name of a current currency or funds code.</summary>
    /// <returns>A registry currency or funds name.</returns>
    string CurrencyName();
    /// <summary>Returns source and update metadata for the bundled currency catalog.</summary>
    /// <returns>The source, publication date, retrieval date, and deduplicated record count.</returns>
    CurrencyCatalogMetadata CurrencyMetadata();
    /// <summary>Generates a BBAN matching the bundled format for a country or territory.</summary>
    /// <remarks>The generated value is synthetic and makes no claim that an account exists.</remarks>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>A synthetic BBAN matching the registry character pattern.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the country code is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the country code is malformed or absent.</exception>
    string Bban(string countryCode);
    /// <summary>Generates a synthetic electronic IBAN using a randomly selected registry format.</summary>
    /// <returns>An uppercase, space-free IBAN with a valid MOD 97 checksum.</returns>
    string Iban();
    /// <summary>Generates a synthetic electronic IBAN for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>An uppercase, space-free IBAN with a valid MOD 97 checksum.</returns>
    string IbanFromCountryCode(string countryCode);
    /// <summary>Generates a synthetic print-formatted IBAN using a randomly selected registry format.</summary>
    /// <returns>A valid IBAN grouped in blocks of four characters.</returns>
    string IbanPrint();
    /// <summary>Generates a synthetic print-formatted IBAN for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>A valid IBAN grouped in blocks of four characters.</returns>
    string IbanPrintFromCountryCode(string countryCode);
    /// <summary>Generates a correlated synthetic IBAN record using a randomly selected registry format.</summary>
    /// <returns>Electronic, print, BBAN, component, registry, checksum, and safety fields.</returns>
    IbanRecord IbanRecord();
    /// <summary>Generates a correlated synthetic IBAN record for a country or territory.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>Electronic, print, BBAN, component, registry, checksum, and safety fields.</returns>
    IbanRecord IbanRecordFromCountryCode(string countryCode);
    /// <summary>Looks up one country or territory format from the bundled IBAN registry transcription.</summary>
    /// <param name="countryCode">The case-insensitive two-letter registry country or territory code.</param>
    /// <returns>The matching format as an independent copy.</returns>
    IbanFormat IbanFormat(string countryCode);
    /// <summary>Returns every bundled IBAN country and territory format in country-code order.</summary>
    /// <returns>Independent copies of all bundled format records.</returns>
    IbanFormat[] IbanFormats();
    /// <summary>Reports whether an IBAN has a known country format and a valid MOD 97 checksum.</summary>
    /// <remarks>Validation checks syntax and checksum only, not account existence, ownership, or payment reachability.</remarks>
    /// <param name="value">An electronic or space-grouped IBAN.</param>
    /// <returns><c>true</c> when the value matches its registry format and checksum; otherwise <c>false</c>.</returns>
    bool IsIbanValid(string value);
    /// <summary>Normalizes a valid electronic or space-grouped IBAN to print form.</summary>
    /// <param name="value">A structurally valid IBAN with a valid MOD 97 checksum.</param>
    /// <returns>The uppercase IBAN grouped in blocks of four characters.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or checksum-invalid.</exception>
    string FormatIban(string value);
    /// <summary>Returns source and update metadata for the bundled IBAN format catalog.</summary>
    /// <returns>The transcription source, upstream registry, retrieval date, and record count.</returns>
    IbanCatalogMetadata IbanMetadata();
    /// <summary>Generates an eight-character BIC shape using the configured locale's country.</summary>
    /// <remarks>The value is not checked against the BIC Directory and makes no registration or connectivity claim.</remarks>
    /// <returns>An uppercase ISO 9362-shaped business-party identifier.</returns>
    string Bic();
    /// <summary>Generates an eight-character BIC shape for a registered two-letter region code.</summary>
    /// <remarks>The value is not checked against the BIC Directory and makes no registration or connectivity claim.</remarks>
    /// <param name="countryCode">The case-insensitive two-letter region code.</param>
    /// <returns>An uppercase ISO 9362-shaped business-party identifier.</returns>
    string BicFromCountryCode(string countryCode);
    /// <summary>Generates an eleven-character BIC shape with a branch identifier.</summary>
    /// <returns>An uppercase ISO 9362-shaped business-party and branch identifier.</returns>
    string BranchBic();
    /// <summary>Generates an eleven-character BIC shape for a registered two-letter region code.</summary>
    /// <param name="countryCode">The case-insensitive two-letter region code.</param>
    /// <returns>An uppercase ISO 9362-shaped business-party and branch identifier.</returns>
    string BranchBicFromCountryCode(string countryCode);
    /// <summary>Reports whether text has an eight- or eleven-character BIC structure and registered region code.</summary>
    /// <remarks>Structural validity is not evidence of BIC Directory registration, status, or network connectivity.</remarks>
    /// <param name="value">The uppercase BIC-shaped text to inspect.</param>
    /// <returns><c>true</c> for a supported BIC shape; otherwise <c>false</c>.</returns>
    bool IsBicValid(string value);
    /// <summary>Generates a nine-digit ABA routing-number shape with a valid checksum.</summary>
    /// <remarks>Checksum validity is not evidence that a routing number is assigned, active, or suitable for payments.</remarks>
    /// <returns>A synthetic nine-digit routing-number shape.</returns>
    string RoutingNumber();
    /// <summary>Generates a nine-digit ABA routing-number shape with a valid checksum.</summary>
    /// <remarks>This explicit alias has the same shape-only semantics as <see cref="RoutingNumber"/>.</remarks>
    /// <returns>A synthetic nine-digit routing-number shape.</returns>
    string AbaRoutingNumber();
    /// <summary>Reports whether text is nine decimal digits satisfying the ABA routing checksum.</summary>
    /// <remarks>The checksum does not establish assignment, institution identity, status, or payment reachability.</remarks>
    /// <param name="value">The routing-number-shaped text to inspect.</param>
    /// <returns><c>true</c> when the decimal shape and checksum are valid; otherwise <c>false</c>.</returns>
    bool IsRoutingNumberValid(string value);
    /// <summary>Reports whether text is nine decimal digits satisfying the ABA routing checksum.</summary>
    /// <param name="value">The routing-number-shaped text to inspect.</param>
    /// <returns><c>true</c> when the decimal shape and checksum are valid; otherwise <c>false</c>.</returns>
    bool IsAbaRoutingNumberValid(string value);
    /// <summary>Generates a synthetic domestic-account shape selected from the configured locale.</summary>
    /// <remarks>The generated value is not checked against an account directory and does not represent an active account.</remarks>
    /// <returns>A value generated from the locale-selected domestic mask.</returns>
    string DomesticAccountNumber();
    /// <summary>Returns the domestic-account mask selected from the configured locale.</summary>
    /// <returns>A mask using <c>#</c> for digits, <c>?</c> for letters, and <c>*</c> for uppercase alphanumeric characters.</returns>
    string DomesticAccountMask();
    /// <summary>Selects a synthetic account category.</summary>
    /// <returns>A deterministic account category.</returns>
    BankAccountType AccountType();
    /// <summary>Selects the configured-locale display name of a synthetic account category.</summary>
    /// <returns>A localized account-category name.</returns>
    string AccountTypeName();
    /// <summary>Returns the configured-locale display name of an account category.</summary>
    /// <param name="type">The account category.</param>
    /// <returns>A localized account-category name.</returns>
    string AccountTypeNameFromType(BankAccountType type);
    /// <summary>Generates an explicitly synthetic account display name.</summary>
    /// <returns>A project-authored marker combined with a localized account category.</returns>
    string AccountName();
    /// <summary>Selects an explicitly fictional bank name.</summary>
    /// <returns>A project-authored bank fixture name.</returns>
    string BankName();
    /// <summary>Selects an explicitly fictional branch name.</summary>
    /// <returns>A project-authored branch fixture name.</returns>
    string BranchName();
    /// <summary>Generates a locale-shaped synthetic domestic bank-account record.</summary>
    /// <returns>Correlated name, type, number, mask, locale, and safety fields.</returns>
    BankAccountRecord BankAccount();
    /// <summary>Generates a project-authored synthetic bank record.</summary>
    /// <returns>A fictional name with correlated BIC country, locale, and safety fields.</returns>
    BankRecord Bank();
    /// <summary>Generates a correlated synthetic bank-branch record.</summary>
    /// <returns>Fictional names with BIC, domestic routing, locale, and safety fields.</returns>
    BankBranchRecord BankBranch();
    /// <summary>Selects a synthetic financial transaction category.</summary>
    /// <returns>A deterministic deposit, withdrawal, transfer, payment, refund, fee, interest, or check category.</returns>
    FinancialTransactionType TransactionType();
    /// <summary>Selects a project-authored synthetic transaction description.</summary>
    /// <returns>A description correlated with a deterministically selected transaction category.</returns>
    string TransactionDescription();
    /// <summary>Returns the project-authored description for a transaction category.</summary>
    /// <param name="type">The transaction category.</param>
    /// <returns>A synthetic description correlated with the category.</returns>
    string TransactionDescriptionFromType(FinancialTransactionType type);
    /// <summary>Generates a synthetic transaction-reference shape.</summary>
    /// <returns>A value beginning with <c>TXN-</c> followed by twelve digits.</returns>
    string TransactionReference();
    /// <summary>Generates a synthetic check-number shape.</summary>
    /// <returns>A value beginning with <c>CHK-</c> followed by eight digits.</returns>
    string CheckNumber();
    /// <summary>Generates a synthetic invoice-number shape.</summary>
    /// <returns>A value beginning with <c>INV-</c> followed by eight digits.</returns>
    string InvoiceNumber();
    /// <summary>Generates a positive exchange-rate-shaped decimal through 10000 with six decimal places.</summary>
    /// <remarks>The value is synthetic and is not a current or historical market quote.</remarks>
    /// <returns>A deterministic positive decimal value.</returns>
    decimal ExchangeRate();
    /// <summary>Generates a positive exchange-rate-shaped decimal within inclusive bounds.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>A deterministic value with six decimal places of resolution.</returns>
    decimal ExchangeRateFromMinimumAndMaximum(decimal minimum, decimal maximum);
    /// <summary>Generates a positive exchange-rate-shaped decimal with explicit precision and rounding.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>A deterministic value representable at the requested precision.</returns>
    decimal ExchangeRateFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(decimal minimum, decimal maximum, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Generates a synthetic exchange-rate record for two distinct current currencies.</summary>
    /// <returns>An ordered currency pair and positive rate with shape-only metadata.</returns>
    ExchangeRateRecord ExchangeRateRecord();
    /// <summary>Generates a synthetic exchange-rate record for an ordered currency pair.</summary>
    /// <param name="baseCurrencyCode">The ISO 4217 code representing one base unit.</param>
    /// <param name="quoteCurrencyCode">The distinct ISO 4217 quoted-currency code.</param>
    /// <returns>The requested pair and a positive six-place rate.</returns>
    ExchangeRateRecord ExchangeRateRecordFromBaseAndQuoteCurrencyCodes(string baseCurrencyCode, string quoteCurrencyCode);
    /// <summary>Creates a synthetic exchange-rate record from a supplied value.</summary>
    /// <param name="baseCurrencyCode">The ISO 4217 code representing one base unit.</param>
    /// <param name="quoteCurrencyCode">The distinct ISO 4217 quoted-currency code.</param>
    /// <param name="rate">The positive quoted units per base unit.</param>
    /// <param name="decimalPlaces">The number of decimal places from zero through 28.</param>
    /// <param name="roundingMode">The rounding rule.</param>
    /// <returns>The requested pair and rounded synthetic rate.</returns>
    ExchangeRateRecord ExchangeRateRecordFromBaseAndQuoteCurrencyCodesAndRateAndDecimalPlacesAndRoundingMode(string baseCurrencyCode, string quoteCurrencyCode, decimal rate, int decimalPlaces, MoneyRoundingMode roundingMode);
    /// <summary>Generates a positive quantity from one through one hundred.</summary>
    /// <returns>A deterministic integer quantity.</returns>
    int Quantity();
    /// <summary>Generates a positive quantity within inclusive configured bounds.</summary>
    /// <param name="minimum">The inclusive positive minimum.</param>
    /// <param name="maximum">The inclusive maximum, limited by the configured maximum count.</param>
    /// <returns>A deterministic integer quantity.</returns>
    int QuantityFromMinimumAndMaximum(int minimum, int maximum);
    /// <summary>Creates a thirty-day inclusive ledger interval ending at the captured reference time.</summary>
    /// <returns>An ordered interval and its duration.</returns>
    LedgerIntervalRecord LedgerInterval();
    /// <summary>Creates an inclusive ledger interval from supplied ordered bounds.</summary>
    /// <param name="start">The inclusive interval beginning.</param>
    /// <param name="end">The inclusive interval end, not earlier than <c>start</c>.</param>
    /// <returns>The supplied bounds and their elapsed duration.</returns>
    LedgerIntervalRecord LedgerIntervalFromStartAndEnd(DateTimeOffset start, DateTimeOffset end);
    /// <summary>Generates a decimal tax-rate field from zero through twenty-five percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    decimal TaxRate();
    /// <summary>Generates a decimal discount-rate field from zero through thirty percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    decimal DiscountRate();
    /// <summary>Generates a positive annual interest-rate field through twenty percent.</summary>
    /// <returns>A deterministic rate with four decimal places of resolution.</returns>
    decimal InterestRate();
    /// <summary>Generates a nonnegative fee field through 25 with two decimal places.</summary>
    /// <returns>A deterministic decimal fee.</returns>
    decimal Fee();
    /// <summary>Generates a nonnegative fee using a currency's ISO minor units.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A deterministic decimal fee rounded to the currency's minor units.</returns>
    decimal FeeFromCurrencyCode(string currencyCode);
    /// <summary>Generates a balance from negative 10000 through positive 10000 with two decimal places.</summary>
    /// <returns>A deterministic decimal balance.</returns>
    decimal Balance();
    /// <summary>Generates a balance using a currency's ISO minor units.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A deterministic decimal balance rounded to the currency's minor units.</returns>
    decimal BalanceFromCurrencyCode(string currencyCode);
    /// <summary>Calculates and rounds a tax amount using currency minor units.</summary>
    /// <param name="amount">The nonnegative amount to which the rate applies.</param>
    /// <param name="rate">The decimal rate from zero through one.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The amount multiplied by the rate and rounded to ISO minor units.</returns>
    decimal TaxAmount(decimal amount, decimal rate, string currencyCode);
    /// <summary>Calculates and rounds a discount amount using currency minor units.</summary>
    /// <param name="amount">The nonnegative amount to which the rate applies.</param>
    /// <param name="rate">The decimal rate from zero through one.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>The amount multiplied by the rate and rounded to ISO minor units.</returns>
    decimal DiscountAmount(decimal amount, decimal rate, string currencyCode);
    /// <summary>Calculates simple annual interest for a bounded day count using currency minor units.</summary>
    /// <param name="principal">The nonnegative principal.</param>
    /// <param name="annualRate">The decimal annual rate from zero through one.</param>
    /// <param name="days">The day count from zero through 3660.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>Simple interest calculated on a 365-day denominator and rounded to ISO minor units.</returns>
    decimal InterestAmount(decimal principal, decimal annualRate, int days, string currencyCode);
    /// <summary>Generates an explicitly synthetic, non-negotiable U.S. check fixture in USD.</summary>
    /// <returns>A correlated check number, ABA routing shape, account shape, payee, memo, amount, date, and safety fields.</returns>
    CheckRecord Check();
    /// <summary>Generates an explicitly synthetic, non-negotiable U.S. check fixture.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code for the numeric amount.</param>
    /// <returns>A correlated check fixture rounded to the currency's minor units.</returns>
    CheckRecord CheckFromCurrencyCode(string currencyCode);
    /// <summary>Generates an arithmetically correlated synthetic invoice fixture in USD.</summary>
    /// <returns>An unpaid invoice with subtotal, tax, discount, fee, total, balance, and dates.</returns>
    InvoiceRecord Invoice();
    /// <summary>Generates an arithmetically correlated synthetic invoice fixture.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>An unpaid invoice rounded to the currency's minor units.</returns>
    InvoiceRecord InvoiceFromCurrencyCode(string currencyCode);
    /// <summary>Generates a correlated synthetic payment calculation in USD.</summary>
    /// <returns>A payment-shaped record with invoice arithmetic, reference, type, description, and balances.</returns>
    PaymentRecord Payment();
    /// <summary>Generates a correlated synthetic payment calculation.</summary>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A payment-shaped record rounded to the currency's minor units.</returns>
    PaymentRecord PaymentFromCurrencyCode(string currencyCode);
    /// <summary>Generates an arithmetically correlated synthetic purchase order in USD.</summary>
    /// <returns>A quantity, numeric amounts, separate displays, dates, and safety fields.</returns>
    PurchaseOrderRecord PurchaseOrder();
    /// <summary>Generates an arithmetically correlated synthetic purchase order.</summary>
    /// <param name="quantity">The positive quantity, limited by the configured maximum count.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code.</param>
    /// <returns>A purchase order using the requested quantity and currency.</returns>
    PurchaseOrderRecord PurchaseOrderFromQuantityAndCurrencyCode(int quantity, string currencyCode);
    /// <summary>Generates a correlated synthetic account ledger with ten USD entries.</summary>
    /// <returns>An account, interval, balances, entries, separate displays, and safety fields.</returns>
    AccountLedgerRecord AccountLedger();
    /// <summary>Generates a correlated synthetic account ledger with explicit inputs.</summary>
    /// <param name="openingBalance">The opening numeric balance.</param>
    /// <param name="entryCount">The nonnegative entry count, limited by configured count and output bounds.</param>
    /// <param name="currencyCode">The case-insensitive three-letter ISO 4217 code shared by every entry.</param>
    /// <returns>An account ledger whose chronologically ordered entries reconcile to its closing balance.</returns>
    AccountLedgerRecord AccountLedgerFromOpeningBalanceAndEntryCountAndCurrencyCode(decimal openingBalance, int entryCount, string currencyCode);
    /// <summary>Generates a Bitcoin P2PKH Base58Check address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic mainnet-shaped address with a valid structural checksum.</returns>
    string BitcoinAddress();
    /// <summary>Generates a Litecoin P2PKH Base58Check address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic mainnet-shaped address with a valid structural checksum.</returns>
    string LitecoinAddress();
    /// <summary>Generates an Ethereum EIP-55 hexadecimal address shape.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <returns>A deterministic address shape with valid mixed-case checksum presentation.</returns>
    string EthereumAddress();
    /// <summary>Generates an address shape for a supported cryptocurrency.</summary>
    /// <remarks>The value has no corresponding private key and makes no ownership, balance, activity, or spendability claim.</remarks>
    /// <param name="cryptocurrency">The requested address family.</param>
    /// <returns>A deterministic address shape with a valid structural checksum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    string CryptocurrencyAddress(Cryptocurrency cryptocurrency);
    /// <summary>Validates a Bitcoin P2PKH Base58Check address shape.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the version, length, alphabet, and checksum are structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsBitcoinAddressValid(string value);
    /// <summary>Validates a Litecoin P2PKH Base58Check address shape.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the version, length, alphabet, and checksum are structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsLitecoinAddressValid(string value);
    /// <summary>Validates an Ethereum hexadecimal address shape and its mixed-case checksum when present.</summary>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the value is structurally valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsEthereumAddressValid(string value);
    /// <summary>Validates an address shape for a supported cryptocurrency.</summary>
    /// <param name="cryptocurrency">The address family to validate.</param>
    /// <param name="value">The address-shaped value.</param>
    /// <returns><c>true</c> when the value is structurally valid for the selected family.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    bool IsCryptocurrencyAddressValid(Cryptocurrency cryptocurrency, string value);
    /// <summary>Generates an explicitly synthetic cryptocurrency address fixture.</summary>
    /// <remarks>The fixture contains no private key and never claims ownership, activity, balance, or spendability.</remarks>
    /// <returns>A deterministic address fixture for a randomly selected supported family.</returns>
    CryptoAddressRecord CryptoAddress();
    /// <summary>Generates an explicitly synthetic cryptocurrency address fixture for a selected family.</summary>
    /// <remarks>The fixture contains no private key and never claims ownership, activity, balance, or spendability.</remarks>
    /// <param name="cryptocurrency">The requested address family.</param>
    /// <returns>A deterministic address fixture.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    CryptoAddressRecord CryptoAddressFromCryptocurrency(Cryptocurrency cryptocurrency);
    /// <summary>Generates a generic 256-bit lowercase hexadecimal hash shape.</summary>
    /// <remarks>The value is random fixture data and is not a transaction or block identifier from a cryptocurrency network.</remarks>
    /// <returns>A deterministic 64-character hexadecimal value.</returns>
    string CryptoHash();
    /// <summary>Generates a 256-bit hash shape using a supported cryptocurrency's conventional presentation.</summary>
    /// <remarks>The value is random fixture data and is not a transaction or block identifier from a cryptocurrency network.</remarks>
    /// <param name="cryptocurrency">The requested presentation family.</param>
    /// <returns>A deterministic hexadecimal hash shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the cryptocurrency or a configured limit is unsupported.</exception>
    string CryptoHashFromCryptocurrency(Cryptocurrency cryptocurrency);
    /// <summary>Selects a supported payment-card issuer shape.</summary>
    /// <returns>A deterministic issuer selection.</returns>
    CardIssuer CardIssuer();
    /// <summary>Identifies the supported issuer shape of a PAN.</summary>
    /// <param name="number">A decimal PAN with a supported prefix and length.</param>
    /// <returns>The matching issuer shape without making an account or authorization claim.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the number is empty, malformed, or unsupported.</exception>
    CardIssuer CardIssuerFromNumber(string number);
    /// <summary>Returns PAN-prefix, length, CVV, and safe test-fixture metadata for an issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>An independent issuer-pattern record.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    CardIssuerPattern CardPattern(CardIssuer issuer);
    /// <summary>Returns every supported payment-card issuer pattern.</summary>
    /// <returns>Independent pattern records in issuer order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    CardIssuerPattern[] CardPatterns();
    /// <summary>Selects a Luhn-valid documented test PAN from a supported issuer shape.</summary>
    /// <remarks>The value is test-only and does not represent an active or authorized account.</remarks>
    /// <returns>A deterministic Luhn-valid test PAN.</returns>
    string CardNumber();
    /// <summary>Returns the Luhn-valid documented test PAN for an issuer shape.</summary>
    /// <remarks>The value is test-only and does not represent an active or authorized account.</remarks>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>The issuer's Luhn-valid test PAN.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    string CardNumberFromIssuer(CardIssuer issuer);
    /// <summary>Selects a checksum-invalid mutation of a supported test PAN.</summary>
    /// <returns>A PAN-shaped value retaining its issuer shape but failing Luhn validation.</returns>
    string InvalidCardNumber();
    /// <summary>Returns a checksum-invalid mutation of an issuer's test PAN.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>A PAN-shaped value retaining its issuer shape but failing Luhn validation.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    string InvalidCardNumberFromIssuer(CardIssuer issuer);
    /// <summary>Reports whether a PAN has a supported issuer shape and valid Luhn digit.</summary>
    /// <remarks>Validation checks structure and checksum only, not account state, ownership, authorization, or acceptance.</remarks>
    /// <param name="number">The PAN text to inspect.</param>
    /// <returns><c>true</c> for a supported decimal shape with a valid Luhn digit; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    bool IsCardNumberValid(string number);
    /// <summary>Selects and masks a supported test PAN, retaining its final four digits.</summary>
    /// <returns>A masked test PAN.</returns>
    string MaskedCard();
    /// <summary>Masks an issuer's supported test PAN, retaining its final four digits.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>A masked test PAN.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    string MaskedCardFromIssuer(CardIssuer issuer);
    /// <summary>Masks a supported PAN shape, retaining its final four digits.</summary>
    /// <param name="number">A decimal PAN with a supported prefix and length.</param>
    /// <returns>A same-length string containing asterisks and the final four digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the number is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the number is empty, malformed, or unsupported.</exception>
    string MaskedCardFromNumber(string number);
    /// <summary>Generates a synthetic CVV shape for a randomly selected supported issuer.</summary>
    /// <returns>Three digits, or four digits when American Express is selected.</returns>
    string Cvv();
    /// <summary>Generates a synthetic CVV shape for a requested issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>Three or four decimal digits according to the issuer shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    string CvvFromIssuer(CardIssuer issuer);
    /// <summary>Generates a four-digit synthetic payment-card PIN.</summary>
    /// <returns>Exactly four decimal digits, retaining leading zeroes.</returns>
    string Pin();
    /// <summary>Generates a future synthetic card expiry in <c>MM/yy</c> form.</summary>
    /// <returns>An expiry from one through 60 months after the captured reference time.</returns>
    /// <exception cref="FakeException">Thrown when no future month is representable.</exception>
    string Expiry();
    /// <summary>Selects an explicitly synthetic uppercase cardholder label.</summary>
    /// <returns>An example, sample, or test cardholder label.</returns>
    string Cardholder();
    /// <summary>Generates a correlated, explicitly synthetic test-card record.</summary>
    /// <returns>Issuer, PAN, mask, CVV, PIN, expiry, cardholder, and test-safety fields.</returns>
    CardRecord Card();
    /// <summary>Generates a correlated, explicitly synthetic test-card record for an issuer.</summary>
    /// <param name="issuer">The requested supported issuer.</param>
    /// <returns>Issuer, PAN, mask, CVV, PIN, expiry, cardholder, and test-safety fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the issuer is unsupported.</exception>
    CardRecord CardFromIssuer(CardIssuer issuer);
}
