using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListThreeAggregator
{
    internal HistoricalCurrencyAggregationData Aggregate(
        IsoListThreeData listThreeData,
        CrossSourceCurrencyData crossSourceData,
        TerritoryReconciliationData territoryReconciliationData)
    {
        List<CrossSourceCurrencyRow> crossRows = GetListThreeCrossRows(crossSourceData);
        if (crossRows.Count != listThreeData.Rows.Count)
        {
            throw Invalid(
                $"Cross-source List Three row count is {crossRows.Count}; expected {listThreeData.Rows.Count}.");
        }

        Dictionary<string, TerritoryReconciliation> territories =
            IndexListThreeTerritories(territoryReconciliationData);
        Dictionary<string, HistoricalCurrencyIdentity> identities =
            new Dictionary<string, HistoricalCurrencyIdentity>(StringComparer.Ordinal);
        List<PendingUsage> pendingUsages = new List<PendingUsage>();
        HashSet<string> usageKeys = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < listThreeData.Rows.Count; index++)
        {
            IsoListThreeRow sourceRow = listThreeData.Rows[index];
            CrossSourceCurrencyRow crossRow = crossRows[index];
            ValidateSourceAlignment(sourceRow, crossRow);
            if (!territories.TryGetValue(sourceRow.EntityName, out TerritoryReconciliation? territory))
            {
                throw Invalid($"List Three entity '{sourceRow.EntityName}' has no territory reconciliation.");
            }

            string identityKey = CreateDesignationKey(sourceRow);
            HistoricalCurrencyIdentity candidate = CreateIdentity(sourceRow, crossRow);
            if (identities.TryGetValue(identityKey, out HistoricalCurrencyIdentity? existing))
            {
                if (!IdentitiesMatch(existing, candidate))
                {
                    throw Invalid(
                        $"Historical designation '{sourceRow.AlphabeticCode}:{sourceRow.OfficialName}' has conflicting fields.");
                }
            }
            else
            {
                identities.Add(identityKey, candidate);
            }

            string usageKey = sourceRow.EntityName + "\0" + identityKey;
            if (!usageKeys.Add(usageKey))
            {
                throw Invalid(
                    $"List Three repeats usage '{sourceRow.EntityName}:{sourceRow.AlphabeticCode}:{sourceRow.OfficialName}'.");
            }

            pendingUsages.Add(new PendingUsage(identityKey, sourceRow, territory));
        }

        List<HistoricalCurrencyIdentity> currencies =
            new List<HistoricalCurrencyIdentity>(identities.Values);
        currencies.Sort(CompareCurrencies);
        Dictionary<string, int> identityIndexes = CreateIdentityIndexes(currencies);
        List<HistoricalCurrencyUsage> usages = CreateUsages(pendingUsages, identityIndexes);
        ValidateCanonicalOrdering(currencies, usages);
        return new HistoricalCurrencyAggregationData(currencies.AsReadOnly(), usages.AsReadOnly());
    }

    private static List<CrossSourceCurrencyRow> GetListThreeCrossRows(CrossSourceCurrencyData data)
    {
        List<CrossSourceCurrencyRow> rows = new List<CrossSourceCurrencyRow>();
        foreach (CrossSourceCurrencyRow row in data.Rows)
        {
            if (row.SourceList == "list-three")
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static Dictionary<string, TerritoryReconciliation> IndexListThreeTerritories(
        TerritoryReconciliationData data)
    {
        Dictionary<string, TerritoryReconciliation> result =
            new Dictionary<string, TerritoryReconciliation>(StringComparer.Ordinal);
        foreach (TerritoryReconciliation reconciliation in data.Reconciliations)
        {
            if (reconciliation.SourceList == "list-three"
                && !result.TryAdd(reconciliation.SourceEntityName, reconciliation))
            {
                throw Invalid(
                    $"Territory reconciliation repeats List Three entity '{reconciliation.SourceEntityName}'.");
            }
        }

        return result;
    }

    private static void ValidateSourceAlignment(
        IsoListThreeRow sourceRow,
        CrossSourceCurrencyRow crossRow)
    {
        if (crossRow.EntityName != sourceRow.EntityName
            || crossRow.OfficialName != sourceRow.OfficialName
            || crossRow.AlphabeticCode != sourceRow.AlphabeticCode
            || crossRow.IsoNumericCodeText != sourceRow.NumericCode
            || crossRow.HasIsoMinorUnit != sourceRow.HasMinorUnit
            || crossRow.IsoMinorUnitDigits != sourceRow.MinorUnitDigits)
        {
            throw Invalid($"Cross-source row does not align with List Three entity '{sourceRow.EntityName}'.");
        }
    }

    private static HistoricalCurrencyIdentity CreateIdentity(
        IsoListThreeRow sourceRow,
        CrossSourceCurrencyRow crossRow)
    {
        if (!crossRow.HasCldrFormatRule)
        {
            throw Invalid($"Historical currency '{sourceRow.AlphabeticCode}' has no resolved CLDR format rule.");
        }

        return new HistoricalCurrencyIdentity(
            sourceRow.AlphabeticCode,
            sourceRow.NumericCode,
            sourceRow.OfficialName,
            sourceRow.FundIndicator,
            sourceRow.HasMinorUnit,
            sourceRow.MinorUnitDigits,
            sourceRow.WithdrawalText,
            sourceRow.WithdrawalDate,
            crossRow.HasCldrNumericCode,
            crossRow.CldrSourceNumericCodeText,
            crossRow.CldrNumericCodeText,
            crossRow.CldrNumericCodeValue,
            crossRow.CldrStandardDigits,
            crossRow.CldrStandardRounding,
            crossRow.CldrCashDigits,
            crossRow.CldrCashRounding,
            crossRow.CldrStandardDigitsExplicit,
            crossRow.CldrStandardRoundingExplicit,
            crossRow.CldrCashDigitsExplicit,
            crossRow.CldrCashRoundingExplicit,
            crossRow.IdentityConflict,
            crossRow.IdentityConflictReviewed);
    }

    private static Dictionary<string, int> CreateIdentityIndexes(
        List<HistoricalCurrencyIdentity> currencies)
    {
        Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < currencies.Count; index++)
        {
            string key = CreateDesignationKey(currencies[index]);
            if (!result.TryAdd(key, index))
            {
                throw Invalid("Canonical historical identity order contains a duplicate designation key.");
            }
        }

        return result;
    }

    private static List<HistoricalCurrencyUsage> CreateUsages(
        List<PendingUsage> pendingUsages,
        Dictionary<string, int> identityIndexes)
    {
        List<HistoricalCurrencyUsage> usages = new List<HistoricalCurrencyUsage>();
        foreach (PendingUsage pending in pendingUsages)
        {
            if (!identityIndexes.TryGetValue(pending.IdentityKey, out int identityIndex))
            {
                throw Invalid("Historical usage references an unknown designation key.");
            }

            TerritoryReconciliation territory = pending.Territory;
            usages.Add(new HistoricalCurrencyUsage(
                identityIndex,
                pending.SourceRow.AlphabeticCode,
                pending.SourceRow.EntityName,
                territory.CanonicalName,
                territory.TerritoryKind,
                territory.CldrMatchCode,
                territory.Alpha2Code,
                territory.Alpha3Code,
                territory.NumericCodeText,
                territory.ReplacementCodes));
        }

        usages.Sort(CompareUsages);
        return usages;
    }

    private static bool IdentitiesMatch(
        HistoricalCurrencyIdentity left,
        HistoricalCurrencyIdentity right)
    {
        return left.AlphabeticCode == right.AlphabeticCode
            && left.IsoNumericCodeText == right.IsoNumericCodeText
            && left.OfficialName == right.OfficialName
            && left.FundIndicator == right.FundIndicator
            && left.HasIsoMinorUnit == right.HasIsoMinorUnit
            && left.IsoMinorUnitDigits == right.IsoMinorUnitDigits
            && left.WithdrawalText == right.WithdrawalText
            && PartialDatesMatch(left.WithdrawalDate, right.WithdrawalDate)
            && left.HasCldrNumericCode == right.HasCldrNumericCode
            && left.CldrSourceNumericCodeText == right.CldrSourceNumericCodeText
            && left.CldrNumericCodeText == right.CldrNumericCodeText
            && left.CldrNumericCodeValue == right.CldrNumericCodeValue
            && left.CldrStandardDigits == right.CldrStandardDigits
            && left.CldrStandardRounding == right.CldrStandardRounding
            && left.CldrCashDigits == right.CldrCashDigits
            && left.CldrCashRounding == right.CldrCashRounding
            && left.CldrStandardDigitsExplicit == right.CldrStandardDigitsExplicit
            && left.CldrStandardRoundingExplicit == right.CldrStandardRoundingExplicit
            && left.CldrCashDigitsExplicit == right.CldrCashDigitsExplicit
            && left.CldrCashRoundingExplicit == right.CldrCashRoundingExplicit
            && left.IdentityConflict == right.IdentityConflict
            && left.IdentityConflictReviewed == right.IdentityConflictReviewed;
    }

    private static bool PartialDatesMatch(IsoPartialDate left, IsoPartialDate right)
    {
        return left.IsKnown == right.IsKnown
            && left.Year == right.Year
            && left.Month == right.Month
            && left.Day == right.Day
            && left.Precision == right.Precision;
    }

    private static int CompareCurrencies(
        HistoricalCurrencyIdentity left,
        HistoricalCurrencyIdentity right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.AlphabeticCode, right.AlphabeticCode);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareWithdrawalDates(left.WithdrawalDate, right.WithdrawalDate);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.Ordinal.Compare(left.OfficialName, right.OfficialName);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareNullableOrdinal(left.IsoNumericCodeText, right.IsoNumericCodeText);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.Ordinal.Compare(left.WithdrawalText, right.WithdrawalText);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareNullableOrdinal(left.FundIndicator, right.FundIndicator);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.HasIsoMinorUnit.CompareTo(right.HasIsoMinorUnit);
        return comparison != 0
            ? comparison
            : left.IsoMinorUnitDigits.CompareTo(right.IsoMinorUnitDigits);
    }

    private static int CompareWithdrawalDates(IsoPartialDate left, IsoPartialDate right)
    {
        if (!left.IsKnown)
        {
            return right.IsKnown ? 1 : 0;
        }

        if (!right.IsKnown)
        {
            return -1;
        }

        DateOnly leftDate = NormalizeWithdrawalDate(left);
        DateOnly rightDate = NormalizeWithdrawalDate(right);
        int comparison = leftDate.CompareTo(rightDate);
        return comparison != 0 ? comparison : ((int)left.Precision).CompareTo((int)right.Precision);
    }

    private static DateOnly NormalizeWithdrawalDate(IsoPartialDate date)
    {
        int month = date.Precision == IsoDatePrecision.Year ? 12 : date.Month;
        int day = date.Precision == IsoDatePrecision.Day
            ? date.Day
            : DateTime.DaysInMonth(date.Year, month);
        return new DateOnly(date.Year, month, day);
    }

    private static int CompareUsages(HistoricalCurrencyUsage left, HistoricalCurrencyUsage right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.SourceEntityName, right.SourceEntityName);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.CurrencyIdentityIndex.CompareTo(right.CurrencyIdentityIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        return StringComparer.Ordinal.Compare(left.CanonicalTerritoryName, right.CanonicalTerritoryName);
    }

    private static int CompareNullableOrdinal(string? left, string? right)
    {
        if (left == null)
        {
            return right == null ? 0 : -1;
        }

        return right == null ? 1 : StringComparer.Ordinal.Compare(left, right);
    }

    private static void ValidateCanonicalOrdering(
        List<HistoricalCurrencyIdentity> currencies,
        List<HistoricalCurrencyUsage> usages)
    {
        for (int index = 1; index < currencies.Count; index++)
        {
            if (CompareCurrencies(currencies[index - 1], currencies[index]) >= 0)
            {
                throw Invalid("Historical currency identities are not in unique canonical order.");
            }
        }

        for (int index = 1; index < usages.Count; index++)
        {
            if (CompareUsages(usages[index - 1], usages[index]) >= 0)
            {
                throw Invalid("Historical currency usages are not in unique canonical order.");
            }
        }

        foreach (HistoricalCurrencyUsage usage in usages)
        {
            if (usage.CurrencyIdentityIndex < 0 || usage.CurrencyIdentityIndex >= currencies.Count)
            {
                throw Invalid("Historical currency usage has an out-of-range identity index.");
            }

            if (currencies[usage.CurrencyIdentityIndex].AlphabeticCode != usage.AlphabeticCode)
            {
                throw Invalid("Historical currency usage identity index resolves to the wrong code.");
            }
        }
    }

    private static string CreateDesignationKey(IsoListThreeRow row)
    {
        return CreateDesignationKey(
            row.AlphabeticCode,
            row.NumericCode,
            row.OfficialName,
            row.FundIndicator,
            row.HasMinorUnit,
            row.MinorUnitDigits,
            row.WithdrawalText);
    }

    private static string CreateDesignationKey(HistoricalCurrencyIdentity currency)
    {
        return CreateDesignationKey(
            currency.AlphabeticCode,
            currency.IsoNumericCodeText,
            currency.OfficialName,
            currency.FundIndicator,
            currency.HasIsoMinorUnit,
            currency.IsoMinorUnitDigits,
            currency.WithdrawalText);
    }

    private static string CreateDesignationKey(
        string alphabeticCode,
        string? numericCode,
        string officialName,
        string? fundIndicator,
        bool hasMinorUnit,
        int minorUnitDigits,
        string withdrawalText)
    {
        StringBuilder key = new StringBuilder();
        AppendKeyPart(key, alphabeticCode);
        AppendKeyPart(key, numericCode ?? string.Empty);
        AppendKeyPart(key, officialName);
        AppendKeyPart(key, fundIndicator ?? string.Empty);
        AppendKeyPart(key, hasMinorUnit ? "1" : "0");
        AppendKeyPart(key, minorUnitDigits.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(key, withdrawalText);
        return key.ToString();
    }

    private static void AppendKeyPart(StringBuilder key, string value)
    {
        key.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        key.Append(':');
        key.Append(value);
    }

    private static InvalidDataException Invalid(string message)
    {
        return new InvalidDataException("List Three aggregation failed: " + message);
    }

    private sealed class PendingUsage
    {
        internal PendingUsage(
            string identityKey,
            IsoListThreeRow sourceRow,
            TerritoryReconciliation territory)
        {
            IdentityKey = identityKey;
            SourceRow = sourceRow;
            Territory = territory;
        }

        internal string IdentityKey { get; }

        internal IsoListThreeRow SourceRow { get; }

        internal TerritoryReconciliation Territory { get; }
    }
}
