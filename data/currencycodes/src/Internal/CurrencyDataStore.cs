namespace currencycodes;

internal class CurrencyDataStore
{
    string[] currencyAlphabeticKeys;
    string[] currencyNumericKeys;
    string[] currencyOfficialNameKeys;
    string[] territoryKeyKeys;
    string[] territoryAlpha2Keys;
    string[] territoryAlpha3Keys;
    string[] territoryNumericKeys;
    string[] territoryOfficialNameKeys;
    string[] usageTerritoryKeys;
    string[] usageCurrencyKeys;
    string[] formatCurrencyKeys;

    internal CurrencyDataStore()
    {
        currencyAlphabeticKeys = new string[GeneratedLookupIndexes.CurrencyAlphabeticIndexes.Length];
        for (int index = 0; index < currencyAlphabeticKeys.Length; index++)
        {
            currencyAlphabeticKeys[index] = GeneratedCurrencyRows.Rows[
                GeneratedLookupIndexes.CurrencyAlphabeticIndexes[index]].AlphabeticCode;
        }

        currencyNumericKeys = new string[GeneratedLookupIndexes.CurrencyNumericIndexes.Length];
        for (int index = 0; index < currencyNumericKeys.Length; index++)
        {
            currencyNumericKeys[index] = GeneratedCurrencyRows.Rows[
                GeneratedLookupIndexes.CurrencyNumericIndexes[index]].NumericCode;
        }

        currencyOfficialNameKeys = new string[GeneratedLookupIndexes.CurrencyOfficialNameIndexes.Length];
        for (int index = 0; index < currencyOfficialNameKeys.Length; index++)
        {
            currencyOfficialNameKeys[index] = AsciiCodeText.TrimWhitespace(
                GeneratedCurrencyRows.Rows[
                    GeneratedLookupIndexes.CurrencyOfficialNameIndexes[index]].OfficialName);
        }

        territoryKeyKeys = new string[GeneratedLookupIndexes.TerritoryKeyIndexes.Length];
        for (int index = 0; index < territoryKeyKeys.Length; index++)
        {
            territoryKeyKeys[index] = GeneratedTerritoryRows.Rows[
                GeneratedLookupIndexes.TerritoryKeyIndexes[index]].TerritoryKey;
        }

        territoryAlpha2Keys = new string[GeneratedLookupIndexes.TerritoryAlpha2Indexes.Length];
        for (int index = 0; index < territoryAlpha2Keys.Length; index++)
        {
            territoryAlpha2Keys[index] = GeneratedTerritoryRows.Rows[
                GeneratedLookupIndexes.TerritoryAlpha2Indexes[index]].Alpha2Code;
        }

        territoryAlpha3Keys = new string[GeneratedLookupIndexes.TerritoryAlpha3Indexes.Length];
        for (int index = 0; index < territoryAlpha3Keys.Length; index++)
        {
            territoryAlpha3Keys[index] = GeneratedTerritoryRows.Rows[
                GeneratedLookupIndexes.TerritoryAlpha3Indexes[index]].Alpha3Code;
        }

        territoryNumericKeys = new string[GeneratedLookupIndexes.TerritoryNumericIndexes.Length];
        for (int index = 0; index < territoryNumericKeys.Length; index++)
        {
            territoryNumericKeys[index] = GeneratedTerritoryRows.Rows[
                GeneratedLookupIndexes.TerritoryNumericIndexes[index]].NumericCode;
        }

        territoryOfficialNameKeys = new string[GeneratedLookupIndexes.TerritoryOfficialNameIndexes.Length];
        for (int index = 0; index < territoryOfficialNameKeys.Length; index++)
        {
            territoryOfficialNameKeys[index] = GeneratedTerritoryRows.Rows[
                GeneratedLookupIndexes.TerritoryOfficialNameIndexes[index]].CanonicalName;
        }

        usageTerritoryKeys = new string[GeneratedLookupIndexes.UsageTerritoryIndexes.Length];
        for (int index = 0; index < usageTerritoryKeys.Length; index++)
        {
            usageTerritoryKeys[index] = GeneratedCurrencyUsageRows.Rows[
                GeneratedLookupIndexes.UsageTerritoryIndexes[index]].TerritoryKey;
        }

        usageCurrencyKeys = new string[GeneratedLookupIndexes.UsageCurrencyIndexes.Length];
        for (int index = 0; index < usageCurrencyKeys.Length; index++)
        {
            usageCurrencyKeys[index] = GeneratedCurrencyUsageRows.Rows[
                GeneratedLookupIndexes.UsageCurrencyIndexes[index]].CurrencyCode;
        }

        formatCurrencyKeys = new string[GeneratedLookupIndexes.FormatCurrencyIndexes.Length];
        for (int index = 0; index < formatCurrencyKeys.Length; index++)
        {
            formatCurrencyKeys[index] = GeneratedCurrencyFormatRows.Rows[
                GeneratedLookupIndexes.FormatCurrencyIndexes[index]].CurrencyCode;
        }
    }

