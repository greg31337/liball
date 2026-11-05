namespace currencycodes;

internal static class CurrencySnapshotFactory
{
    internal static PartialDate CopyPartialDate(PartialDate source)
    {
        return new PartialDate
        {
            IsKnown = source.IsKnown,
            Year = source.Year,
            Month = source.Month,
            Day = source.Day,
            Precision = source.Precision
        };
    }

    internal static CurrencyInfo CreateCurrencyInfo(CurrencyRow row)
    {
        return new CurrencyInfo
        {
            AlphabeticCode = row.AlphabeticCode,
            HasNumericCode = row.HasNumericCode,
            NumericCode = row.NumericCode,
            NumericCodeValue = row.NumericCodeValue,
            OfficialName = row.OfficialName,
            HasIsoMinorUnit = row.HasIsoMinorUnit,
            IsoMinorUnitDigits = row.IsoMinorUnitDigits,
            Status = row.Status,
            Kind = row.Kind,
            IdentityFromIso = row.IdentityFromIso,
            HistoricalStatusFromIso = row.HistoricalStatusFromIso,
            FundClassificationFromIso = row.FundClassificationFromIso,
            EnrichedFromCldr = row.EnrichedFromCldr,
            IntroductionDate = CopyPartialDate(row.IntroductionDate),
            WithdrawalDate = CopyPartialDate(row.WithdrawalDate)
        };
    }

    internal static CurrencyInfo CopyCurrencyInfo(CurrencyInfo source)
    {
        return new CurrencyInfo
        {
            AlphabeticCode = source.AlphabeticCode,
            HasNumericCode = source.HasNumericCode,
            NumericCode = source.NumericCode,
            NumericCodeValue = source.NumericCodeValue,
            OfficialName = source.OfficialName,
            HasIsoMinorUnit = source.HasIsoMinorUnit,
            IsoMinorUnitDigits = source.IsoMinorUnitDigits,
            Status = source.Status,
            Kind = source.Kind,
            IdentityFromIso = source.IdentityFromIso,
            HistoricalStatusFromIso = source.HistoricalStatusFromIso,
            FundClassificationFromIso = source.FundClassificationFromIso,
            EnrichedFromCldr = source.EnrichedFromCldr,
            IntroductionDate = CopyPartialDate(source.IntroductionDate),
            WithdrawalDate = CopyPartialDate(source.WithdrawalDate)
        };
    }

    internal static CurrencyInfo[] CreateCurrencyInfoArray(CurrencyRow[] rows)
    {
        CurrencyInfo[] result = new CurrencyInfo[rows.Length];
        for (int index = 0; index < rows.Length; index++)
        {
            result[index] = CreateCurrencyInfo(rows[index]);
        }

        return result;
    }

    internal static CurrencyInfo[] CopyCurrencyInfoArray(CurrencyInfo[] source)
    {
        CurrencyInfo[] result = new CurrencyInfo[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            result[index] = CopyCurrencyInfo(source[index]);
        }

        return result;
    }

    internal static TerritoryInfo CreateTerritoryInfo(TerritoryRow row)
    {
        return new TerritoryInfo
        {
            CanonicalName = row.CanonicalName,
            HasAlpha2Code = row.HasAlpha2Code,
            Alpha2Code = row.Alpha2Code,
            HasAlpha3Code = row.HasAlpha3Code,
            Alpha3Code = row.Alpha3Code,
            HasNumericCode = row.HasNumericCode,
            NumericCode = row.NumericCode,
            NumericCodeValue = row.NumericCodeValue,
            Kind = row.Kind
        };
    }

    internal static TerritoryInfo CopyTerritoryInfo(TerritoryInfo source)
    {
        return new TerritoryInfo
        {
            CanonicalName = source.CanonicalName,
            HasAlpha2Code = source.HasAlpha2Code,
            Alpha2Code = source.Alpha2Code,
            HasAlpha3Code = source.HasAlpha3Code,
            Alpha3Code = source.Alpha3Code,
            HasNumericCode = source.HasNumericCode,
            NumericCode = source.NumericCode,
            NumericCodeValue = source.NumericCodeValue,
            Kind = source.Kind
        };
    }

    internal static TerritoryInfo[] CreateTerritoryInfoArray(TerritoryRow[] rows)
    {
        TerritoryInfo[] result = new TerritoryInfo[rows.Length];
        for (int index = 0; index < rows.Length; index++)
        {
            result[index] = CreateTerritoryInfo(rows[index]);
        }

        return result;
    }

