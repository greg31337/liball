using System;
using System.Globalization;

namespace fake;

internal class BankingLocaleProfile
{
    internal string Locale = string.Empty;
    internal string CountryCode = string.Empty;
    internal string AccountMask = string.Empty;
    internal string RoutingMask = string.Empty;
}

internal static class BankingGenerator
{
    const string Marker = "fake.synthetic-banking.v1";
    static readonly string[] BankNames = new string[]
    {
        "Example Cooperative Bank",
        "Sample Community Bank",
        "Reference Savings Bank",
        "Test Credit Union"
    };
    static readonly string[] BranchNames = new string[]
    {
        "Example Branch",
        "Sample Branch",
        "Reference Branch",
        "Test Branch"
    };
    static readonly string[] AccountPrefixes = new string[]
    {
        "Example",
        "Sample",
        "Reference",
        "Test"
    };

    internal static string Bic(
        GenerationContext context,
        FakeRandom random,
        bool includeBranch,
        int maximumCount,
        int maximumOutputSize)
    {
        string countryCode = Profile(context).CountryCode;
        return Bic(
            random,
            countryCode,
            includeBranch,
            maximumCount,
            maximumOutputSize);
    }

    internal static string Bic(
        FakeRandom random,
        string countryCode,
        bool includeBranch,
        int maximumCount,
        int maximumOutputSize)
    {
        string normalized = NormalizeCountryCode(countryCode, "countryCode");
        int length = includeBranch ? 11 : 8;
        Guard.Count(length, maximumCount, "bic");
        Guard.OutputSize(length, maximumOutputSize, "bic");
        char[] result = new char[length];
        int index = 0;
        while (index < 4)
        {
            result[index] = RandomLetter(random);
            index = index + 1;
        }
        result[4] = normalized[0];
        result[5] = normalized[1];
        index = 6;
        while (index < result.Length)
        {
            result[index] = RandomAlphaNumeric(random);
            index = index + 1;
        }
        return new string(result);
    }

