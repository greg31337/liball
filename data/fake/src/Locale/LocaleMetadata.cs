using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class LocaleProfileData
{
    internal string EnglishName = string.Empty;
    internal string NativeName = string.Empty;
    internal string Language = string.Empty;
    internal string Script = string.Empty;
    internal WritingDirection Direction;
    internal string Territory = string.Empty;
    internal string Currency = string.Empty;
    internal string CallingCode = string.Empty;
    internal string[] TimeZones = new string[0];
    internal DateOrder DateOrder;
    internal string DecimalSeparator = string.Empty;
    internal string[] NativeProviders = new string[0];
    internal string[] FallbackProviders = new string[0];
}

internal static class LocaleMetadata
{
    const string ProfileField = "locale.profile";
    const string AddressField = "address.catalog";
    const string GenericAddressFormat = "{buildingNumber} {streetName} {streetType}\n{unitType} {unit}\n{district}\n{city}, {subdivision} {postalCode}\n{country}";

    internal static void Populate(
        GenerationContext context,
        LocaleCatalog catalog,
        LanguageTag requested,
        LocaleInfo info)
    {
        string[] candidates = LocaleResolver.CandidateLocales(context, requested);
        LocaleCatalogEntry? profileEntry = Find(catalog, candidates, ProfileField);
        if (profileEntry != null)
        {
            LocaleProfileData profile = ParseProfile(
                profileEntry.Value,
                context.CapturedOptions.MaximumCount,
                context.CapturedOptions.MaximumOutputSize);
            ApplyProfile(context, requested.Normalized, profileEntry, profile, info);
        }

        LocaleCatalogEntry? addressEntry = Find(catalog, candidates, AddressField);
        if (addressEntry != null)
        {
            Guard.OutputSize(
                GenericAddressFormat.Length,
                context.CapturedOptions.MaximumOutputSize,
                "addressFormat");
            info.AddressFormat = GenericAddressFormat;
            info.AddressFormatSourceLocale = addressEntry.Locale;
            info.AddressFormatUsedFallback = addressEntry.Locale != requested.Normalized;
            info.AddressFormatDataVersion = addressEntry.DataVersion;
            info.AddressFormatProvenance = addressEntry.Provenance;
        }
    }

    internal static LocaleNameRecord[] Scripts(
        GenerationContext context,
        LocaleCatalog catalog)
    {
        string[] locales = catalog.Supported();
        Dictionary<string, LocaleNameRecord> records = new Dictionary<string, LocaleNameRecord>(StringComparer.Ordinal);
        int index = 0;
        while (index < locales.Length)
        {
            LocaleCatalogEntry? entry = catalog.Find(locales[index], ProfileField);
            if (entry != null)
            {
                LocaleProfileData profile = ParseProfile(
                    entry.Value,
                    context.CapturedOptions.MaximumCount,
                    context.CapturedOptions.MaximumOutputSize);
                if (!records.ContainsKey(profile.Script))
                {
                    records[profile.Script] = Record(
                        profile.Script,
                        ScriptName(profile.Script),
                        entry,
                        false);
                }
            }
            index = index + 1;
        }
        return Ordered(context, records, "scripts");
    }

    internal static LocaleNameRecord[] Territories(
        GenerationContext context,
        LocaleCatalog catalog)
    {
        string[] locales = catalog.Supported();
        Dictionary<string, LocaleNameRecord> records = new Dictionary<string, LocaleNameRecord>(StringComparer.Ordinal);
        int index = 0;
        while (index < locales.Length)
        {
            LocaleCatalogEntry? entry = catalog.Find(locales[index], ProfileField);
            if (entry != null)
            {
                LocaleProfileData profile = ParseProfile(
                    entry.Value,
                    context.CapturedOptions.MaximumCount,
                    context.CapturedOptions.MaximumOutputSize);
                records[profile.Territory] = Record(
                    profile.Territory,
                    TerritoryName(profile.NativeName),
                    entry,
                    false);
            }
            index = index + 1;
        }
        return Ordered(context, records, "territories");
    }

    static void ApplyProfile(
        GenerationContext context,
        string requestedLocale,
        LocaleCatalogEntry entry,
        LocaleProfileData profile,
        LocaleInfo info)
    {
        info.HasProfile = true;
        info.EnglishName = profile.EnglishName;
        info.NativeName = profile.NativeName;
        info.LanguageName = LanguageName(profile.NativeName);
        info.DefaultScript = profile.Script;
        info.ScriptName = ScriptName(profile.Script);
        info.TerritoryName = TerritoryName(profile.NativeName);
        info.Direction = profile.Direction;
        info.DefaultCurrencyCode = profile.Currency;
        info.DefaultCurrencyName = CurrencyRegistryStore.Current().Find(
            profile.Currency,
            context.CapturedOptions.MaximumOutputSize).Name;
        info.CallingCode = profile.CallingCode;
        info.TimeZoneCodes = Copy(profile.TimeZones);
        info.TimeZoneNames = TimeZoneNames(profile.TimeZones);
        info.DateOrder = profile.DateOrder;
        info.DecimalSeparator = profile.DecimalSeparator;
        info.NativeProviders = Copy(profile.NativeProviders);
        info.FallbackProviders = Copy(profile.FallbackProviders);
        info.AvailableProviders = Combined(
            profile.NativeProviders,
            profile.FallbackProviders,
            context.CapturedOptions.MaximumCount);
        info.ProfileSourceLocale = entry.Locale;
        info.ProfileUsedFallback = entry.Locale != requestedLocale;
        info.ProfileDataVersion = entry.DataVersion;
        info.ProfileProvenance = entry.Provenance;
    }

