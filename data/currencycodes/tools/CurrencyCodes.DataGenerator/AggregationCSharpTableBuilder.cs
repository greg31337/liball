using System;
using System.Collections.Generic;
using System.Globalization;

namespace CurrencyCodes.DataGenerator;

internal sealed class AggregationCSharpTableBuilder
{
    internal CanonicalCSharpFile Build(SemanticSnapshot snapshot)
    {
        List<CanonicalCSharpTable> tables = new List<CanonicalCSharpTable>
        {
            BuildHistoricalUsageTable(snapshot.HistoricalCurrencies),
            BuildFractionRuleTable(snapshot.FractionRules),
            BuildActiveCurrencyTable(snapshot.ActiveCurrencies),
            BuildCldrUsageTable(snapshot.CurrencyUsages),
            BuildHistoricalCurrencyTable(snapshot.HistoricalCurrencies),
            BuildActiveUsageTable(snapshot.ActiveCurrencies)
        };
        return new CanonicalCSharpFile(
            "CurrencyCodes.Generated",
            "GeneratedAggregationTables",
            tables);
    }

    private static CanonicalCSharpTable BuildActiveCurrencyTable(ActiveCurrencyAggregationData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        foreach (ActiveCurrencyIdentity currency in data.Currencies)
        {
            string value = Join(
                currency.AlphabeticCode,
                FormatNullable(currency.IsoNumericCodeText),
                currency.OfficialName,
                FormatBoolean(currency.IsFund),
                FormatOptionalInteger(currency.HasIsoMinorUnit, currency.IsoMinorUnitDigits),
                FormatBoolean(currency.HasCldrNumericCode),
                FormatNullable(currency.CldrSourceNumericCodeText),
                FormatNullable(currency.CldrNumericCodeText),
                FormatInteger(currency.CldrNumericCodeValue),
                FormatInteger(currency.CldrStandardDigits),
                FormatInteger(currency.CldrStandardRounding),
                FormatInteger(currency.CldrCashDigits),
                FormatInteger(currency.CldrCashRounding),
                FormatBoolean(currency.CldrStandardDigitsExplicit),
                FormatBoolean(currency.CldrStandardRoundingExplicit),
                FormatBoolean(currency.CldrCashDigitsExplicit),
                FormatBoolean(currency.CldrCashRoundingExplicit),
                FormatBoolean(currency.IdentityConflict),
                FormatBoolean(currency.IdentityConflictReviewed));
            rows.Add(new CanonicalCSharpRow(currency.AlphabeticCode, value));
        }

        return new CanonicalCSharpTable("ActiveCurrencies", "string", false, rows);
    }

    private static CanonicalCSharpTable BuildActiveUsageTable(ActiveCurrencyAggregationData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        foreach (ActiveCurrencyUsage usage in data.Usages)
        {
            string value = Join(
                usage.SourceEntityName,
                usage.CanonicalTerritoryName,
                FormatNullable(usage.TerritoryKind),
                FormatNullable(usage.TerritoryMatchCode),
                FormatNullable(usage.TerritoryAlpha2Code),
                FormatNullable(usage.TerritoryAlpha3Code),
                FormatNullable(usage.TerritoryNumericCodeText),
                string.Join(" ", usage.TerritoryReplacementCodes),
                FormatBoolean(usage.HasCurrencyAssignment),
                FormatNullable(usage.AlphabeticCode),
                usage.SourceCurrencyName);
            string key = usage.SourceEntityName
                + "\t"
                + (usage.AlphabeticCode ?? string.Empty)
                + "\t"
                + usage.CanonicalTerritoryName;
            rows.Add(new CanonicalCSharpRow(key, value));
        }

        return new CanonicalCSharpTable("ActiveCurrencyUsages", "string", false, rows);
    }

    private static CanonicalCSharpTable BuildHistoricalCurrencyTable(
        HistoricalCurrencyAggregationData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        foreach (HistoricalCurrencyIdentity currency in data.Currencies)
        {
            string value = Join(
                currency.AlphabeticCode,
                FormatNullable(currency.IsoNumericCodeText),
                currency.OfficialName,
                FormatNullable(currency.FundIndicator),
                FormatOptionalInteger(currency.HasIsoMinorUnit, currency.IsoMinorUnitDigits),
                currency.WithdrawalText,
                FormatPartialDate(currency.WithdrawalDate),
                FormatBoolean(currency.HasCldrNumericCode),
                FormatNullable(currency.CldrSourceNumericCodeText),
                FormatNullable(currency.CldrNumericCodeText),
                FormatInteger(currency.CldrNumericCodeValue),
                FormatInteger(currency.CldrStandardDigits),
                FormatInteger(currency.CldrStandardRounding),
                FormatInteger(currency.CldrCashDigits),
                FormatInteger(currency.CldrCashRounding),
                FormatBoolean(currency.CldrStandardDigitsExplicit),
                FormatBoolean(currency.CldrStandardRoundingExplicit),
                FormatBoolean(currency.CldrCashDigitsExplicit),
                FormatBoolean(currency.CldrCashRoundingExplicit),
                FormatBoolean(currency.IdentityConflict),
                FormatBoolean(currency.IdentityConflictReviewed));
            string key = currency.AlphabeticCode
                + "\t"
                + FormatPartialDate(currency.WithdrawalDate)
                + "\t"
                + currency.OfficialName
                + "\t"
                + (currency.IsoNumericCodeText ?? string.Empty);
            rows.Add(new CanonicalCSharpRow(key, value));
        }

        return new CanonicalCSharpTable("HistoricalCurrencies", "string", false, rows);
    }

