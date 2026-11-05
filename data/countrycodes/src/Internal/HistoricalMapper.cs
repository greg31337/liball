namespace countrycodes;

internal static class HistoricalMapper
{
    internal static HistoricalCountry MapHistoricalCountry(
        GeneratedHistoricalCountryRow row)
    {
        GeneratedCountryNameRow[] names =
            NameIndex.GetNamesForHistoricalCountry(row.Alpha4);
        string name = string.Empty;
        if (names.Length > 0)
        {
            name = names[0].DisplayName;
        }

        GeneratedCountryChangeLinkRow[] predecessorRows =
            HistoricalIndex.GetPredecessorsForChange(row.Alpha4);
        GeneratedCountryChangeLinkRow[] successorRows =
            HistoricalIndex.GetSuccessorsForChange(row.Alpha4);
        string[] predecessors = MapLinkIdentifiers(predecessorRows);
        string[] successors = MapLinkIdentifiers(successorRows);

        GeneratedCountryChangeRow? changeRow =
            HistoricalIndex.FindCountryChangeByAlpha4(row.Alpha4);
        CountryChange[] changes = new CountryChange[0];
        if (changeRow != null)
        {
            changes = new CountryChange[1];
            changes[0] = MapCountryChange(changeRow);
        }

        string? numericText = null;
        if (row.HasNumeric)
        {
            numericText = row.NumericText;
        }

        string? withdrawalDate = null;
        if (row.HasWithdrawalDate)
        {
            withdrawalDate = row.WithdrawalDate;
        }

        return new HistoricalCountry(
            row.Alpha2,
            row.Alpha3,
            row.Alpha4,
            row.HasNumeric,
            numericText,
            row.NumericValue,
            name,
            withdrawalDate,
            CountryMapper.MapAssignmentStatus(row.AssignmentState),
            predecessors,
            successors,
            changes);
    }

    internal static HistoricalCountry[] MapHistoricalCountries(
        GeneratedHistoricalCountryRow[] rows)
    {
        HistoricalCountry[] countries = new HistoricalCountry[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            countries[index] = MapHistoricalCountry(rows[index]);
            index += 1;
        }

        return countries;
    }

    internal static CountryChange MapCountryChange(GeneratedCountryChangeRow row)
    {
        GeneratedCountryChangeLinkRow[] predecessorRows =
            HistoricalIndex.GetPredecessorsForChange(row.Alpha4);
        GeneratedCountryChangeLinkRow[] successorRows =
            HistoricalIndex.GetSuccessorsForChange(row.Alpha4);
        string? effectiveDate = null;
        if (row.HasEffectiveDate)
        {
            effectiveDate = row.EffectiveDate;
        }

        return new CountryChange(
            row.Alpha4,
            MapCountryChangeKind(row.Kind),
            MapLinkIdentifiers(predecessorRows),
            MapLinkIdentifiers(successorRows),
            effectiveDate);
    }

    internal static CountryChange[] MapCountryChanges(GeneratedCountryChangeRow[] rows)
    {
        CountryChange[] changes = new CountryChange[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            changes[index] = MapCountryChange(rows[index]);
            index += 1;
        }

        return changes;
    }

    internal static string[] MapLinkIdentifiers(GeneratedCountryChangeLinkRow[] rows)
    {
        string[] identifiers = new string[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            identifiers[index] = rows[index].TargetCode;
            index += 1;
        }

        return identifiers;
    }

    internal static CountryChangeKind MapCountryChangeKind(
        GeneratedCountryChangeKind kind)
    {
        return (CountryChangeKind)(int)kind;
    }
}
