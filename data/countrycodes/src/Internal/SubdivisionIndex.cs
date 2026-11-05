namespace countrycodes;

internal static class SubdivisionIndex
{
    internal static int GetSubdivisionCount()
    {
        return RuntimeCatalog.GetState().Subdivisions.Length;
    }

    internal static GeneratedSubdivisionRow? GetSubdivisionAt(int index)
    {
        if (index < 0 || index >= RuntimeCatalog.GetState().Subdivisions.Length)
        {
            return null;
        }

        return RuntimeCatalog.GetState().Subdivisions[index];
    }

    internal static GeneratedSubdivisionRow? FindSubdivisionByCode(string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.SubdivisionCode,
            value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().Subdivisions[rowIndex];
    }

    internal static GeneratedSubdivisionAliasRow? FindSubdivisionAliasByCode(string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.SubdivisionAlias,
            value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().SubdivisionAliases[rowIndex];
    }

    internal static GeneratedSubdivisionRow[] GetSubdivisionsForCountry(string? countryCode)
    {
        int first = FindFirstCountryPosition(countryCode);
        if (first < 0)
        {
            return new GeneratedSubdivisionRow[0];
        }

        int last = FindLastCountryPosition(countryCode);
        GeneratedSubdivisionRow[] result = new GeneratedSubdivisionRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = RuntimeCatalog.GetState().Subdivisions[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedSubdivisionReplacementRow[] GetReplacementsForAlias(
        string? aliasCode)
    {
        int first = FindFirstReplacementPosition(aliasCode);
        if (first < 0)
        {
            return new GeneratedSubdivisionReplacementRow[0];
        }

        int last = FindLastReplacementPosition(aliasCode);
        GeneratedSubdivisionReplacementRow[] result =
            new GeneratedSubdivisionReplacementRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = RuntimeCatalog.GetState().SubdivisionReplacements[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static int FindFirstCountryPosition(string? countryCode)
    {
        if (countryCode == null)
        {
            return -1;
        }

        GeneratedSubdivisionRow[] rows = RuntimeCatalog.GetState().Subdivisions;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].CountryCode,
                countryCode);
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

    internal static int FindLastCountryPosition(string? countryCode)
    {
        if (countryCode == null)
        {
            return -1;
        }

        GeneratedSubdivisionRow[] rows = RuntimeCatalog.GetState().Subdivisions;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].CountryCode,
                countryCode);
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

    internal static int FindFirstReplacementPosition(string? aliasCode)
    {
        if (aliasCode == null)
        {
            return -1;
        }

        GeneratedSubdivisionReplacementRow[] rows = RuntimeCatalog.GetState().SubdivisionReplacements;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].AliasCode,
                aliasCode);
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

    internal static int FindLastReplacementPosition(string? aliasCode)
    {
        if (aliasCode == null)
        {
            return -1;
        }

        GeneratedSubdivisionReplacementRow[] rows = RuntimeCatalog.GetState().SubdivisionReplacements;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].AliasCode,
                aliasCode);
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
