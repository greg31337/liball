namespace countrycodes;

internal static class RegionMapper
{
    internal static GeographicRegion MapRegion(GeneratedGeographicRegionRow row)
    {
        GeneratedGeographicRegionNameRow[] nameRows =
            NameIndex.GetNamesForRegion(row.M49Code);
        LocalizedName[] localizedNames = new LocalizedName[nameRows.Length];
        string name = string.Empty;
        int index = 0;
        while (index < nameRows.Length)
        {
            GeneratedGeographicRegionNameRow nameRow = nameRows[index];
            localizedNames[index] = new LocalizedName(
                nameRow.Locale,
                nameRow.Name,
                CountryNameKind.NativeOrLocalized,
                nameRow.SourceId);
            if (AsciiCodeUtility.EqualsOrdinal(nameRow.Locale, "en"))
            {
                name = nameRow.Name;
            }

            index += 1;
        }

        GeneratedGeographicRegionRow[] childRows =
            RegionIndex.GetChildRegions(row.M49Code);
        string[] childCodes = new string[childRows.Length];
        index = 0;
        while (index < childRows.Length)
        {
            childCodes[index] = childRows[index].M49Code;
            index += 1;
        }

        GeneratedCountryRegionMappingRow[] mappingRows =
            RegionIndex.GetCountryMappingsForRegion(row.M49Code);
        string[] countryCodes = new string[mappingRows.Length];
        index = 0;
        while (index < mappingRows.Length)
        {
            countryCodes[index] = mappingRows[index].CountryAlpha2;
            index += 1;
        }

        string? parentM49Code = null;
        if (row.HasParent)
        {
            parentM49Code = row.ParentM49Code;
        }

        return new GeographicRegion(
            row.M49Code,
            row.M49Value,
            name,
            localizedNames,
            MapRegionLevel(row.Level),
            parentM49Code,
            childCodes,
            countryCodes);
    }

    internal static GeographicRegion[] MapRegions(GeneratedGeographicRegionRow[] rows)
    {
        GeographicRegion[] regions = new GeographicRegion[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            regions[index] = MapRegion(rows[index]);
            index += 1;
        }

        return regions;
    }

    internal static GeographicRegionLevel MapRegionLevel(
        GeneratedGeographicRegionLevel level)
    {
        return (GeographicRegionLevel)(int)level;
    }
}
