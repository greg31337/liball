namespace countrycodes;

internal static class NameIndex
{
    internal static GeneratedCountryNameRow[] FindCurrentCountryNamesBySearchKey(
        string? searchKey)
    {
        return CollectCountrySearchRows(GeneratedSearchIndexKind.CountryName, searchKey);
    }

    internal static GeneratedCountryNameRow[] FindHistoricalCountryNamesBySearchKey(
        string? searchKey)
    {
        return CollectCountrySearchRows(
            GeneratedSearchIndexKind.HistoricalCountryName,
            searchKey);
    }

    internal static GeneratedSubdivisionNameRow[] FindSubdivisionNamesBySearchKey(
        string? searchKey)
    {
        int first = IndexSearchUtility.FindFirstSearchPosition(
            searchKey,
            GeneratedSearchIndexKind.SubdivisionName);
        if (first < 0)
        {
            return new GeneratedSubdivisionNameRow[0];
        }

        int last = IndexSearchUtility.FindLastSearchPosition(
            searchKey,
            GeneratedSearchIndexKind.SubdivisionName);
        GeneratedSubdivisionNameRow[] result =
            new GeneratedSubdivisionNameRow[last - first + 1];
        int position = first;
        int resultIndex = 0;
        while (position <= last)
        {
            int rowIndex = RuntimeCatalog.GetState().SearchIndexRows[position].RowIndex;
            result[resultIndex] = RuntimeCatalog.GetState().SubdivisionNames[rowIndex];
            position += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedGeographicRegionNameRow[] FindRegionNamesBySearchKey(
        string? searchKey)
    {
        int first = IndexSearchUtility.FindFirstSearchPosition(
            searchKey,
            GeneratedSearchIndexKind.GeographicRegionName);
        if (first < 0)
        {
            return new GeneratedGeographicRegionNameRow[0];
        }

        int last = IndexSearchUtility.FindLastSearchPosition(
            searchKey,
            GeneratedSearchIndexKind.GeographicRegionName);
        GeneratedGeographicRegionNameRow[] result =
            new GeneratedGeographicRegionNameRow[last - first + 1];
        int position = first;
        int resultIndex = 0;
        while (position <= last)
        {
            int rowIndex = RuntimeCatalog.GetState().SearchIndexRows[position].RowIndex;
            result[resultIndex] = RuntimeCatalog.GetState().RegionNames[rowIndex];
            position += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedCountryNameRow[] GetNamesForCountry(string? alpha2)
    {
        return CollectCountryTargetRows(GeneratedCountryNameTargetKind.Country, alpha2);
    }

    internal static GeneratedCountryNameRow[] GetNamesForHistoricalCountry(string? alpha4)
    {
        return CollectCountryTargetRows(
            GeneratedCountryNameTargetKind.HistoricalCountry,
            alpha4);
    }

    internal static GeneratedSubdivisionNameRow[] GetNamesForSubdivision(string? code)
    {
        int first = FindFirstSubdivisionNamePosition(code);
        if (first < 0)
        {
            return new GeneratedSubdivisionNameRow[0];
        }

        int last = FindLastSubdivisionNamePosition(code);
        GeneratedSubdivisionNameRow[] result =
            new GeneratedSubdivisionNameRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = RuntimeCatalog.GetState().SubdivisionNames[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedGeographicRegionNameRow[] GetNamesForRegion(string? m49Code)
    {
        int first = FindFirstRegionNamePosition(m49Code);
        if (first < 0)
        {
            return new GeneratedGeographicRegionNameRow[0];
        }

        int last = FindLastRegionNamePosition(m49Code);
        GeneratedGeographicRegionNameRow[] result =
            new GeneratedGeographicRegionNameRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = RuntimeCatalog.GetState().RegionNames[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedCountryNameRow[] CollectCountrySearchRows(
        GeneratedSearchIndexKind kind,
        string? searchKey)
    {
        int first = IndexSearchUtility.FindFirstSearchPosition(searchKey, kind);
        if (first < 0)
        {
            return new GeneratedCountryNameRow[0];
        }

        int last = IndexSearchUtility.FindLastSearchPosition(searchKey, kind);
        GeneratedCountryNameRow[] result = new GeneratedCountryNameRow[last - first + 1];
        int position = first;
        int resultIndex = 0;
        while (position <= last)
        {
            int rowIndex = RuntimeCatalog.GetState().SearchIndexRows[position].RowIndex;
            result[resultIndex] = RuntimeCatalog.GetState().CountryNames[rowIndex];
            position += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedCountryNameRow[] CollectCountryTargetRows(
        GeneratedCountryNameTargetKind kind,
        string? targetCode)
    {
        int first = FindFirstCountryNamePosition(kind, targetCode);
        if (first < 0)
        {
            return new GeneratedCountryNameRow[0];
        }

        int last = FindLastCountryNamePosition(kind, targetCode);
        GeneratedCountryNameRow[] result = new GeneratedCountryNameRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = RuntimeCatalog.GetState().CountryNames[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static int FindFirstCountryNamePosition(
        GeneratedCountryNameTargetKind kind,
        string? targetCode)
    {
        if (targetCode == null)
        {
            return -1;
        }

        GeneratedCountryNameRow[] rows = RuntimeCatalog.GetState().CountryNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareCountryNameTarget(rows[middle], kind, targetCode);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                high = middle - 1;
            }
        }

        return result;
    }

    internal static int FindLastCountryNamePosition(
        GeneratedCountryNameTargetKind kind,
        string? targetCode)
    {
        if (targetCode == null)
        {
            return -1;
        }

        GeneratedCountryNameRow[] rows = RuntimeCatalog.GetState().CountryNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareCountryNameTarget(rows[middle], kind, targetCode);
            if (comparison > 0)
            {
                high = middle - 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                low = middle + 1;
            }
        }

        return result;
    }

    internal static int CompareCountryNameTarget(
        GeneratedCountryNameRow row,
        GeneratedCountryNameTargetKind kind,
        string targetCode)
    {
        if ((int)row.TargetKind < (int)kind)
        {
            return -1;
        }

        if ((int)row.TargetKind > (int)kind)
        {
            return 1;
        }

        return AsciiCodeUtility.CompareOrdinal(row.TargetCode, targetCode);
    }

    internal static int FindFirstSubdivisionNamePosition(string? code)
    {
        if (code == null)
        {
            return -1;
        }

        GeneratedSubdivisionNameRow[] rows = RuntimeCatalog.GetState().SubdivisionNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].SubdivisionCode,
                code);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                high = middle - 1;
            }
        }

        return result;
    }

    internal static int FindLastSubdivisionNamePosition(string? code)
    {
        if (code == null)
        {
            return -1;
        }

        GeneratedSubdivisionNameRow[] rows = RuntimeCatalog.GetState().SubdivisionNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].SubdivisionCode,
                code);
            if (comparison > 0)
            {
                high = middle - 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                low = middle + 1;
            }
        }

        return result;
    }

    internal static int FindFirstRegionNamePosition(string? m49Code)
    {
        if (m49Code == null)
        {
            return -1;
        }

        GeneratedGeographicRegionNameRow[] rows = RuntimeCatalog.GetState().RegionNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].RegionM49Code,
                m49Code);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                high = middle - 1;
            }
        }

        return result;
    }

    internal static int FindLastRegionNamePosition(string? m49Code)
    {
        if (m49Code == null)
        {
            return -1;
        }

        GeneratedGeographicRegionNameRow[] rows = RuntimeCatalog.GetState().RegionNames;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].RegionM49Code,
                m49Code);
            if (comparison > 0)
            {
                high = middle - 1;
            }
            else
            {
                if (comparison == 0)
                {
                    result = middle;
                }

                low = middle + 1;
            }
        }

        return result;
    }
}
