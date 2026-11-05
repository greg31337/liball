using System;
using System.Collections.Generic;
using System.IO;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListOneAggregator
{
    internal ActiveCurrencyAggregationData Aggregate(
        IsoListOneData listOneData,
        CrossSourceCurrencyData crossSourceData,
        TerritoryReconciliationData territoryReconciliationData)
    {
        List<CrossSourceCurrencyRow> crossRows = GetListOneCrossRows(crossSourceData);
        if (crossRows.Count != listOneData.Rows.Count)
        {
            throw Invalid(
                $"Cross-source List One row count is {crossRows.Count}; expected {listOneData.Rows.Count}.");
        }

        Dictionary<string, TerritoryReconciliation> territories =
            IndexListOneTerritories(territoryReconciliationData);
        Dictionary<string, ActiveCurrencyIdentity> identities =
            new Dictionary<string, ActiveCurrencyIdentity>(StringComparer.Ordinal);
        List<ActiveCurrencyUsage> usages = new List<ActiveCurrencyUsage>();

        for (int index = 0; index < listOneData.Rows.Count; index++)
        {
            IsoListOneRow sourceRow = listOneData.Rows[index];
            CrossSourceCurrencyRow crossRow = crossRows[index];
            ValidateSourceAlignment(sourceRow, crossRow);

            if (!territories.TryGetValue(sourceRow.EntityName, out TerritoryReconciliation? territory))
            {
                throw Invalid($"List One entity '{sourceRow.EntityName}' has no territory reconciliation.");
            }

            usages.Add(CreateUsage(sourceRow, territory));
            if (sourceRow.AlphabeticCode == null)
            {
                continue;
            }

            ActiveCurrencyIdentity candidate = CreateIdentity(sourceRow, crossRow);
            if (identities.TryGetValue(sourceRow.AlphabeticCode, out ActiveCurrencyIdentity? existing))
            {
                if (!IdentitiesMatch(existing, candidate))
                {
                    throw Invalid(
                        $"Repeated List One code '{sourceRow.AlphabeticCode}' has conflicting active identity fields.");
                }
            }
            else
            {
                identities.Add(sourceRow.AlphabeticCode, candidate);
            }
        }

        List<ActiveCurrencyIdentity> currencies = new List<ActiveCurrencyIdentity>(identities.Values);
        currencies.Sort(CompareCurrencies);
        usages.Sort(CompareUsages);
        ValidateCanonicalOrdering(currencies, usages);
        return new ActiveCurrencyAggregationData(currencies.AsReadOnly(), usages.AsReadOnly());
    }

    private static List<CrossSourceCurrencyRow> GetListOneCrossRows(CrossSourceCurrencyData data)
    {
        List<CrossSourceCurrencyRow> rows = new List<CrossSourceCurrencyRow>();
        foreach (CrossSourceCurrencyRow row in data.Rows)
        {
            if (row.SourceList == "list-one")
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static Dictionary<string, TerritoryReconciliation> IndexListOneTerritories(
        TerritoryReconciliationData data)
    {
        Dictionary<string, TerritoryReconciliation> result =
            new Dictionary<string, TerritoryReconciliation>(StringComparer.Ordinal);
        foreach (TerritoryReconciliation reconciliation in data.Reconciliations)
        {
            if (reconciliation.SourceList == "list-one"
                && !result.TryAdd(reconciliation.SourceEntityName, reconciliation))
            {
                throw Invalid(
                    $"Territory reconciliation repeats List One entity '{reconciliation.SourceEntityName}'.");
            }
        }

        return result;
    }

    private static void ValidateSourceAlignment(
        IsoListOneRow sourceRow,
        CrossSourceCurrencyRow crossRow)
    {
        if (crossRow.EntityName != sourceRow.EntityName
            || crossRow.OfficialName != sourceRow.OfficialName
            || crossRow.AlphabeticCode != sourceRow.AlphabeticCode
            || crossRow.IsoNumericCodeText != sourceRow.NumericCode
            || crossRow.HasIsoMinorUnit != sourceRow.HasMinorUnit
            || crossRow.IsoMinorUnitDigits != sourceRow.MinorUnitDigits)
        {
            throw Invalid($"Cross-source row does not align with List One entity '{sourceRow.EntityName}'.");
        }
    }

    private static ActiveCurrencyIdentity CreateIdentity(
        IsoListOneRow sourceRow,
        CrossSourceCurrencyRow crossRow)
    {
        string alphabeticCode = sourceRow.AlphabeticCode
            ?? throw Invalid($"Cannot create active identity for code-less entity '{sourceRow.EntityName}'.");
        if (!crossRow.HasCldrFormatRule)
        {
            throw Invalid($"Active currency '{alphabeticCode}' has no resolved CLDR format rule.");
        }

        return new ActiveCurrencyIdentity(
            alphabeticCode,
            sourceRow.NumericCode,
            sourceRow.OfficialName,
            sourceRow.IsFund,
            sourceRow.HasMinorUnit,
            sourceRow.MinorUnitDigits,
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

    private static ActiveCurrencyUsage CreateUsage(
        IsoListOneRow sourceRow,
        TerritoryReconciliation territory)
    {
        return new ActiveCurrencyUsage(
            sourceRow.EntityName,
            territory.CanonicalName,
            territory.TerritoryKind,
            territory.CldrMatchCode,
            territory.Alpha2Code,
            territory.Alpha3Code,
            territory.NumericCodeText,
            territory.ReplacementCodes,
            sourceRow.AlphabeticCode != null,
            sourceRow.AlphabeticCode,
            sourceRow.OfficialName);
    }

    private static bool IdentitiesMatch(
        ActiveCurrencyIdentity left,
        ActiveCurrencyIdentity right)
    {
        return left.AlphabeticCode == right.AlphabeticCode
            && left.IsoNumericCodeText == right.IsoNumericCodeText
            && left.OfficialName == right.OfficialName
            && left.IsFund == right.IsFund
            && left.HasIsoMinorUnit == right.HasIsoMinorUnit
            && left.IsoMinorUnitDigits == right.IsoMinorUnitDigits
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

    private static int CompareCurrencies(ActiveCurrencyIdentity left, ActiveCurrencyIdentity right)
    {
        return StringComparer.Ordinal.Compare(left.AlphabeticCode, right.AlphabeticCode);
    }

    private static int CompareUsages(ActiveCurrencyUsage left, ActiveCurrencyUsage right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.SourceEntityName, right.SourceEntityName);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareNullableOrdinal(left.AlphabeticCode, right.AlphabeticCode);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.Ordinal.Compare(left.SourceCurrencyName, right.SourceCurrencyName);
        if (comparison != 0)
        {
            return comparison;
        }

        return StringComparer.Ordinal.Compare(
            left.CanonicalTerritoryName,
            right.CanonicalTerritoryName);
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
        List<ActiveCurrencyIdentity> currencies,
        List<ActiveCurrencyUsage> usages)
    {
        for (int index = 1; index < currencies.Count; index++)
        {
            if (CompareCurrencies(currencies[index - 1], currencies[index]) >= 0)
            {
                throw Invalid("Active currency identities are not in unique ordinal code order.");
            }
        }

        for (int index = 1; index < usages.Count; index++)
        {
            if (CompareUsages(usages[index - 1], usages[index]) > 0)
            {
                throw Invalid("Active currency usages are not in canonical ordinal order.");
            }
        }
    }

    private static InvalidDataException Invalid(string message)
    {
        return new InvalidDataException("List One aggregation failed: " + message);
    }
}