    static LocaleProfileData ParseProfile(
        object value,
        int maximumCount,
        int maximumOutputSize)
    {
        if (value.GetType() != default(JsonElement).GetType())
        {
            throw FakeException.Create("Locale profile data must be a JSON object.");
        }
        JsonElement root = (JsonElement)value;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw FakeException.Create("Locale profile data must be a JSON object.");
        }
        LocaleProfileData result = new LocaleProfileData();
        result.EnglishName = Text(Required(root, "englishName"), "englishName", maximumOutputSize);
        result.NativeName = Text(Required(root, "nativeName"), "nativeName", maximumOutputSize);
        result.Language = Text(Required(root, "language"), "language", maximumOutputSize);
        result.Script = Text(Required(root, "script"), "script", maximumOutputSize);
        result.Direction = Direction(Text(Required(root, "direction"), "direction", maximumOutputSize));
        result.Territory = Text(Required(root, "territory"), "territory", maximumOutputSize);
        result.Currency = Text(Required(root, "currency"), "currency", maximumOutputSize);
        result.CallingCode = Text(Required(root, "callingCode"), "callingCode", maximumOutputSize);
        result.TimeZones = TextArray(Required(root, "timeZones"), "timeZones", maximumCount, maximumOutputSize);
        result.DateOrder = Order(Text(Required(root, "dateOrder"), "dateOrder", maximumOutputSize));
        result.DecimalSeparator = Text(Required(root, "decimalSeparator"), "decimalSeparator", maximumOutputSize);
        result.NativeProviders = TextArray(Required(root, "nativeProviders"), "nativeProviders", maximumCount, maximumOutputSize);
        result.FallbackProviders = TextArray(Required(root, "fallbackProviders"), "fallbackProviders", maximumCount, maximumOutputSize);
        ValidateProfile(result);
        return result;
    }

    static JsonElement Required(JsonElement value, string name)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        throw FakeException.Create($"Locale profile data is missing required member '{name}'.");
    }

    static string Text(JsonElement value, string name, int maximumOutputSize)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw FakeException.Create($"Locale profile member '{name}' must be a string.");
        }
        string? result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw FakeException.Create($"Locale profile member '{name}' cannot be empty or whitespace.");
        }
        Guard.OutputSize(result.Length, maximumOutputSize, name);
        return result;
    }

    static string[] TextArray(
        JsonElement value,
        string name,
        int maximumCount,
        int maximumOutputSize)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw FakeException.Create($"Locale profile member '{name}' must be an array.");
        }
        Guard.Count(value.GetArrayLength(), maximumCount, name);
        string[] result = new string[value.GetArrayLength()];
        int size = 0;
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result[index] = Text(item, name, maximumOutputSize);
            size = size + result[index].Length;
            index = index + 1;
        }
        Guard.OutputSize(size, maximumOutputSize, name);
        return result;
    }

    static void ValidateProfile(LocaleProfileData profile)
    {
        if (profile.Language.Length < 2 || profile.Language.Length > 8)
        {
            throw FakeException.Create("Locale profile language code is malformed.");
        }
        if (profile.Script.Length != 4)
        {
            throw FakeException.Create("Locale profile script code is malformed.");
        }
        if (profile.Territory.Length != 2)
        {
            throw FakeException.Create("Locale profile territory code is malformed.");
        }
        if (profile.Currency.Length != 3)
        {
            throw FakeException.Create("Locale profile currency code is malformed.");
        }
        if (profile.CallingCode.Length < 2 || profile.CallingCode[0] != '+')
        {
            throw FakeException.Create("Locale profile calling code is malformed.");
        }
        if (profile.TimeZones.Length == 0)
        {
            throw FakeException.Create("Locale profile must declare at least one time zone.");
        }
    }

    static WritingDirection Direction(string value)
    {
        if (value == "ltr")
        {
            return WritingDirection.LeftToRight;
        }
        if (value == "rtl")
        {
            return WritingDirection.RightToLeft;
        }
        throw FakeException.Create($"Locale profile direction '{value}' is unsupported.");
    }

    static DateOrder Order(string value)
    {
        if (value == "mdy")
        {
            return DateOrder.MonthDayYear;
        }
        if (value == "dmy")
        {
            return DateOrder.DayMonthYear;
        }
        if (value == "ymd")
        {
            return DateOrder.YearMonthDay;
        }
        throw FakeException.Create($"Locale profile date order '{value}' is unsupported.");
    }

    static LocaleCatalogEntry? Find(
        LocaleCatalog catalog,
        string[] candidates,
        string field)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            LocaleCatalogEntry? entry = catalog.Find(candidates[index], field);
            if (entry != null)
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    static LocaleNameRecord Record(
        string code,
        string name,
        LocaleCatalogEntry entry,
        bool usedFallback)
    {
        LocaleNameRecord result = new LocaleNameRecord();
        result.Code = code;
        result.Name = name;
        result.SourceLocale = entry.Locale;
        result.UsedFallback = usedFallback;
        result.DataVersion = entry.DataVersion;
        result.Provenance = entry.Provenance;
        return result;
    }

    static LocaleNameRecord[] Ordered(
        GenerationContext context,
        Dictionary<string, LocaleNameRecord> records,
        string parameterName)
    {
        Guard.Count(records.Count, context.CapturedOptions.MaximumCount, parameterName);
        string[] codes = new string[records.Count];
        records.Keys.CopyTo(codes, 0);
        Array.Sort(codes, StringComparer.Ordinal);
        LocaleNameRecord[] result = new LocaleNameRecord[codes.Length];
        int size = 0;
        int index = 0;
        while (index < codes.Length)
        {
            LocaleNameRecord value = records[codes[index]];
            result[index] = Record(
                value.Code,
                value.Name,
                Entry(value),
                value.UsedFallback);
            size = size + RecordSize(result[index]);
            index = index + 1;
        }
        Guard.OutputSize(size, context.CapturedOptions.MaximumOutputSize, parameterName);
        return result;
    }

    static LocaleCatalogEntry Entry(LocaleNameRecord value)
    {
        LocaleCatalogEntry result = new LocaleCatalogEntry();
        result.Locale = value.SourceLocale;
        result.DataVersion = value.DataVersion;
        result.Provenance = value.Provenance;
        return result;
    }

    static int RecordSize(LocaleNameRecord value)
    {
        return value.Code.Length + value.Name.Length + value.SourceLocale.Length +
            value.DataVersion.Length + value.Provenance.Length;
    }

    static string[] Copy(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static string[] Combined(string[] first, string[] second, int maximumCount)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < first.Length)
        {
            values.Add(first[index]);
            index = index + 1;
        }
        index = 0;
        while (index < second.Length)
        {
            values.Add(second[index]);
            index = index + 1;
        }
        Guard.Count(values.Count, maximumCount, "availableProviders");
        string[] result = new string[values.Count];
        values.CopyTo(result);
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    static string[] TimeZoneNames(string[] codes)
    {
        string[] result = new string[codes.Length];
        int index = 0;
        while (index < codes.Length)
        {
            int separator = codes[index].LastIndexOf('/');
            string name = separator < 0
                ? codes[index]
                : codes[index].Substring(separator + 1);
            result[index] = name.Replace('_', ' ');
            index = index + 1;
        }
        return result;
    }

    static string LanguageName(string nativeName)
    {
        int separator = nativeName.IndexOf(" (", StringComparison.Ordinal);
        if (separator < 0)
        {
            separator = nativeName.IndexOf('（');
        }
        if (separator < 0)
        {
            return nativeName;
        }
        return nativeName.Substring(0, separator).Trim();
    }

    static string TerritoryName(string nativeName)
    {
        int opening = nativeName.LastIndexOf('(');
        int closing = nativeName.LastIndexOf(')');
        if (opening < 0 || closing <= opening)
        {
            opening = nativeName.LastIndexOf('（');
            closing = nativeName.LastIndexOf('）');
        }
        if (opening < 0 || closing <= opening)
        {
            return nativeName;
        }
        return nativeName.Substring(opening + 1, closing - opening - 1).Trim();
    }

    static string ScriptName(string code)
    {
        if (code == "Arab")
        {
            return "Arabic";
        }
        if (code == "Cyrl")
        {
            return "Cyrillic";
        }
        if (code == "Deva")
        {
            return "Devanagari";
        }
        if (code == "Grek")
        {
            return "Greek";
        }
        if (code == "Hans")
        {
            return "Simplified Han";
        }
        if (code == "Hant")
        {
            return "Traditional Han";
        }
        if (code == "Hebr")
        {
            return "Hebrew";
        }
        if (code == "Jpan")
        {
            return "Japanese";
        }
        if (code == "Kore")
        {
            return "Korean";
        }
        if (code == "Latn")
        {
            return "Latin";
        }
        if (code == "Thai")
        {
            return "Thai";
        }
        return code;
    }
}
