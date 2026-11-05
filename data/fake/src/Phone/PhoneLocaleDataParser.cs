using System;
using System.Text.Json;

namespace fake;

internal static class PhoneLocaleDataParser
{
    const string CatalogField = "phone.catalog";

    internal static PhoneLocaleMetadata Resolve(GenerationContext context)
    {
        FallbackTrace trace = LocaleResolver.Trace(
            context,
            context.CapturedOptions.Locale,
            CatalogField,
            context.LocaleValues());
        object source = trace.Value;
        string json = JsonSerializer.Serialize(source);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireObject(root, CatalogField);
            PhoneLocaleMetadata result = new PhoneLocaleMetadata();
            result.RequestedLocale = trace.RequestedLocale;
            result.SourceLocale = trace.SourceLocale;
            result.UsedLocaleFallback = trace.UsedFallback;
            result.RegionCode = RequiredString(context, root, "regionCode", CatalogField);
            result.CallingCode = RequiredString(context, root, "callingCode", CatalogField);
            result.InternationalDialingPrefix = RequiredString(
                context,
                root,
                "internationalDialingPrefix",
                CatalogField);
            result.NationalPrefix = StringValue(
                context,
                Required(root, "nationalPrefix", CatalogField),
                CatalogField + ".nationalPrefix",
                true);
            result.AreaCodeMask = RequiredString(context, root, "areaCodeMask", CatalogField);
            result.SubscriberMask = RequiredString(context, root, "subscriberMask", CatalogField);
            result.ExtensionPrefix = RequiredString(context, root, "extensionPrefix", CatalogField);
            result.MinimumExtensionDigits = RequiredInteger(
                root,
                "minimumExtensionDigits",
                CatalogField);
            result.MaximumExtensionDigits = RequiredInteger(
                root,
                "maximumExtensionDigits",
                CatalogField);
            result.SupportsPhoneWords = OptionalBoolean(
                root,
                "supportsPhoneWords",
                CatalogField,
                false);
            string safeRangeField = StringValue(
                context,
                Required(root, "safeRangeField", CatalogField),
                CatalogField + ".safeRangeField",
                true);
            ValidateCatalog(result);
            result.Formats = Formats(
                context,
                Required(root, "formats", CatalogField),
                CatalogField + ".formats");
            result.SafeRanges = SafeRanges(context, result, safeRangeField);
            result.HasAuthoritativeSafeRange = result.SafeRanges.Length != 0;
            return result;
        }
    }

    internal static void ValidateOutput(
        PhoneLocaleMetadata metadata,
        int maximumOutputSize)
    {
        Guard.OutputSize(metadata.RegionCode.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.CallingCode.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.InternationalDialingPrefix.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.NationalPrefix.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.AreaCodeMask.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.SubscriberMask.Length, maximumOutputSize, "phoneMetadata");
        Guard.OutputSize(metadata.ExtensionPrefix.Length, maximumOutputSize, "phoneMetadata");
        int index = 0;
        while (index < metadata.Formats.Length)
        {
            PhoneFormatRule rule = metadata.Formats[index];
            Guard.OutputSize(rule.AreaCodeMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(rule.SubscriberMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(rule.NationalMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(rule.InternationalMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(rule.E164Mask.Length, maximumOutputSize, "phoneMetadata");
            int areaIndex = 0;
            while (areaIndex < rule.AreaCodes.Length)
            {
                Guard.OutputSize(
                    rule.AreaCodes[areaIndex].Length,
                    maximumOutputSize,
                    "phoneMetadata");
                areaIndex = areaIndex + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < metadata.SafeRanges.Length)
        {
            PhoneSafeRange range = metadata.SafeRanges[index];
            Guard.OutputSize(range.AreaCodeMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.Exchange.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.FirstSubscriber.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.LastSubscriber.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.NationalMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.InternationalMask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.E164Mask.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.Authority.Length, maximumOutputSize, "phoneMetadata");
            Guard.OutputSize(range.SourceVersion.Length, maximumOutputSize, "phoneMetadata");
            index = index + 1;
        }
    }

    static PhoneFormatRule[] Formats(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(values, path);
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "phoneFormats");
        if (count == 0)
        {
            Invalid(path + " must not be empty.");
        }
        PhoneFormatRule[] result = new PhoneFormatRule[count];
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireObject(value, itemPath);
            PhoneFormatRule rule = new PhoneFormatRule();
            rule.Category = Category(RequiredString(context, value, "category", itemPath), itemPath);
            rule.AreaCodeMask = RequiredString(context, value, "areaCodeMask", itemPath);
            rule.SubscriberMask = RequiredString(context, value, "subscriberMask", itemPath);
            rule.NationalMask = RequiredString(context, value, "nationalMask", itemPath);
            rule.InternationalMask = RequiredString(context, value, "internationalMask", itemPath);
            rule.E164Mask = RequiredString(context, value, "e164Mask", itemPath);
            rule.AreaCodes = AreaCodes(
                context,
                Required(value, "areaCodes", itemPath),
                itemPath + ".areaCodes");
            rule.AllowsExtension = RequiredBoolean(value, "allowsExtension", itemPath);
            rule.DistinguishableByNumber = RequiredBoolean(
                value,
                "distinguishableByNumber",
                itemPath);
            ValidateGenerationMask(rule.AreaCodeMask, itemPath + ".areaCodeMask", false);
            ValidateGenerationMask(rule.SubscriberMask, itemPath + ".subscriberMask", true);
            ValidateMask(rule.NationalMask, itemPath + ".nationalMask", false);
            ValidateMask(rule.InternationalMask, itemPath + ".internationalMask", true);
            ValidateE164Mask(rule.E164Mask, itemPath + ".e164Mask");
            int previous = 0;
            while (previous < index)
            {
                if (result[previous].Category == rule.Category)
                {
                    Invalid(itemPath + " duplicates a phone category.");
                }
                previous = previous + 1;
            }
            result[index] = rule;
            index = index + 1;
        }
        bool hasGeneral = false;
        index = 0;
        while (index < result.Length)
        {
            if (result[index].Category == PhoneNumberCategory.General)
            {
                hasGeneral = true;
            }
            index = index + 1;
        }
        if (!hasGeneral)
        {
            Invalid(path + " must define the general phone category.");
        }
        return result;
    }

    static string[] AreaCodes(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(values, path);
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "phoneAreaCodes");
        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            string value = StringValue(
                context,
                values[index],
                path + "[" + index + "]",
                false);
            ValidateDigits(value, path + "[" + index + "]", 1, 6);
            int previous = 0;
            while (previous < index)
            {
                if (result[previous] == value)
                {
                    Invalid(path + " contains a duplicate area code.");
                }
                previous = previous + 1;
            }
            result[index] = value;
            index = index + 1;
        }
        return result;
    }

    static PhoneSafeRange[] SafeRanges(
        GenerationContext context,
        PhoneLocaleMetadata catalog,
        string field)
    {
        if (field.Length == 0)
        {
            return new PhoneSafeRange[0];
        }
        FallbackTrace trace = LocaleResolver.Trace(
            context,
            context.CapturedOptions.Locale,
            field,
            context.LocaleValues());
        string json = JsonSerializer.Serialize(trace.Value);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireObject(root, field);
            string exchange = RequiredString(context, root, "exchange", field);
            string first = RequiredString(context, root, "first", field);
            string last = RequiredString(context, root, "last", field);
            ValidateDigits(exchange, field + ".exchange", 1, 6);
            ValidateDigits(first, field + ".first", 1, 10);
            ValidateDigits(last, field + ".last", 1, 10);
            if (first.Length != last.Length || string.CompareOrdinal(first, last) > 0)
            {
                Invalid(field + " subscriber bounds must have equal lengths in ascending order.");
            }
            int commonLength = CommonPrefix(first, last);
            ValidateMaskableRange(first, last, commonLength, field);
            string subscriberMask = first.Substring(0, commonLength) +
                new string('#', first.Length - commonLength);
            PhoneSafeRange range = new PhoneSafeRange();
            range.Category = PhoneNumberCategory.General;
            range.AreaCodeMask = catalog.AreaCodeMask;
            range.Exchange = exchange;
            range.FirstSubscriber = first;
            range.LastSubscriber = last;
            range.NationalMask = "(" + catalog.AreaCodeMask + ") " +
                exchange + "-" + subscriberMask;
            range.InternationalMask = catalog.CallingCode + " " +
                catalog.AreaCodeMask + " " + exchange + " " + subscriberMask;
            range.E164Mask = catalog.CallingCode + catalog.AreaCodeMask +
                exchange + subscriberMask;
            range.Authority = ProvenanceSource(trace.Provenance);
            range.SourceVersion = ProvenanceVersion(trace.Provenance, trace.DataVersion);
            range.IsAuthoritative = field == "phone.nanpa.fictional-subscriber-range" &&
                catalog.RegionCode == "US" &&
                catalog.CallingCode == "+1" &&
                trace.SourceLocale == LocaleFieldValue.InvariantLocale &&
                trace.Provenance.Contains("NANPA 555", StringComparison.Ordinal);
            return new PhoneSafeRange[] { range };
        }
    }

    static void ValidateCatalog(PhoneLocaleMetadata value)
    {
        if (value.RegionCode.Length != 2 ||
            value.RegionCode[0] < 'A' || value.RegionCode[0] > 'Z' ||
            value.RegionCode[1] < 'A' || value.RegionCode[1] > 'Z')
        {
            Invalid(CatalogField + ".regionCode must contain two uppercase ASCII letters.");
        }
        if (value.CallingCode.Length < 2 || value.CallingCode[0] != '+')
        {
            Invalid(CatalogField + ".callingCode must begin with plus and contain digits.");
        }
        ValidateDigits(
            value.CallingCode.Substring(1),
            CatalogField + ".callingCode",
            1,
            3);
        ValidateDigits(
            value.InternationalDialingPrefix,
            CatalogField + ".internationalDialingPrefix",
            1,
            5);
        if (value.NationalPrefix.Length != 0)
        {
            ValidateDigits(value.NationalPrefix, CatalogField + ".nationalPrefix", 1, 5);
        }
        ValidateMask(value.AreaCodeMask, CatalogField + ".areaCodeMask", false);
        ValidateMask(value.SubscriberMask, CatalogField + ".subscriberMask", false);
        if (value.MinimumExtensionDigits < 1 ||
            value.MaximumExtensionDigits < value.MinimumExtensionDigits ||
            value.MaximumExtensionDigits > 20)
        {
            Invalid(CatalogField + " extension digit bounds must be between 1 and 20 in ascending order.");
        }
    }

    static PhoneNumberCategory Category(string value, string path)
    {
        switch (value)
        {
            case "general":
                return PhoneNumberCategory.General;
            case "landline":
                return PhoneNumberCategory.Landline;
            case "mobile":
                return PhoneNumberCategory.Mobile;
            case "toll-free":
                return PhoneNumberCategory.TollFree;
            case "premium":
                return PhoneNumberCategory.Premium;
            default:
                Invalid(path + ".category is not supported.");
                return PhoneNumberCategory.General;
        }
    }

    static void ValidateMask(string value, string path, bool allowPlus)
    {
        bool placeholder = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '#' || character == 'N')
            {
                placeholder = true;
            }
            else if (!(character >= '0' && character <= '9') &&
                character != ' ' && character != '(' && character != ')' &&
                character != '-' && character != '.')
            {
                if (!allowPlus || character != '+')
                {
                    Invalid(path + " contains an unsupported mask character.");
                }
            }
            index = index + 1;
        }
        if (!placeholder)
        {
            Invalid(path + " must contain at least one digit placeholder.");
        }
    }

    static void ValidateGenerationMask(
        string value,
        string path,
        bool requirePlaceholder)
    {
        bool placeholder = false;
        bool digit = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '#' || character == 'N')
            {
                placeholder = true;
                digit = true;
            }
            else if (character >= '0' && character <= '9')
            {
                digit = true;
            }
            else if (character != ' ' && character != '(' && character != ')' &&
                character != '-' && character != '.')
            {
                Invalid(path + " contains an unsupported mask character.");
            }
            index = index + 1;
        }
        if (!digit || requirePlaceholder && !placeholder)
        {
            Invalid(path + " must contain the required digit shape.");
        }
    }

    static void ValidateE164Mask(string value, string path)
    {
        if (value.Length < 2 || value[0] != '+')
        {
            Invalid(path + " must begin with plus.");
        }
        bool placeholder = false;
        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '#' || character == 'N')
            {
                placeholder = true;
            }
            else if (character < '0' || character > '9')
            {
                Invalid(path + " may contain only plus, digits, and digit placeholders.");
            }
            index = index + 1;
        }
        if (!placeholder)
        {
            Invalid(path + " must contain at least one digit placeholder.");
        }
    }

    static int CommonPrefix(string first, string last)
    {
        int index = 0;
        while (index < first.Length && first[index] == last[index])
        {
            index = index + 1;
        }
        return index;
    }

    static string ProvenanceSource(string provenance)
    {
        const string marker = "; source version ";
        int markerIndex = provenance.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return provenance;
        }
        return provenance.Substring(0, markerIndex);
    }

    static string ProvenanceVersion(string provenance, string dataVersion)
    {
        const string startMarker = "; source version ";
        const string endMarker = "; license ";
        int start = provenance.IndexOf(startMarker, StringComparison.Ordinal);
        int end = provenance.IndexOf(endMarker, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            return dataVersion;
        }
        start = start + startMarker.Length;
        return provenance.Substring(start, end - start);
    }

    static void ValidateMaskableRange(
        string first,
        string last,
        int commonLength,
        string path)
    {
        int index = commonLength;
        while (index < first.Length)
        {
            if (first[index] != '0' || last[index] != '9')
            {
                Invalid(path + " must describe a complete decimal range expressible as a mask.");
            }
            index = index + 1;
        }
    }

    static void ValidateDigits(
        string value,
        string path,
        int minimumLength,
        int maximumLength)
    {
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            Invalid(path + " has an invalid digit length.");
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                Invalid(path + " must contain only decimal digits.");
            }
            index = index + 1;
        }
    }

    static string RequiredString(
        GenerationContext context,
        JsonElement parent,
        string name,
        string path)
    {
        return StringValue(
            context,
            Required(parent, name, path),
            path + "." + name,
            false);
    }

    static string StringValue(
        GenerationContext context,
        JsonElement value,
        string path,
        bool allowEmpty)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            Invalid(path + " must be a string.");
        }
        string result = JsonText.Required(value, path);
        UnicodeText.Validate(result, path);
        if (!allowEmpty && result.Length == 0)
        {
            Invalid(path + " must not be empty.");
        }
        return result;
    }

    static int RequiredInteger(JsonElement parent, string name, string path)
    {
        JsonElement value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.Number)
        {
            Invalid(path + "." + name + " must be an integer.");
        }
        return value.GetInt32();
    }

    static bool RequiredBoolean(JsonElement parent, string name, string path)
    {
        JsonElement value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
        {
            Invalid(path + "." + name + " must be a Boolean.");
        }
        return value.GetBoolean();
    }

    static bool OptionalBoolean(
        JsonElement parent,
        string name,
        string path,
        bool fallback)
    {
        foreach (JsonProperty property in parent.EnumerateObject())
        {
            if (property.Name == name)
            {
                if (property.Value.ValueKind != JsonValueKind.True &&
                    property.Value.ValueKind != JsonValueKind.False)
                {
                    Invalid(path + "." + name + " must be a Boolean.");
                }
                return property.Value.GetBoolean();
            }
        }
        return fallback;
    }

    static JsonElement Required(JsonElement parent, string name, string path)
    {
        foreach (JsonProperty property in parent.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        Invalid(path + " is missing required member '" + name + "'.");
        return default(JsonElement);
    }

    static void RequireObject(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            Invalid(path + " must be an object.");
        }
    }

    static void RequireArray(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            Invalid(path + " must be an array.");
        }
    }

    static void Invalid(string message)
    {
        throw FakeException.Create("Invalid phone locale data: " + message);
    }
}
