using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CurrencyCodes.DataGenerator;

internal sealed class DataInvariantValidator
{
    internal void Validate(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrFractionData fractionData,
        CldrCurrencyUsageData usageData,
        CldrTerritoryCodeData territoryCodeData,
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings,
        TerritoryReconciliationData territoryReconciliations,
        CrossSourceCurrencyData crossSourceData)
    {
        ValidateCodeGrammarAndNumericWidth(
            listOneData,
            listThreeData,
            fractionData,
            usageData,
            territoryCodeData,
            currencyMappings);
        ValidateDuplicateIdentities(listOneData, listThreeData, usageData);
        ValidateRangeBounds(listOneData, listThreeData, fractionData, territoryCodeData, currencyMappings);
        ValidateDateOrdering(listThreeData, usageData);
        ValidateStatusConsistency(listOneData, listThreeData);
        ValidateSourceCompleteness(
            listOneData,
            listThreeData,
            fractionData,
            usageData,
            territoryCodeData,
            currencyMappings,
            territoryReconciliations,
            crossSourceData);
        ValidateForwardReverseMappings(usageData);
    }

    private static void ValidateCodeGrammarAndNumericWidth(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrFractionData fractionData,
        CldrCurrencyUsageData usageData,
        CldrTerritoryCodeData territoryCodeData,
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings)
    {
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            ValidateOptionalCurrencyIdentity(row.AlphabeticCode, row.NumericCode, "List One");
        }

        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            if (!IsAlphabeticCode(row.AlphabeticCode))
            {
                throw Invalid($"List Three contains invalid alphabetic code '{row.AlphabeticCode}'.");
            }

