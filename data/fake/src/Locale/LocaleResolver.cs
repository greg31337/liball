using System;
using System.Collections.Generic;

namespace fake;

internal static class LocaleResolver
{
    internal static object Resolve(
        GenerationContext context,
        string requestedLocale,
        string field,
        LocaleFieldValue[] values)
    {
        return Trace(context, requestedLocale, field, values).Value;
    }

    internal static FallbackTrace Trace(
        GenerationContext context,
        string requestedLocale,
        string field,
        LocaleFieldValue[] values)
    {
        Guard.NotNullOrWhiteSpace(field, "field");
        Guard.NotNull(values, "values");
        Guard.Count(values.Length, context.CapturedOptions.MaximumCount, "values");
        LanguageTag requestedTag = LanguageTagParser.Parse(requestedLocale, "locale");
        LocaleCatalog catalog = new LocaleCatalog(context, values);
        string[] candidates = CandidateLocales(context, requestedTag);
        int index = 0;
        while (index < candidates.Length)
        {
            LocaleCatalogEntry? entry = catalog.Find(candidates[index], field);
            if (entry != null)
            {
                FallbackTrace trace = new FallbackTrace();
                trace.RequestedLocale = requestedTag.Normalized;
                trace.Field = field;
                trace.SourceLocale = entry.Locale;
                trace.AttemptedLocales = CopyPrefix(candidates, index + 1);
                trace.UsedFallback = entry.Locale != requestedTag.Normalized;
                trace.Value = entry.Value;
                trace.DataVersion = entry.DataVersion;
                trace.Provenance = entry.Provenance;
                return trace;
            }
            index = index + 1;
        }
        if (context.StrictnessModeValue == StrictnessMode.Strict)
        {
            throw FakeException.Create($"Locale field '{field}' is unavailable for exact locale '{requestedTag.Normalized}' because strict fallback is enabled.");
        }
        throw FakeException.Create($"Locale field '{field}' is unavailable for locale '{requestedTag.Normalized}' after trying {string.Join(" -> ", candidates)}.");
    }

    internal static string[] CandidateLocales(GenerationContext context, LanguageTag requestedTag)
    {
        List<string> candidates = new List<string>();
        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        Add(candidates, added, requestedTag.Normalized);
        if (context.StrictnessModeValue == StrictnessMode.Strict)
        {
            return candidates.ToArray();
        }
        AddParents(candidates, added, requestedTag);
        LanguageTag configuredTag = LanguageTagParser.Parse(context.CapturedOptions.FallbackLocale, "FallbackLocale");
        Add(candidates, added, configuredTag.Normalized);
        AddParents(candidates, added, configuredTag);
        Add(candidates, added, LocaleFieldValue.InvariantLocale);
        return candidates.ToArray();
    }

    static void AddParents(List<string> candidates, HashSet<string> added, LanguageTag tag)
    {
        if (tag.Language == null || tag.Grandfathered)
        {
            return;
        }
        string parent = tag.Normalized;
        while (parent.Contains('-'))
        {
            int separator = parent.LastIndexOf('-');
            parent = parent.Substring(0, separator);
            int previousSeparator = parent.LastIndexOf('-');
            string finalSubtag = previousSeparator < 0
                ? parent
                : parent.Substring(previousSeparator + 1);
            if (finalSubtag.Length == 1)
            {
                if (previousSeparator < 0)
                {
                    return;
                }
                parent = parent.Substring(0, previousSeparator);
            }
            Add(candidates, added, parent);
        }
    }

    static void Add(List<string> candidates, HashSet<string> added, string locale)
    {
        if (added.Add(locale))
        {
            candidates.Add(locale);
        }
    }

    static string[] CopyPrefix(string[] values, int count)
    {
        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            result[index] = values[index];
            index = index + 1;
        }
        return result;
    }
}
