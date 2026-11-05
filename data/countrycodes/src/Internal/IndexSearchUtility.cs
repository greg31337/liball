namespace countrycodes;

internal static class IndexSearchUtility
{
    internal static int FindFirstExactPosition(
        GeneratedExactIndexKind kind,
        string? key)
    {
        if (key == null)
        {
            return -1;
        }

        GeneratedExactIndexRow[] rows = RuntimeCatalog.GetState().ExactIndexRows;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareExactLookup(rows[middle], kind, key);
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

    internal static int FindLastExactPosition(
        GeneratedExactIndexKind kind,
        string? key)
    {
        if (key == null)
        {
            return -1;
        }

        GeneratedExactIndexRow[] rows = RuntimeCatalog.GetState().ExactIndexRows;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareExactLookup(rows[middle], kind, key);
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

    internal static int FindFirstSearchPosition(
        string? searchKey,
        GeneratedSearchIndexKind kind)
    {
        if (searchKey == null)
        {
            return -1;
        }

        GeneratedSearchIndexRow[] rows = RuntimeCatalog.GetState().SearchIndexRows;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareSearchLookup(rows[middle], searchKey, kind);
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

    internal static int FindLastSearchPosition(
        string? searchKey,
        GeneratedSearchIndexKind kind)
    {
        if (searchKey == null)
        {
            return -1;
        }

        GeneratedSearchIndexRow[] rows = RuntimeCatalog.GetState().SearchIndexRows;
        int low = 0;
        int high = rows.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareSearchLookup(rows[middle], searchKey, kind);
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

    internal static int CompareExactLookup(
        GeneratedExactIndexRow row,
        GeneratedExactIndexKind kind,
        string key)
    {
        if ((int)row.Kind < (int)kind)
        {
            return -1;
        }

        if ((int)row.Kind > (int)kind)
        {
            return 1;
        }

        return AsciiCodeUtility.CompareOrdinal(row.Key, key);
    }

    internal static int CompareSearchLookup(
        GeneratedSearchIndexRow row,
        string searchKey,
        GeneratedSearchIndexKind kind)
    {
        int comparison = AsciiCodeUtility.CompareOrdinal(row.SearchKey, searchKey);
        if (comparison != 0)
        {
            return comparison;
        }

        if ((int)row.Kind < (int)kind)
        {
            return -1;
        }

        if ((int)row.Kind > (int)kind)
        {
            return 1;
        }

        return 0;
    }
}