    internal static bool IsBicValid(string value, int maximumCount)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, maximumCount, "value");
        if (value.Length != 8 && value.Length != 11)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!UpperAsciiLetter(value[index]) && !AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        if (!UpperAsciiLetter(value[4]) || !UpperAsciiLetter(value[5]))
        {
            return false;
        }
        string countryCode = value.Substring(4, 2);
        return LanguageRegistryStore.Current().Region(countryCode) != null &&
            !PrivateRegion(countryCode);
    }

    internal static string RoutingNumber(
        GenerationContext context,
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(9, maximumCount, "routingNumber");
        Guard.OutputSize(9, maximumOutputSize, "routingNumber");
        int prefix;
        if (context.SafetyModeValue == SafetyMode.Reserved)
        {
            prefix = 13 + random.IntFromMaximumExclusive(8);
        }
        else if (context.SafetyModeValue == SafetyMode.SyntheticPlausible)
        {
            prefix = 1 + random.IntFromMaximumExclusive(12);
        }
        else
        {
            prefix = random.IntFromMaximumExclusive(100);
        }
        char[] result = new char[9];
        string prefixText = prefix.ToString("D2", CultureInfo.InvariantCulture);
        result[0] = prefixText[0];
        result[1] = prefixText[1];
        int index = 2;
        while (index < 8)
        {
            result[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index = index + 1;
        }
        int sum = RoutingWeightedSum(result, 8);
        result[8] = (char)('0' + (10 - sum % 10) % 10);
        return new string(result);
    }

    internal static bool IsRoutingNumberValid(string value, int maximumCount)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, maximumCount, "value");
        if (value.Length != 9)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return RoutingWeightedSum(value.ToCharArray(), 9) % 10 == 0;
    }

    internal static string DomesticAccountNumber(
        GenerationContext context,
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        BankingLocaleProfile profile = Profile(context);
        return GenerateMask(
            random,
            profile.AccountMask,
            maximumCount,
            maximumOutputSize);
    }

    internal static string DomesticAccountMask(
        GenerationContext context,
        int maximumOutputSize)
    {
        string result = Profile(context).AccountMask;
        Guard.OutputSize(result.Length, maximumOutputSize, "domesticAccountMask");
        return result;
    }

    internal static BankAccountType AccountType(FakeRandom random)
    {
        return (BankAccountType)random.IntFromMaximumExclusive(4);
    }

    internal static string AccountTypeName(
        GenerationContext context,
        BankAccountType type,
        int maximumOutputSize)
    {
        string? language = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale").Language;
        string result = LocalizedType(language, type);
        Guard.OutputSize(result.Length, maximumOutputSize, "accountTypeName");
        return result;
    }

    internal static string AccountTypeName(
        GenerationContext context,
        FakeRandom random,
        int maximumOutputSize)
    {
        return AccountTypeName(
            context,
            AccountType(random),
            maximumOutputSize);
    }

    internal static string AccountName(
        GenerationContext context,
        FakeRandom random,
        int maximumOutputSize)
    {
        BankAccountType type = AccountType(random);
        string result = AccountPrefixes[random.IntFromMaximumExclusive(AccountPrefixes.Length)] + " " +
            LocalizedType(
                LanguageTagParser.Parse(context.CapturedOptions.Locale, "Locale").Language,
                type);
        Guard.OutputSize(result.Length, maximumOutputSize, "accountName");
        return result;
    }

    internal static string BankName(FakeRandom random, int maximumOutputSize)
    {
        string result = BankNames[random.IntFromMaximumExclusive(BankNames.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "bankName");
        return result;
    }

    internal static string BranchName(FakeRandom random, int maximumOutputSize)
    {
        string result = BranchNames[random.IntFromMaximumExclusive(BranchNames.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "branchName");
        return result;
    }

    internal static BankAccountRecord Account(
        GenerationContext context,
        FakeRandom random)
    {
        BankingLocaleProfile profile = Profile(context);
        BankAccountRecord result = new BankAccountRecord();
        result.Number = GenerateMask(
            random,
            profile.AccountMask,
            context.CapturedOptions.MaximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        result.Type = AccountType(random);
        result.TypeName = LocalizedType(
            LanguageTagParser.Parse(profile.Locale, "locale").Language,
            result.Type);
        result.Name = AccountPrefixes[random.IntFromMaximumExclusive(AccountPrefixes.Length)] + " " + result.TypeName;
        result.MaskedNumber = Mask(result.Number);
        result.Mask = profile.AccountMask;
        result.CountryCode = profile.CountryCode;
        result.Locale = profile.Locale;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsActiveAccount = false;
        Guard.OutputSize(
            AccountSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "bankAccount");
        return result;
    }

    internal static BankRecord Bank(
        GenerationContext context,
        FakeRandom random)
    {
        BankingLocaleProfile profile = Profile(context);
        BankRecord result = new BankRecord();
        result.Name = BankName(random, FakeLimits.DefaultMaximumOutputSize);
        result.Bic = Bic(
            random,
            profile.CountryCode,
            false,
            context.CapturedOptions.MaximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        result.CountryCode = profile.CountryCode;
        result.Locale = profile.Locale;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsRegisteredInstitution = false;
        Guard.OutputSize(
            BankSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "bank");
        return result;
    }

    internal static BankBranchRecord Branch(
        GenerationContext context,
        FakeRandom random)
    {
        BankingLocaleProfile profile = Profile(context);
        BankBranchRecord result = new BankBranchRecord();
        result.BankName = BankName(random, FakeLimits.DefaultMaximumOutputSize);
        result.BranchName = BranchName(random, FakeLimits.DefaultMaximumOutputSize);
        result.Bic = Bic(
            random,
            profile.CountryCode,
            true,
            context.CapturedOptions.MaximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        result.Bic8 = result.Bic.Substring(0, 8);
        result.BranchCode = result.Bic.Substring(8, 3);
        if (profile.CountryCode == "US")
        {
            result.DomesticRoutingNumber = RoutingNumber(
                context,
                random,
                context.CapturedOptions.MaximumCount,
                FakeLimits.DefaultMaximumOutputSize);
            result.IsAbaChecksumValid = IsRoutingNumberValid(
                result.DomesticRoutingNumber,
                FakeLimits.DefaultMaximumCount);
        }
        else
        {
            result.DomesticRoutingNumber = GenerateMask(
                random,
                profile.RoutingMask,
                context.CapturedOptions.MaximumCount,
                FakeLimits.DefaultMaximumOutputSize);
            result.IsAbaChecksumValid = false;
        }
        result.DomesticRoutingMask = profile.RoutingMask;
        result.CountryCode = profile.CountryCode;
        result.Locale = profile.Locale;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsRegisteredInstitution = false;
        Guard.OutputSize(
            BranchSize(result),
            context.CapturedOptions.MaximumOutputSize,
            "bankBranch");
        return result;
    }

    static BankingLocaleProfile Profile(GenerationContext context)
    {
        LanguageTag tag = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale");
        string countryCode = CountryCode(tag);
        BankingLocaleProfile result = new BankingLocaleProfile();
        result.Locale = tag.Normalized;
        result.CountryCode = countryCode;
        IbanFormat? iban = IbanRegistryStore.Current().FindCountryOrNull(countryCode);
        if (iban != null)
        {
            string expanded = ExpandPattern(iban.BbanPattern);
            result.AccountMask = expanded.Substring(
                iban.AccountCodeStart,
                iban.AccountCodeLength);
            int routingStart = iban.BankCodeStart;
            int routingEnd = iban.BankCodeStart + iban.BankCodeLength;
            if (iban.BranchCodeLength > 0)
            {
                if (iban.BranchCodeStart < routingStart)
                {
                    routingStart = iban.BranchCodeStart;
                }
                int branchEnd = iban.BranchCodeStart + iban.BranchCodeLength;
                if (branchEnd > routingEnd)
                {
                    routingEnd = branchEnd;
                }
            }
            result.RoutingMask = routingEnd > routingStart
                ? expanded.Substring(routingStart, routingEnd - routingStart)
                : "######";
            return result;
        }
        result.AccountMask = NonIbanAccountMask(countryCode);
        result.RoutingMask = NonIbanRoutingMask(countryCode);
        return result;
    }

    static string CountryCode(LanguageTag tag)
    {
        if (tag.Region != null && tag.Region.Length == 2 &&
            LanguageRegistryStore.Current().Region(tag.Region) != null)
        {
            return tag.Region;
        }
        string? language = tag.Language;
        if (language == "de") return "DE";
        if (language == "fr") return "FR";
        if (language == "es") return "ES";
        if (language == "pt") return "PT";
        if (language == "it") return "IT";
        if (language == "nl") return "NL";
        if (language == "pl") return "PL";
        if (language == "cs") return "CZ";
        if (language == "sv") return "SE";
        if (language == "nb") return "NO";
        if (language == "da") return "DK";
        if (language == "fi") return "FI";
        if (language == "ar") return "EG";
        if (language == "fa") return "IR";
        if (language == "he") return "IL";
        if (language == "hi") return "IN";
        if (language == "th") return "TH";
        if (language == "tr") return "TR";
        if (language == "ru") return "RU";
        if (language == "uk") return "UA";
        if (language == "el") return "GR";
        if (language == "ja") return "JP";
        if (language == "ko") return "KR";
        if (language == "zh") return "CN";
        if (language == "id") return "ID";
        if (language == "vi") return "VN";
        return "US";
    }

    static string NonIbanAccountMask(string countryCode)
    {
        if (countryCode == "AU") return "#########";
        if (countryCode == "CA") return "############";
        if (countryCode == "JP") return "#######";
        if (countryCode == "MX") return "##################";
        if (countryCode == "NZ") return "##-####-#######-###";
        return "############";
    }

    static string NonIbanRoutingMask(string countryCode)
    {
        if (countryCode == "AU") return "###-###";
        if (countryCode == "CA") return "########";
        if (countryCode == "JP") return "####";
        if (countryCode == "NZ") return "##-####";
        if (countryCode == "US") return "#########";
        return "######";
    }

    static string ExpandPattern(string pattern)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < pattern.Length)
        {
            int length = 0;
            while (index < pattern.Length && AsciiDigit(pattern[index]))
            {
                length = length * 10 + pattern[index] - '0';
                index = index + 1;
            }
            char kind = pattern[index + 1];
            char mask = kind == 'n' ? '#' : kind == 'a' ? '?' : '*';
            int count = 0;
            while (count < length)
            {
                result.Append(mask);
                count = count + 1;
            }
            index = index + 2;
        }
        return result.ToString();
    }

    static string GenerateMask(
        FakeRandom random,
        string mask,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(mask.Length, maximumCount, "domesticMask");
        Guard.OutputSize(mask.Length, maximumOutputSize, "domesticMask");
        char[] result = new char[mask.Length];
        int index = 0;
        while (index < mask.Length)
        {
            if (mask[index] == '#')
            {
                result[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            }
            else if (mask[index] == '?')
            {
                result[index] = RandomLetter(random);
            }
            else if (mask[index] == '*')
            {
                result[index] = RandomAlphaNumeric(random);
            }
            else
            {
                result[index] = mask[index];
            }
            index = index + 1;
        }
        return new string(result);
    }

    static string Mask(string value)
    {
        char[] result = value.ToCharArray();
        int visible = 4;
        int index = result.Length - 1;
        while (index >= 0)
        {
            if (UpperAsciiLetter(result[index]) || AsciiDigit(result[index]))
            {
                if (visible > 0)
                {
                    visible = visible - 1;
                }
                else
                {
                    result[index] = '*';
                }
            }
            index = index - 1;
        }
        return new string(result);
    }

    static string LocalizedType(string? language, BankAccountType type)
    {
        ValidateType(type);
        if (language == "de")
        {
            if (type == BankAccountType.Checking) return "Girokonto";
            if (type == BankAccountType.Savings) return "Sparkonto";
            if (type == BankAccountType.MoneyMarket) return "Tagesgeldkonto";
            return "Geschäftskonto";
        }
        if (language == "fr")
        {
            if (type == BankAccountType.Checking) return "Compte courant";
            if (type == BankAccountType.Savings) return "Compte d’épargne";
            if (type == BankAccountType.MoneyMarket) return "Compte monétaire";
            return "Compte professionnel";
        }
        if (language == "es")
        {
            if (type == BankAccountType.Checking) return "Cuenta corriente";
            if (type == BankAccountType.Savings) return "Cuenta de ahorro";
            if (type == BankAccountType.MoneyMarket) return "Cuenta monetaria";
            return "Cuenta empresarial";
        }
        if (language == "pt")
        {
            if (type == BankAccountType.Checking) return "Conta corrente";
            if (type == BankAccountType.Savings) return "Conta poupança";
            if (type == BankAccountType.MoneyMarket) return "Conta monetária";
            return "Conta empresarial";
        }
        if (language == "it")
        {
            if (type == BankAccountType.Checking) return "Conto corrente";
            if (type == BankAccountType.Savings) return "Conto di risparmio";
            if (type == BankAccountType.MoneyMarket) return "Conto monetario";
            return "Conto aziendale";
        }
        if (language == "nl")
        {
            if (type == BankAccountType.Checking) return "Betaalrekening";
            if (type == BankAccountType.Savings) return "Spaarrekening";
            if (type == BankAccountType.MoneyMarket) return "Geldmarktrekening";
            return "Zakelijke rekening";
        }
        if (type == BankAccountType.Checking) return "Checking";
        if (type == BankAccountType.Savings) return "Savings";
        if (type == BankAccountType.MoneyMarket) return "Money Market";
        return "Business Checking";
    }

    static void ValidateType(BankAccountType type)
    {
        switch (type)
        {
            case BankAccountType.Checking:
            case BankAccountType.Savings:
            case BankAccountType.MoneyMarket:
            case BankAccountType.Business:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "type",
                    type,
                    "type is not supported.");
        }
    }

    static string NormalizeCountryCode(string countryCode, string parameterName)
    {
        Guard.NotNullOrWhiteSpace(countryCode, parameterName);
        if (countryCode.Length != 2 ||
            !AsciiLetter(countryCode[0]) ||
            !AsciiLetter(countryCode[1]))
        {
            throw new ArgumentException(
                $"{parameterName} must contain exactly two ASCII letters.",
                parameterName);
        }
        string normalized = countryCode.ToUpperInvariant();
        LanguageRegistryEntry? entry = LanguageRegistryStore.Current().Region(normalized);
        if (entry == null || PrivateRegion(normalized))
        {
            throw new ArgumentException(
                $"{parameterName} '{countryCode}' is not a registered region code.",
                parameterName);
        }
        if (!string.IsNullOrEmpty(entry.PreferredValue))
        {
            return entry.PreferredValue.ToUpperInvariant();
        }
        return normalized;
    }

    static bool PrivateRegion(string countryCode)
    {
        if (countryCode == "AA" || countryCode == "ZZ")
        {
            return true;
        }
        if (countryCode[0] == 'Q' && countryCode[1] >= 'M' && countryCode[1] <= 'Z')
        {
            return true;
        }
        return countryCode[0] == 'X' && countryCode[1] >= 'A' && countryCode[1] <= 'Z';
    }

    static int RoutingWeightedSum(char[] value, int length)
    {
        int[] weights = new int[] { 3, 7, 1 };
        int sum = 0;
        int index = 0;
        while (index < length)
        {
            sum = sum + (value[index] - '0') * weights[index % 3];
            index = index + 1;
        }
        return sum;
    }

    static char RandomLetter(FakeRandom random)
    {
        return (char)('A' + random.IntFromMaximumExclusive(26));
    }

    static char RandomAlphaNumeric(FakeRandom random)
    {
        int value = random.IntFromMaximumExclusive(36);
        return value < 10
            ? (char)('0' + value)
            : (char)('A' + value - 10);
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool AsciiLetter(char value)
    {
        return UpperAsciiLetter(value) || value >= 'a' && value <= 'z';
    }

    static bool UpperAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    static int AccountSize(BankAccountRecord value)
    {
        return value.Name.Length + value.TypeName.Length + value.Number.Length +
            value.MaskedNumber.Length + value.Mask.Length + value.CountryCode.Length +
            value.Locale.Length + value.TestMarker.Length;
    }

    static int BankSize(BankRecord value)
    {
        return value.Name.Length + value.Bic.Length + value.CountryCode.Length +
            value.Locale.Length + value.TestMarker.Length;
    }

    static int BranchSize(BankBranchRecord value)
    {
        return value.BankName.Length + value.BranchName.Length + value.Bic.Length +
            value.Bic8.Length + value.BranchCode.Length + value.DomesticRoutingNumber.Length +
            value.DomesticRoutingMask.Length + value.CountryCode.Length + value.Locale.Length +
            value.TestMarker.Length;
    }
}
