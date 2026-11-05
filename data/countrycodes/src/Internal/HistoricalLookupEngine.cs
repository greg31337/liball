using System.Collections.Generic;

namespace countrycodes;

internal static class HistoricalLookupEngine
{
    internal static HistoricalCountry[] GetAll()
    {
        return HistoricalMapper.MapHistoricalCountries(
            RuntimeCatalog.GetState().HistoricalCountries);
    }

    internal static HistoricalLookupResult FindFormerCode(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateInvalidResult();
        }

        if (value.Length == CountryCodeConstants.Alpha2Length)
        {
            char[] canonicalAlpha2 = new char[CountryCodeConstants.Alpha2Length];
            if (!AsciiCodeUtility.TryNormalizeAlpha2(value, canonicalAlpha2))
            {
                return CreateInvalidResult();
            }

            return CreateHistoricalCandidateResult(
                HistoricalIndex.FindHistoricalCountriesByAlpha2(
                    new string(canonicalAlpha2)));
        }

        if (value.Length == CountryCodeConstants.Alpha3Length)
        {
            if (AsciiCodeUtility.IsNumeric3(value))
            {
                return CreateHistoricalCandidateResult(
                    HistoricalIndex.FindHistoricalCountriesByNumericText(value));
            }

            char[] canonicalAlpha3 = new char[CountryCodeConstants.Alpha3Length];
            if (!AsciiCodeUtility.TryNormalizeAlpha3(value, canonicalAlpha3))
            {
                return CreateInvalidResult();
            }

            return CreateHistoricalCandidateResult(
                HistoricalIndex.FindHistoricalCountriesByAlpha3(
                    new string(canonicalAlpha3)));
        }

        if (value.Length == CountryCodeConstants.Alpha4Length)
        {
            char[] canonicalAlpha4 = new char[CountryCodeConstants.Alpha4Length];
            if (!AsciiCodeUtility.TryNormalizeAlpha4(value, canonicalAlpha4))
            {
                return CreateInvalidResult();
            }

            GeneratedHistoricalCountryRow? row =
                HistoricalIndex.FindHistoricalCountryByAlpha4(
                    new string(canonicalAlpha4));
            return CreateHistoricalResult(row);
        }