    internal CurrencyInfo[] FindCurrencyAlphabeticSnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            currencyAlphabeticKeys,
            GeneratedLookupIndexes.CurrencyAlphabeticIndexes,
            key);
        return CreateCurrencySnapshotsFromIndexes(indexes);
    }

    internal CurrencyInfo[] FindCurrencyNumericSnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            currencyNumericKeys,
            GeneratedLookupIndexes.CurrencyNumericIndexes,
            key);
        return CreateCurrencySnapshotsFromIndexes(indexes);
    }

    internal CurrencyInfo[] FindCurrencyOfficialNameSnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            currencyOfficialNameKeys,
            GeneratedLookupIndexes.CurrencyOfficialNameIndexes,
            key);
        return CreateCurrencySnapshotsFromIndexes(indexes);
    }

    internal TerritoryInfo[] FindTerritoryKeySnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            territoryKeyKeys,
            GeneratedLookupIndexes.TerritoryKeyIndexes,
            key);
        return CreateTerritorySnapshotsFromIndexes(indexes);
    }

    internal TerritoryInfo[] FindTerritoryAlpha2Snapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            territoryAlpha2Keys,
            GeneratedLookupIndexes.TerritoryAlpha2Indexes,
            key);
        return CreateTerritorySnapshotsFromIndexes(indexes);
    }

    internal TerritoryInfo[] FindTerritoryAlpha3Snapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            territoryAlpha3Keys,
            GeneratedLookupIndexes.TerritoryAlpha3Indexes,
            key);
        return CreateTerritorySnapshotsFromIndexes(indexes);
    }

    internal TerritoryInfo[] FindTerritoryNumericSnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            territoryNumericKeys,
            GeneratedLookupIndexes.TerritoryNumericIndexes,
            key);
        return CreateTerritorySnapshotsFromIndexes(indexes);
    }

    internal TerritoryInfo[] FindTerritoryOfficialNameSnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            territoryOfficialNameKeys,
            GeneratedLookupIndexes.TerritoryOfficialNameIndexes,
            key);
        return CreateTerritorySnapshotsFromIndexes(indexes);
    }

    internal CurrencyUsage[] FindUsageTerritorySnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            usageTerritoryKeys,
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            key);
        return CreateUsageSnapshotsFromIndexes(indexes);
    }

    internal CurrencyUsage[] FindUsageCurrencySnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            usageCurrencyKeys,
            GeneratedLookupIndexes.UsageCurrencyIndexes,
            key);
        return CreateUsageSnapshotsFromIndexes(indexes);
    }

    internal CurrencyFormatMetadata[] FindFormatCurrencySnapshots(string key)
    {
        int[] indexes = FindExactIndexMatches(
            formatCurrencyKeys,
            GeneratedLookupIndexes.FormatCurrencyIndexes,
            key);
        return CreateFormatSnapshotsFromIndexes(indexes);
    }

    internal CurrencyInfo[] GetCurrencyCatalogSnapshots(CurrencySet currencySet)
    {
        ValidateCurrencySet(currencySet);

        int count = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            if (IncludesCurrencyStatus(currencySet, GeneratedCurrencyRows.Rows[index].Status))
            {
                count++;
            }
        }

        CurrencyInfo[] result = new CurrencyInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (IncludesCurrencyStatus(currencySet, row.Status))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateCurrencyInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    internal TerritoryInfo[] GetTerritoryCatalogSnapshots()
    {
        return CurrencySnapshotFactory.CreateTerritoryInfoArray(GeneratedTerritoryRows.Rows);
    }

    internal CurrencyUsage[] GetCurrencyUsageCatalogSnapshots()
    {
        return CurrencySnapshotFactory.CreateCurrencyUsageArray(GeneratedCurrencyUsageRows.Rows);
    }

    internal CurrencyInfo[] GetCurrentCurrenciesForTerritorySnapshots(
        string? territoryCode,
        bool tenderOnly)
    {
        string normalized = NormalizeTerritoryUsageKey(territoryCode);
        if (normalized.Length == 0)
        {
            return new CurrencyInfo[0];
        }

        int[] usageIndexes = FindExactIndexMatches(
            usageTerritoryKeys,
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            normalized);
        int count = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (IsFirstCurrencyCodeRow(index)
                && HasCurrentCurrencyUsage(usageIndexes, row.AlphabeticCode, tenderOnly))
            {
                count++;
            }
        }

        CurrencyInfo[] result = new CurrencyInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (IsFirstCurrencyCodeRow(index)
                && HasCurrentCurrencyUsage(usageIndexes, row.AlphabeticCode, tenderOnly))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateCurrencyInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    internal TerritoryInfo[] GetCurrentTerritoriesForCurrencySnapshots(
        string? currencyCode,
        bool tenderOnly)
    {
        string normalized = NormalizeCurrencyUsageCode(currencyCode);
        if (normalized.Length == 0)
        {
            return new TerritoryInfo[0];
        }

        int[] usageIndexes = FindExactIndexMatches(
            usageCurrencyKeys,
            GeneratedLookupIndexes.UsageCurrencyIndexes,
            normalized);
        int count = 0;
        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (HasCurrentTerritoryUsage(usageIndexes, row.TerritoryKey, tenderOnly))
            {
                count++;
            }
        }

        TerritoryInfo[] result = new TerritoryInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (HasCurrentTerritoryUsage(usageIndexes, row.TerritoryKey, tenderOnly))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateTerritoryInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    internal CurrencyInfo[] GetCurrenciesForTerritoryOnDateSnapshots(
        string? territoryCode,
        System.DateOnly date,
        bool tenderOnly)
    {
        string normalized = NormalizeTerritoryUsageKey(territoryCode);
        if (normalized.Length == 0)
        {
            return new CurrencyInfo[0];
        }

        int[] usageIndexes = FindExactIndexMatches(
            usageTerritoryKeys,
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            normalized);
        int count = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (IsFirstCurrencyCodeRow(index)
                && HasCurrencyUsageOnDate(
                    usageIndexes,
                    row.AlphabeticCode,
                    date,
                    tenderOnly))
            {
                count++;
            }
        }

        CurrencyInfo[] result = new CurrencyInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (IsFirstCurrencyCodeRow(index)
                && HasCurrencyUsageOnDate(
                    usageIndexes,
                    row.AlphabeticCode,
                    date,
                    tenderOnly))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateCurrencyInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    internal CurrencyUsage[] GetCurrencyUsageHistoryForTerritorySnapshots(string? territoryCode)
    {
        string normalized = NormalizeTerritoryUsageKey(territoryCode);
        if (normalized.Length == 0)
        {
            return new CurrencyUsage[0];
        }

        int[] indexes = FindExactIndexMatches(
            usageTerritoryKeys,
            GeneratedLookupIndexes.UsageTerritoryIndexes,
            normalized);
        return CreateUsageSnapshotsFromIndexes(indexes);
    }

    internal CurrencyUsage[] GetCurrencyUsageHistoryForCurrencySnapshots(string? currencyCode)
    {
        string normalized = NormalizeCurrencyUsageCode(currencyCode);
        if (normalized.Length == 0)
        {
            return new CurrencyUsage[0];
        }

        int[] indexes = FindExactIndexMatches(
            usageCurrencyKeys,
            GeneratedLookupIndexes.UsageCurrencyIndexes,
            normalized);
        return CreateUsageSnapshotsFromIndexes(indexes);
    }

    internal CurrencyFormatMetadata GetCurrencyFormatMetadataSnapshot(string? code)
    {
        string normalized = NormalizeCurrencyUsageCode(code);
        if (normalized.Length == 0)
        {
            throw new InvalidCurrencyCodeException("Currency alphabetic code is malformed.");
        }

        CurrencyFormatMetadata[] matches = FindFormatCurrencySnapshots(normalized);
        if (matches.Length == 0)
        {
            throw new CurrencyNotFoundException(
                "Currency format metadata for '" + normalized + "' was not found.");
        }

        return matches[0];
    }

    internal CurrencyLookupResult FindCurrencyAlphabeticLookup(string? code, CurrencySet currencySet)
    {
        ValidateCurrencySet(currencySet);
        if (code == null)
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length != CurrencyCodeConstants.CurrencyAlphabeticCodeLength
            || !AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.UppercaseAscii(trimmed);
        CurrencyInfo[] allMatches = FindCurrencyAlphabeticSnapshots(normalized);
        if (allMatches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.Unassigned, normalized);
        }

        CurrencyInfo[] matches = FilterCurrencySnapshots(allMatches, currencySet);
        if (matches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.OutOfScope, normalized);
        }

        return CreateCurrencyLookupSuccess(normalized, matches);
    }

    internal CurrencyLookupResult FindCurrencyNumericLookup(string? code, CurrencySet currencySet)
    {
        ValidateCurrencySet(currencySet);
        if (code == null)
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.TrimWhitespace(code);
        if (normalized.Length != CurrencyCodeConstants.CurrencyNumericCodeLength
            || !AsciiCodeText.IsAsciiDigitText(normalized))
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        CurrencyInfo[] allMatches = FindCurrencyNumericSnapshots(normalized);
        if (allMatches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.Unassigned, normalized);
        }

        CurrencyInfo[] matches = FilterCurrencySnapshots(allMatches, currencySet);
        if (matches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.OutOfScope, normalized);
        }

        return CreateCurrencyMultipleLookupSuccess(normalized, matches);
    }

    internal CurrencyLookupResult FindCurrencyOfficialNameLookup(
        string? name,
        NameMatchMode matchMode,
        CurrencySet currencySet)
    {
        ValidateCurrencySet(currencySet);
        ValidateNameMatchMode(matchMode);
        if (name == null)
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.TrimWhitespace(name);
        if (normalized.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        CurrencyInfo[] allMatches = matchMode == NameMatchMode.ExactOrdinal
            ? FindCurrencyOfficialNameSnapshots(normalized)
            : FindCurrencyOfficialNameIgnoreCaseSnapshots(normalized);
        if (allMatches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.Unassigned, normalized);
        }

        CurrencyInfo[] matches = FilterCurrencySnapshots(allMatches, currencySet);
        if (matches.Length == 0)
        {
            return CreateCurrencyLookupFailure(LookupError.OutOfScope, normalized);
        }

        return CreateCurrencyLookupSuccess(normalized, matches);
    }

    internal TerritoryLookupResult FindTerritoryAlpha2Lookup(string? code)
    {
        if (code == null)
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length != CurrencyCodeConstants.TerritoryAlpha2CodeLength
            || !AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.UppercaseAscii(trimmed);
        TerritoryInfo[] matches = FindTerritoryAlpha2Snapshots(normalized);
        if (matches.Length == 0)
        {
            return CreateTerritoryLookupFailure(LookupError.Unassigned, normalized);
        }

        return CreateTerritoryLookupSuccess(normalized, matches);
    }

    internal TerritoryLookupResult FindTerritoryAlpha3Lookup(string? code)
    {
        if (code == null)
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string trimmed = AsciiCodeText.TrimWhitespace(code);
        if (trimmed.Length != CurrencyCodeConstants.TerritoryAlpha3CodeLength
            || !AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.UppercaseAscii(trimmed);
        TerritoryInfo[] matches = FindTerritoryAlpha3Snapshots(normalized);
        if (matches.Length == 0)
        {
            return CreateTerritoryLookupFailure(LookupError.Unassigned, normalized);
        }

        return CreateTerritoryLookupSuccess(normalized, matches);
    }

    internal TerritoryLookupResult FindTerritoryNumericLookup(string? code)
    {
        if (code == null)
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.TrimWhitespace(code);
        if (normalized.Length != CurrencyCodeConstants.TerritoryNumericCodeLength
            || !AsciiCodeText.IsAsciiDigitText(normalized))
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        TerritoryInfo[] matches = FindTerritoryNumericSnapshots(normalized);
        if (matches.Length == 0)
        {
            return CreateTerritoryLookupFailure(LookupError.Unassigned, normalized);
        }

        return CreateTerritoryLookupSuccess(normalized, matches);
    }

    internal TerritoryLookupResult FindTerritoryOfficialNameLookup(
        string? name,
        NameMatchMode matchMode)
    {
        ValidateNameMatchMode(matchMode);
        if (name == null)
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        string normalized = AsciiCodeText.TrimWhitespace(name);
        if (normalized.Length == 0)
        {
            return CreateTerritoryLookupFailure(LookupError.MalformedInput, string.Empty);
        }

        TerritoryInfo[] matches = matchMode == NameMatchMode.ExactOrdinal
            ? FindTerritoryOfficialNameSnapshots(normalized)
            : FindTerritoryOfficialNameIgnoreCaseSnapshots(normalized);
        if (matches.Length == 0)
        {
            return CreateTerritoryLookupFailure(LookupError.Unassigned, normalized);
        }

        return CreateTerritoryLookupSuccess(normalized, matches);
    }

    internal CurrencyInfo GetRequiredCurrencyAlphabeticSnapshot(string? code, CurrencySet currencySet)
    {
        CurrencyLookupResult result = FindCurrencyAlphabeticLookup(code, currencySet);
        if (result.Error == LookupError.MalformedInput)
        {
            throw new InvalidCurrencyCodeException("Currency alphabetic code is malformed.");
        }

        if (result.Error == LookupError.Unassigned)
        {
            throw new InvalidCurrencyCodeException(
                "Currency alphabetic code '" + result.NormalizedQuery + "' is not assigned.");
        }

        if (result.Error == LookupError.OutOfScope)
        {
            throw new InvalidCurrencyCodeException(
                "Currency alphabetic code '"
                + result.NormalizedQuery
                + "' is outside the requested scope.");
        }

        if (result.Error == LookupError.Ambiguous)
        {
            throw new InvalidCurrencyCodeException(
                "Currency alphabetic code '"
                + result.NormalizedQuery
                + "' identifies more than one designation in the requested scope.");
        }

        CurrencyInfo? match = result.Match;
        if (match == null)
        {
            throw new InvalidCurrencyCodeException("Currency alphabetic lookup did not produce a match.");
        }

        return CurrencySnapshotFactory.CopyCurrencyInfo(match);
    }

    static bool IncludesCurrencyStatus(CurrencySet currencySet, CurrencyStatus status)
    {
        if (currencySet == CurrencySet.All)
        {
            return true;
        }

        if (currencySet == CurrencySet.ActiveOnly)
        {
            return status == CurrencyStatus.Active;
        }

        return status == CurrencyStatus.Historical;
    }

    static void ValidateCurrencySet(CurrencySet currencySet)
    {
        if (currencySet != CurrencySet.ActiveOnly
            && currencySet != CurrencySet.HistoricalOnly
            && currencySet != CurrencySet.All)
        {
            throw new System.ArgumentException("The currency catalog scope is invalid.");
        }
    }

    static void ValidateNameMatchMode(NameMatchMode matchMode)
    {
        if (matchMode != NameMatchMode.ExactOrdinal
            && matchMode != NameMatchMode.ExactOrdinalIgnoreCase)
        {
            throw new System.ArgumentException("The name match mode is invalid.");
        }
    }

    static CurrencyInfo[] FindCurrencyOfficialNameIgnoreCaseSnapshots(string name)
    {
        int count = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            if (string.Equals(
                AsciiCodeText.TrimWhitespace(
                    GeneratedCurrencyRows.Rows[index].OfficialName),
                name,
                System.StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        CurrencyInfo[] result = new CurrencyInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (string.Equals(
                AsciiCodeText.TrimWhitespace(row.OfficialName),
                name,
                System.StringComparison.OrdinalIgnoreCase))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateCurrencyInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    static TerritoryInfo[] FindTerritoryOfficialNameIgnoreCaseSnapshots(string name)
    {
        int count = 0;
        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            if (string.Equals(
                GeneratedTerritoryRows.Rows[index].CanonicalName,
                name,
                System.StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        TerritoryInfo[] result = new TerritoryInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (string.Equals(row.CanonicalName, name, System.StringComparison.OrdinalIgnoreCase))
            {
                result[resultIndex] = CurrencySnapshotFactory.CreateTerritoryInfo(row);
                resultIndex++;
            }
        }

        return result;
    }

    static string NormalizeTerritoryUsageKey(string? territoryCode)
    {
        if (territoryCode == null)
        {
            return string.Empty;
        }

        string trimmed = AsciiCodeText.TrimWhitespace(territoryCode);
        if (trimmed.Length != CurrencyCodeConstants.TerritoryAlpha2CodeLength)
        {
            return trimmed;
        }

        if (!AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return string.Empty;
        }

        return AsciiCodeText.UppercaseAscii(trimmed);
    }

    static string NormalizeCurrencyUsageCode(string? currencyCode)
    {
        if (currencyCode == null)
        {
            return string.Empty;
        }

        string trimmed = AsciiCodeText.TrimWhitespace(currencyCode);
        if (trimmed.Length != CurrencyCodeConstants.CurrencyAlphabeticCodeLength
            || !AsciiCodeText.IsAsciiLetterText(trimmed))
        {
            return string.Empty;
        }

        return AsciiCodeText.UppercaseAscii(trimmed);
    }

    static bool IsFirstCurrencyCodeRow(int rowIndex)
    {
        if (rowIndex == 0)
        {
            return true;
        }

        return string.CompareOrdinal(
            GeneratedCurrencyRows.Rows[rowIndex - 1].AlphabeticCode,
            GeneratedCurrencyRows.Rows[rowIndex].AlphabeticCode) != 0;
    }

    static bool HasCurrentCurrencyUsage(
        int[] usageIndexes,
        string currencyCode,
        bool tenderOnly)
    {
        for (int index = 0; index < usageIndexes.Length; index++)
        {
            CurrencyUsageRow usage = GeneratedCurrencyUsageRows.Rows[usageIndexes[index]];
            if (usage.IsCurrent
                && (!tenderOnly || usage.IsTender)
                && string.CompareOrdinal(usage.CurrencyCode, currencyCode) == 0)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasCurrentTerritoryUsage(
        int[] usageIndexes,
        string territoryKey,
        bool tenderOnly)
    {
        for (int index = 0; index < usageIndexes.Length; index++)
        {
            CurrencyUsageRow usage = GeneratedCurrencyUsageRows.Rows[usageIndexes[index]];
            if (usage.IsCurrent
                && (!tenderOnly || usage.IsTender)
                && string.CompareOrdinal(usage.TerritoryKey, territoryKey) == 0)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasCurrencyUsageOnDate(
        int[] usageIndexes,
        string currencyCode,
        System.DateOnly date,
        bool tenderOnly)
    {
        for (int index = 0; index < usageIndexes.Length; index++)
        {
            CurrencyUsageRow usage = GeneratedCurrencyUsageRows.Rows[usageIndexes[index]];
            if ((!tenderOnly || usage.IsTender)
                && string.CompareOrdinal(usage.CurrencyCode, currencyCode) == 0
                && PartialDateRules.IsWithinInclusiveBounds(
                    date,
                    usage.HasInclusiveStartDate,
                    usage.InclusiveStartDate,
                    usage.HasInclusiveEndDate,
                    usage.InclusiveEndDate))
            {
                return true;
            }
        }

        return false;
    }

    static CurrencyInfo[] FilterCurrencySnapshots(CurrencyInfo[] source, CurrencySet currencySet)
    {
        int count = 0;
        for (int index = 0; index < source.Length; index++)
        {
            if (IncludesCurrencyStatus(currencySet, source[index].Status))
            {
                count++;
            }
        }

        CurrencyInfo[] result = new CurrencyInfo[count];
        int resultIndex = 0;
        for (int index = 0; index < source.Length; index++)
        {
            if (IncludesCurrencyStatus(currencySet, source[index].Status))
            {
                result[resultIndex] = CurrencySnapshotFactory.CopyCurrencyInfo(source[index]);
                resultIndex++;
            }
        }

        return result;
    }

    static CurrencyLookupResult CreateCurrencyLookupFailure(LookupError error, string normalized)
    {
        return new CurrencyLookupResult
        {
            Found = false,
            Error = error,
            NormalizedQuery = normalized,
            Match = null,
            Matches = new CurrencyInfo[0]
        };
    }

    static CurrencyLookupResult CreateCurrencyLookupSuccess(string normalized, CurrencyInfo[] matches)
    {
        LookupError error = matches.Length == 1
            ? LookupError.None
            : LookupError.Ambiguous;
        return new CurrencyLookupResult
        {
            Found = true,
            Error = error,
            NormalizedQuery = normalized,
            Match = CurrencySnapshotFactory.CopyCurrencyInfo(matches[0]),
            Matches = CurrencySnapshotFactory.CopyCurrencyInfoArray(matches)
        };
    }

    static CurrencyLookupResult CreateCurrencyMultipleLookupSuccess(
        string normalized,
        CurrencyInfo[] matches)
    {
        return new CurrencyLookupResult
        {
            Found = true,
            Error = LookupError.None,
            NormalizedQuery = normalized,
            Match = CurrencySnapshotFactory.CopyCurrencyInfo(matches[0]),
            Matches = CurrencySnapshotFactory.CopyCurrencyInfoArray(matches)
        };
    }

    static TerritoryLookupResult CreateTerritoryLookupFailure(LookupError error, string normalized)
    {
        return new TerritoryLookupResult
        {
            Found = false,
            Error = error,
            NormalizedQuery = normalized,
            Match = null,
            Matches = new TerritoryInfo[0]
        };
    }

    static TerritoryLookupResult CreateTerritoryLookupSuccess(
        string normalized,
        TerritoryInfo[] matches)
    {
        LookupError error = matches.Length == 1
            ? LookupError.None
            : LookupError.Ambiguous;
        return new TerritoryLookupResult
        {
            Found = true,
            Error = error,
            NormalizedQuery = normalized,
            Match = CurrencySnapshotFactory.CopyTerritoryInfo(matches[0]),
            Matches = CurrencySnapshotFactory.CopyTerritoryInfoArray(matches)
        };
    }

    static int[] FindExactIndexMatches(string[] keys, int[] rowIndexes, string key)
    {
        int low = 0;
        int high = keys.Length;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            int comparison = string.CompareOrdinal(keys[middle], key);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        int start = low;
        if (start == keys.Length || string.CompareOrdinal(keys[start], key) != 0)
        {
            return new int[0];
        }

        int end = start;
        while (end < keys.Length && string.CompareOrdinal(keys[end], key) == 0)
        {
            end++;
        }

        int[] matches = new int[end - start];
        for (int index = 0; index < matches.Length; index++)
        {
            matches[index] = rowIndexes[start + index];
        }

        return matches;
    }

    static CurrencyInfo[] CreateCurrencySnapshotsFromIndexes(int[] indexes)
    {
        CurrencyInfo[] result = new CurrencyInfo[indexes.Length];
        for (int index = 0; index < indexes.Length; index++)
        {
            result[index] = CurrencySnapshotFactory.CreateCurrencyInfo(
                GeneratedCurrencyRows.Rows[indexes[index]]);
        }

        return result;
    }

    static TerritoryInfo[] CreateTerritorySnapshotsFromIndexes(int[] indexes)
    {
        TerritoryInfo[] result = new TerritoryInfo[indexes.Length];
        for (int index = 0; index < indexes.Length; index++)
        {
            result[index] = CurrencySnapshotFactory.CreateTerritoryInfo(
                GeneratedTerritoryRows.Rows[indexes[index]]);
        }

        return result;
    }

    static CurrencyUsage[] CreateUsageSnapshotsFromIndexes(int[] indexes)
    {
        CurrencyUsage[] result = new CurrencyUsage[indexes.Length];
        for (int index = 0; index < indexes.Length; index++)
        {
            result[index] = CurrencySnapshotFactory.CreateCurrencyUsage(
                GeneratedCurrencyUsageRows.Rows[indexes[index]]);
        }

        return result;
    }

    static CurrencyFormatMetadata[] CreateFormatSnapshotsFromIndexes(int[] indexes)
    {
        CurrencyFormatMetadata[] result = new CurrencyFormatMetadata[indexes.Length];
        for (int index = 0; index < indexes.Length; index++)
        {
            result[index] = CurrencySnapshotFactory.CreateCurrencyFormatMetadata(
                GeneratedCurrencyFormatRows.Rows[indexes[index]]);
        }

        return result;
    }
}