            ValidateOptionalNumericCode(row.NumericCode, "List Three");
        }

        if (fractionData.DefaultRule.CurrencyCode != "DEFAULT")
        {
            throw Invalid("CLDR fraction default rule must use code 'DEFAULT'.");
        }

        foreach (CldrFractionRule rule in fractionData.ExplicitRules)
        {
            if (!IsAlphabeticCode(rule.CurrencyCode))
            {
                throw Invalid($"CLDR fractions contain invalid currency code '{rule.CurrencyCode}'.");
            }
        }

        foreach (CldrCurrencyUsage usage in usageData.Usages)
        {
            if (!IsTerritoryCode(usage.TerritoryCode) || !IsAlphabeticCode(usage.CurrencyCode))
            {
                throw Invalid(
                    $"CLDR usage contains invalid territory/currency pair '{usage.TerritoryCode}:{usage.CurrencyCode}'.");
            }
        }

        HashSet<string> mappedTerritoryCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (CldrTerritoryCode territoryCode in territoryCodeData.TerritoryCodes)
        {
            if (!IsTerritoryCode(territoryCode.Alpha2Code)
                || (territoryCode.Alpha3Code != null && !IsAlphabeticCode(territoryCode.Alpha3Code)))
            {
                throw Invalid($"CLDR contains invalid territory identity '{territoryCode.Alpha2Code}'.");
            }

            ValidateNumericTextAndValue(
                territoryCode.NumericCodeText,
                territoryCode.HasNumericCode,
                territoryCode.NumericCodeValue,
                $"CLDR territory '{territoryCode.Alpha2Code}'");
            if (!mappedTerritoryCodes.Add(territoryCode.Alpha2Code))
            {
                throw Invalid($"CLDR repeats territory code '{territoryCode.Alpha2Code}'.");
            }
        }

        foreach (CldrTerritoryAlias alias in territoryCodeData.Aliases)
        {
            if (!IsTerritoryAliasCode(alias.AliasCode) || alias.Reason.Length == 0)
            {
                throw Invalid($"CLDR contains invalid territory alias '{alias.AliasCode}'.");
            }

            if (alias.ReplacementText != string.Join(" ", alias.ReplacementCodes))
            {
                throw Invalid($"CLDR territory alias '{alias.AliasCode}' has inconsistent replacement metadata.");
            }

            foreach (string replacementCode in alias.ReplacementCodes)
            {
                if (!IsTerritoryCode(replacementCode) && !IsNumericCode(replacementCode))
                {
                    throw Invalid(
                        $"CLDR territory alias '{alias.AliasCode}' has invalid replacement '{replacementCode}'.");
                }

                if (IsTerritoryCode(replacementCode)
                    && !mappedTerritoryCodes.Contains(replacementCode))
                {
                    throw Invalid(
                        $"CLDR territory alias '{alias.AliasCode}' targets unknown territory '{replacementCode}'.");
                }
            }
        }

        foreach (CldrCurrencyCodeMapping mapping in currencyMappings)
        {
            if (!IsAlphabeticCode(mapping.AlphabeticCode)
                || mapping.SourceNumericCodeText.Length < 1
                || mapping.SourceNumericCodeText.Length > 3
                || !ContainsAsciiDigits(mapping.SourceNumericCodeText))
            {
                throw Invalid($"CLDR contains invalid currency mapping '{mapping.AlphabeticCode}'.");
            }

            ValidateNumericTextAndValue(
                mapping.NumericCodeText,
                true,
                mapping.NumericCodeValue,
                $"CLDR currency '{mapping.AlphabeticCode}'");
        }
    }

    private static void ValidateDuplicateIdentities(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrCurrencyUsageData usageData)
    {
        Dictionary<string, string> activeIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            if (row.AlphabeticCode == null)
            {
                continue;
            }

            string identity = CreateActiveIdentity(row);
            if (activeIdentities.TryGetValue(row.AlphabeticCode, out string? existingIdentity)
                && existingIdentity != identity)
            {
                throw Invalid($"Active alphabetic code '{row.AlphabeticCode}' has conflicting identities.");
            }

            activeIdentities[row.AlphabeticCode] = identity;
        }

        HashSet<string> historicalRows = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            string identity = CreateHistoricalRowIdentity(row);
            if (!historicalRows.Add(identity))
            {
                throw Invalid(
                    $"List Three repeats historical designation '{row.AlphabeticCode}:{row.EntityName}:{row.OfficialName}'.");
            }
        }

        HashSet<string> usageRows = new HashSet<string>(StringComparer.Ordinal);
        foreach (CldrCurrencyUsage usage in usageData.Usages)
        {
            string identity = CreateUsageIdentity(usage);
            if (!usageRows.Add(identity))
            {
                throw Invalid($"CLDR repeats currency usage '{usage.TerritoryCode}:{usage.CurrencyCode}'.");
            }
        }
    }

    private static void ValidateRangeBounds(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrFractionData fractionData,
        CldrTerritoryCodeData territoryCodeData,
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings)
    {
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            ValidateMinorUnitRange(row.HasMinorUnit, row.MinorUnitDigits, $"List One '{row.EntityName}'");
        }

        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            ValidateMinorUnitRange(row.HasMinorUnit, row.MinorUnitDigits, $"List Three '{row.EntityName}'");
        }

        ValidateFractionRuleRange(fractionData.DefaultRule);
        foreach (CldrFractionRule rule in fractionData.ExplicitRules)
        {
            ValidateFractionRuleRange(rule);
        }

        foreach (CldrTerritoryCode territoryCode in territoryCodeData.TerritoryCodes)
        {
            if (territoryCode.HasNumericCode
                && (territoryCode.NumericCodeValue < 0 || territoryCode.NumericCodeValue > 999))
            {
                throw Invalid($"CLDR territory '{territoryCode.Alpha2Code}' numeric value is out of range.");
            }
        }

        foreach (CldrCurrencyCodeMapping mapping in currencyMappings)
        {
            if (mapping.NumericCodeValue < 0 || mapping.NumericCodeValue > 999)
            {
                throw Invalid($"CLDR currency '{mapping.AlphabeticCode}' numeric value is out of range.");
            }
        }
    }

    private static void ValidateDateOrdering(
        IsoListThreeData listThreeData,
        CldrCurrencyUsageData usageData)
    {
        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            ValidatePartialDate(row.WithdrawalDate, $"List Three '{row.AlphabeticCode}' withdrawal");
            if (row.WithdrawalText.Length == 0)
            {
                throw Invalid($"List Three '{row.AlphabeticCode}' has no withdrawal text.");
            }
        }

        foreach (CldrCurrencyUsage usage in usageData.Usages)
        {
            ValidatePartialDate(
                usage.SourceStartDate,
                $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' start");
            ValidatePartialDate(
                usage.SourceEndDate,
                $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' end");

            if (usage.HasInclusiveStartDate != usage.SourceStartDate.IsKnown
                || usage.HasInclusiveEndDate != usage.SourceEndDate.IsKnown)
            {
                throw Invalid(
                    $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' has inconsistent normalized-date presence.");
            }

            if (usage.HasInclusiveStartDate
                && usage.InclusiveStartDate != NormalizeStartDate(usage.SourceStartDate))
            {
                throw Invalid(
                    $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' has an invalid normalized start date.");
            }

            if (usage.HasInclusiveEndDate
                && usage.InclusiveEndDate != NormalizeEndDate(usage.SourceEndDate))
            {
                throw Invalid(
                    $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' has an invalid normalized end date.");
            }

            if (usage.HasInclusiveStartDate
                && usage.HasInclusiveEndDate
                && usage.InclusiveStartDate > usage.InclusiveEndDate)
            {
                throw Invalid($"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' ends before it starts.");
            }

            if ((usage.StartTimeZone != null && !usage.HasInclusiveStartDate)
                || (usage.EndTimeZone != null && !usage.HasInclusiveEndDate))
            {
                throw Invalid(
                    $"CLDR usage '{usage.TerritoryCode}:{usage.CurrencyCode}' has a time zone without its boundary.");
            }
        }
    }

    private static void ValidateStatusConsistency(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData)
    {
        if (listOneData.PublicationDate != listThreeData.PublicationDate)
        {
            throw Invalid("List One and List Three publication dates do not describe the same ISO snapshot.");
        }

        HashSet<string> activeDesignations = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            if (row.AlphabeticCode != null)
            {
                activeDesignations.Add(CreateStatusIdentity(
                    row.EntityName,
                    row.OfficialName,
                    row.AlphabeticCode,
                    row.NumericCode));
            }
        }

        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            string identity = CreateStatusIdentity(
                row.EntityName,
                row.OfficialName,
                row.AlphabeticCode,
                row.NumericCode);
            if (activeDesignations.Contains(identity))
            {
                throw Invalid(
                    $"Designation '{row.AlphabeticCode}:{row.EntityName}:{row.OfficialName}' is both active and historical.");
            }
        }
    }

    private static void ValidateSourceCompleteness(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrFractionData fractionData,
        CldrCurrencyUsageData usageData,
        CldrTerritoryCodeData territoryCodeData,
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings,
        TerritoryReconciliationData territoryReconciliations,
        CrossSourceCurrencyData crossSourceData)
    {
        ValidateCrossSourceCompleteness(
            listOneData,
            listThreeData,
            fractionData,
            currencyMappings,
            crossSourceData);
        ValidateTerritoryReconciliationCompleteness(
            listOneData,
            listThreeData,
            territoryReconciliations);

        HashSet<string> territoryCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (CldrTerritoryCode territoryCode in territoryCodeData.TerritoryCodes)
        {
            territoryCodes.Add(territoryCode.Alpha2Code);
        }

        HashSet<string> knownCurrencyCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            if (row.AlphabeticCode != null)
            {
                knownCurrencyCodes.Add(row.AlphabeticCode);
            }
        }

        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            knownCurrencyCodes.Add(row.AlphabeticCode);
        }

        foreach (CldrCurrencyUsage usage in usageData.Usages)
        {
            if (!territoryCodes.Contains(usage.TerritoryCode))
            {
                throw Invalid($"CLDR usage territory '{usage.TerritoryCode}' has no territory-code mapping.");
            }

            knownCurrencyCodes.Add(usage.CurrencyCode);
        }

        foreach (CldrFractionRule rule in fractionData.ExplicitRules)
        {
            if (!knownCurrencyCodes.Contains(rule.CurrencyCode))
            {
                throw Invalid($"CLDR fraction rule '{rule.CurrencyCode}' is unused by all pinned currency sources.");
            }
        }

        foreach (CldrCurrencyCodeMapping mapping in currencyMappings)
        {
            if (!knownCurrencyCodes.Contains(mapping.AlphabeticCode))
            {
                throw Invalid($"CLDR currency mapping '{mapping.AlphabeticCode}' is unused by all pinned currency sources.");
            }
        }
    }

    private static void ValidateCrossSourceCompleteness(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        CldrFractionData fractionData,
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings,
        CrossSourceCurrencyData crossSourceData)
    {
        int expectedCount = listOneData.Rows.Count + listThreeData.Rows.Count;
        if (crossSourceData.Rows.Count != expectedCount)
        {
            throw Invalid(
                $"Cross-source reconciliation contains {crossSourceData.Rows.Count} rows; expected {expectedCount}.");
        }

        Dictionary<string, CldrCurrencyCodeMapping> mappings =
            new Dictionary<string, CldrCurrencyCodeMapping>(StringComparer.Ordinal);
        foreach (CldrCurrencyCodeMapping mapping in currencyMappings)
        {
            mappings.Add(mapping.AlphabeticCode, mapping);
        }

        int crossIndex = 0;
        foreach (IsoListOneRow sourceRow in listOneData.Rows)
        {
            ValidateCrossSourceRow(
                "list-one",
                sourceRow.EntityName,
                sourceRow.OfficialName,
                sourceRow.AlphabeticCode,
                sourceRow.NumericCode,
                sourceRow.HasMinorUnit,
                sourceRow.MinorUnitDigits,
                crossSourceData.Rows[crossIndex],
                mappings,
                fractionData);
            crossIndex++;
        }

        foreach (IsoListThreeRow sourceRow in listThreeData.Rows)
        {
            ValidateCrossSourceRow(
                "list-three",
                sourceRow.EntityName,
                sourceRow.OfficialName,
                sourceRow.AlphabeticCode,
                sourceRow.NumericCode,
                sourceRow.HasMinorUnit,
                sourceRow.MinorUnitDigits,
                crossSourceData.Rows[crossIndex],
                mappings,
                fractionData);
            crossIndex++;
        }
    }

    private static void ValidateCrossSourceRow(
        string sourceList,
        string entityName,
        string officialName,
        string? alphabeticCode,
        string? numericCode,
        bool hasMinorUnit,
        int minorUnitDigits,
        CrossSourceCurrencyRow row,
        Dictionary<string, CldrCurrencyCodeMapping> mappings,
        CldrFractionData fractionData)
    {
        if (row.SourceList != sourceList
            || row.EntityName != entityName
            || row.OfficialName != officialName
            || row.AlphabeticCode != alphabeticCode
            || row.IsoNumericCodeText != numericCode
            || row.HasIsoMinorUnit != hasMinorUnit
            || row.IsoMinorUnitDigits != minorUnitDigits)
        {
            throw Invalid($"Cross-source reconciliation does not preserve source row '{sourceList}:{entityName}'.");
        }

        if (alphabeticCode == null)
        {
            if (row.HasCldrNumericCode || row.HasCldrFormatRule)
            {
                throw Invalid($"Code-less source row '{sourceList}:{entityName}' acquired CLDR metadata.");
            }

            return;
        }

        mappings.TryGetValue(alphabeticCode, out CldrCurrencyCodeMapping? mapping);
        if (row.HasCldrNumericCode != (mapping != null)
            || row.CldrSourceNumericCodeText != mapping?.SourceNumericCodeText
            || row.CldrNumericCodeText != mapping?.NumericCodeText
            || row.CldrNumericCodeValue != (mapping?.NumericCodeValue ?? 0))
        {
            throw Invalid($"Cross-source reconciliation has incorrect CLDR identity for '{alphabeticCode}'.");
        }

        bool conflict = mapping != null && numericCode != null && numericCode != mapping.NumericCodeText;
        if (row.IdentityConflict != conflict || row.IdentityConflictReviewed != conflict)
        {
            throw Invalid($"Cross-source reconciliation has inconsistent conflict state for '{alphabeticCode}'.");
        }

        CldrFractionRule rule = fractionData.ResolveRule(alphabeticCode);
        if (!row.HasCldrFormatRule
            || row.CldrStandardDigits != rule.Digits
            || row.CldrStandardRounding != rule.Rounding
            || row.CldrCashDigits != rule.CashDigits
            || row.CldrCashRounding != rule.CashRounding
            || row.CldrStandardDigitsExplicit != rule.DigitsExplicit
            || row.CldrStandardRoundingExplicit != rule.RoundingExplicit
            || row.CldrCashDigitsExplicit != rule.CashDigitsExplicit
            || row.CldrCashRoundingExplicit != rule.CashRoundingExplicit)
        {
            throw Invalid($"Cross-source reconciliation has incorrect CLDR format rule for '{alphabeticCode}'.");
        }
    }

    private static void ValidateTerritoryReconciliationCompleteness(
        IsoListOneData listOneData,
        IsoListThreeData listThreeData,
        TerritoryReconciliationData territoryReconciliations)
    {
        HashSet<string> expectedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListOneRow row in listOneData.Rows)
        {
            expectedKeys.Add(CreateSourceEntityKey("list-one", row.EntityName));
        }

        foreach (IsoListThreeRow row in listThreeData.Rows)
        {
            expectedKeys.Add(CreateSourceEntityKey("list-three", row.EntityName));
        }

        HashSet<string> actualKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (TerritoryReconciliation reconciliation in territoryReconciliations.Reconciliations)
        {
            string key = CreateSourceEntityKey(
                reconciliation.SourceList,
                reconciliation.SourceEntityName);
            if (!actualKeys.Add(key))
            {
                throw Invalid(
                    $"Territory reconciliation repeats '{reconciliation.SourceList}:{reconciliation.SourceEntityName}'.");
            }
        }

        if (!expectedKeys.SetEquals(actualKeys))
        {
            throw Invalid("Territory reconciliation does not cover every unique SIX source entity exactly once.");
        }
    }

    private static void ValidateForwardReverseMappings(CldrCurrencyUsageData usageData)
    {
        Dictionary<string, List<CldrCurrencyUsage>> forward =
            new Dictionary<string, List<CldrCurrencyUsage>>(StringComparer.Ordinal);
        Dictionary<string, List<CldrCurrencyUsage>> reverse =
            new Dictionary<string, List<CldrCurrencyUsage>>(StringComparer.Ordinal);
        Dictionary<string, List<CldrCurrencyUsage>> pairs =
            new Dictionary<string, List<CldrCurrencyUsage>>(StringComparer.Ordinal);

        foreach (CldrCurrencyUsage usage in usageData.Usages)
        {
            AddUsage(forward, usage.TerritoryCode, usage);
            AddUsage(reverse, usage.CurrencyCode, usage);
            AddUsage(pairs, usage.TerritoryCode + "\0" + usage.CurrencyCode, usage);
        }

        int forwardCount = ValidateIndexReferences(forward, usageData.Usages, true);
        int reverseCount = ValidateIndexReferences(reverse, usageData.Usages, false);
        if (forwardCount != usageData.Usages.Count || reverseCount != usageData.Usages.Count)
        {
            throw Invalid("Forward/reverse currency-usage indexes do not contain every source row exactly once.");
        }

        foreach (KeyValuePair<string, List<CldrCurrencyUsage>> pair in pairs)
        {
            List<CldrCurrencyUsage> rows = pair.Value;
            rows.Sort(CompareUsageStarts);
            for (int index = 1; index < rows.Count; index++)
            {
                CldrCurrencyUsage previous = rows[index - 1];
                CldrCurrencyUsage current = rows[index];
                if ((!previous.HasInclusiveEndDate
                        || !current.HasInclusiveStartDate
                        || previous.InclusiveEndDate >= current.InclusiveStartDate))
                {
                    throw Invalid(
                        $"CLDR usage intervals overlap for '{current.TerritoryCode}:{current.CurrencyCode}'.");
                }
            }
        }
    }

    private static int ValidateIndexReferences(
        Dictionary<string, List<CldrCurrencyUsage>> index,
        IReadOnlyList<CldrCurrencyUsage> sourceRows,
        bool forward)
    {
        HashSet<CldrCurrencyUsage> references = new HashSet<CldrCurrencyUsage>();
        int count = 0;
        foreach (KeyValuePair<string, List<CldrCurrencyUsage>> entry in index)
        {
            foreach (CldrCurrencyUsage usage in entry.Value)
            {
                string expectedKey = forward ? usage.TerritoryCode : usage.CurrencyCode;
                if (entry.Key != expectedKey || !references.Add(usage))
                {
                    throw Invalid("Currency-usage index contains a wrong or duplicate row reference.");
                }

                count++;
            }
        }

        foreach (CldrCurrencyUsage sourceRow in sourceRows)
        {
            if (!references.Contains(sourceRow))
            {
                throw Invalid("Currency-usage index omits a source row.");
            }
        }

        return count;
    }

    private static void AddUsage(
        Dictionary<string, List<CldrCurrencyUsage>> index,
        string key,
        CldrCurrencyUsage usage)
    {
        if (!index.TryGetValue(key, out List<CldrCurrencyUsage>? rows))
        {
            rows = new List<CldrCurrencyUsage>();
            index.Add(key, rows);
        }

        rows.Add(usage);
    }

    private static int CompareUsageStarts(CldrCurrencyUsage left, CldrCurrencyUsage right)
    {
        if (!left.HasInclusiveStartDate)
        {
            return right.HasInclusiveStartDate ? -1 : 0;
        }

        if (!right.HasInclusiveStartDate)
        {
            return 1;
        }

        return left.InclusiveStartDate.CompareTo(right.InclusiveStartDate);
    }

    private static void ValidateOptionalCurrencyIdentity(
        string? alphabeticCode,
        string? numericCode,
        string sourceName)
    {
        if (alphabeticCode != null && !IsAlphabeticCode(alphabeticCode))
        {
            throw Invalid($"{sourceName} contains invalid alphabetic code '{alphabeticCode}'.");
        }

        if (numericCode != null && alphabeticCode == null)
        {
            throw Invalid($"{sourceName} contains a numeric code without an alphabetic code.");
        }

        ValidateOptionalNumericCode(numericCode, sourceName);
    }

    private static void ValidateOptionalNumericCode(string? numericCode, string sourceName)
    {
        if (numericCode != null && !IsNumericCode(numericCode))
        {
            throw Invalid($"{sourceName} contains non-canonical numeric code '{numericCode}'.");
        }
    }

    private static void ValidateNumericTextAndValue(
        string? numericText,
        bool hasNumericValue,
        int numericValue,
        string context)
    {
        if (hasNumericValue != (numericText != null))
        {
            throw Invalid($"{context} has inconsistent numeric-code presence.");
        }

        if (numericText == null)
        {
            if (numericValue != 0)
            {
                throw Invalid($"{context} has a numeric value without numeric text.");
            }

            return;
        }

        if (!IsNumericCode(numericText)
            || !int.TryParse(
                numericText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int parsedValue)
            || parsedValue != numericValue)
        {
            throw Invalid($"{context} has inconsistent canonical numeric text/value.");
        }
    }

    private static void ValidateMinorUnitRange(bool hasValue, int value, string context)
    {
        if (hasValue && (value < 0 || value > 28))
        {
            throw Invalid($"{context} minor-unit digits are outside decimal scale range 0..28.");
        }

        if (!hasValue && value != 0)
        {
            throw Invalid($"{context} has a minor-unit value without presence.");
        }
    }

    private static void ValidateFractionRuleRange(CldrFractionRule rule)
    {
        if (rule.Digits < 0
            || rule.Digits > 28
            || rule.CashDigits < 0
            || rule.CashDigits > 28
            || rule.Rounding < 0
            || rule.CashRounding < 0)
        {
            throw Invalid($"CLDR fraction rule '{rule.CurrencyCode}' has an out-of-range value.");
        }
    }

    private static void ValidatePartialDate(IsoPartialDate date, string context)
    {
        if (!date.IsKnown)
        {
            if (date.Precision != IsoDatePrecision.Unknown
                || date.Year != 0
                || date.Month != 0
                || date.Day != 0)
            {
                throw Invalid($"{context} has inconsistent unknown date fields.");
            }

            return;
        }

        if (date.Year < 1 || date.Year > 9999)
        {
            throw Invalid($"{context} year is out of range.");
        }

        if (date.Precision == IsoDatePrecision.Year)
        {
            if (date.Month != 0 || date.Day != 0)
            {
                throw Invalid($"{context} has components beyond year precision.");
            }

            return;
        }

        if (date.Precision == IsoDatePrecision.Month)
        {
            if (date.Month < 1 || date.Month > 12 || date.Day != 0)
            {
                throw Invalid($"{context} has invalid month precision fields.");
            }

            return;
        }

        if (date.Precision != IsoDatePrecision.Day
            || date.Month < 1
            || date.Month > 12
            || date.Day < 1
            || date.Day > DateTime.DaysInMonth(date.Year, date.Month))
        {
            throw Invalid($"{context} has invalid day precision fields.");
        }
    }

    private static DateOnly NormalizeStartDate(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 1 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day ? date.Day : 1;
        return new DateOnly(date.Year, month, day);
    }

    private static DateOnly NormalizeEndDate(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 12 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day
            ? date.Day
            : DateTime.DaysInMonth(date.Year, month);
        return new DateOnly(date.Year, month, day);
    }

    private static string CreateActiveIdentity(IsoListOneRow row)
    {
        return row.OfficialName
            + "\0"
            + (row.NumericCode ?? string.Empty)
            + "\0"
            + row.HasMinorUnit
            + "\0"
            + row.MinorUnitDigits.ToString(CultureInfo.InvariantCulture)
            + "\0"
            + row.IsFund;
    }

    private static string CreateHistoricalRowIdentity(IsoListThreeRow row)
    {
        return row.EntityName
            + "\0"
            + row.OfficialName
            + "\0"
            + row.AlphabeticCode
            + "\0"
            + (row.NumericCode ?? string.Empty)
            + "\0"
            + row.WithdrawalText;
    }

    private static string CreateStatusIdentity(
        string entityName,
        string officialName,
        string alphabeticCode,
        string? numericCode)
    {
        return entityName
            + "\0"
            + officialName
            + "\0"
            + alphabeticCode
            + "\0"
            + (numericCode ?? string.Empty);
    }

    private static string CreateUsageIdentity(CldrCurrencyUsage usage)
    {
        return usage.TerritoryCode
            + "\0"
            + usage.CurrencyCode
            + "\0"
            + FormatPartialDate(usage.SourceStartDate)
            + "\0"
            + FormatPartialDate(usage.SourceEndDate)
            + "\0"
            + usage.Tender
            + "\0"
            + (usage.StartTimeZone ?? string.Empty)
            + "\0"
            + (usage.EndTimeZone ?? string.Empty);
    }

    private static string FormatPartialDate(IsoPartialDate date)
    {
        return date.IsKnown
            ? date.Year.ToString("D4", CultureInfo.InvariantCulture)
                + ":"
                + date.Month.ToString("D2", CultureInfo.InvariantCulture)
                + ":"
                + date.Day.ToString("D2", CultureInfo.InvariantCulture)
                + ":"
                + ((int)date.Precision).ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string CreateSourceEntityKey(string sourceList, string entityName)
    {
        return sourceList + "\0" + entityName;
    }

    private static bool IsAlphabeticCode(string value)
    {
        return value.Length == 3 && ContainsAsciiLetters(value);
    }

    private static bool IsTerritoryCode(string value)
    {
        return value.Length == 2 && ContainsAsciiLetters(value);
    }

    private static bool IsTerritoryAliasCode(string value)
    {
        return IsTerritoryCode(value) || IsAlphabeticCode(value) || IsNumericCode(value);
    }

    private static bool IsNumericCode(string value)
    {
        return value.Length == 3 && ContainsAsciiDigits(value);
    }

    private static bool ContainsAsciiLetters(string value)
    {
        foreach (char character in value)
        {
            if (character < 'A' || character > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsAsciiDigits(string value)
    {
        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return value.Length != 0;
    }

    private static InvalidDataException Invalid(string message)
    {
        return new InvalidDataException("Data invariant failed: " + message);
    }
}
