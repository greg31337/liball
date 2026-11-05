using System;
using System.Text.Json;

namespace fake;

internal static class GenericIdentifierGenerator
{
    internal const string SkuMask = "SKU-****-####";
    internal const string SerialMask = "SN-************";
    internal const string LotMask = "LOT-########";
    internal const string OrderMask = "ORD-##########";
    internal const string InvoiceMask = "INV-########";
    internal const string TrackingMask = "TRK-****************";

    internal static string Generate(
        GenerationContext context,
        IRandomSource random,
        string field,
        string defaultMask)
    {
        string[] masks = ResolveMasks(context, field, defaultMask);
        int selected = 0;
        if (masks.Length > 1)
        {
            selected = random.IntFromMaximumExclusive(masks.Length);
        }
        return MaskFormatter.Format(
            random,
            masks[selected],
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string Generate(
        GenerationContext context,
        IRandomSource random,
        string mask)
    {
        return MaskFormatter.Format(
            random,
            mask,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string[] ResolveMasks(
        GenerationContext context,
        string field,
        string defaultMask)
    {
        LocaleFieldValue[] values = context.LocaleValues();
        LocaleCatalog catalog = new LocaleCatalog(context, values);
        LanguageTag requested = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "locale");
        string[] locales = LocaleResolver.CandidateLocales(context, requested);
        int index = 0;
        while (index < locales.Length)
        {
            LocaleCatalogEntry? entry = catalog.Find(locales[index], field);
            if (entry != null)
            {
                return ParseMasks(context, field, entry.Value);
            }
            index++;
        }
        MaskFormatter.OutputLength(
            defaultMask,
            context.CapturedOptions.MaximumOutputSize);
        return new string[] { defaultMask };
    }

    static string[] ParseMasks(
        GenerationContext context,
        string field,
        object source)
    {
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localeMaskData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                string mask = JsonText.Required(root, field);
                ValidateMask(context, field, mask, 0);
                return new string[] { mask };
            }
            if (root.ValueKind != JsonValueKind.Array)
            {
                Invalid(field, "must be a string or a nonempty array of strings");
            }
            int count = root.GetArrayLength();
            Guard.Count(count, context.CapturedOptions.MaximumCount, "localeMaskData");
            if (count == 0)
            {
                Invalid(field, "must contain at least one mask");
            }
            string[] masks = new string[count];
            int index = 0;
            while (index < count)
            {
                JsonElement item = root[index];
                if (item.ValueKind != JsonValueKind.String)
                {
                    Invalid(field, $"contains a non-string value at index {index}");
                }
                masks[index] = JsonText.Required(item, field);
                ValidateMask(context, field, masks[index], index);
                index++;
            }
            return masks;
        }
    }

    static void ValidateMask(
        GenerationContext context,
        string field,
        string mask,
        int index)
    {
        if (mask.Length == 0)
        {
            Invalid(field, $"contains an empty mask at index {index}");
        }
        MaskFormatter.OutputLength(
            mask,
            context.CapturedOptions.MaximumOutputSize);
    }

    static void Invalid(string field, string reason)
    {
        throw FakeException.Create($"Locale identifier-mask field '{field}' {reason}.");
    }
}
