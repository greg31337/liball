namespace countrycodes;

internal static class AssignmentClassifier
{
    internal static CodeAssignmentStatus Classify(
        CountryCodeKind kind,
        string? canonical)
    {
        if (canonical == null ||
            (kind != CountryCodeKind.Alpha2 &&
             kind != CountryCodeKind.Alpha3 &&
             kind != CountryCodeKind.Numeric3 &&
             kind != CountryCodeKind.Alpha4))
        {
            return CodeAssignmentStatus.Unassigned;
        }

        GeneratedCountryRow? current = FindCurrent(kind, canonical);
        if (current != null)
        {
            return CountryMapper.MapAssignmentStatus(current.AssignmentState);
        }

        GeneratedCodeKind generatedKind = MapKind(kind);
        GeneratedReservedCodeRow? reserved =
            CountryIndex.FindReservedCode(generatedKind, canonical);
        if (reserved != null)
        {
            return CountryMapper.MapAssignmentStatus(reserved.AssignmentState);
        }

        if (IsKnownHistorical(kind, canonical))
        {
            return CodeAssignmentStatus.FormerlyAssigned;
        }

        if (IsUserAssigned(generatedKind, canonical))
        {
            return CodeAssignmentStatus.UserAssigned;
        }

        return CodeAssignmentStatus.Unassigned;
    }

    internal static GeneratedCountryRow? FindCurrent(
        CountryCodeKind kind,
        string canonical)
    {
        if (kind == CountryCodeKind.Alpha2)
        {
            return CountryIndex.FindCountryByAlpha2(canonical);
        }

        if (kind == CountryCodeKind.Alpha3)
        {
            return CountryIndex.FindCountryByAlpha3(canonical);
        }

        if (kind == CountryCodeKind.Numeric3)
        {
            return CountryIndex.FindCountryByNumericText(canonical);
        }

        return null;
    }

    internal static GeneratedCodeKind MapKind(CountryCodeKind kind)
    {
        if (kind == CountryCodeKind.Alpha3)
        {
            return GeneratedCodeKind.Alpha3;
        }

        if (kind == CountryCodeKind.Numeric3)
        {
            return GeneratedCodeKind.Numeric3;
        }

        if (kind == CountryCodeKind.Alpha4)
        {
            return GeneratedCodeKind.Alpha4;
        }

        return GeneratedCodeKind.Alpha2;
    }

    internal static bool IsKnownHistorical(
        CountryCodeKind kind,
        string canonical)
    {
        if (kind == CountryCodeKind.Alpha2)
        {
            return HistoricalIndex.FindHistoricalCountriesByAlpha2(canonical).Length > 0;
        }

        if (kind == CountryCodeKind.Alpha3)
        {
            return HistoricalIndex.FindHistoricalCountriesByAlpha3(canonical).Length > 0;
        }

        if (kind == CountryCodeKind.Numeric3)
        {
            return HistoricalIndex.FindHistoricalCountriesByNumericText(canonical).Length > 0;
        }

        if (kind == CountryCodeKind.Alpha4)
        {
            return HistoricalIndex.FindHistoricalCountryByAlpha4(canonical) != null;
        }

        return false;
    }

    internal static bool IsUserAssigned(
        GeneratedCodeKind kind,
        string canonical)
    {
        GeneratedUserAssignedRange[] ranges =
            RuntimeCatalog.GetState().UserAssignedRanges;
        int index = 0;
        while (index < ranges.Length)
        {
            GeneratedUserAssignedRange range = ranges[index];
            if (range.Kind == kind &&
                AsciiCodeUtility.CompareOrdinal(canonical, range.Start) >= 0 &&
                AsciiCodeUtility.CompareOrdinal(canonical, range.End) <= 0)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
