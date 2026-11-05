using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CurrencyCodes.DataGenerator;

internal sealed class RuntimeDataCSharpBuilder
{
    internal int WriteFiles(
        SemanticSnapshot snapshot,
        string currencyKindPath,
        GeneratedVersionData versionData,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        Dictionary<string, string> kinds = ReadCurrencyKinds(currencyKindPath);
        HashSet<string> usedKinds = new HashSet<string>(StringComparer.Ordinal);
        List<CurrencyOutputRow> currencies = BuildCurrencies(snapshot, kinds, usedKinds);
        ValidateKindCoverage(kinds, usedKinds);
        List<TerritoryOutputRow> territories = BuildTerritories(snapshot);
        List<UsageOutputRow> usages = BuildUsages(snapshot);
        List<FormatOutputRow> formats = BuildFormats(snapshot);
        CanonicalCSharpEmitter emitter = new CanonicalCSharpEmitter();
        int byteCount = 0;
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedCurrencyRows.g.cs"),
            BuildCurrencyFile(currencies));
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedTerritoryRows.g.cs"),
            BuildTerritoryFile(territories));
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedCurrencyUsageRows.g.cs"),
            BuildUsageFile(usages));
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedCurrencyFormatRows.g.cs"),
            BuildFormatFile(formats));
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedLookupIndexes.g.cs"),
            BuildLookupFile(currencies, territories, usages, formats));
        byteCount += emitter.WriteFile(
            Path.Combine(outputDirectory, "GeneratedDataVersion.g.cs"),
            BuildVersionFile(versionData));
        return byteCount;
    }

    private static List<CurrencyOutputRow> BuildCurrencies(
        SemanticSnapshot snapshot,
        Dictionary<string, string> kinds,
        HashSet<string> usedKinds)
    {
        Dictionary<string, IsoPartialDate> introductions = BuildIntroductions(snapshot.CurrencyUsages);
        List<CurrencyOutputRow> rows = new List<CurrencyOutputRow>();
        foreach (ActiveCurrencyIdentity currency in snapshot.ActiveCurrencies.Currencies)
        {
            string kind = ResolveKind(
                currency.AlphabeticCode,
                "Active",
                currency.IsFund,
                kinds,
                usedKinds);
            bool hasNumericCode = currency.IsoNumericCodeText != null;
            string numericCode = currency.IsoNumericCodeText ?? string.Empty;
            int numericCodeValue = hasNumericCode
                ? int.Parse(numericCode, NumberStyles.None, CultureInfo.InvariantCulture)
                : 0;
            IsoPartialDate introduction = introductions.TryGetValue(
                currency.AlphabeticCode,
                out IsoPartialDate? knownIntroduction)
                    ? knownIntroduction
                    : UnknownDate();
            List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
            {
                CanonicalCSharpValue.String(currency.AlphabeticCode),
                CanonicalCSharpValue.Boolean(hasNumericCode),
                CanonicalCSharpValue.String(numericCode),
                CanonicalCSharpValue.Integer(numericCodeValue),
                CanonicalCSharpValue.String(currency.OfficialName),
                CanonicalCSharpValue.Boolean(currency.HasIsoMinorUnit),
                CanonicalCSharpValue.Integer(currency.IsoMinorUnitDigits),
                CanonicalCSharpValue.Raw("CurrencyStatus.Active"),
                CanonicalCSharpValue.Raw("CurrencyKind." + kind),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.Boolean(kind == "Fund"),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(introduction)),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(UnknownDate()))
            };
            string sortKey = currency.AlphabeticCode + "\t0\t" + currency.OfficialName;
            rows.Add(new CurrencyOutputRow(
                sortKey,
                currency.AlphabeticCode,
                hasNumericCode,
                numericCode,
                currency.OfficialName,
                new CanonicalCSharpRow(sortKey, values)));
        }

        foreach (HistoricalCurrencyIdentity currency in snapshot.HistoricalCurrencies.Currencies)
        {
            string kind = ResolveKind(
                currency.AlphabeticCode,
                "Historical",
                currency.FundIndicator != null,
                kinds,
                usedKinds);
            bool hasNumericCode = currency.IsoNumericCodeText != null;
            string numericCode = currency.IsoNumericCodeText ?? string.Empty;
            int numericCodeValue = hasNumericCode
                ? int.Parse(numericCode, NumberStyles.None, CultureInfo.InvariantCulture)
                : 0;
            List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
            {
                CanonicalCSharpValue.String(currency.AlphabeticCode),
                CanonicalCSharpValue.Boolean(hasNumericCode),
                CanonicalCSharpValue.String(numericCode),
                CanonicalCSharpValue.Integer(numericCodeValue),
                CanonicalCSharpValue.String(currency.OfficialName),
                CanonicalCSharpValue.Boolean(currency.HasIsoMinorUnit),
                CanonicalCSharpValue.Integer(currency.IsoMinorUnitDigits),
                CanonicalCSharpValue.Raw("CurrencyStatus.Historical"),
                CanonicalCSharpValue.Raw("CurrencyKind." + kind),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Boolean(kind == "Fund"),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(UnknownDate())),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(currency.WithdrawalDate))
            };
            string sortKey = currency.AlphabeticCode
                + "\t1\t"
                + FormatPartialDateKey(currency.WithdrawalDate)
                + "\t"
                + currency.OfficialName
                + "\t"
                + numericCode;
            rows.Add(new CurrencyOutputRow(
                sortKey,
                currency.AlphabeticCode,
                hasNumericCode,
                numericCode,
                currency.OfficialName,
                new CanonicalCSharpRow(sortKey, values)));
        }

        AddCldrOnlyCurrencies(snapshot, rows);

        rows.Sort(CompareCurrencyRows);
        return rows;
    }

    private static void AddCldrOnlyCurrencies(
        SemanticSnapshot snapshot,
        List<CurrencyOutputRow> rows)
    {
        HashSet<string> knownCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (CurrencyOutputRow row in rows)
        {
            knownCodes.Add(row.AlphabeticCode);
        }

        Dictionary<string, IsoPartialDate> introductions = BuildIntroductions(snapshot.CurrencyUsages);
        Dictionary<string, IsoPartialDate> withdrawals =
            new Dictionary<string, IsoPartialDate>(StringComparer.Ordinal);
        HashSet<string> currentCodes = new HashSet<string>(StringComparer.Ordinal);
        DateOnly snapshotDate = DateOnly.ParseExact(
            "2026-01-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        foreach (CldrCurrencyUsage usage in snapshot.CurrencyUsages.Usages)
        {
            if (!usage.HasInclusiveEndDate || usage.InclusiveEndDate >= snapshotDate)
            {
                currentCodes.Add(usage.CurrencyCode);
            }

            if (usage.SourceEndDate.IsKnown
                && (!withdrawals.TryGetValue(usage.CurrencyCode, out IsoPartialDate? withdrawal)
                    || NormalizeEnd(usage.SourceEndDate) > NormalizeEnd(withdrawal)))
            {
                withdrawals[usage.CurrencyCode] = usage.SourceEndDate;
            }
        }

        foreach (CldrCurrencyUsage usage in snapshot.CurrencyUsages.Usages)
        {
            string code = usage.CurrencyCode;
            if (knownCodes.Contains(code))
            {
                continue;
            }

            bool isCurrent = currentCodes.Contains(code);
            IsoPartialDate introduction = introductions.TryGetValue(code, out IsoPartialDate? start)
                ? start
                : UnknownDate();
            IsoPartialDate withdrawal = !isCurrent
                && withdrawals.TryGetValue(code, out IsoPartialDate? end)
                    ? end
                    : UnknownDate();
            string sortKey = code + "\t2\t" + FormatPartialDateKey(withdrawal);
            List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
            {
                CanonicalCSharpValue.String(code),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.String(string.Empty),
                CanonicalCSharpValue.Integer(0),
                CanonicalCSharpValue.String(string.Empty),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.Integer(0),
                CanonicalCSharpValue.Raw(
                    isCurrent ? "CurrencyStatus.Active" : "CurrencyStatus.Historical"),
                CanonicalCSharpValue.Raw("CurrencyKind.Unknown"),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.Boolean(false),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(introduction)),
                CanonicalCSharpValue.Raw(FormatPartialDateExpression(withdrawal))
            };
            rows.Add(new CurrencyOutputRow(
                sortKey,
                code,
                false,
                string.Empty,
                string.Empty,
                new CanonicalCSharpRow(sortKey, values)));
            knownCodes.Add(code);
        }
    }

    private static Dictionary<string, IsoPartialDate> BuildIntroductions(CldrCurrencyUsageData data)
    {
        Dictionary<string, IsoPartialDate> result = new Dictionary<string, IsoPartialDate>(StringComparer.Ordinal);
        foreach (CldrCurrencyUsage usage in data.Usages)
        {
            if (!usage.SourceStartDate.IsKnown)
            {
                continue;
            }

            if (!result.TryGetValue(usage.CurrencyCode, out IsoPartialDate? existing)
                || NormalizeStart(usage.SourceStartDate) < NormalizeStart(existing))
            {
                result[usage.CurrencyCode] = usage.SourceStartDate;
            }
        }

        return result;
    }

    private static List<TerritoryOutputRow> BuildTerritories(SemanticSnapshot snapshot)
    {
        Dictionary<string, TerritoryOutputRow> byKey =
            new Dictionary<string, TerritoryOutputRow>(StringComparer.Ordinal);
        foreach (TerritoryReconciliation territory in snapshot.TerritoryReconciliations.Reconciliations)
        {
            string alpha2Code = territory.Alpha2Code ?? string.Empty;
            string numericCode = territory.NumericCodeText ?? string.Empty;
            if (alpha2Code.Length == 0
                && territory.CldrMatchCode != null
                && IsAlpha2(territory.CldrMatchCode))
            {
                alpha2Code = territory.CldrMatchCode;
            }

            if (numericCode.Length == 0
                && territory.CldrMatchCode != null
                && IsNumeric3(territory.CldrMatchCode))
            {
                numericCode = territory.CldrMatchCode;
            }

            bool hasAlpha2Code = alpha2Code.Length != 0;
            bool hasAlpha3Code = territory.Alpha3Code != null;
            string alpha3Code = territory.Alpha3Code ?? string.Empty;
            bool hasNumericCode = numericCode.Length != 0;
            int numericCodeValue = hasNumericCode
                ? int.Parse(numericCode, NumberStyles.None, CultureInfo.InvariantCulture)
                : 0;
            string key = hasAlpha2Code
                ? alpha2Code
                : territory.CldrMatchCode ?? territory.CanonicalName;
            string kind = ResolveTerritoryKind(territory.TerritoryKind);
            string sortKey = territory.CanonicalName + "\t" + key;
            List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
            {
                CanonicalCSharpValue.String(key),
                CanonicalCSharpValue.String(territory.CanonicalName),
                CanonicalCSharpValue.Boolean(hasAlpha2Code),
                CanonicalCSharpValue.String(alpha2Code),
                CanonicalCSharpValue.Boolean(hasAlpha3Code),
                CanonicalCSharpValue.String(alpha3Code),
                CanonicalCSharpValue.Boolean(hasNumericCode),
                CanonicalCSharpValue.String(numericCode),
                CanonicalCSharpValue.Integer(numericCodeValue),
                CanonicalCSharpValue.Raw("TerritoryKind." + kind)
            };
            TerritoryOutputRow candidate = new TerritoryOutputRow(
                sortKey,
                key,
                territory.CanonicalName,
                hasAlpha2Code,
                alpha2Code,
                hasAlpha3Code,
                alpha3Code,
                hasNumericCode,
                numericCode,
                new CanonicalCSharpRow(sortKey, values));
            if (!byKey.ContainsKey(key))
            {
                byKey.Add(key, candidate);
            }
        }

        AddCldrUsageTerritories(snapshot, byKey);

        List<TerritoryOutputRow> rows = new List<TerritoryOutputRow>(byKey.Values);
        rows.Sort(CompareTerritoryRows);
        return rows;
    }

    private static void AddCldrUsageTerritories(
        SemanticSnapshot snapshot,
        Dictionary<string, TerritoryOutputRow> byKey)
    {
        Dictionary<string, CldrTerritoryCode> codes =
            new Dictionary<string, CldrTerritoryCode>(StringComparer.Ordinal);
        foreach (CldrTerritoryCode code in snapshot.TerritoryCodes.TerritoryCodes)
        {
            codes.Add(code.Alpha2Code, code);
        }

        foreach (CldrCurrencyUsage usage in snapshot.CurrencyUsages.Usages)
        {
            if (byKey.ContainsKey(usage.TerritoryCode))
            {
                continue;
            }

            if (!codes.TryGetValue(usage.TerritoryCode, out CldrTerritoryCode? code))
            {
                throw new InvalidDataException(
                    "CLDR currency usage territory '"
                    + usage.TerritoryCode
                    + "' has no territory-code mapping.");
            }

            string canonicalName = ResolveCldrUsageTerritoryName(usage.TerritoryCode);
            string kind = ResolveCldrUsageTerritoryKind(usage.TerritoryCode);
            string alpha3Code = code.Alpha3Code ?? string.Empty;
            string numericCode = code.NumericCodeText ?? string.Empty;
            string sortKey = canonicalName + "\t" + usage.TerritoryCode;
            List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
            {
                CanonicalCSharpValue.String(usage.TerritoryCode),
                CanonicalCSharpValue.String(canonicalName),
                CanonicalCSharpValue.Boolean(true),
                CanonicalCSharpValue.String(usage.TerritoryCode),
                CanonicalCSharpValue.Boolean(code.Alpha3Code != null),
                CanonicalCSharpValue.String(alpha3Code),
                CanonicalCSharpValue.Boolean(code.HasNumericCode),
                CanonicalCSharpValue.String(numericCode),
                CanonicalCSharpValue.Integer(code.NumericCodeValue),
                CanonicalCSharpValue.Raw("TerritoryKind." + kind)
            };
            byKey.Add(
                usage.TerritoryCode,
                new TerritoryOutputRow(
                    sortKey,
                    usage.TerritoryCode,
                    canonicalName,
                    true,
                    usage.TerritoryCode,
                    code.Alpha3Code != null,
                    alpha3Code,
                    code.HasNumericCode,
                    numericCode,
                    new CanonicalCSharpRow(sortKey, values)));
        }
    }

    private static string ResolveCldrUsageTerritoryName(string code)
    {
        if (code == "AC")
        {
            return "Ascension Island";
        }

        if (code == "CP")
        {
            return "Clipperton Island";
        }

        if (code == "DG")
        {
            return "Diego Garcia";
        }

        if (code == "EA")
        {
            return "Ceuta & Melilla";
        }

        if (code == "IC")
        {
            return "Canary Islands";
        }

        if (code == "TA")
        {
            return "Tristan da Cunha";
        }

        if (code == "TP")
        {
            return "East Timor";
        }

        if (code == "XK")
        {
            return "Kosovo";
        }

        if (code == "ZZ")
        {
            return "Unknown Region";
        }

        throw new InvalidDataException(
            "CLDR-only currency usage territory '" + code + "' has no reviewed canonical name.");
    }

    private static string ResolveCldrUsageTerritoryKind(string code)
    {
        if (code == "AC" || code == "CP" || code == "DG" || code == "TA")
        {
            return "DependentTerritory";
        }

        if (code == "TP" || code == "XK")
        {
            return "Country";
        }

        return "Other";
    }

    private static List<UsageOutputRow> BuildUsages(SemanticSnapshot snapshot)
    {
        HashSet<string> activePairs = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> historicalPairs = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, IsoPartialDate> historicalEnds =
            new Dictionary<string, IsoPartialDate>(StringComparer.Ordinal);
        foreach (ActiveCurrencyUsage usage in snapshot.ActiveCurrencies.Usages)
        {
            if (usage.HasCurrencyAssignment && usage.AlphabeticCode != null)
            {
                activePairs.Add(CreatePair(GetTerritoryKey(usage), usage.AlphabeticCode));
            }
        }

        foreach (HistoricalCurrencyUsage usage in snapshot.HistoricalCurrencies.Usages)
        {
            string pair = CreatePair(GetTerritoryKey(usage), usage.AlphabeticCode);
            historicalPairs.Add(pair);
            historicalEnds[pair] = snapshot.HistoricalCurrencies.Currencies[
                usage.CurrencyIdentityIndex].WithdrawalDate;
        }

        List<UsageOutputRow> rows = new List<UsageOutputRow>();
        HashSet<string> currentCldrPairs = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> allCldrPairs = new HashSet<string>(StringComparer.Ordinal);
        DateOnly snapshotDate = DateOnly.ParseExact(
            "2026-01-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        foreach (CldrCurrencyUsage usage in snapshot.CurrencyUsages.Usages)
        {
            string pair = CreatePair(usage.TerritoryCode, usage.CurrencyCode);
            bool isCurrent = !usage.HasInclusiveEndDate || usage.InclusiveEndDate >= snapshotDate;
            allCldrPairs.Add(pair);
            if (isCurrent)
            {
                currentCldrPairs.Add(pair);
            }

            rows.Add(CreateUsageRow(
                usage.TerritoryCode,
                usage.CurrencyCode,
                usage.SourceStartDate,
                usage.SourceEndDate,
                usage.HasInclusiveStartDate,
                usage.InclusiveStartDate,
                usage.HasInclusiveEndDate,
                usage.InclusiveEndDate,
                usage.Tender,
                isCurrent,
                activePairs.Contains(pair),
                historicalPairs.Contains(pair),
                true));
        }

        foreach (string pair in activePairs)
        {
            if (!currentCldrPairs.Contains(pair))
            {
                SplitPair(pair, out string territoryKey, out string currencyCode);
                rows.Add(CreateUsageRow(
                    territoryKey,
                    currencyCode,
                    UnknownDate(),
                    UnknownDate(),
                    false,
                    default(DateOnly),
                    false,
                    default(DateOnly),
                    false,
                    true,
                    true,
                    false,
                    false));
            }
        }

        foreach (string pair in historicalPairs)
        {
            if (!allCldrPairs.Contains(pair))
            {
                SplitPair(pair, out string territoryKey, out string currencyCode);
                IsoPartialDate end = historicalEnds[pair];
                bool hasEnd = end.IsKnown;
                rows.Add(CreateUsageRow(
                    territoryKey,
                    currencyCode,
                    UnknownDate(),
                    end,
                    false,
                    default(DateOnly),
                    hasEnd,
                    hasEnd ? NormalizeEnd(end) : default(DateOnly),
                    false,
                    false,
                    false,
                    true,
                    false));
            }
        }

        rows.Sort(CompareUsageRows);
        return rows;
    }

    private static UsageOutputRow CreateUsageRow(
        string territoryKey,
        string currencyCode,
        IsoPartialDate sourceStartDate,
        IsoPartialDate sourceEndDate,
        bool hasInclusiveStartDate,
        DateOnly inclusiveStartDate,
        bool hasInclusiveEndDate,
        DateOnly inclusiveEndDate,
        bool isTender,
        bool isCurrent,
        bool fromIsoListOne,
        bool fromIsoListThree,
        bool fromCldr)
    {
        string sortKey = territoryKey
            + "\t"
            + currencyCode
            + "\t"
            + FormatPartialDateKey(sourceStartDate)
            + "\t"
            + FormatPartialDateKey(sourceEndDate)
            + "\t"
            + (fromCldr ? "0" : "1");
        List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
        {
            CanonicalCSharpValue.String(territoryKey),
            CanonicalCSharpValue.String(currencyCode),
            CanonicalCSharpValue.Raw(FormatPartialDateExpression(sourceStartDate)),
            CanonicalCSharpValue.Raw(FormatPartialDateExpression(sourceEndDate)),
            CanonicalCSharpValue.Boolean(hasInclusiveStartDate),
            CanonicalCSharpValue.Raw(FormatDateExpression(hasInclusiveStartDate, inclusiveStartDate)),
            CanonicalCSharpValue.Boolean(hasInclusiveEndDate),
            CanonicalCSharpValue.Raw(FormatDateExpression(hasInclusiveEndDate, inclusiveEndDate)),
            CanonicalCSharpValue.Boolean(isTender),
            CanonicalCSharpValue.Boolean(isCurrent),
            CanonicalCSharpValue.Boolean(fromIsoListOne),
            CanonicalCSharpValue.Boolean(fromIsoListThree),
            CanonicalCSharpValue.Boolean(fromCldr)
        };
        return new UsageOutputRow(
            sortKey,
            territoryKey,
            currencyCode,
            new CanonicalCSharpRow(sortKey, values));
    }

    private static List<FormatOutputRow> BuildFormats(SemanticSnapshot snapshot)
    {
        Dictionary<string, FormatOutputRow> byCode =
            new Dictionary<string, FormatOutputRow>(StringComparer.Ordinal);
        foreach (ActiveCurrencyIdentity currency in snapshot.ActiveCurrencies.Currencies)
        {
            byCode[currency.AlphabeticCode] = CreateFormatRow(
                currency.AlphabeticCode,
                currency.HasIsoMinorUnit,
                currency.IsoMinorUnitDigits,
                currency.CldrStandardDigits,
                currency.CldrStandardRounding,
                currency.CldrCashDigits,
                currency.CldrCashRounding);
        }

        foreach (HistoricalCurrencyIdentity currency in snapshot.HistoricalCurrencies.Currencies)
        {
            if (!byCode.ContainsKey(currency.AlphabeticCode))
            {
                byCode.Add(
                    currency.AlphabeticCode,
                    CreateFormatRow(
                        currency.AlphabeticCode,
                        currency.HasIsoMinorUnit,
                        currency.IsoMinorUnitDigits,
                        currency.CldrStandardDigits,
                        currency.CldrStandardRounding,
                        currency.CldrCashDigits,
                        currency.CldrCashRounding));
            }
        }

        foreach (CldrCurrencyUsage usage in snapshot.CurrencyUsages.Usages)
        {
            if (!byCode.ContainsKey(usage.CurrencyCode))
            {
                CldrFractionRule rule = snapshot.FractionRules.ResolveRule(usage.CurrencyCode);
                byCode.Add(
                    usage.CurrencyCode,
                    CreateFormatRow(
                        usage.CurrencyCode,
                        false,
                        0,
                        rule.Digits,
                        rule.Rounding,
                        rule.CashDigits,
                        rule.CashRounding));
            }
        }

        List<FormatOutputRow> rows = new List<FormatOutputRow>(byCode.Values);
        rows.Sort(CompareFormatRows);
        return rows;
    }

    private static FormatOutputRow CreateFormatRow(
        string currencyCode,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        int standardDigits,
        int standardRounding,
        int cashDigits,
        int cashRounding)
    {
        List<CanonicalCSharpValue> values = new List<CanonicalCSharpValue>
        {
            CanonicalCSharpValue.String(currencyCode),
            CanonicalCSharpValue.Boolean(hasIsoMinorUnit),
            CanonicalCSharpValue.Integer(isoMinorUnitDigits),
            CanonicalCSharpValue.Raw(hasIsoMinorUnit ? FormatFactor(isoMinorUnitDigits) : "0m"),
            CanonicalCSharpValue.Boolean(true),
            CanonicalCSharpValue.Integer(standardDigits),
            CanonicalCSharpValue.Integer(standardRounding),
            CanonicalCSharpValue.Raw(FormatFactor(standardDigits)),
            CanonicalCSharpValue.Boolean(true),
            CanonicalCSharpValue.Integer(cashDigits),
            CanonicalCSharpValue.Integer(cashRounding),
            CanonicalCSharpValue.Raw(FormatFactor(cashDigits)),
            CanonicalCSharpValue.String("48.2")
        };
        return new FormatOutputRow(
            currencyCode,
            currencyCode,
            new CanonicalCSharpRow(currencyCode, values));
    }

    private static CanonicalCSharpFile BuildCurrencyFile(List<CurrencyOutputRow> rows)
    {
        List<CanonicalCSharpRow> values = new List<CanonicalCSharpRow>();
        foreach (CurrencyOutputRow row in rows)
        {
            values.Add(row.Row);
        }

        return BuildRowFile("GeneratedCurrencyRows", "CurrencyRow", values);
    }

    private static CanonicalCSharpFile BuildTerritoryFile(List<TerritoryOutputRow> rows)
    {
        List<CanonicalCSharpRow> values = new List<CanonicalCSharpRow>();
        foreach (TerritoryOutputRow row in rows)
        {
            values.Add(row.Row);
        }

        return BuildRowFile("GeneratedTerritoryRows", "TerritoryRow", values);
    }

    private static CanonicalCSharpFile BuildUsageFile(List<UsageOutputRow> rows)
    {
        List<CanonicalCSharpRow> values = new List<CanonicalCSharpRow>();
        foreach (UsageOutputRow row in rows)
        {
            values.Add(row.Row);
        }

        return BuildRowFile("GeneratedCurrencyUsageRows", "CurrencyUsageRow", values);
    }

    private static CanonicalCSharpFile BuildFormatFile(List<FormatOutputRow> rows)
    {
        List<CanonicalCSharpRow> values = new List<CanonicalCSharpRow>();
        foreach (FormatOutputRow row in rows)
        {
            values.Add(row.Row);
        }

        return BuildRowFile("GeneratedCurrencyFormatRows", "CurrencyFormatRow", values);
    }

    private static CanonicalCSharpFile BuildRowFile(
        string className,
        string rowType,
        List<CanonicalCSharpRow> rows)
    {
        return new CanonicalCSharpFile(
            "currencycodes",
            className,
            new CanonicalCSharpTable[]
            {
                new CanonicalCSharpTable("Rows", rowType, true, rows)
            });
    }

    private static CanonicalCSharpFile BuildLookupFile(
        List<CurrencyOutputRow> currencies,
        List<TerritoryOutputRow> territories,
        List<UsageOutputRow> usages,
        List<FormatOutputRow> formats)
    {
        List<CanonicalCSharpTable> tables = new List<CanonicalCSharpTable>
        {
            BuildCurrencyIndex("CurrencyAlphabeticIndexes", currencies, "alphabetic"),
            BuildCurrencyIndex("CurrencyNumericIndexes", currencies, "numeric"),
            BuildCurrencyIndex("CurrencyOfficialNameIndexes", currencies, "name"),
            BuildTerritoryIndex("TerritoryKeyIndexes", territories, "key"),
            BuildTerritoryIndex("TerritoryAlpha2Indexes", territories, "alpha2"),
            BuildTerritoryIndex("TerritoryAlpha3Indexes", territories, "alpha3"),
            BuildTerritoryIndex("TerritoryNumericIndexes", territories, "numeric"),
            BuildTerritoryIndex("TerritoryOfficialNameIndexes", territories, "name"),
            BuildUsageIndex("UsageTerritoryIndexes", usages, true),
            BuildUsageIndex("UsageCurrencyIndexes", usages, false),
            BuildFormatIndex(formats)
        };
        return new CanonicalCSharpFile("currencycodes", "GeneratedLookupIndexes", tables);
    }

    private static CanonicalCSharpTable BuildCurrencyIndex(
        string fieldName,
        List<CurrencyOutputRow> rows,
        string kind)
    {
        List<CanonicalCSharpRow> indexes = new List<CanonicalCSharpRow>();
        for (int index = 0; index < rows.Count; index++)
        {
            CurrencyOutputRow row = rows[index];
            if (kind == "numeric" && !row.HasNumericCode)
            {
                continue;
            }

            string key = kind == "numeric"
                ? row.NumericCode
                : kind == "name" ? row.OfficialName : row.AlphabeticCode;
            indexes.Add(IndexRow(key + "\t" + row.SortKey, index));
        }

        return new CanonicalCSharpTable(fieldName, "int", false, indexes);
    }

    private static CanonicalCSharpTable BuildTerritoryIndex(
        string fieldName,
        List<TerritoryOutputRow> rows,
        string kind)
    {
        List<CanonicalCSharpRow> indexes = new List<CanonicalCSharpRow>();
        for (int index = 0; index < rows.Count; index++)
        {
            TerritoryOutputRow row = rows[index];
            if ((kind == "alpha2" && !row.HasAlpha2Code)
                || (kind == "alpha3" && !row.HasAlpha3Code)
                || (kind == "numeric" && !row.HasNumericCode))
            {
                continue;
            }

            string key = kind == "key"
                ? row.TerritoryKey
                : kind == "alpha2"
                    ? row.Alpha2Code
                    : kind == "alpha3"
                        ? row.Alpha3Code
                        : kind == "numeric" ? row.NumericCode : row.CanonicalName;
            indexes.Add(IndexRow(key + "\t" + row.SortKey, index));
        }

        return new CanonicalCSharpTable(fieldName, "int", false, indexes);
    }

    private static CanonicalCSharpTable BuildUsageIndex(
        string fieldName,
        List<UsageOutputRow> rows,
        bool byTerritory)
    {
        List<CanonicalCSharpRow> indexes = new List<CanonicalCSharpRow>();
        for (int index = 0; index < rows.Count; index++)
        {
            UsageOutputRow row = rows[index];
            string key = byTerritory ? row.TerritoryKey : row.CurrencyCode;
            indexes.Add(IndexRow(key + "\t" + row.SortKey, index));
        }

        return new CanonicalCSharpTable(fieldName, "int", false, indexes);
    }

    private static CanonicalCSharpTable BuildFormatIndex(List<FormatOutputRow> rows)
    {
        List<CanonicalCSharpRow> indexes = new List<CanonicalCSharpRow>();
        for (int index = 0; index < rows.Count; index++)
        {
            indexes.Add(IndexRow(rows[index].CurrencyCode, index));
        }

        return new CanonicalCSharpTable("FormatCurrencyIndexes", "int", false, indexes);
    }

    private static CanonicalCSharpRow IndexRow(string sortKey, int index)
    {
        return new CanonicalCSharpRow(
            sortKey,
            new CanonicalCSharpValue[] { CanonicalCSharpValue.Integer(index) });
    }

    private static CanonicalCSharpFile BuildVersionFile(GeneratedVersionData versionData)
    {
        List<CanonicalCSharpField> fields = new List<CanonicalCSharpField>
        {
            CreateConstant("CldrVersion", versionData.CldrVersion),
            CreateConstant("GeneratorSchemaVersion", versionData.GeneratorSchemaVersion),
            CreateConstant("IsoPublicationDate", versionData.IsoPublicationDate),
            CreateConstant("SnapshotIdentifier", versionData.SnapshotIdentifier)
        };
        List<CanonicalCSharpRow> checksums = new List<CanonicalCSharpRow>();
        foreach (SourceChecksumEntry checksum in versionData.SourceChecksums)
        {
            string value = checksum.Path
                + "\t"
                + checksum.ByteLength.ToString(CultureInfo.InvariantCulture)
                + "\t"
                + checksum.Sha256;
            checksums.Add(new CanonicalCSharpRow(checksum.Path, value));
        }

        return new CanonicalCSharpFile(
            "currencycodes",
            "GeneratedDataVersion",
            fields,
            new CanonicalCSharpTable[]
            {
                new CanonicalCSharpTable("SourceChecksums", "string", false, checksums)
            });
    }

    private static CanonicalCSharpField CreateConstant(string name, string value)
    {
        return new CanonicalCSharpField(
            name,
            "string",
            true,
            CanonicalCSharpValue.String(value));
    }

    private static Dictionary<string, string> ReadCurrencyKinds(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0
            || lines[0] != "alphabetic_code,status,currency_kind,review_basis")
        {
            throw new InvalidDataException("The currency-kind overlay header is invalid.");
        }

        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 1; index < lines.Length; index++)
        {
            if (lines[index].Length == 0)
            {
                continue;
            }

            string[] fields = lines[index].Split(',');
            if (fields.Length != 4 || !IsCurrencyKind(fields[2]))
            {
                throw new InvalidDataException(
                    "Invalid currency-kind overlay entry at line " + (index + 1) + ".");
            }

            string key = fields[0] + "\t" + fields[1];
            if (!result.TryAdd(key, fields[2]))
            {
                throw new InvalidDataException("Duplicate currency-kind overlay key '" + key + "'.");
            }
        }

        return result;
    }

    private static string ResolveKind(
        string code,
        string status,
        bool sourceFund,
        Dictionary<string, string> kinds,
        HashSet<string> usedKinds)
    {
        string key = code + "\t" + status;
        if (kinds.TryGetValue(key, out string? kind))
        {
            usedKinds.Add(key);
            return kind;
        }

        return sourceFund ? "Fund" : "MonetaryCurrency";
    }

    private static void ValidateKindCoverage(
        Dictionary<string, string> kinds,
        HashSet<string> usedKinds)
    {
        foreach (string key in kinds.Keys)
        {
            if (!usedKinds.Contains(key))
            {
                throw new InvalidDataException("Unused currency-kind overlay key '" + key + "'.");
            }
        }
    }

    private static bool IsCurrencyKind(string value)
    {
        return value == "MonetaryCurrency"
            || value == "Fund"
            || value == "PreciousMetal"
            || value == "SpecialUnit"
            || value == "Testing"
            || value == "NoCurrency"
            || value == "Unknown";
    }

    private static string ResolveTerritoryKind(string? value)
    {
        if (value == "DependentTerritory")
        {
            return "DependentTerritory";
        }

        if (value == "SupranationalOrganization")
        {
            return "Supranational";
        }

        if (value == "MonetaryInstitution")
        {
            return "Institution";
        }

        if (value == "OtherSpecialEntity")
        {
            return "Other";
        }

        return "Country";
    }

    private static string GetTerritoryKey(ActiveCurrencyUsage usage)
    {
        return usage.TerritoryAlpha2Code
            ?? usage.TerritoryMatchCode
            ?? usage.CanonicalTerritoryName;
    }

    private static string GetTerritoryKey(HistoricalCurrencyUsage usage)
    {
        return usage.TerritoryAlpha2Code
            ?? usage.TerritoryMatchCode
            ?? usage.CanonicalTerritoryName;
    }

    private static string CreatePair(string territoryKey, string currencyCode)
    {
        return territoryKey + "\t" + currencyCode;
    }

    private static void SplitPair(string pair, out string territoryKey, out string currencyCode)
    {
        int separator = pair.IndexOf('\t');
        territoryKey = pair.Substring(0, separator);
        currencyCode = pair.Substring(separator + 1);
    }

    private static bool IsAlpha2(string value)
    {
        return value.Length == 2
            && value[0] >= 'A'
            && value[0] <= 'Z'
            && value[1] >= 'A'
            && value[1] <= 'Z';
    }

    private static bool IsNumeric3(string value)
    {
        return value.Length == 3
            && value[0] >= '0'
            && value[0] <= '9'
            && value[1] >= '0'
            && value[1] <= '9'
            && value[2] >= '0'
            && value[2] <= '9';
    }

    private static string FormatFactor(int digits)
    {
        return "1" + new string('0', digits) + "m";
    }

    private static string FormatDateExpression(bool hasDate, DateOnly date)
    {
        return hasDate
            ? "new System.DateOnly("
                + date.Year.ToString(CultureInfo.InvariantCulture)
                + ", "
                + date.Month.ToString(CultureInfo.InvariantCulture)
                + ", "
                + date.Day.ToString(CultureInfo.InvariantCulture)
                + ")"
            : "default(System.DateOnly)";
    }

    private static string FormatPartialDateExpression(IsoPartialDate date)
    {
        if (!date.IsKnown)
        {
            return "new PartialDate()";
        }

        return "new PartialDate() { IsKnown = true, Year = "
            + date.Year.ToString(CultureInfo.InvariantCulture)
            + ", Month = "
            + date.Month.ToString(CultureInfo.InvariantCulture)
            + ", Day = "
            + date.Day.ToString(CultureInfo.InvariantCulture)
            + ", Precision = PartialDatePrecision."
            + date.Precision
            + " }";
    }

    private static string FormatPartialDateKey(IsoPartialDate date)
    {
        if (!date.IsKnown)
        {
            return "0000-00-00-0";
        }

        return date.Year.ToString("0000", CultureInfo.InvariantCulture)
            + "-"
            + date.Month.ToString("00", CultureInfo.InvariantCulture)
            + "-"
            + date.Day.ToString("00", CultureInfo.InvariantCulture)
            + "-"
            + ((int)date.Precision).ToString(CultureInfo.InvariantCulture);
    }

    private static IsoPartialDate UnknownDate()
    {
        return new IsoPartialDate(false, 0, 0, 0, IsoDatePrecision.Unknown);
    }

    private static DateOnly NormalizeStart(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 1 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day ? date.Day : 1;
        return new DateOnly(date.Year, month, day);
    }

    private static DateOnly NormalizeEnd(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 12 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day
            ? date.Day
            : DateTime.DaysInMonth(date.Year, month);
        return new DateOnly(date.Year, month, day);
    }

    private static int CompareCurrencyRows(CurrencyOutputRow left, CurrencyOutputRow right)
    {
        return string.CompareOrdinal(left.SortKey, right.SortKey);
    }

    private static int CompareTerritoryRows(TerritoryOutputRow left, TerritoryOutputRow right)
    {
        return string.CompareOrdinal(left.SortKey, right.SortKey);
    }

    private static int CompareUsageRows(UsageOutputRow left, UsageOutputRow right)
    {
        return string.CompareOrdinal(left.SortKey, right.SortKey);
    }

    private static int CompareFormatRows(FormatOutputRow left, FormatOutputRow right)
    {
        return string.CompareOrdinal(left.SortKey, right.SortKey);
    }

    private sealed class CurrencyOutputRow
    {
        internal CurrencyOutputRow(
            string sortKey,
            string alphabeticCode,
            bool hasNumericCode,
            string numericCode,
            string officialName,
            CanonicalCSharpRow row)
        {
            SortKey = sortKey;
            AlphabeticCode = alphabeticCode;
            HasNumericCode = hasNumericCode;
            NumericCode = numericCode;
            OfficialName = officialName;
            Row = row;
        }

        internal string SortKey { get; }
        internal string AlphabeticCode { get; }
        internal bool HasNumericCode { get; }
        internal string NumericCode { get; }
        internal string OfficialName { get; }
        internal CanonicalCSharpRow Row { get; }
    }

    private sealed class TerritoryOutputRow
    {
        internal TerritoryOutputRow(
            string sortKey,
            string territoryKey,
            string canonicalName,
            bool hasAlpha2Code,
            string alpha2Code,
            bool hasAlpha3Code,
            string alpha3Code,
            bool hasNumericCode,
            string numericCode,
            CanonicalCSharpRow row)
        {
            SortKey = sortKey;
            TerritoryKey = territoryKey;
            CanonicalName = canonicalName;
            HasAlpha2Code = hasAlpha2Code;
            Alpha2Code = alpha2Code;
            HasAlpha3Code = hasAlpha3Code;
            Alpha3Code = alpha3Code;
            HasNumericCode = hasNumericCode;
            NumericCode = numericCode;
            Row = row;
        }

        internal string SortKey { get; }
        internal string TerritoryKey { get; }
        internal string CanonicalName { get; }
        internal bool HasAlpha2Code { get; }
        internal string Alpha2Code { get; }
        internal bool HasAlpha3Code { get; }
        internal string Alpha3Code { get; }
        internal bool HasNumericCode { get; }
        internal string NumericCode { get; }
        internal CanonicalCSharpRow Row { get; }
    }

    private sealed class UsageOutputRow
    {
        internal UsageOutputRow(
            string sortKey,
            string territoryKey,
            string currencyCode,
            CanonicalCSharpRow row)
        {
            SortKey = sortKey;
            TerritoryKey = territoryKey;
            CurrencyCode = currencyCode;
            Row = row;
        }

        internal string SortKey { get; }
        internal string TerritoryKey { get; }
        internal string CurrencyCode { get; }
        internal CanonicalCSharpRow Row { get; }
    }

    private sealed class FormatOutputRow
    {
        internal FormatOutputRow(string sortKey, string currencyCode, CanonicalCSharpRow row)
        {
            SortKey = sortKey;
            CurrencyCode = currencyCode;
            Row = row;
        }

        internal string SortKey { get; }
        internal string CurrencyCode { get; }
        internal CanonicalCSharpRow Row { get; }
    }
}
