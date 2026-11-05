using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CurrencyCodes.DataGenerator;

internal sealed class TerritoryReconciler
{
    private static readonly string[] OverlayHeaders =
    {
        "source_list",
        "six_entity_name",
        "canonical_name",
        "territory_kind",
        "cldr_match_code",
        "replacement_codes",
        "un_m49_codes",
        "review_basis"
    };

    private static readonly HashSet<string> TerritoryKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "Country",
        "DependentTerritory",
        "SupranationalOrganization",
        "MonetaryInstitution",
        "OtherSpecialEntity"
    };

    private static readonly TimeSpan HtmlRegexTimeout = TimeSpan.FromSeconds(5);

    internal TerritoryReconciliationData Reconcile(
        IReadOnlyList<IsoListOneRow> listOneRows,
        IReadOnlyList<IsoListThreeRow> listThreeRows,
        CldrTerritoryCodeData cldrData,
        string unM49Path,
        string overlayPath)
    {
        Dictionary<string, CldrTerritoryCode> cldrCodes = IndexCldrCodes(cldrData.TerritoryCodes);
        Dictionary<string, CldrTerritoryAlias> cldrAliases = IndexCldrAliases(cldrData.Aliases);
        List<UnM49Territory> unTerritories = ReadUnM49(unM49Path);
        Dictionary<string, UnM49Territory> unByName = IndexUnByName(unTerritories);
        Dictionary<string, UnM49Territory> unByAlpha2 = IndexUnByAlpha2(unTerritories);
        List<TerritoryNameMapEntry> overlayEntries = ReadOverlay(overlayPath);
        Dictionary<string, TerritoryNameMapEntry> overlayByKey = IndexOverlay(overlayEntries);
        ValidateOverlay(overlayEntries, cldrCodes, cldrAliases, unByAlpha2);

        List<TerritoryReconciliation> reconciliations = new List<TerritoryReconciliation>();
        HashSet<string> usedOverlayKeys = new HashSet<string>(StringComparer.Ordinal);
        List<string> unmappedEntities = new List<string>();
        ReconcileListOne(
            listOneRows,
            cldrCodes,
            unByName,
            overlayByKey,
            usedOverlayKeys,
            unmappedEntities,
            reconciliations);
        ReconcileListThree(
            listThreeRows,
            cldrCodes,
            unByName,
            overlayByKey,
            usedOverlayKeys,
            unmappedEntities,
            reconciliations);

        List<string> staleOverlayKeys = new List<string>();
        foreach (TerritoryNameMapEntry entry in overlayEntries)
        {
            string key = CreateKey(entry.SourceList, entry.SixEntityName);
            if (!usedOverlayKeys.Contains(key))
            {
                staleOverlayKeys.Add($"{entry.SourceList}:{entry.SixEntityName}");
            }
        }

        if (unmappedEntities.Count != 0 || staleOverlayKeys.Count != 0)
        {
            throw new InvalidDataException(CreateCoverageError(unmappedEntities, staleOverlayKeys));
        }

        return new TerritoryReconciliationData(reconciliations.AsReadOnly());
    }

    private static void ReconcileListOne(
        IReadOnlyList<IsoListOneRow> rows,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, UnM49Territory> unByName,
        Dictionary<string, TerritoryNameMapEntry> overlayByKey,
        HashSet<string> usedOverlayKeys,
        List<string> unmappedEntities,
        List<TerritoryReconciliation> reconciliations)
    {
        HashSet<string> entityNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListOneRow row in rows)
        {
            if (entityNames.Add(row.EntityName))
            {
                ReconcileEntity(
                    "list-one",
                    row.EntityName,
                    cldrCodes,
                    unByName,
                    overlayByKey,
                    usedOverlayKeys,
                    unmappedEntities,
                    reconciliations);
            }
        }
    }

    private static void ReconcileListThree(
        IReadOnlyList<IsoListThreeRow> rows,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, UnM49Territory> unByName,
        Dictionary<string, TerritoryNameMapEntry> overlayByKey,
        HashSet<string> usedOverlayKeys,
        List<string> unmappedEntities,
        List<TerritoryReconciliation> reconciliations)
    {
        HashSet<string> entityNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (IsoListThreeRow row in rows)
        {
            if (entityNames.Add(row.EntityName))
            {
                ReconcileEntity(
                    "list-three",
                    row.EntityName,
                    cldrCodes,
                    unByName,
                    overlayByKey,
                    usedOverlayKeys,
                    unmappedEntities,
                    reconciliations);
            }
        }
    }

    private static void ReconcileEntity(
        string sourceList,
        string entityName,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, UnM49Territory> unByName,
        Dictionary<string, TerritoryNameMapEntry> overlayByKey,
        HashSet<string> usedOverlayKeys,
        List<string> unmappedEntities,
        List<TerritoryReconciliation> reconciliations)
    {
        string key = CreateKey(sourceList, entityName);
        if (overlayByKey.TryGetValue(key, out TerritoryNameMapEntry? entry))
        {
            usedOverlayKeys.Add(key);
            reconciliations.Add(CreateExplicitReconciliation(entry, cldrCodes));
            return;
        }

        if (!unByName.TryGetValue(entityName, out UnM49Territory? unTerritory))
        {
            unmappedEntities.Add($"{sourceList}:{entityName}");
            return;
        }

        if (!cldrCodes.TryGetValue(unTerritory.Alpha2Code, out CldrTerritoryCode? cldrCode))
        {
            throw new InvalidDataException(
                $"UN M49 territory '{unTerritory.Name}' uses alpha-2 '{unTerritory.Alpha2Code}', which is absent from CLDR.");
        }

        ValidateUnAndCldrIdentity(unTerritory, cldrCode);
        reconciliations.Add(new TerritoryReconciliation(
            sourceList,
            entityName,
            unTerritory.Name,
            null,
            cldrCode.Alpha2Code,
            cldrCode.Alpha2Code,
            cldrCode.Alpha3Code,
            cldrCode.NumericCodeText,
            cldrCode.HasNumericCode,
            cldrCode.NumericCodeValue,
            Array.AsReadOnly(Array.Empty<string>()),
            false));
    }

    private static TerritoryReconciliation CreateExplicitReconciliation(
        TerritoryNameMapEntry entry,
        Dictionary<string, CldrTerritoryCode> cldrCodes)
    {
        CldrTerritoryCode? cldrCode = null;
        if (entry.CldrMatchCode != null)
        {
            cldrCodes.TryGetValue(entry.CldrMatchCode, out cldrCode);
        }

        return new TerritoryReconciliation(
            entry.SourceList,
            entry.SixEntityName,
            entry.CanonicalName,
            entry.TerritoryKind,
            entry.CldrMatchCode,
            cldrCode?.Alpha2Code,
            cldrCode?.Alpha3Code,
            cldrCode?.NumericCodeText,
            cldrCode != null && cldrCode.HasNumericCode,
            cldrCode?.NumericCodeValue ?? 0,
            entry.ReplacementCodes,
            true);
    }

    private static Dictionary<string, CldrTerritoryCode> IndexCldrCodes(
        IReadOnlyList<CldrTerritoryCode> territoryCodes)
    {
        Dictionary<string, CldrTerritoryCode> result = new Dictionary<string, CldrTerritoryCode>(StringComparer.Ordinal);
        foreach (CldrTerritoryCode territoryCode in territoryCodes)
        {
            if (!result.TryAdd(territoryCode.Alpha2Code, territoryCode))
            {
                throw new InvalidDataException($"Duplicate indexed CLDR alpha-2 code '{territoryCode.Alpha2Code}'.");
            }
        }

        return result;
    }

    private static Dictionary<string, CldrTerritoryAlias> IndexCldrAliases(
        IReadOnlyList<CldrTerritoryAlias> aliases)
    {
        Dictionary<string, CldrTerritoryAlias> result = new Dictionary<string, CldrTerritoryAlias>(StringComparer.Ordinal);
        foreach (CldrTerritoryAlias alias in aliases)
        {
            if (!result.TryAdd(alias.AliasCode, alias))
            {
                throw new InvalidDataException($"Duplicate indexed CLDR territory alias '{alias.AliasCode}'.");
            }
        }

        return result;
    }

    private static Dictionary<string, UnM49Territory> IndexUnByName(List<UnM49Territory> territories)
    {
        Dictionary<string, UnM49Territory> result = new Dictionary<string, UnM49Territory>(StringComparer.OrdinalIgnoreCase);
        foreach (UnM49Territory territory in territories)
        {
            if (!result.TryAdd(territory.Name, territory))
            {
                throw new InvalidDataException($"Duplicate UN M49 English name '{territory.Name}'.");
            }
        }

        return result;
    }

    private static Dictionary<string, UnM49Territory> IndexUnByAlpha2(List<UnM49Territory> territories)
    {
        Dictionary<string, UnM49Territory> result = new Dictionary<string, UnM49Territory>(StringComparer.Ordinal);
        foreach (UnM49Territory territory in territories)
        {
            if (!result.TryAdd(territory.Alpha2Code, territory))
            {
                throw new InvalidDataException($"Duplicate UN M49 alpha-2 code '{territory.Alpha2Code}'.");
            }
        }

        return result;
    }

    private static Dictionary<string, TerritoryNameMapEntry> IndexOverlay(
        List<TerritoryNameMapEntry> entries)
    {
        Dictionary<string, TerritoryNameMapEntry> result = new Dictionary<string, TerritoryNameMapEntry>(StringComparer.Ordinal);
        foreach (TerritoryNameMapEntry entry in entries)
        {
            string key = CreateKey(entry.SourceList, entry.SixEntityName);
            if (!result.TryAdd(key, entry))
            {
                throw new InvalidDataException(
                    $"Duplicate territory-name-map key '{entry.SourceList}:{entry.SixEntityName}' at line {entry.LineNumber}.");
            }
        }

        return result;
    }

    private static void ValidateOverlay(
        List<TerritoryNameMapEntry> entries,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, CldrTerritoryAlias> cldrAliases,
        Dictionary<string, UnM49Territory> unByAlpha2)
    {
        foreach (TerritoryNameMapEntry entry in entries)
        {
            if (entry.CldrMatchCode == null)
            {
                if (entry.ReplacementCodes.Count != 0 || entry.UnM49Codes.Count != 0)
                {
                    throw OverlayInvalid(entry, "A no-code entity cannot define replacements or UN M49 codes.");
                }

                continue;
            }

            bool hasCode = cldrCodes.ContainsKey(entry.CldrMatchCode);
            bool hasAlias = cldrAliases.TryGetValue(entry.CldrMatchCode, out CldrTerritoryAlias? alias);
            if (!hasCode && !hasAlias)
            {
                throw OverlayInvalid(entry, $"Unknown CLDR match code '{entry.CldrMatchCode}'.");
            }

            if (hasAlias)
            {
                ValidateAliasReplacement(entry, alias!);
            }
            else if (entry.ReplacementCodes.Count != 0)
            {
                throw OverlayInvalid(entry, $"CLDR match code '{entry.CldrMatchCode}' is not an alias.");
            }

            if (entry.ReplacementCodes.Count != 0)
            {
                ValidateSuccessorCodes(entry, cldrCodes, unByAlpha2);
            }
            else if (entry.UnM49Codes.Count != 0)
            {
                ValidateDirectUnCrossCheck(entry, cldrCodes, unByAlpha2);
            }
        }
    }

    private static void ValidateAliasReplacement(
        TerritoryNameMapEntry entry,
        CldrTerritoryAlias alias)
    {
        if (entry.ReplacementCodes.Count != alias.ReplacementCodes.Count)
        {
            throw OverlayInvalid(entry, $"Replacement list for '{entry.CldrMatchCode}' does not match CLDR.");
        }

        for (int index = 0; index < entry.ReplacementCodes.Count; index++)
        {
            if (entry.ReplacementCodes[index] != alias.ReplacementCodes[index])
            {
                throw OverlayInvalid(entry, $"Replacement list for '{entry.CldrMatchCode}' does not match CLDR.");
            }
        }
    }

    private static void ValidateSuccessorCodes(
        TerritoryNameMapEntry entry,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, UnM49Territory> unByAlpha2)
    {
        if (entry.UnM49Codes.Count != entry.ReplacementCodes.Count)
        {
            throw OverlayInvalid(entry, "The UN M49 successor count does not match the replacement count.");
        }

        for (int index = 0; index < entry.ReplacementCodes.Count; index++)
        {
            string replacementCode = entry.ReplacementCodes[index];
            if (!unByAlpha2.TryGetValue(replacementCode, out UnM49Territory? territory))
            {
                throw OverlayInvalid(entry, $"CLDR replacement '{replacementCode}' is absent from UN M49.");
            }

            if (entry.UnM49Codes[index] != territory.NumericCode)
            {
                throw OverlayInvalid(
                    entry,
                    $"UN M49 code '{entry.UnM49Codes[index]}' does not match replacement '{replacementCode}'.");
            }

            if (!cldrCodes.TryGetValue(replacementCode, out CldrTerritoryCode? cldrCode))
            {
                throw OverlayInvalid(entry, $"CLDR replacement '{replacementCode}' has no territory mapping.");
            }

            ValidateUnAndCldrIdentity(territory, cldrCode);
        }
    }

    private static void ValidateDirectUnCrossCheck(
        TerritoryNameMapEntry entry,
        Dictionary<string, CldrTerritoryCode> cldrCodes,
        Dictionary<string, UnM49Territory> unByAlpha2)
    {
        if (entry.CldrMatchCode == null
            || !unByAlpha2.TryGetValue(entry.CldrMatchCode, out UnM49Territory? territory))
        {
            throw OverlayInvalid(entry, "A direct UN M49 cross-check requires an alpha-2 match code.");
        }

        if (entry.UnM49Codes.Count != 1 || entry.UnM49Codes[0] != territory.NumericCode)
        {
            throw OverlayInvalid(entry, $"UN M49 numeric code does not match '{entry.CldrMatchCode}'.");
        }

        if (entry.CanonicalName != territory.Name)
        {
            throw OverlayInvalid(entry, $"Canonical name does not match UN M49 name '{territory.Name}'.");
        }

        if (!cldrCodes.TryGetValue(entry.CldrMatchCode, out CldrTerritoryCode? cldrCode))
        {
            throw OverlayInvalid(entry, $"CLDR match code '{entry.CldrMatchCode}' has no territory mapping.");
        }

        ValidateUnAndCldrIdentity(territory, cldrCode);
    }

    private static void ValidateUnAndCldrIdentity(UnM49Territory unTerritory, CldrTerritoryCode cldrCode)
    {
        if (cldrCode.Alpha3Code != unTerritory.Alpha3Code
            || cldrCode.NumericCodeText != unTerritory.NumericCode)
        {
            throw new InvalidDataException(
                $"CLDR and UN M49 identity differs for alpha-2 '{unTerritory.Alpha2Code}'.");
        }
    }

    private static List<TerritoryNameMapEntry> ReadOverlay(string path)
    {
        string[] lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        if (lines.Length == 0)
        {
            throw new InvalidDataException("territory-name-map.csv is empty.");
        }

        List<string> headers = ParseCsvLine(lines[0], 1);
        if (headers.Count != OverlayHeaders.Length)
        {
            throw new InvalidDataException("territory-name-map.csv has an invalid header.");
        }

        for (int index = 0; index < OverlayHeaders.Length; index++)
        {
            if (headers[index] != OverlayHeaders[index])
            {
                throw new InvalidDataException("territory-name-map.csv has an invalid header.");
            }
        }

        List<TerritoryNameMapEntry> entries = new List<TerritoryNameMapEntry>();
        for (int index = 1; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            if (lines[index].Length == 0)
            {
                throw new InvalidDataException($"territory-name-map.csv contains a blank line at line {lineNumber}.");
            }

            List<string> fields = ParseCsvLine(lines[index], lineNumber);
            if (fields.Count != OverlayHeaders.Length)
            {
                throw new InvalidDataException(
                    $"territory-name-map.csv line {lineNumber} has {fields.Count} fields; expected {OverlayHeaders.Length}.");
            }

            ValidateRequiredOverlayFields(fields, lineNumber);
            entries.Add(new TerritoryNameMapEntry(
                lineNumber,
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                EmptyToNull(fields[4]),
                ParseSpaceSeparatedCodes(fields[5], lineNumber, "replacement_codes"),
                ParseSpaceSeparatedCodes(fields[6], lineNumber, "un_m49_codes"),
                fields[7]));
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException("territory-name-map.csv must contain at least one data row.");
        }

        return entries;
    }

    private static void ValidateRequiredOverlayFields(List<string> fields, int lineNumber)
    {
        if (fields[0] != "list-one" && fields[0] != "list-three")
        {
            throw new InvalidDataException($"territory-name-map.csv has an invalid source_list at line {lineNumber}.");
        }

        if (fields[1].Length == 0 || fields[2].Length == 0 || fields[7].Length == 0)
        {
            throw new InvalidDataException($"territory-name-map.csv has an empty required field at line {lineNumber}.");
        }

        if (!TerritoryKinds.Contains(fields[3]))
        {
            throw new InvalidDataException($"territory-name-map.csv has an invalid territory_kind at line {lineNumber}.");
        }
    }

    private static List<string> ParseCsvLine(string line, int lineNumber)
    {
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false;
        bool quoteClosed = false;
        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                        quoteClosed = true;
                    }
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                quoteClosed = false;
            }
            else if (character == '"' && field.Length == 0 && !quoteClosed)
            {
                quoted = true;
            }
            else if (character == '"')
            {
                throw new InvalidDataException($"Invalid CSV quoting at line {lineNumber}.");
            }
            else if (quoteClosed)
            {
                throw new InvalidDataException($"Invalid CSV quoting at line {lineNumber}.");
            }
            else
            {
                field.Append(character);
            }
        }

        if (quoted)
        {
            throw new InvalidDataException($"Unclosed CSV quote at line {lineNumber}.");
        }

        fields.Add(field.ToString());
        return fields;
    }

    private static IReadOnlyList<string> ParseSpaceSeparatedCodes(
        string text,
        int lineNumber,
        string fieldName)
    {
        if (text.Length == 0)
        {
            return Array.AsReadOnly(Array.Empty<string>());
        }

        string[] values = text.Split(' ');
        foreach (string value in values)
        {
            if (!IsAlpha2Code(value) && !IsNumericCode(value))
            {
                throw new InvalidDataException(
                    $"territory-name-map.csv has an invalid {fieldName} value at line {lineNumber}.");
            }
        }

        return Array.AsReadOnly(values);
    }

    private static List<UnM49Territory> ReadUnM49(string path)
    {
        string html = File.ReadAllText(path, new UTF8Encoding(false, true));
        string withoutComments = Regex.Replace(
            html,
            "<!--.*?-->",
            string.Empty,
            RegexOptions.Singleline,
            HtmlRegexTimeout);
        Match tableMatch = Regex.Match(
            withoutComments,
            "<table\\b[^>]*\\bid\\s*=\\s*[\"']downloadTableEN[\"'][^>]*>(.*?)</table>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            HtmlRegexTimeout);
        if (!tableMatch.Success)
        {
            throw new InvalidDataException("UN M49 HTML is missing the English overview table.");
        }

        Match bodyMatch = Regex.Match(
            tableMatch.Groups[1].Value,
            "<tbody\\b[^>]*>(.*?)</tbody>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            HtmlRegexTimeout);
        if (!bodyMatch.Success)
        {
            throw new InvalidDataException("UN M49 English overview table is missing tbody.");
        }

        MatchCollection rowMatches = Regex.Matches(
            bodyMatch.Groups[1].Value,
            "<tr\\b[^>]*>(.*?)</tr>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            HtmlRegexTimeout);
        List<UnM49Territory> territories = new List<UnM49Territory>();
        foreach (Match rowMatch in rowMatches)
        {
            MatchCollection cellMatches = Regex.Matches(
                rowMatch.Groups[1].Value,
                "<td\\b[^>]*>(.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                HtmlRegexTimeout);
            if (cellMatches.Count != 15)
            {
                throw new InvalidDataException(
                    $"UN M49 English overview row has {cellMatches.Count} cells; expected 15.");
            }

            string name = ReadHtmlCell(cellMatches[8].Groups[1].Value);
            string numericCode = ReadHtmlCell(cellMatches[9].Groups[1].Value);
            string alpha2Code = ReadHtmlCell(cellMatches[10].Groups[1].Value);
            string alpha3Code = ReadHtmlCell(cellMatches[11].Groups[1].Value);
            if (name.Length == 0
                || !IsNumericCode(numericCode)
                || !IsAlpha2Code(alpha2Code)
                || !IsAlpha3Code(alpha3Code))
            {
                throw new InvalidDataException("UN M49 English overview contains an invalid country/area row.");
            }

            territories.Add(new UnM49Territory(name, numericCode, alpha2Code, alpha3Code));
        }

        if (territories.Count == 0)
        {
            throw new InvalidDataException("UN M49 English overview table contains no country/area rows.");
        }

        return territories;
    }

    private static string ReadHtmlCell(string html)
    {
        string withoutTags = Regex.Replace(
            html,
            "<[^>]+>",
            string.Empty,
            RegexOptions.Singleline,
            HtmlRegexTimeout);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static bool IsAlpha2Code(string value)
    {
        return value.Length == 2 && IsAsciiLetters(value);
    }

    private static bool IsAlpha3Code(string value)
    {
        return value.Length == 3 && IsAsciiLetters(value);
    }

    private static bool IsAsciiLetters(string value)
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

    private static bool IsNumericCode(string value)
    {
        if (value.Length != 3)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int _);
    }

    private static string? EmptyToNull(string value)
    {
        return value.Length == 0 ? null : value;
    }

    private static string CreateKey(string sourceList, string entityName)
    {
        return sourceList + "\0" + entityName;
    }

    private static InvalidDataException OverlayInvalid(TerritoryNameMapEntry entry, string message)
    {
        return new InvalidDataException($"{message} territory-name-map.csv line {entry.LineNumber}.");
    }

    private static string CreateCoverageError(List<string> unmappedEntities, List<string> staleOverlayKeys)
    {
        StringBuilder message = new StringBuilder("Territory reconciliation coverage failed.");
        if (unmappedEntities.Count != 0)
        {
            message.Append(" Unmapped SIX entities: ");
            message.Append(string.Join(" | ", unmappedEntities));
            message.Append('.');
        }

        if (staleOverlayKeys.Count != 0)
        {
            message.Append(" Stale territory-name-map rows: ");
            message.Append(string.Join(" | ", staleOverlayKeys));
            message.Append('.');
        }

        return message.ToString();
    }

    private sealed class TerritoryNameMapEntry
    {
        internal TerritoryNameMapEntry(
            int lineNumber,
            string sourceList,
            string sixEntityName,
            string canonicalName,
            string territoryKind,
            string? cldrMatchCode,
            IReadOnlyList<string> replacementCodes,
            IReadOnlyList<string> unM49Codes,
            string reviewBasis)
        {
            LineNumber = lineNumber;
            SourceList = sourceList;
            SixEntityName = sixEntityName;
            CanonicalName = canonicalName;
            TerritoryKind = territoryKind;
            CldrMatchCode = cldrMatchCode;
            ReplacementCodes = replacementCodes;
            UnM49Codes = unM49Codes;
            ReviewBasis = reviewBasis;
        }

        internal int LineNumber { get; }

        internal string SourceList { get; }

        internal string SixEntityName { get; }

        internal string CanonicalName { get; }

        internal string TerritoryKind { get; }

        internal string? CldrMatchCode { get; }

        internal IReadOnlyList<string> ReplacementCodes { get; }

        internal IReadOnlyList<string> UnM49Codes { get; }

        internal string ReviewBasis { get; }
    }

    private sealed class UnM49Territory
    {
        internal UnM49Territory(string name, string numericCode, string alpha2Code, string alpha3Code)
        {
            Name = name;
            NumericCode = numericCode;
            Alpha2Code = alpha2Code;
            Alpha3Code = alpha3Code;
        }

        internal string Name { get; }

        internal string NumericCode { get; }

        internal string Alpha2Code { get; }

        internal string Alpha3Code { get; }
    }
}
