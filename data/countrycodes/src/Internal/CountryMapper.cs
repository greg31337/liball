using System.Collections.Generic;

namespace countrycodes;

internal static class CountryMapper
{
    internal static Country MapCountry(GeneratedCountryRow row)
    {
        GeneratedCountryNameRow[] names = NameIndex.GetNamesForCountry(row.Alpha2);
        string shortName = string.Empty;
        string? officialName = null;
        List<string> aliases = new List<string>();
        int localizedCount = 0;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedCountryNameRow name = names[index];
            if (name.NameKind == GeneratedCountryNameKind.IsoShort)
            {
                shortName = name.DisplayName;
            }
            else if (name.NameKind == GeneratedCountryNameKind.IsoOfficial)
            {
                officialName = name.DisplayName;
            }
            else if (name.NameKind == GeneratedCountryNameKind.Localized)
            {
                localizedCount += 1;
            }
            else if (name.NameKind == GeneratedCountryNameKind.Common ||
                     name.NameKind == GeneratedCountryNameKind.Alias ||
                     name.NameKind == GeneratedCountryNameKind.Deprecated)
            {
                AddUniqueAlias(aliases, name.DisplayName);
            }

            index += 1;
        }

        LocalizedName[] localizedNames = new LocalizedName[localizedCount];
        index = 0;
        int localizedIndex = 0;
        while (index < names.Length)
        {
            GeneratedCountryNameRow name = names[index];
            if (name.NameKind == GeneratedCountryNameKind.Localized)
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

        string[] regionCodes = GetCountryRegionCodes(row.Alpha2);
        return new Country(
            row.Alpha2,
            row.Alpha3,
            row.NumericText,
            row.NumericValue,
            row.Alpha2,
            MapAssignmentStatus(row.AssignmentState),
            shortName,
            officialName,
            aliases.ToArray(),
            localizedNames,
            regionCodes,
            CountryFlagEmojiUtility.CreateForCountryRow(row));
    }

    internal static Country[] MapCountries(GeneratedCountryRow[] rows)
    {
        Country[] countries = new Country[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            countries[index] = MapCountry(rows[index]);
            index += 1;
        }

        return countries;
    }

    internal static void AddUniqueAlias(List<string> aliases, string value)
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

    internal static string[] GetCountryRegionCodes(string alpha2)
    {
        GeneratedCountryRegionMappingRow? mapping =
            RegionIndex.FindCountryRegionMapping(alpha2);
        if (mapping == null)
        {
            return new string[0];
        }

        int count = 1;
        if (mapping.HasRegion)
        {
            count += 1;
        }

        if (mapping.HasSubregion)
        {
            count += 1;
        }

        if (mapping.HasIntermediateRegion)
        {
            count += 1;
        }

        string[] result = new string[count];
        result[0] = mapping.WorldM49Code;
        int index = 1;
        if (mapping.HasRegion)
        {
            result[index] = mapping.RegionM49Code;
            index += 1;
        }

        if (mapping.HasSubregion)
        {
            result[index] = mapping.SubregionM49Code;
            index += 1;
        }

        if (mapping.HasIntermediateRegion)
        {
            result[index] = mapping.IntermediateRegionM49Code;
        }

        return result;
    }

    internal static CodeAssignmentStatus MapAssignmentStatus(
        GeneratedCodeAssignmentState status)
    {
        return (CodeAssignmentStatus)(int)status;
    }

}