        return CreateInvalidResult();
    }

    internal static HistoricalLookupResult FindName(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateInvalidResult();
        }

        string? searchKey = UnicodeNameUtility.CreateMatchKey(
            value,
            true,
            true,
            true,
            true);
        if (searchKey == null || searchKey.Length == 0)
        {
            return CreateInvalidResult();
        }

        GeneratedCountryNameRow[] nameRows =
            NameIndex.FindHistoricalCountryNamesBySearchKey(searchKey);
        List<GeneratedHistoricalCountryRow> matches =
            new List<GeneratedHistoricalCountryRow>();
        int index = 0;
        while (index < nameRows.Length)
        {
            GeneratedCountryNameRow nameRow = nameRows[index];
            if (nameRow.TargetKind == GeneratedCountryNameTargetKind.HistoricalCountry &&
                nameRow.NameKind == GeneratedCountryNameKind.Historical &&
                global::System.String.Equals(
                    nameRow.DisplayName,
                    value,
                    global::System.StringComparison.OrdinalIgnoreCase))
            {
                GeneratedHistoricalCountryRow? historicalRow =
                    HistoricalIndex.FindHistoricalCountryByAlpha4(nameRow.TargetCode);
                if (historicalRow != null)
                {
                    AddHistoricalInOrder(matches, historicalRow);
                }
            }

            index += 1;
        }

        return CreateHistoricalCandidateResult(matches.ToArray());
    }

    internal static HistoricalLookupResult FindChangeAlpha4(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateInvalidResult();
        }

        char[] canonical = new char[CountryCodeConstants.Alpha4Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha4(value, canonical))
        {
            return CreateInvalidResult();
        }

        GeneratedCountryChangeRow? row = HistoricalIndex.FindCountryChangeByAlpha4(
            new string(canonical));
        return CreateChangeResult(row);
    }

    internal static CountryChange[] GetChangeHistory(string? value)
    {
        if (value == null || !IsValidFormerIdentifier(value))
        {
            return new CountryChange[0];
        }

        GeneratedHistoricalCountryRow[] historicalRows =
            FindHistoricalRowsForIdentifier(value);
        GeneratedCountryRow? currentRow = FindCurrentRowForIdentifier(value);
        if (historicalRows.Length == 0 && currentRow == null)
        {
            return new CountryChange[0];
        }

        List<string> historicalCodes = new List<string>();
        List<string> currentCodes = new List<string>();
        List<GeneratedCountryChangeRow> changes =
            new List<GeneratedCountryChangeRow>();
        int index = 0;
        while (index < historicalRows.Length)
        {
            AddUniqueCode(historicalCodes, historicalRows[index].Alpha4);
            index += 1;
        }

        if (currentRow != null)
        {
            AddUniqueCode(currentCodes, currentRow.Alpha2);
        }

        int historicalIndex = 0;
        int currentIndex = 0;
        while (historicalIndex < historicalCodes.Count ||
               currentIndex < currentCodes.Count)
        {
            if (historicalIndex < historicalCodes.Count)
            {
                string historicalCode = historicalCodes[historicalIndex];
                GeneratedCountryChangeRow? ownChange =
                    HistoricalIndex.FindCountryChangeByAlpha4(historicalCode);
                if (ownChange != null)
                {
                    AddChangeAndLinks(
                        changes,
                        historicalCodes,
                        ownChange);
                }

                AddChangesTargetingCode(
                    changes,
                    historicalCodes,
                    GeneratedCountryChangeTargetKind.HistoricalCountry,
                    historicalCode);
                historicalIndex += 1;
            }

            if (currentIndex < currentCodes.Count)
            {
                AddChangesTargetingCode(
                    changes,
                    historicalCodes,
                    GeneratedCountryChangeTargetKind.CurrentCountry,
                    currentCodes[currentIndex]);
                currentIndex += 1;
            }
        }

        return HistoricalMapper.MapCountryChanges(changes.ToArray());
    }

    internal static HistoricalLookupResult ResolveSuccessors(string? value)
    {
        if (value == null || !IsValidFormerIdentifier(value))
        {
            return CreateInvalidResult();
        }

        GeneratedHistoricalCountryRow[] startingRows =
            FindHistoricalRowsForIdentifier(value);
        if (startingRows.Length == 0)
        {
            return CreateNotFoundResult();
        }

        List<GeneratedCountryRow> currentRows = new List<GeneratedCountryRow>();
        List<GeneratedHistoricalCountryRow> terminalHistoricalRows =
            new List<GeneratedHistoricalCountryRow>();
        List<string> completed = new List<string>();
        int index = 0;
        while (index < startingRows.Length)
        {
            ResolveHistoricalRow(
                startingRows[index],
                currentRows,
                terminalHistoricalRows,
                new List<string>(),
                completed);
            index += 1;
        }

        return CreateSuccessorResult(currentRows, terminalHistoricalRows);
    }

    internal static CountryChange[] GetChangesByKind(CountryChangeKind kind)
    {
        List<GeneratedCountryChangeRow> matches =
            new List<GeneratedCountryChangeRow>();
        GeneratedCountryChangeRow[] rows = RuntimeCatalog.GetState().CountryChanges;
        int index = 0;
        while (index < rows.Length)
        {
            if (HistoricalMapper.MapCountryChangeKind(rows[index].Kind) == kind)
            {
                matches.Add(rows[index]);
            }

            index += 1;
        }

        return HistoricalMapper.MapCountryChanges(matches.ToArray());
    }

    internal static bool IsValidFormerIdentifier(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        if (value.Length == CountryCodeConstants.Alpha2Length)
        {
            return AsciiCodeUtility.IsAlpha2(value);
        }

        if (value.Length == CountryCodeConstants.Alpha3Length)
        {
            return AsciiCodeUtility.IsAlpha3(value) ||
                   AsciiCodeUtility.IsNumeric3(value);
        }

        if (value.Length == CountryCodeConstants.Alpha4Length)
        {
            char[] canonical = new char[CountryCodeConstants.Alpha4Length];
            return AsciiCodeUtility.TryNormalizeAlpha4(value, canonical);
        }

        return false;
    }

    internal static GeneratedHistoricalCountryRow[] FindHistoricalRowsForIdentifier(
        string value)
    {
        if (value.Length == CountryCodeConstants.Alpha2Length)
        {
            char[] canonical = new char[CountryCodeConstants.Alpha2Length];
            AsciiCodeUtility.TryNormalizeAlpha2(value, canonical);
            return HistoricalIndex.FindHistoricalCountriesByAlpha2(new string(canonical));
        }

        if (value.Length == CountryCodeConstants.Alpha3Length)
        {
            if (AsciiCodeUtility.IsNumeric3(value))
            {
                return HistoricalIndex.FindHistoricalCountriesByNumericText(value);
            }

            char[] canonical = new char[CountryCodeConstants.Alpha3Length];
            AsciiCodeUtility.TryNormalizeAlpha3(value, canonical);
            return HistoricalIndex.FindHistoricalCountriesByAlpha3(new string(canonical));
        }

        char[] canonicalAlpha4 = new char[CountryCodeConstants.Alpha4Length];
        AsciiCodeUtility.TryNormalizeAlpha4(value, canonicalAlpha4);
        GeneratedHistoricalCountryRow? row =
            HistoricalIndex.FindHistoricalCountryByAlpha4(new string(canonicalAlpha4));
        if (row == null)
        {
            return new GeneratedHistoricalCountryRow[0];
        }

        return new GeneratedHistoricalCountryRow[] { row };
    }

    internal static GeneratedCountryRow? FindCurrentRowForIdentifier(string value)
    {
        if (value.Length == CountryCodeConstants.Alpha2Length)
        {
            char[] canonical = new char[CountryCodeConstants.Alpha2Length];
            AsciiCodeUtility.TryNormalizeAlpha2(value, canonical);
            return CountryIndex.FindCountryByAlpha2(new string(canonical));
        }

        if (value.Length == CountryCodeConstants.Alpha3Length)
        {
            if (AsciiCodeUtility.IsNumeric3(value))
            {
                return CountryIndex.FindCountryByNumericText(value);
            }

            char[] canonical = new char[CountryCodeConstants.Alpha3Length];
            AsciiCodeUtility.TryNormalizeAlpha3(value, canonical);
            return CountryIndex.FindCountryByAlpha3(new string(canonical));
        }

        return null;
    }

    internal static void AddChangesTargetingCode(
        List<GeneratedCountryChangeRow> changes,
        List<string> historicalCodes,
        GeneratedCountryChangeTargetKind targetKind,
        string targetCode)
    {
        GeneratedCountryChangeLinkRow[] links =
            RuntimeCatalog.GetState().CountryChangeSuccessors;
        int index = 0;
        while (index < links.Length)
        {
            GeneratedCountryChangeLinkRow link = links[index];
            if (link.TargetKind == targetKind &&
                AsciiCodeUtility.EqualsOrdinal(link.TargetCode, targetCode))
            {
                GeneratedCountryChangeRow? change =
                    HistoricalIndex.FindCountryChangeByAlpha4(link.ChangeAlpha4);
                if (change != null)
                {
                    AddChangeAndLinks(
                        changes,
                        historicalCodes,
                        change);
                }
            }

            index += 1;
        }
    }

    internal static void AddChangeAndLinks(
        List<GeneratedCountryChangeRow> changes,
        List<string> historicalCodes,
        GeneratedCountryChangeRow change)
    {
        AddChangeInChronologicalOrder(changes, change);
        GeneratedCountryChangeLinkRow[] predecessors =
            HistoricalIndex.GetPredecessorsForChange(change.Alpha4);
        GeneratedCountryChangeLinkRow[] successors =
            HistoricalIndex.GetSuccessorsForChange(change.Alpha4);
        AddHistoricalLinkedCodes(historicalCodes, predecessors);
        AddHistoricalLinkedCodes(historicalCodes, successors);
    }

    internal static void AddHistoricalLinkedCodes(
        List<string> historicalCodes,
        GeneratedCountryChangeLinkRow[] links)
    {
        int index = 0;
        while (index < links.Length)
        {
            if (links[index].TargetKind ==
                GeneratedCountryChangeTargetKind.HistoricalCountry)
            {
                AddUniqueCode(historicalCodes, links[index].TargetCode);
            }

            index += 1;
        }
    }

    internal static void AddUniqueCode(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(values[index], value))
            {
                return;
            }

            index += 1;
        }

        values.Add(value);
    }

    internal static void AddChangeInChronologicalOrder(
        List<GeneratedCountryChangeRow> changes,
        GeneratedCountryChangeRow value)
    {
        int index = 0;
        while (index < changes.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(changes[index].Alpha4, value.Alpha4))
            {
                return;
            }

            bool insert = false;
            if (value.HasEffectiveDate && !changes[index].HasEffectiveDate)
            {
                insert = true;
            }
            else if (value.HasEffectiveDate == changes[index].HasEffectiveDate)
            {
                int dateComparison = AsciiCodeUtility.CompareOrdinal(
                    value.EffectiveDate,
                    changes[index].EffectiveDate);
                if (dateComparison < 0 ||
                    dateComparison == 0 &&
                    AsciiCodeUtility.CompareOrdinal(
                        value.Alpha4,
                        changes[index].Alpha4) < 0)
                {
                    insert = true;
                }
            }

            if (insert)
            {
                changes.Insert(index, value);
                return;
            }

            index += 1;
        }

        changes.Add(value);
    }

    internal static void ResolveHistoricalRow(
        GeneratedHistoricalCountryRow row,
        List<GeneratedCountryRow> currentRows,
        List<GeneratedHistoricalCountryRow> terminalHistoricalRows,
        List<string> active,
        List<string> completed)
    {
        if (ContainsCode(completed, row.Alpha4))
        {
            return;
        }

        if (ContainsCode(active, row.Alpha4))
        {
            AddHistoricalInOrder(terminalHistoricalRows, row);
            return;
        }

        active.Add(row.Alpha4);
        GeneratedCountryChangeLinkRow[] successors =
            HistoricalIndex.GetSuccessorsForChange(row.Alpha4);
        if (successors.Length == 0)
        {
            AddHistoricalInOrder(terminalHistoricalRows, row);
        }

        int index = 0;
        while (index < successors.Length)
        {
            GeneratedCountryChangeLinkRow successor = successors[index];
            if (successor.TargetKind == GeneratedCountryChangeTargetKind.CurrentCountry)
            {
                GeneratedCountryRow? currentRow =
                    CountryIndex.FindCountryByAlpha2(successor.TargetCode);
                if (currentRow != null)
                {
                    CountryLookupEngine.AddCountryInOrder(currentRows, currentRow);
                }
            }
            else
            {
                GeneratedHistoricalCountryRow? historicalRow =
                    HistoricalIndex.FindHistoricalCountryByAlpha4(successor.TargetCode);
                if (historicalRow != null)
                {
                    ResolveHistoricalRow(
                        historicalRow,
                        currentRows,
                        terminalHistoricalRows,
                        active,
                        completed);
                }
            }

            index += 1;
        }

        active.RemoveAt(active.Count - 1);
        AddUniqueCode(completed, row.Alpha4);
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

    internal static HistoricalLookupResult CreateSuccessorResult(
        List<GeneratedCountryRow> currentRows,
        List<GeneratedHistoricalCountryRow> historicalRows)
    {
        int count = currentRows.Count + historicalRows.Count;
        if (count == 0)
        {
            return CreateNotFoundResult();
        }

        if (count == 1)
        {
            if (currentRows.Count == 1)
            {
                return new HistoricalLookupResult(
                    LookupStatus.Found,
                    CountryMapper.MapCountry(currentRows[0]),
                    null,
                    null,
                    new Country[0],
                    new HistoricalCountry[0],
                    new CountryChange[0]);
            }

            return CreateHistoricalResult(historicalRows[0]);
        }

        return new HistoricalLookupResult(
            LookupStatus.Ambiguous,
            null,
            null,
            null,
            CountryMapper.MapCountries(currentRows.ToArray()),
            HistoricalMapper.MapHistoricalCountries(historicalRows.ToArray()),
            new CountryChange[0]);
    }

    internal static HistoricalLookupResult CreateHistoricalResult(
        GeneratedHistoricalCountryRow? row)
    {
        if (row == null)
        {
            return CreateNotFoundResult();
        }

        HistoricalCountry historicalCountry =
            HistoricalMapper.MapHistoricalCountry(row);
        return new HistoricalLookupResult(
            LookupStatus.Found,
            null,
            historicalCountry,
            null,
            new Country[0],
            new HistoricalCountry[0],
            new CountryChange[0]);
    }

    internal static HistoricalLookupResult CreateHistoricalCandidateResult(
        GeneratedHistoricalCountryRow[] rows)
    {
        List<GeneratedHistoricalCountryRow> ordered =
            new List<GeneratedHistoricalCountryRow>();
        int index = 0;
        while (index < rows.Length)
        {
            AddHistoricalInOrder(ordered, rows[index]);
            index += 1;
        }

        if (ordered.Count == 0)
        {
            return CreateNotFoundResult();
        }

        if (ordered.Count == 1)
        {
            return CreateHistoricalResult(ordered[0]);
        }

        HistoricalCountry[] candidates =
            HistoricalMapper.MapHistoricalCountries(ordered.ToArray());
        return new HistoricalLookupResult(
            LookupStatus.Ambiguous,
            null,
            null,
            null,
            new Country[0],
            candidates,
            new CountryChange[0]);
    }

    internal static HistoricalLookupResult CreateChangeResult(
        GeneratedCountryChangeRow? row)
    {
        if (row == null)
        {
            return CreateNotFoundResult();
        }

        CountryChange change = HistoricalMapper.MapCountryChange(row);
        return new HistoricalLookupResult(
            LookupStatus.Found,
            null,
            null,
            change,
            new Country[0],
            new HistoricalCountry[0],
            new CountryChange[0]);
    }

    internal static HistoricalLookupResult CreateInvalidResult()
    {
        return new HistoricalLookupResult(
            LookupStatus.InvalidInput,
            null,
            null,
            null,
            new Country[0],
            new HistoricalCountry[0],
            new CountryChange[0]);
    }

    internal static HistoricalLookupResult CreateNotFoundResult()
    {
        return new HistoricalLookupResult(
            LookupStatus.NotFound,
            null,
            null,
            null,
            new Country[0],
            new HistoricalCountry[0],
            new CountryChange[0]);
    }

    internal static void AddHistoricalInOrder(
        List<GeneratedHistoricalCountryRow> rows,
        GeneratedHistoricalCountryRow value)
    {
        int index = 0;
        while (index < rows.Count)
        {
            int comparison =
                AsciiCodeUtility.CompareOrdinal(rows[index].Alpha4, value.Alpha4);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                rows.Insert(index, value);
                return;
            }

            index += 1;
        }

        rows.Add(value);
    }
}