    private static CanonicalCSharpTable BuildHistoricalUsageTable(
        HistoricalCurrencyAggregationData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        foreach (HistoricalCurrencyUsage usage in data.Usages)
        {
            HistoricalCurrencyIdentity identity = data.Currencies[usage.CurrencyIdentityIndex];
            string value = Join(
                FormatInteger(usage.CurrencyIdentityIndex),
                usage.AlphabeticCode,
                usage.SourceEntityName,
                usage.CanonicalTerritoryName,
                FormatNullable(usage.TerritoryKind),
                FormatNullable(usage.TerritoryMatchCode),
                FormatNullable(usage.TerritoryAlpha2Code),
                FormatNullable(usage.TerritoryAlpha3Code),
                FormatNullable(usage.TerritoryNumericCodeText),
                string.Join(" ", usage.TerritoryReplacementCodes));
            string key = usage.AlphabeticCode
                + "\t"
                + FormatPartialDate(identity.WithdrawalDate)
                + "\t"
                + identity.OfficialName
                + "\t"
                + (identity.IsoNumericCodeText ?? string.Empty)
                + "\t"
                + usage.SourceEntityName
                + "\t"
                + usage.CanonicalTerritoryName;
            rows.Add(new CanonicalCSharpRow(key, value));
        }

        return new CanonicalCSharpTable("HistoricalCurrencyUsages", "string", false, rows);
    }

    private static CanonicalCSharpTable BuildCldrUsageTable(CldrCurrencyUsageData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        foreach (CldrCurrencyUsage usage in data.Usages)
        {
            string value = Join(
                usage.TerritoryCode,
                usage.CurrencyCode,
                FormatPartialDate(usage.SourceStartDate),
                FormatPartialDate(usage.SourceEndDate),
                FormatOptionalDate(usage.HasInclusiveStartDate, usage.InclusiveStartDate),
                FormatOptionalDate(usage.HasInclusiveEndDate, usage.InclusiveEndDate),
                FormatBoolean(usage.Tender),
                FormatBoolean(usage.TenderExplicit),
                FormatNullable(usage.StartTimeZone),
                FormatNullable(usage.EndTimeZone));
            string key = usage.TerritoryCode
                + "\t"
                + usage.CurrencyCode
                + "\t"
                + FormatPartialDate(usage.SourceStartDate)
                + "\t"
                + FormatPartialDate(usage.SourceEndDate);
            rows.Add(new CanonicalCSharpRow(key, value));
        }

        return new CanonicalCSharpTable("CldrCurrencyUsages", "string", false, rows);
    }

    private static CanonicalCSharpTable BuildFractionRuleTable(CldrFractionData data)
    {
        List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>();
        AddFractionRule(rows, data.DefaultRule);
        foreach (CldrFractionRule rule in data.ExplicitRules)
        {
            AddFractionRule(rows, rule);
        }

        return new CanonicalCSharpTable("CldrFractionRules", "string", false, rows);
    }

    private static void AddFractionRule(List<CanonicalCSharpRow> rows, CldrFractionRule rule)
    {
        string value = Join(
            rule.CurrencyCode,
            FormatInteger(rule.Digits),
            FormatInteger(rule.Rounding),
            FormatInteger(rule.CashDigits),
            FormatInteger(rule.CashRounding),
            FormatBoolean(rule.DigitsExplicit),
            FormatBoolean(rule.RoundingExplicit),
            FormatBoolean(rule.CashDigitsExplicit),
            FormatBoolean(rule.CashRoundingExplicit));
        rows.Add(new CanonicalCSharpRow(rule.CurrencyCode, value));
    }

    private static string Join(params string[] values)
    {
        return string.Join("\t", values);
    }

    private static string FormatNullable(string? value)
    {
        return value ?? "<null>";
    }

    private static string FormatOptionalInteger(bool hasValue, int value)
    {
        return hasValue ? FormatInteger(value) : "<unknown>";
    }

    private static string FormatInteger(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FormatOptionalDate(bool hasDate, DateOnly date)
    {
        return hasDate ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "<unknown>";
    }

    private static string FormatPartialDate(IsoPartialDate date)
    {
        if (!date.IsKnown)
        {
            return "<unknown>";
        }

        if (date.Precision == IsoDatePrecision.Year)
        {
            return date.Year.ToString("0000", CultureInfo.InvariantCulture);
        }

        if (date.Precision == IsoDatePrecision.Month)
        {
            return date.Year.ToString("0000", CultureInfo.InvariantCulture)
                + "-"
                + date.Month.ToString("00", CultureInfo.InvariantCulture);
        }

        return date.Year.ToString("0000", CultureInfo.InvariantCulture)
            + "-"
            + date.Month.ToString("00", CultureInfo.InvariantCulture)
            + "-"
            + date.Day.ToString("00", CultureInfo.InvariantCulture);
    }
}
