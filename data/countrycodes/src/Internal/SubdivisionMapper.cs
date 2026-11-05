using System.Collections.Generic;

namespace countrycodes;

internal static class SubdivisionMapper
{
    internal static Subdivision MapSubdivision(GeneratedSubdivisionRow row)
    {
        GeneratedSubdivisionNameRow[] names = NameIndex.GetNamesForSubdivision(row.Code);
        string canonicalName = string.Empty;
        List<string> aliases = new List<string>();
        int localizedCount = 0;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedSubdivisionNameRow name = names[index];
            if (name.NameKind == GeneratedSubdivisionNameKind.Canonical)
            {
                canonicalName = name.DisplayName;
            }
            else if (name.NameKind == GeneratedSubdivisionNameKind.Localized)
            {
                localizedCount += 1;
            }
            else
            {
                AddUniqueSubdivisionAlias(aliases, name.DisplayName);
            }

            index += 1;
        }

        AddReplacementAliases(row.Code, aliases);
        LocalizedName[] localizedNames = new LocalizedName[localizedCount];
        index = 0;
        int localizedIndex = 0;
        while (index < names.Length)
        {
            GeneratedSubdivisionNameRow name = names[index];
            if (name.NameKind == GeneratedSubdivisionNameKind.Localized)
            {
                localizedNames[localizedIndex] = new LocalizedName(
                    name.Locale,
                    name.DisplayName,
                    CountryNameKind.NativeOrLocalized,
                    name.SourceId);
                localizedIndex += 1;
            }

            index += 1;
        }

        GeneratedSubdivisionReplacementRow[] replacementRows =
            SubdivisionIndex.GetReplacementsForAlias(row.Code);
        string[] replacementCodes = new string[replacementRows.Length];
        index = 0;
        while (index < replacementRows.Length)
        {
            replacementCodes[index] = replacementRows[index].ReplacementCode;
            index += 1;
        }

        string? parentCode = null;
        if (row.HasParent)
        {
            parentCode = row.ParentCode;
        }

        return new Subdivision(
            row.Code,
            row.CountryCode,
            row.Suffix,
            canonicalName,
            row.Type,
            parentCode,
            GetSubdivisionStatus(row),
            aliases.ToArray(),
            localizedNames,
            replacementCodes);
    }

    internal static Subdivision MapSubdivisionAlias(GeneratedSubdivisionAliasRow row)
    {
        GeneratedSubdivisionReplacementRow[] replacementRows =
            SubdivisionIndex.GetReplacementsForAlias(row.Code);
        string[] replacementCodes = new string[replacementRows.Length];
        int index = 0;
        while (index < replacementRows.Length)
        {
            replacementCodes[index] = replacementRows[index].ReplacementCode;
            index += 1;
        }

        return new Subdivision(
            row.Code,
            row.CountryCode,
            row.Code.Substring(CountryCodeConstants.SubdivisionCountryCodeLength + 1),
            string.Empty,
            string.Empty,
            null,
            MapAliasStatus(row.Reason),
            new string[0],
            new LocalizedName[0],
            replacementCodes);
    }

    internal static Subdivision[] MapSubdivisions(GeneratedSubdivisionRow[] rows)
    {
        Subdivision[] subdivisions = new Subdivision[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            subdivisions[index] = MapSubdivision(rows[index]);
            index += 1;
        }

        return subdivisions;
    }

    internal static void AddReplacementAliases(string subdivisionCode, List<string> aliases)
    {
        GeneratedSubdivisionReplacementRow[] replacements = RuntimeCatalog.GetState().SubdivisionReplacements;
        int index = 0;
        while (index < replacements.Length)
        {
            GeneratedSubdivisionReplacementRow replacement = replacements[index];
            if (replacement.TargetKind ==
                    GeneratedSubdivisionReplacementTargetKind.Subdivision &&
                AsciiCodeUtility.EqualsOrdinal(
                    replacement.ReplacementCode,
                    subdivisionCode))
            {
                AddUniqueSubdivisionAlias(aliases, replacement.AliasCode);
            }

            index += 1;
        }
    }

    internal static void AddUniqueSubdivisionAlias(List<string> aliases, string value)
    {
        int index = 0;
        while (index < aliases.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(aliases[index], value))
            {
                return;
            }

            index += 1;
        }

        aliases.Add(value);
    }

    internal static SubdivisionStatus MapSubdivisionStatus(
        GeneratedSubdivisionStatus status)
    {
        return (SubdivisionStatus)(int)status;
    }

    internal static SubdivisionStatus GetSubdivisionStatus(GeneratedSubdivisionRow row)
    {
        GeneratedSubdivisionAliasRow? alias =
            SubdivisionIndex.FindSubdivisionAliasByCode(row.Code);
        if (alias != null)
        {
            return MapAliasStatus(alias.Reason);
        }

        return MapSubdivisionStatus(row.Status);
    }

    internal static SubdivisionStatus MapAliasStatus(
        GeneratedSubdivisionAliasReason reason)
    {
        if (reason == GeneratedSubdivisionAliasReason.Deprecated)
        {
            return SubdivisionStatus.Deprecated;
        }

        return SubdivisionStatus.Withdrawn;
    }
}
