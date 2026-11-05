using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class SemanticDiffWriter
{
    internal SemanticDiffData Compare(SemanticSnapshot oldSnapshot, SemanticSnapshot newSnapshot)
    {
        List<SemanticDiffEntry> entries = new List<SemanticDiffEntry>();
        CompareCodeStatus(oldSnapshot, newSnapshot, entries);
        CompareActiveCurrencies(oldSnapshot.ActiveCurrencies, newSnapshot.ActiveCurrencies, entries);
        CompareHistoricalCurrencies(
            oldSnapshot.HistoricalCurrencies,
            newSnapshot.HistoricalCurrencies,
            entries);
        CompareIsoTerritoryMappings(oldSnapshot, newSnapshot, entries);
        CompareCldrUsages(oldSnapshot.CurrencyUsages, newSnapshot.CurrencyUsages, entries);
        CompareFractionRules(oldSnapshot.FractionRules, newSnapshot.FractionRules, entries);
        entries.Sort(CompareEntries);
        return new SemanticDiffData(entries);
    }

    internal void Write(TextWriter writer, SemanticDiffData data)
    {
        writer.WriteLine("category\tkey\tfield\told-value\tnew-value");
        foreach (SemanticDiffEntry entry in data.Entries)
        {
            writer.Write(Escape(entry.Category));
            writer.Write('\t');
            writer.Write(Escape(entry.Key));
            writer.Write('\t');
            writer.Write(Escape(entry.Field));
            writer.Write('\t');
            writer.Write(Escape(entry.OldValue));
            writer.Write('\t');
            writer.WriteLine(Escape(entry.NewValue));
        }
    }

    private static void CompareCodeStatus(
        SemanticSnapshot oldSnapshot,
        SemanticSnapshot newSnapshot,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, string> oldStates = BuildCodeStates(oldSnapshot);
        Dictionary<string, string> newStates = BuildCodeStates(newSnapshot);
        SortedSet<string> codes = new SortedSet<string>(oldStates.Keys, StringComparer.Ordinal);
        codes.UnionWith(newStates.Keys);
        foreach (string code in codes)
        {
            bool hasOld = oldStates.TryGetValue(code, out string? oldState);
            bool hasNew = newStates.TryGetValue(code, out string? newState);
            if (!hasOld)
            {
                Add(entries, "code-added", code, "status", string.Empty, newState ?? string.Empty);
            }
            else if (!hasNew)
            {
                Add(entries, "code-removed", code, "status", oldState ?? string.Empty, string.Empty);
            }
            else if (!string.Equals(oldState, newState, StringComparison.Ordinal))
            {
                Add(entries, "status-transition", code, "status", oldState ?? string.Empty, newState ?? string.Empty);
            }
        }
    }

    private static Dictionary<string, string> BuildCodeStates(SemanticSnapshot snapshot)
    {
        HashSet<string> active = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> historical = new HashSet<string>(StringComparer.Ordinal);
        foreach (ActiveCurrencyIdentity currency in snapshot.ActiveCurrencies.Currencies)
        {
            active.Add(currency.AlphabeticCode);
        }

        foreach (HistoricalCurrencyIdentity currency in snapshot.HistoricalCurrencies.Currencies)
        {
            historical.Add(currency.AlphabeticCode);
        }

        SortedSet<string> codes = new SortedSet<string>(active, StringComparer.Ordinal);
        codes.UnionWith(historical);
        Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string code in codes)
        {
            bool isActive = active.Contains(code);
            bool isHistorical = historical.Contains(code);
            string state = isActive && isHistorical
                ? "active-and-historical"
                : isActive ? "active" : "historical";
            states.Add(code, state);
        }

        return states;
    }

    private static void CompareActiveCurrencies(
        ActiveCurrencyAggregationData oldData,
        ActiveCurrencyAggregationData newData,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, ActiveCurrencyIdentity> oldCurrencies = IndexActiveCurrencies(oldData);
        Dictionary<string, ActiveCurrencyIdentity> newCurrencies = IndexActiveCurrencies(newData);
        SortedSet<string> codes = new SortedSet<string>(oldCurrencies.Keys, StringComparer.Ordinal);
        codes.IntersectWith(newCurrencies.Keys);
        foreach (string code in codes)
        {
            ActiveCurrencyIdentity oldCurrency = oldCurrencies[code];
            ActiveCurrencyIdentity newCurrency = newCurrencies[code];
            CompareValue(
                entries,
                "currency-changed",
                code,
                "numeric-code",
                oldCurrency.IsoNumericCodeText,
                newCurrency.IsoNumericCodeText);
            CompareValue(
                entries,
                "currency-changed",
                code,
                "official-name",
                oldCurrency.OfficialName,
                newCurrency.OfficialName);
            CompareValue(
                entries,
                "currency-changed",
                code,
                "minor-unit",
                FormatOptionalInteger(oldCurrency.HasIsoMinorUnit, oldCurrency.IsoMinorUnitDigits),
                FormatOptionalInteger(newCurrency.HasIsoMinorUnit, newCurrency.IsoMinorUnitDigits));
        }
    }

    private static Dictionary<string, ActiveCurrencyIdentity> IndexActiveCurrencies(
        ActiveCurrencyAggregationData data)
    {
        Dictionary<string, ActiveCurrencyIdentity> result =
            new Dictionary<string, ActiveCurrencyIdentity>(StringComparer.Ordinal);
        foreach (ActiveCurrencyIdentity currency in data.Currencies)
        {
            result.Add(currency.AlphabeticCode, currency);
        }

        return result;
    }

    private static void CompareHistoricalCurrencies(
        HistoricalCurrencyAggregationData oldData,
        HistoricalCurrencyAggregationData newData,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, List<HistoricalCurrencyIdentity>> oldGroups = GroupHistoricalCurrencies(oldData);
        Dictionary<string, List<HistoricalCurrencyIdentity>> newGroups = GroupHistoricalCurrencies(newData);
        SortedSet<string> codes = new SortedSet<string>(oldGroups.Keys, StringComparer.Ordinal);
        codes.IntersectWith(newGroups.Keys);
        foreach (string code in codes)
        {
            CompareHistoricalGroup(code, oldGroups[code], newGroups[code], entries);
        }
    }

    private static Dictionary<string, List<HistoricalCurrencyIdentity>> GroupHistoricalCurrencies(
        HistoricalCurrencyAggregationData data)
    {
        Dictionary<string, List<HistoricalCurrencyIdentity>> groups =
            new Dictionary<string, List<HistoricalCurrencyIdentity>>(StringComparer.Ordinal);
        foreach (HistoricalCurrencyIdentity currency in data.Currencies)
        {
            if (!groups.TryGetValue(currency.AlphabeticCode, out List<HistoricalCurrencyIdentity>? group))
            {
                group = new List<HistoricalCurrencyIdentity>();
                groups.Add(currency.AlphabeticCode, group);
            }

            group.Add(currency);
        }

        foreach (List<HistoricalCurrencyIdentity> group in groups.Values)
        {
            group.Sort(CompareHistoricalIdentity);
        }

        return groups;
    }

    private static void CompareHistoricalGroup(
        string code,
        List<HistoricalCurrencyIdentity> oldGroup,
        List<HistoricalCurrencyIdentity> newGroup,
        List<SemanticDiffEntry> entries)
    {
        List<HistoricalCurrencyIdentity> oldRemaining = new List<HistoricalCurrencyIdentity>(oldGroup);
        List<HistoricalCurrencyIdentity> newRemaining = new List<HistoricalCurrencyIdentity>(newGroup);
        RemoveExactHistoricalMatches(oldRemaining, newRemaining);
        while (oldRemaining.Count > 0 && newRemaining.Count > 0)
        {
            int bestOldIndex = 0;
            int bestNewIndex = 0;
            int bestScore = int.MaxValue;
            for (int oldIndex = 0; oldIndex < oldRemaining.Count; oldIndex++)
            {
                for (int newIndex = 0; newIndex < newRemaining.Count; newIndex++)
                {
                    int score = HistoricalDifferenceScore(
                        oldRemaining[oldIndex],
                        newRemaining[newIndex]);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestOldIndex = oldIndex;
                        bestNewIndex = newIndex;
                    }
                }
            }

            HistoricalCurrencyIdentity oldCurrency = oldRemaining[bestOldIndex];
            HistoricalCurrencyIdentity newCurrency = newRemaining[bestNewIndex];
            string key = code + ":" + HistoricalIdentityKey(oldCurrency);
            CompareValue(entries, "currency-changed", key, "numeric-code", oldCurrency.IsoNumericCodeText, newCurrency.IsoNumericCodeText);
            CompareValue(entries, "currency-changed", key, "official-name", oldCurrency.OfficialName, newCurrency.OfficialName);
            CompareValue(
                entries,
                "currency-changed",
                key,
                "minor-unit",
                FormatOptionalInteger(oldCurrency.HasIsoMinorUnit, oldCurrency.IsoMinorUnitDigits),
                FormatOptionalInteger(newCurrency.HasIsoMinorUnit, newCurrency.IsoMinorUnitDigits));
            CompareValue(entries, "date-changed", key, "withdrawal", oldCurrency.WithdrawalText, newCurrency.WithdrawalText);
            oldRemaining.RemoveAt(bestOldIndex);
            newRemaining.RemoveAt(bestNewIndex);
        }

        foreach (HistoricalCurrencyIdentity currency in oldRemaining)
        {
            Add(
                entries,
                "historical-designation-removed",
                code,
                "designation",
                DescribeHistoricalIdentity(currency),
                string.Empty);
        }

        foreach (HistoricalCurrencyIdentity currency in newRemaining)
        {
            Add(
                entries,
                "historical-designation-added",
                code,
                "designation",
                string.Empty,
                DescribeHistoricalIdentity(currency));
        }
    }

    private static void RemoveExactHistoricalMatches(
        List<HistoricalCurrencyIdentity> oldCurrencies,
        List<HistoricalCurrencyIdentity> newCurrencies)
    {
        int oldIndex = oldCurrencies.Count - 1;
        while (oldIndex >= 0)
        {
            string oldDescription = DescribeHistoricalIdentity(oldCurrencies[oldIndex]);
            int matchingNewIndex = -1;
            for (int newIndex = 0; newIndex < newCurrencies.Count; newIndex++)
            {
                if (string.Equals(
                    oldDescription,
                    DescribeHistoricalIdentity(newCurrencies[newIndex]),
                    StringComparison.Ordinal))
                {
                    matchingNewIndex = newIndex;
                    break;
                }
            }

            if (matchingNewIndex >= 0)
            {
                oldCurrencies.RemoveAt(oldIndex);
                newCurrencies.RemoveAt(matchingNewIndex);
            }

            oldIndex--;
        }
    }

    private static int HistoricalDifferenceScore(
        HistoricalCurrencyIdentity oldCurrency,
        HistoricalCurrencyIdentity newCurrency)
    {
        int score = 0;
        score += Difference(oldCurrency.IsoNumericCodeText, newCurrency.IsoNumericCodeText);
        score += Difference(oldCurrency.OfficialName, newCurrency.OfficialName);
        score += Difference(
            FormatOptionalInteger(oldCurrency.HasIsoMinorUnit, oldCurrency.IsoMinorUnitDigits),
            FormatOptionalInteger(newCurrency.HasIsoMinorUnit, newCurrency.IsoMinorUnitDigits));
        score += Difference(oldCurrency.WithdrawalText, newCurrency.WithdrawalText);
        return score;
    }

    private static int Difference(string? oldValue, string? newValue)
    {
        return string.Equals(oldValue, newValue, StringComparison.Ordinal) ? 0 : 1;
    }

    private static int CompareHistoricalIdentity(
        HistoricalCurrencyIdentity left,
        HistoricalCurrencyIdentity right)
    {
        return string.CompareOrdinal(DescribeHistoricalIdentity(left), DescribeHistoricalIdentity(right));
    }

    private static string HistoricalIdentityKey(HistoricalCurrencyIdentity currency)
    {
        return EscapeKeyPart(currency.IsoNumericCodeText)
            + ":"
            + EscapeKeyPart(currency.OfficialName)
            + ":"
            + EscapeKeyPart(currency.WithdrawalText);
    }

    private static string DescribeHistoricalIdentity(HistoricalCurrencyIdentity currency)
    {
        return "numeric=" + (currency.IsoNumericCodeText ?? "unknown")
            + ";name=" + currency.OfficialName
            + ";minor=" + FormatOptionalInteger(currency.HasIsoMinorUnit, currency.IsoMinorUnitDigits)
            + ";withdrawal=" + (currency.WithdrawalText.Length == 0 ? "unknown" : currency.WithdrawalText);
    }

    private static void CompareIsoTerritoryMappings(
        SemanticSnapshot oldSnapshot,
        SemanticSnapshot newSnapshot,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, string> oldMappings = BuildIsoTerritoryMappings(oldSnapshot);
        Dictionary<string, string> newMappings = BuildIsoTerritoryMappings(newSnapshot);
        SortedSet<string> anchors = new SortedSet<string>(oldMappings.Keys, StringComparer.Ordinal);
        anchors.UnionWith(newMappings.Keys);
        foreach (string anchor in anchors)
        {
            bool hasOld = oldMappings.TryGetValue(anchor, out string? oldMapping);
            bool hasNew = newMappings.TryGetValue(anchor, out string? newMapping);
            if (!hasOld)
            {
                Add(entries, "territory-mapping-added", anchor, "iso-mapping", string.Empty, newMapping ?? string.Empty);
            }
            else if (!hasNew)
            {
                Add(entries, "territory-mapping-removed", anchor, "iso-mapping", oldMapping ?? string.Empty, string.Empty);
            }
            else if (!string.Equals(oldMapping, newMapping, StringComparison.Ordinal))
            {
                Add(entries, "territory-mapping-changed", anchor, "iso-mapping", oldMapping ?? string.Empty, newMapping ?? string.Empty);
            }
        }
    }

    private static Dictionary<string, string> BuildIsoTerritoryMappings(SemanticSnapshot snapshot)
    {
        Dictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ActiveCurrencyUsage usage in snapshot.ActiveCurrencies.Usages)
        {
            string currency = usage.HasCurrencyAssignment ? usage.AlphabeticCode ?? "unknown" : "no-currency";
            string anchor = "active:" + EscapeKeyPart(usage.SourceEntityName) + ":" + currency;
            mappings.Add(anchor, DescribeTerritory(
                usage.CanonicalTerritoryName,
                usage.TerritoryMatchCode,
                usage.TerritoryAlpha2Code,
                usage.TerritoryAlpha3Code,
                usage.TerritoryNumericCodeText));
        }

        foreach (HistoricalCurrencyUsage usage in snapshot.HistoricalCurrencies.Usages)
        {
            string anchor = "historical:" + EscapeKeyPart(usage.SourceEntityName) + ":" + usage.AlphabeticCode;
            string mapping = DescribeTerritory(
                usage.CanonicalTerritoryName,
                usage.TerritoryMatchCode,
                usage.TerritoryAlpha2Code,
                usage.TerritoryAlpha3Code,
                usage.TerritoryNumericCodeText);
            if (!mappings.TryAdd(anchor, mapping)
                && !string.Equals(mappings[anchor], mapping, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Conflicting ISO territory mappings share semantic key '" + anchor + "'.");
            }
        }

        return mappings;
    }

    private static string DescribeTerritory(
        string name,
        string? matchCode,
        string? alpha2,
        string? alpha3,
        string? numeric)
    {
        return "name=" + name
            + ";match=" + (matchCode ?? "unknown")
            + ";alpha2=" + (alpha2 ?? "unknown")
            + ";alpha3=" + (alpha3 ?? "unknown")
            + ";numeric=" + (numeric ?? "unknown");
    }

    private static void CompareCldrUsages(
        CldrCurrencyUsageData oldData,
        CldrCurrencyUsageData newData,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, List<CldrCurrencyUsage>> oldGroups = GroupCldrUsages(oldData);
        Dictionary<string, List<CldrCurrencyUsage>> newGroups = GroupCldrUsages(newData);
        SortedSet<string> keys = new SortedSet<string>(oldGroups.Keys, StringComparer.Ordinal);
        keys.UnionWith(newGroups.Keys);
        foreach (string key in keys)
        {
            bool hasOld = oldGroups.TryGetValue(key, out List<CldrCurrencyUsage>? oldGroup);
            bool hasNew = newGroups.TryGetValue(key, out List<CldrCurrencyUsage>? newGroup);
            if (!hasOld)
            {
                foreach (CldrCurrencyUsage usage in newGroup ?? new List<CldrCurrencyUsage>())
                {
                    Add(entries, "territory-mapping-added", key, "cldr-usage", string.Empty, DescribeCldrUsage(usage));
                }
            }
            else if (!hasNew)
            {
                foreach (CldrCurrencyUsage usage in oldGroup ?? new List<CldrCurrencyUsage>())
                {
                    Add(entries, "territory-mapping-removed", key, "cldr-usage", DescribeCldrUsage(usage), string.Empty);
                }
            }
            else
            {
                CompareCldrUsageGroup(key, oldGroup ?? new List<CldrCurrencyUsage>(), newGroup ?? new List<CldrCurrencyUsage>(), entries);
            }
        }
    }

    private static Dictionary<string, List<CldrCurrencyUsage>> GroupCldrUsages(CldrCurrencyUsageData data)
    {
        Dictionary<string, List<CldrCurrencyUsage>> groups =
            new Dictionary<string, List<CldrCurrencyUsage>>(StringComparer.Ordinal);
        foreach (CldrCurrencyUsage usage in data.Usages)
        {
            string key = usage.TerritoryCode + ":" + usage.CurrencyCode;
            if (!groups.TryGetValue(key, out List<CldrCurrencyUsage>? group))
            {
                group = new List<CldrCurrencyUsage>();
                groups.Add(key, group);
            }

            group.Add(usage);
        }

        foreach (List<CldrCurrencyUsage> group in groups.Values)
        {
            group.Sort(CompareCldrUsage);
        }

        return groups;
    }

    private static void CompareCldrUsageGroup(
        string key,
        List<CldrCurrencyUsage> oldGroup,
        List<CldrCurrencyUsage> newGroup,
        List<SemanticDiffEntry> entries)
    {
        List<CldrCurrencyUsage> oldRemaining = new List<CldrCurrencyUsage>(oldGroup);
        List<CldrCurrencyUsage> newRemaining = new List<CldrCurrencyUsage>(newGroup);
        RemoveExactCldrUsageMatches(oldRemaining, newRemaining);
        while (oldRemaining.Count > 0 && newRemaining.Count > 0)
        {
            int bestOldIndex = 0;
            int bestNewIndex = 0;
            int bestScore = int.MaxValue;
            for (int oldIndex = 0; oldIndex < oldRemaining.Count; oldIndex++)
            {
                for (int newIndex = 0; newIndex < newRemaining.Count; newIndex++)
                {
                    int score = CldrUsageDifferenceScore(oldRemaining[oldIndex], newRemaining[newIndex]);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestOldIndex = oldIndex;
                        bestNewIndex = newIndex;
                    }
                }
            }

            CldrCurrencyUsage oldUsage = oldRemaining[bestOldIndex];
            CldrCurrencyUsage newUsage = newRemaining[bestNewIndex];
            string usageKey = key + ":" + FormatPartialDate(oldUsage.SourceStartDate) + ":" + FormatPartialDate(oldUsage.SourceEndDate);
            CompareValue(entries, "date-changed", usageKey, "start", FormatPartialDate(oldUsage.SourceStartDate), FormatPartialDate(newUsage.SourceStartDate));
            CompareValue(entries, "date-changed", usageKey, "end", FormatPartialDate(oldUsage.SourceEndDate), FormatPartialDate(newUsage.SourceEndDate));
            CompareValue(entries, "date-changed", usageKey, "start-time-zone", oldUsage.StartTimeZone, newUsage.StartTimeZone);
            CompareValue(entries, "date-changed", usageKey, "end-time-zone", oldUsage.EndTimeZone, newUsage.EndTimeZone);
            CompareValue(entries, "tender-changed", usageKey, "tender", FormatBoolean(oldUsage.Tender), FormatBoolean(newUsage.Tender));
            oldRemaining.RemoveAt(bestOldIndex);
            newRemaining.RemoveAt(bestNewIndex);
        }

        foreach (CldrCurrencyUsage usage in oldRemaining)
        {
            Add(entries, "territory-mapping-removed", key, "cldr-usage", DescribeCldrUsage(usage), string.Empty);
        }

        foreach (CldrCurrencyUsage usage in newRemaining)
        {
            Add(entries, "territory-mapping-added", key, "cldr-usage", string.Empty, DescribeCldrUsage(usage));
        }
    }

    private static void RemoveExactCldrUsageMatches(
        List<CldrCurrencyUsage> oldUsages,
        List<CldrCurrencyUsage> newUsages)
    {
        int oldIndex = oldUsages.Count - 1;
        while (oldIndex >= 0)
        {
            string oldDescription = DescribeCldrUsage(oldUsages[oldIndex]);
            int matchingNewIndex = -1;
            for (int newIndex = 0; newIndex < newUsages.Count; newIndex++)
            {
                if (string.Equals(oldDescription, DescribeCldrUsage(newUsages[newIndex]), StringComparison.Ordinal))
                {
                    matchingNewIndex = newIndex;
                    break;
                }
            }

            if (matchingNewIndex >= 0)
            {
                oldUsages.RemoveAt(oldIndex);
                newUsages.RemoveAt(matchingNewIndex);
            }

            oldIndex--;
        }
    }

    private static int CldrUsageDifferenceScore(CldrCurrencyUsage oldUsage, CldrCurrencyUsage newUsage)
    {
        int score = 0;
        score += Difference(FormatPartialDate(oldUsage.SourceStartDate), FormatPartialDate(newUsage.SourceStartDate));
        score += Difference(FormatPartialDate(oldUsage.SourceEndDate), FormatPartialDate(newUsage.SourceEndDate));
        score += Difference(oldUsage.StartTimeZone, newUsage.StartTimeZone);
        score += Difference(oldUsage.EndTimeZone, newUsage.EndTimeZone);
        score += Difference(FormatBoolean(oldUsage.Tender), FormatBoolean(newUsage.Tender));
        return score;
    }

    private static int CompareCldrUsage(CldrCurrencyUsage left, CldrCurrencyUsage right)
    {
        return string.CompareOrdinal(DescribeCldrUsage(left), DescribeCldrUsage(right));
    }

    private static string DescribeCldrUsage(CldrCurrencyUsage usage)
    {
        return "start=" + FormatPartialDate(usage.SourceStartDate)
            + ";end=" + FormatPartialDate(usage.SourceEndDate)
            + ";tender=" + FormatBoolean(usage.Tender)
            + ";start-time-zone=" + (usage.StartTimeZone ?? "unknown")
            + ";end-time-zone=" + (usage.EndTimeZone ?? "unknown");
    }

    private static void CompareFractionRules(
        CldrFractionData oldData,
        CldrFractionData newData,
        List<SemanticDiffEntry> entries)
    {
        Dictionary<string, CldrFractionRule> oldRules = IndexFractionRules(oldData);
        Dictionary<string, CldrFractionRule> newRules = IndexFractionRules(newData);
        SortedSet<string> codes = new SortedSet<string>(oldRules.Keys, StringComparer.Ordinal);
        codes.UnionWith(newRules.Keys);
        foreach (string code in codes)
        {
            bool hasOld = oldRules.TryGetValue(code, out CldrFractionRule? oldRule);
            bool hasNew = newRules.TryGetValue(code, out CldrFractionRule? newRule);
            if (!hasOld)
            {
                Add(entries, "format-rule-added", code, "rule", string.Empty, DescribeFractionRule(newRule));
            }
            else if (!hasNew)
            {
                Add(entries, "format-rule-removed", code, "rule", DescribeFractionRule(oldRule), string.Empty);
            }
            else
            {
                if (oldRule == null || newRule == null)
                {
                    throw new InvalidDataException("A matched CLDR fraction rule is missing.");
                }

                CompareFractionRule(code, oldRule, newRule, entries);
            }
        }
    }

    private static Dictionary<string, CldrFractionRule> IndexFractionRules(CldrFractionData data)
    {
        Dictionary<string, CldrFractionRule> rules =
            new Dictionary<string, CldrFractionRule>(StringComparer.Ordinal)
            {
                { "DEFAULT", data.DefaultRule }
            };
        foreach (CldrFractionRule rule in data.ExplicitRules)
        {
            rules.Add(rule.CurrencyCode, rule);
        }

        return rules;
    }

    private static void CompareFractionRule(
        string code,
        CldrFractionRule oldRule,
        CldrFractionRule newRule,
        List<SemanticDiffEntry> entries)
    {
        CompareValue(entries, "format-rule-changed", code, "standard-digits", FormatInteger(oldRule.Digits), FormatInteger(newRule.Digits));
        CompareValue(entries, "format-rule-changed", code, "standard-rounding", FormatInteger(oldRule.Rounding), FormatInteger(newRule.Rounding));
        CompareValue(entries, "format-rule-changed", code, "cash-digits", FormatInteger(oldRule.CashDigits), FormatInteger(newRule.CashDigits));
        CompareValue(entries, "format-rule-changed", code, "cash-rounding", FormatInteger(oldRule.CashRounding), FormatInteger(newRule.CashRounding));
        CompareValue(entries, "format-rule-changed", code, "standard-digits-provenance", FormatProvenance(oldRule.DigitsExplicit), FormatProvenance(newRule.DigitsExplicit));
        CompareValue(entries, "format-rule-changed", code, "standard-rounding-provenance", FormatProvenance(oldRule.RoundingExplicit), FormatProvenance(newRule.RoundingExplicit));
        CompareValue(entries, "format-rule-changed", code, "cash-digits-provenance", FormatProvenance(oldRule.CashDigitsExplicit), FormatProvenance(newRule.CashDigitsExplicit));
        CompareValue(entries, "format-rule-changed", code, "cash-rounding-provenance", FormatProvenance(oldRule.CashRoundingExplicit), FormatProvenance(newRule.CashRoundingExplicit));
    }

    private static string DescribeFractionRule(CldrFractionRule? rule)
    {
        if (rule == null)
        {
            return string.Empty;
        }

        return "standard-digits=" + FormatInteger(rule.Digits)
            + ";standard-rounding=" + FormatInteger(rule.Rounding)
            + ";cash-digits=" + FormatInteger(rule.CashDigits)
            + ";cash-rounding=" + FormatInteger(rule.CashRounding);
    }

    private static void CompareValue(
        List<SemanticDiffEntry> entries,
        string category,
        string key,
        string field,
        string? oldValue,
        string? newValue)
    {
        string normalizedOldValue = oldValue ?? "unknown";
        string normalizedNewValue = newValue ?? "unknown";
        if (!string.Equals(normalizedOldValue, normalizedNewValue, StringComparison.Ordinal))
        {
            Add(entries, category, key, field, normalizedOldValue, normalizedNewValue);
        }
    }

    private static void Add(
        List<SemanticDiffEntry> entries,
        string category,
        string key,
        string field,
        string oldValue,
        string newValue)
    {
        entries.Add(new SemanticDiffEntry(category, key, field, oldValue, newValue));
    }

    private static int CompareEntries(SemanticDiffEntry left, SemanticDiffEntry right)
    {
        int comparison = string.CompareOrdinal(left.Category, right.Category);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Key, right.Key);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Field, right.Field);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.OldValue, right.OldValue);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.NewValue, right.NewValue);
    }

    private static string FormatOptionalInteger(bool hasValue, int value)
    {
        return hasValue ? FormatInteger(value) : "unknown";
    }

    private static string FormatInteger(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FormatProvenance(bool isExplicit)
    {
        return isExplicit ? "explicit" : "inherited";
    }

    private static string FormatPartialDate(IsoPartialDate date)
    {
        if (!date.IsKnown)
        {
            return "unknown";
        }

        if (date.Precision == IsoDatePrecision.Year)
        {
            return date.Year.ToString("0000", CultureInfo.InvariantCulture);
        }

        if (date.Precision == IsoDatePrecision.Month)
        {
            return date.Year.ToString("0000", CultureInfo.InvariantCulture)
                + "-"
                + date.Month.ToString("00", CultureInfo.InvariantCulture);
        }

        return date.Year.ToString("0000", CultureInfo.InvariantCulture)
            + "-"
            + date.Month.ToString("00", CultureInfo.InvariantCulture)
            + "-"
            + date.Day.ToString("00", CultureInfo.InvariantCulture);
    }

    private static string EscapeKeyPart(string? value)
    {
        return (value ?? "unknown").Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal);
    }

    private static string Escape(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
