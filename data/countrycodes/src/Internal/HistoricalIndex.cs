namespace countrycodes;

internal static class HistoricalIndex
{
    internal static int GetHistoricalCountryCount()
    {
        return RuntimeCatalog.GetState().HistoricalCountries.Length;
    }

    internal static GeneratedHistoricalCountryRow? GetHistoricalCountryAt(int index)
    {
        if (index < 0 || index >= RuntimeCatalog.GetState().HistoricalCountries.Length)
        {
            return null;
        }

        return RuntimeCatalog.GetState().HistoricalCountries[index];
    }

    internal static GeneratedHistoricalCountryRow[] FindHistoricalCountriesByAlpha2(
        string? value)
    {
        return CollectHistoricalRows(GeneratedExactIndexKind.HistoricalAlpha2, value);
    }

    internal static GeneratedHistoricalCountryRow[] FindHistoricalCountriesByAlpha3(
        string? value)
    {
        return CollectHistoricalRows(GeneratedExactIndexKind.HistoricalAlpha3, value);
    }

    internal static GeneratedHistoricalCountryRow? FindHistoricalCountryByAlpha4(string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.HistoricalAlpha4,
            value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().HistoricalCountries[rowIndex];
    }

    internal static GeneratedHistoricalCountryRow[] FindHistoricalCountriesByNumericText(
        string? value)
    {
        return CollectHistoricalRows(GeneratedExactIndexKind.HistoricalNumeric, value);
    }

    internal static GeneratedCountryChangeRow? FindCountryChangeByAlpha4(string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(
            GeneratedExactIndexKind.CountryChangeAlpha4,
            value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().CountryChanges[rowIndex];
    }

    internal static GeneratedCountryChangeLinkRow[] GetPredecessorsForChange(string? alpha4)
    {
        return CollectChangeLinks(RuntimeCatalog.GetState().CountryChangePredecessors, alpha4);
    }

    internal static GeneratedCountryChangeLinkRow[] GetSuccessorsForChange(string? alpha4)
    {
        return CollectChangeLinks(RuntimeCatalog.GetState().CountryChangeSuccessors, alpha4);
    }

    internal static GeneratedHistoricalCountryRow[] CollectHistoricalRows(
        GeneratedExactIndexKind kind,
        string? value)
    {
        int first = IndexSearchUtility.FindFirstExactPosition(kind, value);
        if (first < 0)
        {
            return new GeneratedHistoricalCountryRow[0];
        }

        int last = IndexSearchUtility.FindLastExactPosition(kind, value);
        GeneratedHistoricalCountryRow[] result =
            new GeneratedHistoricalCountryRow[last - first + 1];
        int position = first;
        int resultIndex = 0;
        while (position <= last)
        {
            int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
            result[resultIndex] = RuntimeCatalog.GetState().HistoricalCountries[rowIndex];
            position += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static GeneratedCountryChangeLinkRow[] CollectChangeLinks(
        GeneratedCountryChangeLinkRow[] rows,
        string? alpha4)
    {
        int first = FindFirstChangeLinkPosition(rows, alpha4);
        if (first < 0)
        {
            return new GeneratedCountryChangeLinkRow[0];
        }

        int last = FindLastChangeLinkPosition(rows, alpha4);
        GeneratedCountryChangeLinkRow[] result =
            new GeneratedCountryChangeLinkRow[last - first + 1];
        int sourceIndex = first;
        int resultIndex = 0;
        while (sourceIndex <= last)
        {
            result[resultIndex] = rows[sourceIndex];
            sourceIndex += 1;
            resultIndex += 1;
        }

        return result;
    }

    internal static int FindFirstChangeLinkPosition(
        GeneratedCountryChangeLinkRow[] rows,
        string? alpha4)
    {
        if (alpha4 == null)
        {
            return -1;
        }

        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].ChangeAlpha4,
                alpha4);
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

    internal static int FindLastChangeLinkPosition(
        GeneratedCountryChangeLinkRow[] rows,
        string? alpha4)
    {
        if (alpha4 == null)
        {
            return -1;
        }

        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = AsciiCodeUtility.CompareOrdinal(
                rows[middle].ChangeAlpha4,
                alpha4);
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
