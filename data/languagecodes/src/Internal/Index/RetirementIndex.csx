namespace languagecodes;

internal class RetirementChainRow
{
    internal string StartCode;
    internal string[] Codes;
    internal LanguageNormalizationStatus Status;
    internal string? FinalCode;
    internal string[] ReplacementCandidates;
    internal RetirementRow[] Retirements;

    internal RetirementChainRow(
        string startCode,
        string[] codes,
        LanguageNormalizationStatus status,
        string? finalCode,
        string[] replacementCandidates,
        RetirementRow[] retirements)
    {
        StartCode = startCode;
        Codes = codes;
        Status = status;
        FinalCode = finalCode;
        ReplacementCandidates = replacementCandidates;
        Retirements = retirements;
    }
}

internal class RetirementIndexData
{
    internal global::System.Collections.Generic.Dictionary<string, RetirementRow> RecordsByCode;
    internal global::System.Collections.Generic.Dictionary<string, RetirementChainRow> ChainsByCode;

    internal RetirementIndexData(
        global::System.Collections.Generic.Dictionary<string, RetirementRow> recordsByCode,
        global::System.Collections.Generic.Dictionary<string, RetirementChainRow> chainsByCode)
    {
        RecordsByCode = recordsByCode;
        ChainsByCode = chainsByCode;
    }
}

internal static class RetirementIndex
{
    internal static readonly RetirementIndexData Data =
        RetirementIndexBuilder.BuildIndex(LanguageDataTables.RetirementRows);

    internal static RetirementRow? FindRetirement(string? retiredCode)
    {
        if (retiredCode == null || !Data.RecordsByCode.ContainsKey(retiredCode))
        {
            return null;
        }

        return RetirementIndexBuilder.CopyRetirementRecord(Data.RecordsByCode[retiredCode]);
    }

    internal static RetirementChainRow? FindReplacementChain(string? retiredCode)
    {
        if (retiredCode == null || !Data.ChainsByCode.ContainsKey(retiredCode))
        {
            return null;
        }

        return RetirementIndexBuilder.CopyRetirementChain(Data.ChainsByCode[retiredCode]);
    }
}

internal static class RetirementIndexBuilder
{
    internal static RetirementIndexData BuildIndex(RetirementRow[] source)
    {
        global::System.Collections.Generic.Dictionary<string, RetirementRow> records =
            new global::System.Collections.Generic.Dictionary<string, RetirementRow>(
                global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            RetirementRow row = source[index];
            if (records.ContainsKey(row.RetiredCode))
            {
                throw new global::System.InvalidOperationException(
                    "Duplicate retirement code: " + row.RetiredCode);
            }

            records.Add(row.RetiredCode, CopyRetirementRecord(row));
        }

        global::System.Collections.Generic.Dictionary<string, RetirementChainRow> chains =
            new global::System.Collections.Generic.Dictionary<string, RetirementChainRow>(
                global::System.StringComparer.Ordinal);
        foreach (string code in records.Keys)
        {
            chains.Add(code, BuildReplacementChain(code, records));
        }

        return new RetirementIndexData(records, chains);
    }

    internal static RetirementChainRow BuildReplacementChain(
        string startCode,
        global::System.Collections.Generic.Dictionary<string, RetirementRow> records)
    {
        global::System.Collections.Generic.List<string> codes =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.HashSet<string> visited =
            new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<RetirementRow> retirements =
            new global::System.Collections.Generic.List<RetirementRow>();
        string currentCode = startCode;
        while (true)
        {
            if (visited.Contains(currentCode))
            {
                return new RetirementChainRow(
                    startCode,
                    codes.ToArray(),
                    LanguageNormalizationStatus.ReplacementCycle,
                    null,
                    new string[0],
                    retirements.ToArray());
            }

            visited.Add(currentCode);
            codes.Add(currentCode);
            if (!records.ContainsKey(currentCode))
            {
                return new RetirementChainRow(
                    startCode,
                    codes.ToArray(),
                    LanguageNormalizationStatus.Success,
                    currentCode,
                    new string[0],
                    retirements.ToArray());
            }

            RetirementRow row = records[currentCode];
            retirements.Add(CopyRetirementRecord(row));
            if (row.ReplacementCodes.Length == 0)
            {
                return new RetirementChainRow(
                    startCode,
                    codes.ToArray(),
                    LanguageNormalizationStatus.NoReplacement,
                    null,
                    new string[0],
                    retirements.ToArray());
            }

            if (row.ReplacementCodes.Length > 1)
            {
                return new RetirementChainRow(
                    startCode,
                    codes.ToArray(),
                    LanguageNormalizationStatus.Split,
                    null,
                    LanguageTableMaterializer.CopyStrings(row.ReplacementCodes),
                    retirements.ToArray());
            }

            currentCode = row.ReplacementCodes[0];
        }
    }

    internal static RetirementRow CopyRetirementRecord(RetirementRow source)
    {
        return new RetirementRow(
            source.RetiredCode,
            source.FormerReferenceName,
            source.Reason,
            LanguageTableMaterializer.CopyStrings(source.ReplacementCodes),
            source.Remedy,
            source.EffectiveDate);
    }

    internal static RetirementChainRow CopyRetirementChain(RetirementChainRow source)
    {
        return new RetirementChainRow(
            source.StartCode,
            LanguageTableMaterializer.CopyStrings(source.Codes),
            source.Status,
            source.FinalCode,
            LanguageTableMaterializer.CopyStrings(source.ReplacementCandidates),
            CopyRetirementRecords(source.Retirements));
    }

    internal static RetirementRow[] CopyRetirementRecords(RetirementRow[] source)
    {
        RetirementRow[] destination = new RetirementRow[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyRetirementRecord(source[index]);
        }

        return destination;
    }
}