    internal static TerritoryInfo[] CopyTerritoryInfoArray(TerritoryInfo[] source)
    {
        TerritoryInfo[] result = new TerritoryInfo[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            result[index] = CopyTerritoryInfo(source[index]);
        }

        return result;
    }

    internal static CurrencyUsage CreateCurrencyUsage(CurrencyUsageRow row)
    {
        return new CurrencyUsage
        {
            TerritoryKey = row.TerritoryKey,
            CurrencyCode = row.CurrencyCode,
            SourceStartDate = CopyPartialDate(row.SourceStartDate),
            SourceEndDate = CopyPartialDate(row.SourceEndDate),
            HasInclusiveStartDate = row.HasInclusiveStartDate,
            InclusiveStartDate = row.InclusiveStartDate,
            HasInclusiveEndDate = row.HasInclusiveEndDate,
            InclusiveEndDate = row.InclusiveEndDate,
            IsTender = row.IsTender,
            IsCurrent = row.IsCurrent,
            FromIsoListOne = row.FromIsoListOne,
            FromIsoListThree = row.FromIsoListThree,
            FromCldr = row.FromCldr
        };
    }

    internal static CurrencyUsage[] CreateCurrencyUsageArray(CurrencyUsageRow[] rows)
    {
        CurrencyUsage[] result = new CurrencyUsage[rows.Length];
        for (int index = 0; index < rows.Length; index++)
        {
            result[index] = CreateCurrencyUsage(rows[index]);
        }

        return result;
    }

    internal static CurrencyFormatMetadata CreateCurrencyFormatMetadata(CurrencyFormatRow row)
    {
        return new CurrencyFormatMetadata
        {
            CurrencyCode = row.CurrencyCode,
            HasIsoMinorUnit = row.HasIsoMinorUnit,
            IsoMinorUnitDigits = row.IsoMinorUnitDigits,
            IsoMinorUnitFactor = row.IsoMinorUnitFactor,
            HasStandardFormat = row.HasStandardFormat,
            StandardFormatDigits = row.StandardFormatDigits,
            StandardRoundingIncrement = row.StandardRoundingIncrement,
            StandardFormatFactor = row.StandardFormatFactor,
            HasCash = row.HasCash,
            CashDigits = row.CashDigits,
            CashRoundingIncrement = row.CashRoundingIncrement,
            CashFactor = row.CashFactor,
            SourceVersion = row.SourceVersion
        };
    }

    internal static CurrencyFormatMetadata[] CreateCurrencyFormatMetadataArray(CurrencyFormatRow[] rows)
    {
        CurrencyFormatMetadata[] result = new CurrencyFormatMetadata[rows.Length];
        for (int index = 0; index < rows.Length; index++)
        {
            result[index] = CreateCurrencyFormatMetadata(rows[index]);
        }

        return result;
    }

    internal static CurrencyLookupResult CopyCurrencyLookupResult(CurrencyLookupResult source)
    {
        CurrencyInfo? match = source.Match == null
            ? null
            : CopyCurrencyInfo(source.Match);
        return new CurrencyLookupResult
        {
            Found = source.Found,
            Error = source.Error,
            NormalizedQuery = source.NormalizedQuery,
            Match = match,
            Matches = CopyCurrencyInfoArray(source.Matches)
        };
    }

    internal static TerritoryLookupResult CopyTerritoryLookupResult(TerritoryLookupResult source)
    {
        TerritoryInfo? match = source.Match == null
            ? null
            : CopyTerritoryInfo(source.Match);
        return new TerritoryLookupResult
        {
            Found = source.Found,
            Error = source.Error,
            NormalizedQuery = source.NormalizedQuery,
            Match = match,
            Matches = CopyTerritoryInfoArray(source.Matches)
        };
    }

    internal static DataVersionInfo CreateDataVersionInfo()
    {
        return new DataVersionInfo
        {
            IsoPublicationDate = GeneratedDataVersion.IsoPublicationDate,
            CldrVersion = GeneratedDataVersion.CldrVersion,
            GeneratorSchemaVersion = GeneratedDataVersion.GeneratorSchemaVersion,
            SnapshotIdentifier = GeneratedDataVersion.SnapshotIdentifier,
            SourceChecksums = CopyStringArray(GeneratedDataVersion.SourceChecksums)
        };
    }

    internal static string[] CopyStringArray(string[] source)
    {
        string[] result = new string[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            result[index] = source[index];
        }

        return result;
    }
}
