using System;
using System.Globalization;

namespace fake;

internal static class MoneyGenerator
{
    const decimal DefaultMaximum = 1000m;
    const int DefaultDecimalPlaces = 2;

    internal static decimal Amount(
        FakeRandom random,
        decimal minimum,
        decimal maximum,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        ValidateDecimalPlaces(decimalPlaces);
        MidpointRounding rounding = Rounding(roundingMode);
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                "maximum",
                maximum,
                "maximum must be greater than or equal to minimum.");
        }
        decimal lower = decimal.Round(
            minimum,
            decimalPlaces,
            MidpointRounding.ToPositiveInfinity);
        decimal upper = decimal.Round(
            maximum,
            decimalPlaces,
            MidpointRounding.ToNegativeInfinity);
        if (lower > upper)
        {
            throw new ArgumentOutOfRangeException(
                "maximum",
                maximum,
                "The interval contains no value at the requested decimal precision.");
        }
        decimal raw = random.DecimalFromMinimumAndMaximumAndMaximumInclusive(lower, upper, true);
        decimal result = decimal.Round(raw, decimalPlaces, rounding);
        if (result < lower)
        {
            return lower;
        }
        if (result > upper)
        {
            return upper;
        }
        return result;
    }

    internal static decimal DefaultAmount(FakeRandom random)
    {
        return Amount(
            random,
            decimal.Zero,
            DefaultMaximum,
            DefaultDecimalPlaces,
            MoneyRoundingMode.ToEven);
    }

    internal static decimal Price(
        FakeRandom random,
        decimal minimum,
        decimal maximum,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        if (minimum < decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                "minimum",
                minimum,
                "minimum price cannot be negative.");
        }
        return Amount(
            random,
            minimum,
            maximum,
            decimalPlaces,
            roundingMode);
    }

    internal static decimal DefaultPrice(FakeRandom random)
    {
        return Price(
            random,
            0.01m,
            DefaultMaximum,
            DefaultDecimalPlaces,
            MoneyRoundingMode.ToEven);
    }

    internal static MoneyRecord RandomMoney(
        GenerationContext context,
        FakeRandom random)
    {
        CurrencyRecord currency = CurrencyRegistryStore.Current().Choose(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        int decimalPlaces = Places(currency);
        decimal amount = Amount(
            random,
            decimal.Zero,
            DefaultMaximum,
            decimalPlaces,
            MoneyRoundingMode.ToEven);
        return Create(
            context,
            amount,
            currency,
            decimalPlaces,
            MoneyRoundingMode.ToEven);
    }

    internal static MoneyRecord RandomMoney(
        GenerationContext context,
        FakeRandom random,
        string currencyCode)
    {
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        int decimalPlaces = Places(currency);
        decimal amount = Amount(
            random,
            decimal.Zero,
            DefaultMaximum,
            decimalPlaces,
            MoneyRoundingMode.ToEven);
        return Create(
            context,
            amount,
            currency,
            decimalPlaces,
            MoneyRoundingMode.ToEven);
    }

    internal static MoneyRecord Money(
        GenerationContext context,
        decimal amount,
        string currencyCode,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        ValidateDecimalPlaces(decimalPlaces);
        MidpointRounding rounding = Rounding(roundingMode);
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        decimal rounded = decimal.Round(amount, decimalPlaces, rounding);
        return Create(
            context,
            rounded,
            currency,
            decimalPlaces,
            roundingMode);
    }

    internal static MoneyRecord Money(
        GenerationContext context,
        decimal amount,
        string currencyCode,
        MoneyRoundingMode roundingMode)
    {
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        int decimalPlaces = Places(currency);
        MidpointRounding rounding = Rounding(roundingMode);
        decimal rounded = decimal.Round(amount, decimalPlaces, rounding);
        return Create(
            context,
            rounded,
            currency,
            decimalPlaces,
            roundingMode);
    }

    internal static string Display(
        GenerationContext context,
        decimal amount,
        string currencyCode,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        ValidateDecimalPlaces(decimalPlaces);
        MidpointRounding rounding = Rounding(roundingMode);
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        decimal rounded = decimal.Round(amount, decimalPlaces, rounding);
        string symbol = Symbol(context.CapturedOptions.Locale, currency.Code);
        string result = Format(
            rounded,
            symbol,
            decimalPlaces,
            context.CapturedOptions.Locale);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "currencyDisplay");
        return result;
    }

    internal static string Display(
        GenerationContext context,
        decimal amount,
        string currencyCode,
        MoneyRoundingMode roundingMode)
    {
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        return Display(
            context,
            amount,
            currency.Code,
            Places(currency),
            roundingMode);
    }

    internal static string Symbol(
        string locale,
        string currencyCode)
    {
        CurrencyRecord currency = CurrencyRegistryStore.Current().Find(
            currencyCode,
            FakeLimits.DefaultMaximumOutputSize);
        CultureInfo culture = Culture(locale);
        if (culture.Name.Length != 0 && !culture.IsNeutralCulture)
        {
            RegionInfo region = new RegionInfo(culture.Name);
            if (string.Equals(
                region.ISOCurrencySymbol,
                currency.Code,
                StringComparison.OrdinalIgnoreCase))
            {
                return culture.NumberFormat.CurrencySymbol;
            }
        }
        return currency.Code;
    }

    internal static int Places(CurrencyRecord currency)
    {
        return currency.HasMinorUnits
            ? currency.MinorUnits
            : DefaultDecimalPlaces;
    }

    static MoneyRecord Create(
        GenerationContext context,
        decimal amount,
        CurrencyRecord currency,
        int decimalPlaces,
        MoneyRoundingMode roundingMode)
    {
        string locale = context.CapturedOptions.Locale;
        string symbol = Symbol(locale, currency.Code);
        string display = Format(amount, symbol, decimalPlaces, locale);
        MoneyRecord result = new MoneyRecord();
        result.Amount = amount;
        result.CurrencyCode = currency.Code;
        result.CurrencyNumericCode = currency.NumericCode;
        result.CurrencyName = currency.Name;
        result.CurrencySymbol = symbol;
        result.DecimalPlaces = decimalPlaces;
        result.HasMinorUnits = currency.HasMinorUnits;
        result.RoundingMode = roundingMode;
        result.Locale = locale;
        result.Display = display;
        int size = result.CurrencyCode.Length +
            result.CurrencyNumericCode.Length +
            result.CurrencyName.Length +
            result.CurrencySymbol.Length +
            result.Locale.Length +
            result.Display.Length;
        Guard.OutputSize(
            size,
            context.CapturedOptions.MaximumOutputSize,
            "money");
        return result;
    }

    static string Format(
        decimal amount,
        string symbol,
        int decimalPlaces,
        string locale)
    {
        CultureInfo culture = Culture(locale);
        NumberFormatInfo format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.CurrencySymbol = symbol;
        format.CurrencyDecimalDigits = decimalPlaces;
        return amount.ToString("C", format);
    }

    static CultureInfo Culture(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        if (tag.Language == null)
        {
            return CultureInfo.InvariantCulture;
        }
        string full = tag.Language;
        if (tag.Script != null)
        {
            full = full + "-" + tag.Script;
        }
        if (tag.Region != null)
        {
            full = full + "-" + tag.Region;
        }
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        CultureInfo? result = FindCulture(cultures, full);
        if (result != null)
        {
            return result;
        }
        if (tag.Region != null)
        {
            result = FindCulture(cultures, tag.Language + "-" + tag.Region);
            if (result != null)
            {
                return result;
            }
        }
        if (tag.Script != null)
        {
            result = FindCulture(cultures, tag.Language + "-" + tag.Script);
            if (result != null)
            {
                return result;
            }
        }
        result = FindCulture(cultures, tag.Language);
        return result == null ? CultureInfo.InvariantCulture : result;
    }

    static CultureInfo? FindCulture(CultureInfo[] cultures, string name)
    {
        int index = 0;
        while (index < cultures.Length)
        {
            if (string.Equals(
                cultures[index].Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return cultures[index];
            }
            index = index + 1;
        }
        return null;
    }

    static MidpointRounding Rounding(MoneyRoundingMode roundingMode)
    {
        switch (roundingMode)
        {
            case MoneyRoundingMode.ToEven:
                return MidpointRounding.ToEven;
            case MoneyRoundingMode.AwayFromZero:
                return MidpointRounding.AwayFromZero;
            case MoneyRoundingMode.ToZero:
                return MidpointRounding.ToZero;
            case MoneyRoundingMode.ToNegativeInfinity:
                return MidpointRounding.ToNegativeInfinity;
            case MoneyRoundingMode.ToPositiveInfinity:
                return MidpointRounding.ToPositiveInfinity;
            default:
                throw new ArgumentOutOfRangeException(
                    "roundingMode",
                    roundingMode,
                    "roundingMode is not supported.");
        }
    }

    static void ValidateDecimalPlaces(int decimalPlaces)
    {
        if (decimalPlaces < 0 || decimalPlaces > 28)
        {
            throw new ArgumentOutOfRangeException(
                "decimalPlaces",
                decimalPlaces,
                "decimalPlaces must be between zero and 28, inclusive.");
        }
    }
}
