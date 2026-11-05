namespace countrycodes;

internal static class CountryIndex
{
    internal static int GetCountryCount()
    {
        return RuntimeCatalog.GetState().Countries.Length;
    }

    internal static GeneratedCountryRow? GetCountryAt(int index)
    {
        if (index < 0 || index >= RuntimeCatalog.GetState().Countries.Length)
        {
            return null;
        }

        return RuntimeCatalog.GetState().Countries[index];
    }

    internal static GeneratedCountryRow? FindCountryByAlpha2(string? value)
    {
        return FindCountryRow(GeneratedExactIndexKind.CountryAlpha2, value);
    }

    internal static GeneratedCountryRow? FindCountryByAlpha3(string? value)
    {
        return FindCountryRow(GeneratedExactIndexKind.CountryAlpha3, value);
    }

    internal static GeneratedCountryRow? FindCountryByNumericText(string? value)
    {
        return FindCountryRow(GeneratedExactIndexKind.CountryNumeric, value);
    }

    internal static GeneratedReservedCodeRow? FindReservedCode(
        GeneratedCodeKind kind,
        string? value)
    {
        GeneratedExactIndexKind indexKind = GeneratedExactIndexKind.ReservedAlpha2;
        if (kind == GeneratedCodeKind.Alpha3)
        {
            indexKind = GeneratedExactIndexKind.ReservedAlpha3;
        }
        else if (kind == GeneratedCodeKind.Numeric3)
        {
            indexKind = GeneratedExactIndexKind.ReservedNumeric;
        }
        else if (kind == GeneratedCodeKind.Alpha4)
        {
            indexKind = GeneratedExactIndexKind.ReservedAlpha4;
        }

        int position = IndexSearchUtility.FindFirstExactPosition(indexKind, value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().ReservedCodes[rowIndex];
    }

    internal static GeneratedCountryRow? FindCountryRow(
        GeneratedExactIndexKind kind,
        string? value)
    {
        int position = IndexSearchUtility.FindFirstExactPosition(kind, value);
        if (position < 0)
        {
            return null;
        }

        int rowIndex = RuntimeCatalog.GetState().ExactIndexRows[position].RowIndex;
        return RuntimeCatalog.GetState().Countries[rowIndex];
    }
}
