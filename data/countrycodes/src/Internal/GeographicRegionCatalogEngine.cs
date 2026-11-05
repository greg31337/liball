using System.Collections.Generic;

namespace countrycodes;

internal static class GeographicRegionCatalogEngine
{
    internal static GeographicRegion[] GetAll()
    {
        return RegionMapper.MapRegions(RuntimeCatalog.GetState().Regions);
    }

    internal static GeographicRegion[] GetByLevel(GeographicRegionLevel level)
    {
        List<GeneratedGeographicRegionRow> matches =
            new List<GeneratedGeographicRegionRow>();
        GeneratedGeographicRegionRow[] rows = RuntimeCatalog.GetState().Regions;
        int index = 0;
        while (index < rows.Length)
        {
            if (RegionMapper.MapRegionLevel(rows[index].Level) == level)
            {
                matches.Add(rows[index]);
            }

            index += 1;
        }

        return RegionMapper.MapRegions(matches.ToArray());
    }

    internal static GeographicRegion? FindByM49(string? m49Code)
    {
        GeneratedGeographicRegionRow? row = FindRegionRow(m49Code);
        if (row == null)
        {
            return null;
        }

        return RegionMapper.MapRegion(row);
    }

    internal static GeographicRegion[] GetAncestry(string? m49Code)
    {
        GeneratedGeographicRegionRow? row = FindRegionRow(m49Code);
        if (row == null)
        {
            return new GeographicRegion[0];
        }

        List<GeneratedGeographicRegionRow> ancestors =
            new List<GeneratedGeographicRegionRow>();
        List<string> visited = new List<string>();
        visited.Add(row.M49Code);
        GeneratedGeographicRegionRow current = row;
        while (current.HasParent)
        {
            if (ContainsCode(visited, current.ParentM49Code))
            {
                break;
            }

            GeneratedGeographicRegionRow? parent =
                RegionIndex.FindRegionByM49Code(current.ParentM49Code);
            if (parent == null)
            {
                break;
            }

            ancestors.Add(parent);
            visited.Add(parent.M49Code);
            current = parent;
        }

        return RegionMapper.MapRegions(ancestors.ToArray());
    }

    internal static GeographicRegion[] GetForCountry(string? countryAlpha2)
    {
        if (countryAlpha2 == null)
        {
            return new GeographicRegion[0];
        }

        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(countryAlpha2, canonical))
        {
            return new GeographicRegion[0];
        }

        GeneratedCountryRegionMappingRow? mapping =
            RegionIndex.FindCountryRegionMapping(new string(canonical));
        if (mapping == null)
        {
            return new GeographicRegion[0];
        }

        List<GeneratedGeographicRegionRow> rows =
            new List<GeneratedGeographicRegionRow>();
        AddRegion(rows, mapping.WorldM49Code);
        if (mapping.HasRegion)
        {
            AddRegion(rows, mapping.RegionM49Code);
        }

        if (mapping.HasSubregion)
        {
            AddRegion(rows, mapping.SubregionM49Code);
        }

        if (mapping.HasIntermediateRegion)
        {
            AddRegion(rows, mapping.IntermediateRegionM49Code);
        }

        return RegionMapper.MapRegions(rows.ToArray());
    }

    internal static Country[] GetCountries(string? m49Code)
    {
        GeneratedGeographicRegionRow? region = FindRegionRow(m49Code);
        if (region == null)
        {
            return new Country[0];
        }

        GeneratedCountryRegionMappingRow[] mappings =
            RegionIndex.GetCountryMappingsForRegion(region.M49Code);
        List<GeneratedCountryRow> countries = new List<GeneratedCountryRow>();
        int index = 0;
        while (index < mappings.Length)
        {
            GeneratedCountryRow? country =
                CountryIndex.FindCountryByAlpha2(mappings[index].CountryAlpha2);
            if (country != null)
            {
                CountryLookupEngine.AddCountryInOrder(countries, country);
            }

            index += 1;
        }

        return CountryMapper.MapCountries(countries.ToArray());
    }

    internal static GeneratedGeographicRegionRow? FindRegionRow(string? m49Code)
    {
        if (m49Code == null || !AsciiCodeUtility.IsNumeric3(m49Code))
        {
            return null;
        }

        return RegionIndex.FindRegionByM49Code(m49Code);
    }

    internal static void AddRegion(
        List<GeneratedGeographicRegionRow> rows,
        string m49Code)
    {
        GeneratedGeographicRegionRow? row =
            RegionIndex.FindRegionByM49Code(m49Code);
        if (row != null)
        {
            rows.Add(row);
        }
    }

    internal static bool ContainsCode(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(values[index], value))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
