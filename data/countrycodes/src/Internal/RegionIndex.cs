namespace countrycodes;

internal static class RegionIndex
{
    internal static int GetRegionCount()
    {
        return RuntimeCatalog.GetState().Regions.Length;
    }

    internal static GeneratedGeographicRegionRow? GetRegionAt(int index)
    {
        if (index < 0 || index >= RuntimeCatalog.GetState().Regions.Length)
        {
            return null;
        }

        return RuntimeCatalog.GetState().Regions[index];
    }

    internal static GeneratedGeographicRegionRow? FindRegionByM49Code(string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.GeographicRegionM49,
            value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().Regions[rowIndex];
    }

    internal static GeneratedCountryRegionMappingRow? FindCountryRegionMapping(
        string? alpha2)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.CountryRegionAlpha2,
            alpha2);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().CountryRegionMappings[rowIndex];
    }

    internal static GeneratedGeographicRegionRow[] GetChildRegions(string? parentM49Code)
    {
        if (parentM49Code == null)
        {
            return new GeneratedGeographicRegionRow[0];
        }

        GeneratedGeographicRegionRow[] rows = RuntimeCatalog.GetState().Regions;
        int count = 0;
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index].HasParent &&
                AsciiCodeUtility.EqualsOrdinal(rows[index].ParentM49Code, parentM49Code))
            {
                count += 1;
            }

            index += 1;
        }

        GeneratedGeographicRegionRow[] result = new GeneratedGeographicRegionRow[count];
        index = 0;
        int resultIndex = 0;
        while (index < rows.Length)
        {
            if (rows[index].HasParent &&
                AsciiCodeUtility.EqualsOrdinal(rows[index].ParentM49Code, parentM49Code))
            {
                result[resultIndex] = rows[index];
                resultIndex += 1;
            }

            index += 1;
        }

        return result;
    }

    internal static GeneratedCountryRegionMappingRow[] GetCountryMappingsForRegion(
        string? m49Code)
    {
        if (m49Code == null)
        {
            return new GeneratedCountryRegionMappingRow[0];
        }

        GeneratedCountryRegionMappingRow[] rows = RuntimeCatalog.GetState().CountryRegionMappings;
        int count = 0;
        int index = 0;
        while (index < rows.Length)
        {
            if (MappingContainsRegion(rows[index], m49Code))
            {
                count += 1;
            }

            index += 1;
        }

        GeneratedCountryRegionMappingRow[] result =
            new GeneratedCountryRegionMappingRow[count];
        index = 0;
        int resultIndex = 0;
        while (index < rows.Length)
        {
            if (MappingContainsRegion(rows[index], m49Code))
            {
                result[resultIndex] = rows[index];
                resultIndex += 1;
            }

            index += 1;
        }

        return result;
    }

    internal static bool MappingContainsRegion(
        GeneratedCountryRegionMappingRow row,
        string m49Code)
    {
        return AsciiCodeUtility.EqualsOrdinal(row.WorldM49Code, m49Code) ||
               (row.HasRegion &&
                AsciiCodeUtility.EqualsOrdinal(row.RegionM49Code, m49Code)) ||
               (row.HasSubregion &&
                AsciiCodeUtility.EqualsOrdinal(row.SubregionM49Code, m49Code)) ||
               (row.HasIntermediateRegion &&
                AsciiCodeUtility.EqualsOrdinal(row.IntermediateRegionM49Code, m49Code));
    }
}
