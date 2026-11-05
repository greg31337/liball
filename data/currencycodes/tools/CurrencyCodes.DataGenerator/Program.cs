using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace CurrencyCodes.DataGenerator;

internal static class Program
{
    private static int Main(string[] arguments)
    {
        if (arguments.Length == 1 && arguments[0] == "--help")
        {
            WriteUsage();
            return 0;
        }

        try
        {
            if (arguments.Length == 2 && arguments[0] == "read-list-one")
            {
                return ReadListOne(arguments[1]);
            }

            if (arguments.Length == 2 && arguments[0] == "read-list-three")
            {
                return ReadListThree(arguments[1]);
            }

            if (arguments.Length == 2 && arguments[0] == "read-cldr-fractions")
            {
                return ReadCldrFractions(arguments[1]);
            }

            if (arguments.Length == 3 && arguments[0] == "resolve-cldr-fraction")
            {
                return ResolveCldrFraction(arguments[1], arguments[2]);
            }

            if (arguments.Length == 2 && arguments[0] == "read-cldr-usage")
            {
                return ReadCldrUsage(arguments[1]);
            }

            if (arguments.Length == 4 && arguments[0] == "inspect-cldr-usage")
            {
                return InspectCldrUsage(arguments[1], arguments[2], arguments[3]);
            }

            if (arguments.Length == 3 && arguments[0] == "read-cldr-territory-codes")
            {
                return ReadCldrTerritoryCodes(arguments[1], arguments[2]);
            }

            if (arguments.Length == 4 && arguments[0] == "inspect-cldr-territory-code")
            {
                return InspectCldrTerritoryCode(arguments[1], arguments[2], arguments[3]);
            }

            if (arguments.Length == 7 && arguments[0] == "reconcile-territories")
            {
                return ReconcileTerritories(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6]);
            }

            if (arguments.Length == 9 && arguments[0] == "inspect-territory-reconciliation")
            {
                return InspectTerritoryReconciliation(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8]);
            }

            if (arguments.Length == 5 && arguments[0] == "reconcile-cross-source")
            {
                return ReconcileCrossSource(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4]);
            }

            if (arguments.Length == 7 && arguments[0] == "inspect-cross-source")
            {
                return InspectCrossSource(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6]);
            }

            if (arguments.Length == 8 && arguments[0] == "validate-data")
            {
                return ValidateData(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7]);
            }

            if (arguments.Length == 8 && arguments[0] == "aggregate-list-one")
            {
                return AggregateListOne(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7]);
            }

            if (arguments.Length == 9 && arguments[0] == "inspect-active-currency")
            {
                return InspectActiveCurrency(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8]);
            }

            if (arguments.Length == 8 && arguments[0] == "aggregate-list-three")
            {
                return AggregateListThree(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7]);
            }

            if (arguments.Length == 9 && arguments[0] == "inspect-historical-currency")
            {
                return InspectHistoricalCurrency(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8]);
            }

            if (arguments.Length == 15 && arguments[0] == "semantic-diff")
            {
                return SemanticDiff(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8],
                    arguments[9],
                    arguments[10],
                    arguments[11],
                    arguments[12],
                    arguments[13],
                    arguments[14]);
            }

            if (arguments.Length == 9 && arguments[0] == "emit-canonical-tables")
            {
                return EmitCanonicalTables(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8]);
            }

            if (arguments.Length == 4 && arguments[0] == "emit-version-data")
            {
                return EmitVersionData(arguments[1], arguments[2], arguments[3]);
            }

            if (arguments.Length == 12 && arguments[0] == "emit-runtime-data")
            {
                return EmitRuntimeData(
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    arguments[4],
                    arguments[5],
                    arguments[6],
                    arguments[7],
                    arguments[8],
                    arguments[9],
                    arguments[10],
                    arguments[11]);
            }
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is InvalidDataException
            || exception is RegexMatchTimeoutException
            || exception is UnauthorizedAccessException
            || exception is XmlException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        Console.Error.WriteLine("Invalid generator arguments.");
        WriteUsage();
        return 2;
    }

    private static int ReadListOne(string path)
    {
        IsoListOneData data = new IsoListOneReader().Read(path);
        int alphabeticCodeCount = 0;
        int numericCodeCount = 0;
        int minorUnitCount = 0;

        foreach (IsoListOneRow row in data.Rows)
        {
            if (row.AlphabeticCode != null)
            {
                alphabeticCodeCount++;
            }

            if (row.NumericCode != null)
            {
                numericCodeCount++;
            }

            if (row.HasMinorUnit)
            {
                minorUnitCount++;
            }
        }

        Console.WriteLine($"publication-date\t{data.PublicationDate}");
        Console.WriteLine($"rows\t{data.Rows.Count}");
        Console.WriteLine($"alphabetic-codes\t{alphabeticCodeCount}");
        Console.WriteLine($"numeric-codes\t{numericCodeCount}");
        Console.WriteLine($"known-minor-units\t{minorUnitCount}");
        return 0;
    }

    private static int ReadCldrUsage(string path)
    {
        CldrCurrencyUsageData data = new CldrSupplementalReader().ReadCurrencyUsageData(path);
        HashSet<string> territoryCodes = new HashSet<string>(StringComparer.Ordinal);
        int knownStartCount = 0;
        int knownEndCount = 0;
        int nonTenderCount = 0;
        int explicitTenderCount = 0;
        int startTimeZoneCount = 0;
        int endTimeZoneCount = 0;

        foreach (CldrCurrencyUsage usage in data.Usages)
        {
            territoryCodes.Add(usage.TerritoryCode);
            if (usage.HasInclusiveStartDate)
            {
                knownStartCount++;
            }

            if (usage.HasInclusiveEndDate)
            {
                knownEndCount++;
            }

            if (!usage.Tender)
            {
                nonTenderCount++;
            }

            if (usage.TenderExplicit)
            {
                explicitTenderCount++;
            }

            if (usage.StartTimeZone != null)
            {
                startTimeZoneCount++;
            }

            if (usage.EndTimeZone != null)
            {
                endTimeZoneCount++;
            }
        }

        Console.WriteLine($"territories\t{territoryCodes.Count}");
        Console.WriteLine($"usage-rows\t{data.Usages.Count}");
        Console.WriteLine($"known-starts\t{knownStartCount}");
        Console.WriteLine($"known-ends\t{knownEndCount}");
        Console.WriteLine($"non-tender\t{nonTenderCount}");
        Console.WriteLine($"explicit-tender\t{explicitTenderCount}");
        Console.WriteLine($"start-time-zones\t{startTimeZoneCount}");
        Console.WriteLine($"end-time-zones\t{endTimeZoneCount}");
        return 0;
    }

    private static int InspectCldrUsage(string path, string territoryCode, string currencyCode)
    {
        CldrCurrencyUsageData data = new CldrSupplementalReader().ReadCurrencyUsageData(path);
        int matchCount = 0;
        foreach (CldrCurrencyUsage usage in data.Usages)
        {
            if (string.Equals(usage.TerritoryCode, territoryCode, StringComparison.Ordinal)
                && string.Equals(usage.CurrencyCode, currencyCode, StringComparison.Ordinal))
            {
                matchCount++;
                Console.WriteLine($"usage\t{matchCount}");
                Console.WriteLine($"start-precision\t{FormatDatePrecision(usage.SourceStartDate.Precision)}");
                Console.WriteLine($"inclusive-start\t{FormatDate(usage.HasInclusiveStartDate, usage.InclusiveStartDate)}");
                Console.WriteLine($"end-precision\t{FormatDatePrecision(usage.SourceEndDate.Precision)}");
                Console.WriteLine($"inclusive-end\t{FormatDate(usage.HasInclusiveEndDate, usage.InclusiveEndDate)}");
                Console.WriteLine($"tender\t{FormatBoolean(usage.Tender)}\t{FormatProvenance(usage.TenderExplicit)}");
                Console.WriteLine($"start-time-zone\t{usage.StartTimeZone ?? string.Empty}");
                Console.WriteLine($"end-time-zone\t{usage.EndTimeZone ?? string.Empty}");
            }
        }

        Console.WriteLine($"matches\t{matchCount}");
        return 0;
    }

    private static int ReadCldrTerritoryCodes(string dataPath, string metadataPath)
    {
        CldrTerritoryCodeData data = new CldrSupplementalReader().ReadTerritoryCodeData(
            dataPath,
            metadataPath);
        int alpha3Count = 0;
        int numericCount = 0;
        int leadingZeroNumericCount = 0;
        int deprecatedAliasCount = 0;
        int overlongAliasCount = 0;
        int multipleReplacementCount = 0;

        foreach (CldrTerritoryCode territoryCode in data.TerritoryCodes)
        {
            if (territoryCode.Alpha3Code != null)
            {
                alpha3Count++;
            }

            if (territoryCode.HasNumericCode)
            {
                numericCount++;
                if (territoryCode.NumericCodeText != null
                    && territoryCode.NumericCodeText[0] == '0')
                {
                    leadingZeroNumericCount++;
                }
            }
        }

        foreach (CldrTerritoryAlias alias in data.Aliases)
        {
            if (alias.Reason == "deprecated")
            {
                deprecatedAliasCount++;
            }
            else if (alias.Reason == "overlong")
            {
                overlongAliasCount++;
            }

            if (alias.ReplacementCodes.Count > 1)
            {
                multipleReplacementCount++;
            }
        }

        Console.WriteLine($"territory-codes\t{data.TerritoryCodes.Count}");
        Console.WriteLine($"alpha-3-codes\t{alpha3Count}");
        Console.WriteLine($"numeric-codes\t{numericCount}");
        Console.WriteLine($"leading-zero-numeric-codes\t{leadingZeroNumericCount}");
        Console.WriteLine($"aliases\t{data.Aliases.Count}");
        Console.WriteLine($"deprecated-aliases\t{deprecatedAliasCount}");
        Console.WriteLine($"overlong-aliases\t{overlongAliasCount}");
        Console.WriteLine($"multiple-replacement-aliases\t{multipleReplacementCount}");
        return 0;
    }

    private static int InspectCldrTerritoryCode(string dataPath, string metadataPath, string query)
    {
        CldrTerritoryCodeData data = new CldrSupplementalReader().ReadTerritoryCodeData(
            dataPath,
            metadataPath);
        int territoryMatchCount = 0;
        foreach (CldrTerritoryCode territoryCode in data.TerritoryCodes)
        {
            if (query == territoryCode.Alpha2Code
                || query == territoryCode.Alpha3Code
                || query == territoryCode.NumericCodeText)
            {
                territoryMatchCount++;
                Console.WriteLine($"territory\t{territoryMatchCount}");
                Console.WriteLine($"alpha-2\t{territoryCode.Alpha2Code}");
                Console.WriteLine($"alpha-3\t{territoryCode.Alpha3Code ?? string.Empty}");
                Console.WriteLine($"numeric-text\t{territoryCode.NumericCodeText ?? string.Empty}");
                Console.WriteLine(
                    $"numeric-value\t{FormatOptionalInteger(territoryCode.HasNumericCode, territoryCode.NumericCodeValue)}");
            }
        }

        int aliasMatchCount = 0;
        foreach (CldrTerritoryAlias alias in data.Aliases)
        {
            if (query == alias.AliasCode)
            {
                aliasMatchCount++;
                Console.WriteLine($"alias\t{aliasMatchCount}");
                Console.WriteLine($"alias-code\t{alias.AliasCode}");
                Console.WriteLine($"alias-reason\t{alias.Reason}");
                Console.WriteLine($"replacement-text\t{alias.ReplacementText}");
                Console.WriteLine($"replacement-count\t{alias.ReplacementCodes.Count}");
                for (int index = 0; index < alias.ReplacementCodes.Count; index++)
                {
                    Console.WriteLine($"replacement\t{index + 1}\t{alias.ReplacementCodes[index]}");
                }
            }
        }

        Console.WriteLine($"territory-matches\t{territoryMatchCount}");
        Console.WriteLine($"alias-matches\t{aliasMatchCount}");
        return 0;
    }

    private static string FormatOptionalInteger(bool hasValue, int value)
    {
        return hasValue ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static int ReconcileTerritories(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string overlayPath)
    {
        TerritoryReconciliationData data = ReadTerritoryReconciliations(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            overlayPath);
        int listOneCount = 0;
        int listThreeCount = 0;
        int explicitOverrideCount = 0;
        int directMatchCount = 0;
        int aliasCount = 0;
        int noCodeCount = 0;
        foreach (TerritoryReconciliation reconciliation in data.Reconciliations)
        {
            if (reconciliation.SourceList == "list-one")
            {
                listOneCount++;
            }
            else
            {
                listThreeCount++;
            }

            if (reconciliation.ExplicitOverride)
            {
                explicitOverrideCount++;
            }
            else
            {
                directMatchCount++;
            }

            if (reconciliation.ReplacementCodes.Count != 0)
            {
                aliasCount++;
            }

            if (reconciliation.CldrMatchCode == null)
            {
                noCodeCount++;
            }
        }

        Console.WriteLine($"reconciliations\t{data.Reconciliations.Count}");
        Console.WriteLine($"list-one-entities\t{listOneCount}");
        Console.WriteLine($"list-three-entities\t{listThreeCount}");
        Console.WriteLine($"direct-un-m49-matches\t{directMatchCount}");
        Console.WriteLine($"explicit-overrides\t{explicitOverrideCount}");
        Console.WriteLine($"deprecated-aliases\t{aliasCount}");
        Console.WriteLine($"no-code-entities\t{noCodeCount}");
        Console.WriteLine("unmapped-entities\t0");
        Console.WriteLine("stale-overrides\t0");
        return 0;
    }

    private static int InspectTerritoryReconciliation(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string overlayPath,
        string sourceList,
        string sourceEntityName)
    {
        TerritoryReconciliationData data = ReadTerritoryReconciliations(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            overlayPath);
        int matchCount = 0;
        foreach (TerritoryReconciliation reconciliation in data.Reconciliations)
        {
            if (reconciliation.SourceList == sourceList
                && reconciliation.SourceEntityName == sourceEntityName)
            {
                matchCount++;
                Console.WriteLine($"source-list\t{reconciliation.SourceList}");
                Console.WriteLine($"source-entity\t{reconciliation.SourceEntityName}");
                Console.WriteLine($"canonical-name\t{reconciliation.CanonicalName}");
                Console.WriteLine($"territory-kind\t{reconciliation.TerritoryKind ?? string.Empty}");
                Console.WriteLine($"cldr-match-code\t{reconciliation.CldrMatchCode ?? string.Empty}");
                Console.WriteLine($"alpha-2\t{reconciliation.Alpha2Code ?? string.Empty}");
                Console.WriteLine($"alpha-3\t{reconciliation.Alpha3Code ?? string.Empty}");
                Console.WriteLine($"numeric-text\t{reconciliation.NumericCodeText ?? string.Empty}");
                Console.WriteLine(
                    $"numeric-value\t{FormatOptionalInteger(reconciliation.HasNumericCode, reconciliation.NumericCodeValue)}");
                Console.WriteLine($"explicit-override\t{FormatBoolean(reconciliation.ExplicitOverride)}");
                Console.WriteLine($"replacement-count\t{reconciliation.ReplacementCodes.Count}");
                for (int index = 0; index < reconciliation.ReplacementCodes.Count; index++)
                {
                    Console.WriteLine($"replacement\t{index + 1}\t{reconciliation.ReplacementCodes[index]}");
                }
            }
        }

        Console.WriteLine($"matches\t{matchCount}");
        return 0;
    }

    private static TerritoryReconciliationData ReadTerritoryReconciliations(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string overlayPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrTerritoryCodeData cldrData = new CldrSupplementalReader().ReadTerritoryCodeData(
            cldrDataPath,
            cldrMetadataPath);
        return new TerritoryReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            cldrData,
            unM49Path,
            overlayPath);
    }

    private static int ReconcileCrossSource(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string conflictPath)
    {
        CrossSourceCurrencyData data = ReadCrossSourceCurrencyData(
            listOnePath,
            listThreePath,
            cldrDataPath,
            conflictPath);
        int listOneCount = 0;
        int listThreeCount = 0;
        int alphabeticCodeCount = 0;
        int isoMinorUnitCount = 0;
        int cldrNumericCount = 0;
        int explicitStandardDigitsCount = 0;
        int identityConflictCount = 0;
        int reviewedIdentityConflictCount = 0;
        HashSet<string> standardDigitDifferenceCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cashRuleDifferenceCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (CrossSourceCurrencyRow row in data.Rows)
        {
            if (row.SourceList == "list-one")
            {
                listOneCount++;
            }
            else
            {
                listThreeCount++;
            }

            if (row.AlphabeticCode != null)
            {
                alphabeticCodeCount++;
            }

            if (row.HasIsoMinorUnit)
            {
                isoMinorUnitCount++;
                if (row.HasCldrFormatRule
                    && row.AlphabeticCode != null
                    && row.IsoMinorUnitDigits != row.CldrStandardDigits)
                {
                    standardDigitDifferenceCodes.Add(row.AlphabeticCode);
                }
            }

            if (row.HasCldrNumericCode)
            {
                cldrNumericCount++;
            }

            if (row.CldrStandardDigitsExplicit)
            {
                explicitStandardDigitsCount++;
            }

            if (row.HasCldrFormatRule
                && row.AlphabeticCode != null
                && (row.CldrStandardDigits != row.CldrCashDigits
                    || row.CldrStandardRounding != row.CldrCashRounding))
            {
                cashRuleDifferenceCodes.Add(row.AlphabeticCode);
            }

            if (row.IdentityConflict)
            {
                identityConflictCount++;
            }

            if (row.IdentityConflictReviewed)
            {
                reviewedIdentityConflictCount++;
            }
        }

        Console.WriteLine($"rows\t{data.Rows.Count}");
        Console.WriteLine($"list-one-rows\t{listOneCount}");
        Console.WriteLine($"list-three-rows\t{listThreeCount}");
        Console.WriteLine($"alphabetic-code-rows\t{alphabeticCodeCount}");
        Console.WriteLine($"known-iso-minor-units\t{isoMinorUnitCount}");
        Console.WriteLine($"cldr-numeric-mapping-rows\t{cldrNumericCount}");
        Console.WriteLine($"explicit-cldr-standard-digit-rows\t{explicitStandardDigitsCount}");
        Console.WriteLine($"iso-cldr-standard-digit-difference-codes\t{standardDigitDifferenceCodes.Count}");
        Console.WriteLine($"cldr-standard-cash-difference-codes\t{cashRuleDifferenceCodes.Count}");
        Console.WriteLine($"identity-conflicts\t{identityConflictCount}");
        Console.WriteLine($"reviewed-identity-conflicts\t{reviewedIdentityConflictCount}");
        return 0;
    }

    private static int InspectCrossSource(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string conflictPath,
        string sourceList,
        string alphabeticCode)
    {
        CrossSourceCurrencyData data = ReadCrossSourceCurrencyData(
            listOnePath,
            listThreePath,
            cldrDataPath,
            conflictPath);
        int matchCount = 0;
        foreach (CrossSourceCurrencyRow row in data.Rows)
        {
            if (row.SourceList == sourceList && row.AlphabeticCode == alphabeticCode)
            {
                matchCount++;
                Console.WriteLine($"row\t{matchCount}");
                Console.WriteLine($"entity\t{row.EntityName}");
                Console.WriteLine($"official-name\t{row.OfficialName}");
                Console.WriteLine($"alphabetic-code\t{row.AlphabeticCode}");
                Console.WriteLine($"iso-numeric-text\t{row.IsoNumericCodeText ?? string.Empty}");
                Console.WriteLine(
                    $"iso-minor-unit\t{FormatOptionalInteger(row.HasIsoMinorUnit, row.IsoMinorUnitDigits)}");
                Console.WriteLine($"cldr-numeric-source-text\t{row.CldrSourceNumericCodeText ?? string.Empty}");
                Console.WriteLine($"cldr-numeric-text\t{row.CldrNumericCodeText ?? string.Empty}");
                Console.WriteLine(
                    $"cldr-numeric-value\t{FormatOptionalInteger(row.HasCldrNumericCode, row.CldrNumericCodeValue)}");
                Console.WriteLine(
                    $"cldr-standard-digits\t{FormatOptionalInteger(row.HasCldrFormatRule, row.CldrStandardDigits)}\t{FormatProvenance(row.CldrStandardDigitsExplicit)}");
                Console.WriteLine(
                    $"cldr-standard-rounding\t{FormatOptionalInteger(row.HasCldrFormatRule, row.CldrStandardRounding)}\t{FormatProvenance(row.CldrStandardRoundingExplicit)}");
                Console.WriteLine(
                    $"cldr-cash-digits\t{FormatOptionalInteger(row.HasCldrFormatRule, row.CldrCashDigits)}\t{FormatProvenance(row.CldrCashDigitsExplicit)}");
                Console.WriteLine(
                    $"cldr-cash-rounding\t{FormatOptionalInteger(row.HasCldrFormatRule, row.CldrCashRounding)}\t{FormatProvenance(row.CldrCashRoundingExplicit)}");
                Console.WriteLine($"identity-conflict\t{FormatBoolean(row.IdentityConflict)}");
                Console.WriteLine($"identity-conflict-reviewed\t{FormatBoolean(row.IdentityConflictReviewed)}");
            }
        }

        Console.WriteLine($"matches\t{matchCount}");
        return 0;
    }

    private static CrossSourceCurrencyData ReadCrossSourceCurrencyData(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string conflictPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrSupplementalReader cldrReader = new CldrSupplementalReader();
        CldrFractionData fractionData = cldrReader.ReadFractionData(cldrDataPath);
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings =
            cldrReader.ReadCurrencyCodeMappings(cldrDataPath);
        return new CrossSourceReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            currencyMappings,
            fractionData,
            conflictPath);
    }

    private static int ValidateData(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrSupplementalReader cldrReader = new CldrSupplementalReader();
        CldrFractionData fractionData = cldrReader.ReadFractionData(cldrDataPath);
        CldrCurrencyUsageData usageData = cldrReader.ReadCurrencyUsageData(cldrDataPath);
        CldrTerritoryCodeData territoryCodeData = cldrReader.ReadTerritoryCodeData(
            cldrDataPath,
            cldrMetadataPath);
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings =
            cldrReader.ReadCurrencyCodeMappings(cldrDataPath);
        TerritoryReconciliationData territoryReconciliations = new TerritoryReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            territoryCodeData,
            unM49Path,
            territoryOverlayPath);
        CrossSourceCurrencyData crossSourceData = new CrossSourceReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            currencyMappings,
            fractionData,
            conflictPath);

        new DataInvariantValidator().Validate(
            listOneData,
            listThreeData,
            fractionData,
            usageData,
            territoryCodeData,
            currencyMappings,
            territoryReconciliations,
            crossSourceData);

        Console.WriteLine("code-grammar\tvalid");
        Console.WriteLine("canonical-numeric-width\tvalid");
        Console.WriteLine("duplicate-identities\tvalid");
        Console.WriteLine("range-bounds\tvalid");
        Console.WriteLine("date-ordering\tvalid");
        Console.WriteLine("status-consistency\tvalid");
        Console.WriteLine("source-completeness\tvalid");
        Console.WriteLine("forward-reverse-mappings\tvalid");
        Console.WriteLine($"iso-rows\t{listOneData.Rows.Count + listThreeData.Rows.Count}");
        Console.WriteLine($"territory-reconciliations\t{territoryReconciliations.Reconciliations.Count}");
        Console.WriteLine($"currency-usage-rows\t{usageData.Usages.Count}");
        return 0;
    }

    private static int AggregateListOne(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        ActiveCurrencyAggregationData data = ReadActiveCurrencyAggregation(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        int fundCount = 0;
        int noCurrencyUsageCount = 0;
        Dictionary<string, int> usageCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ActiveCurrencyIdentity currency in data.Currencies)
        {
            if (currency.IsFund)
            {
                fundCount++;
            }
        }

        foreach (ActiveCurrencyUsage usage in data.Usages)
        {
            if (!usage.HasCurrencyAssignment || usage.AlphabeticCode == null)
            {
                noCurrencyUsageCount++;
                continue;
            }

            if (usageCounts.TryGetValue(usage.AlphabeticCode, out int count))
            {
                usageCounts[usage.AlphabeticCode] = count + 1;
            }
            else
            {
                usageCounts.Add(usage.AlphabeticCode, 1);
            }
        }

        int multiEntityCurrencyCount = 0;
        foreach (KeyValuePair<string, int> entry in usageCounts)
        {
            if (entry.Value > 1)
            {
                multiEntityCurrencyCount++;
            }
        }

        Console.WriteLine($"active-currency-identities\t{data.Currencies.Count}");
        Console.WriteLine($"active-currency-usages\t{data.Usages.Count}");
        Console.WriteLine($"fund-identities\t{fundCount}");
        Console.WriteLine($"no-currency-usages\t{noCurrencyUsageCount}");
        Console.WriteLine($"multi-entity-currencies\t{multiEntityCurrencyCount}");
        Console.WriteLine($"first-currency-code\t{data.Currencies[0].AlphabeticCode}");
        Console.WriteLine($"last-currency-code\t{data.Currencies[data.Currencies.Count - 1].AlphabeticCode}");
        Console.WriteLine("currency-order\tordinal");
        Console.WriteLine("usage-order\tordinal");
        return 0;
    }

    private static int InspectActiveCurrency(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath,
        string alphabeticCode)
    {
        ActiveCurrencyAggregationData data = ReadActiveCurrencyAggregation(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        int identityMatches = 0;
        foreach (ActiveCurrencyIdentity currency in data.Currencies)
        {
            if (currency.AlphabeticCode == alphabeticCode)
            {
                identityMatches++;
                Console.WriteLine($"alphabetic-code\t{currency.AlphabeticCode}");
                Console.WriteLine($"official-name\t{currency.OfficialName}");
                Console.WriteLine($"iso-numeric-text\t{currency.IsoNumericCodeText ?? string.Empty}");
                Console.WriteLine(
                    $"iso-minor-unit\t{FormatOptionalInteger(currency.HasIsoMinorUnit, currency.IsoMinorUnitDigits)}");
                Console.WriteLine($"is-fund\t{FormatBoolean(currency.IsFund)}");
                Console.WriteLine($"cldr-numeric-text\t{currency.CldrNumericCodeText ?? string.Empty}");
                Console.WriteLine($"cldr-standard-digits\t{currency.CldrStandardDigits}");
                Console.WriteLine($"cldr-standard-rounding\t{currency.CldrStandardRounding}");
                Console.WriteLine($"cldr-cash-digits\t{currency.CldrCashDigits}");
                Console.WriteLine($"cldr-cash-rounding\t{currency.CldrCashRounding}");
                Console.WriteLine($"identity-conflict\t{FormatBoolean(currency.IdentityConflict)}");
                Console.WriteLine(
                    $"identity-conflict-reviewed\t{FormatBoolean(currency.IdentityConflictReviewed)}");
            }
        }

        int usageMatches = 0;
        foreach (ActiveCurrencyUsage usage in data.Usages)
        {
            if (usage.AlphabeticCode == alphabeticCode)
            {
                usageMatches++;
                Console.WriteLine($"usage\t{usageMatches}");
                Console.WriteLine($"source-entity\t{usage.SourceEntityName}");
                Console.WriteLine($"territory-name\t{usage.CanonicalTerritoryName}");
                Console.WriteLine($"territory-match-code\t{usage.TerritoryMatchCode ?? string.Empty}");
                Console.WriteLine($"territory-alpha-2\t{usage.TerritoryAlpha2Code ?? string.Empty}");
            }
        }

        Console.WriteLine($"identity-matches\t{identityMatches}");
        Console.WriteLine($"usage-matches\t{usageMatches}");
        return 0;
    }

    private static ActiveCurrencyAggregationData ReadActiveCurrencyAggregation(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrSupplementalReader cldrReader = new CldrSupplementalReader();
        CldrFractionData fractionData = cldrReader.ReadFractionData(cldrDataPath);
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings =
            cldrReader.ReadCurrencyCodeMappings(cldrDataPath);
        CldrTerritoryCodeData territoryCodeData = cldrReader.ReadTerritoryCodeData(
            cldrDataPath,
            cldrMetadataPath);
        CrossSourceCurrencyData crossSourceData = new CrossSourceReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            currencyMappings,
            fractionData,
            conflictPath);
        TerritoryReconciliationData territoryReconciliations = new TerritoryReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            territoryCodeData,
            unM49Path,
            territoryOverlayPath);
        return new IsoListOneAggregator().Aggregate(
            listOneData,
            crossSourceData,
            territoryReconciliations);
    }

    private static int AggregateListThree(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        HistoricalCurrencyAggregationData data = ReadHistoricalCurrencyAggregation(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        Dictionary<string, int> alphabeticCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, int> numericCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int noNumericCodeCount = 0;
        int unknownWithdrawalCount = 0;
        foreach (HistoricalCurrencyIdentity currency in data.Currencies)
        {
            IncrementCount(alphabeticCounts, currency.AlphabeticCode);
            if (currency.IsoNumericCodeText == null)
            {
                noNumericCodeCount++;
            }
            else
            {
                IncrementCount(numericCounts, currency.IsoNumericCodeText);
            }

            if (!currency.WithdrawalDate.IsKnown)
            {
                unknownWithdrawalCount++;
            }
        }

        int multipleDesignationCodeCount = CountRepeatedKeys(alphabeticCounts);
        int reusedNumericCodeCount = CountRepeatedKeys(numericCounts);
        Console.WriteLine($"historical-currency-identities\t{data.Currencies.Count}");
        Console.WriteLine($"historical-currency-usages\t{data.Usages.Count}");
        Console.WriteLine($"distinct-alphabetic-codes\t{alphabeticCounts.Count}");
        Console.WriteLine($"multiple-designation-alphabetic-codes\t{multipleDesignationCodeCount}");
        Console.WriteLine($"reused-numeric-codes\t{reusedNumericCodeCount}");
        Console.WriteLine($"identities-without-numeric-code\t{noNumericCodeCount}");
        Console.WriteLine($"unknown-withdrawal-identities\t{unknownWithdrawalCount}");
        Console.WriteLine($"first-historical-code\t{data.Currencies[0].AlphabeticCode}");
        Console.WriteLine($"last-historical-code\t{data.Currencies[data.Currencies.Count - 1].AlphabeticCode}");
        Console.WriteLine("identity-order\talphabetic-date-name-ordinal");
        Console.WriteLine("usage-order\tordinal");
        return 0;
    }

    private static int InspectHistoricalCurrency(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath,
        string alphabeticCode)
    {
        HistoricalCurrencyAggregationData data = ReadHistoricalCurrencyAggregation(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        int identityMatches = 0;
        for (int index = 0; index < data.Currencies.Count; index++)
        {
            HistoricalCurrencyIdentity currency = data.Currencies[index];
            if (currency.AlphabeticCode == alphabeticCode)
            {
                identityMatches++;
                Console.WriteLine($"identity\t{identityMatches}");
                Console.WriteLine($"identity-index\t{index}");
                Console.WriteLine($"alphabetic-code\t{currency.AlphabeticCode}");
                Console.WriteLine($"official-name\t{currency.OfficialName}");
                Console.WriteLine($"iso-numeric-text\t{currency.IsoNumericCodeText ?? string.Empty}");
                Console.WriteLine($"fund-indicator\t{currency.FundIndicator ?? string.Empty}");
                Console.WriteLine($"withdrawal-text\t{currency.WithdrawalText}");
                Console.WriteLine($"withdrawal-precision\t{FormatDatePrecision(currency.WithdrawalDate.Precision)}");
                Console.WriteLine($"cldr-numeric-text\t{currency.CldrNumericCodeText ?? string.Empty}");
                Console.WriteLine($"cldr-standard-digits\t{currency.CldrStandardDigits}");
                Console.WriteLine($"cldr-cash-digits\t{currency.CldrCashDigits}");

                int usageMatches = 0;
                foreach (HistoricalCurrencyUsage usage in data.Usages)
                {
                    if (usage.CurrencyIdentityIndex == index)
                    {
                        usageMatches++;
                        Console.WriteLine($"usage\t{usageMatches}");
                        Console.WriteLine($"source-entity\t{usage.SourceEntityName}");
                        Console.WriteLine($"territory-name\t{usage.CanonicalTerritoryName}");
                        Console.WriteLine($"territory-match-code\t{usage.TerritoryMatchCode ?? string.Empty}");
                    }
                }

                Console.WriteLine($"identity-usage-count\t{usageMatches}");
            }
        }

        Console.WriteLine($"identity-matches\t{identityMatches}");
        return 0;
    }

    private static HistoricalCurrencyAggregationData ReadHistoricalCurrencyAggregation(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrSupplementalReader cldrReader = new CldrSupplementalReader();
        CldrFractionData fractionData = cldrReader.ReadFractionData(cldrDataPath);
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings =
            cldrReader.ReadCurrencyCodeMappings(cldrDataPath);
        CldrTerritoryCodeData territoryCodeData = cldrReader.ReadTerritoryCodeData(
            cldrDataPath,
            cldrMetadataPath);
        CrossSourceCurrencyData crossSourceData = new CrossSourceReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            currencyMappings,
            fractionData,
            conflictPath);
        TerritoryReconciliationData territoryReconciliations = new TerritoryReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            territoryCodeData,
            unM49Path,
            territoryOverlayPath);
        return new IsoListThreeAggregator().Aggregate(
            listThreeData,
            crossSourceData,
            territoryReconciliations);
    }

    private static int SemanticDiff(
        string oldListOnePath,
        string oldListThreePath,
        string oldCldrDataPath,
        string oldCldrMetadataPath,
        string oldUnM49Path,
        string oldTerritoryOverlayPath,
        string oldConflictPath,
        string newListOnePath,
        string newListThreePath,
        string newCldrDataPath,
        string newCldrMetadataPath,
        string newUnM49Path,
        string newTerritoryOverlayPath,
        string newConflictPath)
    {
        SemanticSnapshot oldSnapshot = ReadSemanticSnapshot(
            oldListOnePath,
            oldListThreePath,
            oldCldrDataPath,
            oldCldrMetadataPath,
            oldUnM49Path,
            oldTerritoryOverlayPath,
            oldConflictPath);
        SemanticSnapshot newSnapshot = ReadSemanticSnapshot(
            newListOnePath,
            newListThreePath,
            newCldrDataPath,
            newCldrMetadataPath,
            newUnM49Path,
            newTerritoryOverlayPath,
            newConflictPath);
        SemanticDiffWriter writer = new SemanticDiffWriter();
        SemanticDiffData data = writer.Compare(oldSnapshot, newSnapshot);
        writer.Write(Console.Out, data);
        Console.WriteLine($"diff-entries\t{data.Entries.Count}");
        return 0;
    }

    private static SemanticSnapshot ReadSemanticSnapshot(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath)
    {
        IsoListOneData listOneData = new IsoListOneReader().Read(listOnePath);
        IsoListThreeData listThreeData = new IsoListThreeReader().Read(listThreePath);
        CldrSupplementalReader cldrReader = new CldrSupplementalReader();
        CldrFractionData fractionData = cldrReader.ReadFractionData(cldrDataPath);
        CldrCurrencyUsageData usageData = cldrReader.ReadCurrencyUsageData(cldrDataPath);
        CldrTerritoryCodeData territoryCodeData = cldrReader.ReadTerritoryCodeData(
            cldrDataPath,
            cldrMetadataPath);
        IReadOnlyList<CldrCurrencyCodeMapping> currencyMappings =
            cldrReader.ReadCurrencyCodeMappings(cldrDataPath);
        TerritoryReconciliationData territoryReconciliations = new TerritoryReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            territoryCodeData,
            unM49Path,
            territoryOverlayPath);
        CrossSourceCurrencyData crossSourceData = new CrossSourceReconciler().Reconcile(
            listOneData.Rows,
            listThreeData.Rows,
            currencyMappings,
            fractionData,
            conflictPath);
        new DataInvariantValidator().Validate(
            listOneData,
            listThreeData,
            fractionData,
            usageData,
            territoryCodeData,
            currencyMappings,
            territoryReconciliations,
            crossSourceData);
        ActiveCurrencyAggregationData activeCurrencies = new IsoListOneAggregator().Aggregate(
            listOneData,
            crossSourceData,
            territoryReconciliations);
        HistoricalCurrencyAggregationData historicalCurrencies = new IsoListThreeAggregator().Aggregate(
            listThreeData,
            crossSourceData,
            territoryReconciliations);
        return new SemanticSnapshot(
            activeCurrencies,
            historicalCurrencies,
            usageData,
            fractionData,
            territoryCodeData,
            territoryReconciliations);
    }

    private static int EmitCanonicalTables(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath,
        string outputPath)
    {
        SemanticSnapshot snapshot = ReadSemanticSnapshot(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        CanonicalCSharpFile file = new AggregationCSharpTableBuilder().Build(snapshot);
        int byteCount = new CanonicalCSharpEmitter().WriteFile(outputPath, file);
        int rowCount = 0;
        foreach (CanonicalCSharpTable table in file.Tables)
        {
            rowCount += table.Rows.Count;
        }

        Console.WriteLine($"output-path\t{outputPath}");
        Console.WriteLine($"tables\t{file.Tables.Count}");
        Console.WriteLine($"rows\t{rowCount}");
        Console.WriteLine($"bytes\t{byteCount}");
        Console.WriteLine("encoding\tutf-8-no-bom");
        Console.WriteLine("line-endings\tlf");
        return 0;
    }

    private static int EmitVersionData(
        string manifestPath,
        string repositoryRoot,
        string outputPath)
    {
        SourceChecksumManifest manifest = new SourceChecksumManifestReader().Read(
            manifestPath,
            repositoryRoot);
        GeneratedVersionData data = new GeneratedVersionBuilder().Build(
            manifest,
            repositoryRoot);
        int byteCount = new CanonicalCSharpEmitter().WriteFile(outputPath, data.File);
        Console.WriteLine($"output-path\t{outputPath}");
        Console.WriteLine($"iso-publication-date\t{data.IsoPublicationDate}");
        Console.WriteLine($"cldr-version\t{data.CldrVersion}");
        Console.WriteLine($"generator-schema-version\t{data.GeneratorSchemaVersion}");
        Console.WriteLine($"snapshot-identifier\t{data.SnapshotIdentifier}");
        Console.WriteLine($"source-checksums\t{data.SourceChecksums.Count}");
        Console.WriteLine($"bytes\t{byteCount}");
        return 0;
    }

    private static int EmitRuntimeData(
        string listOnePath,
        string listThreePath,
        string cldrDataPath,
        string cldrMetadataPath,
        string unM49Path,
        string territoryOverlayPath,
        string conflictPath,
        string currencyKindPath,
        string manifestPath,
        string repositoryRoot,
        string outputDirectory)
    {
        SemanticSnapshot snapshot = ReadSemanticSnapshot(
            listOnePath,
            listThreePath,
            cldrDataPath,
            cldrMetadataPath,
            unM49Path,
            territoryOverlayPath,
            conflictPath);
        SourceChecksumManifest manifest = new SourceChecksumManifestReader().Read(
            manifestPath,
            repositoryRoot);
        GeneratedVersionData versionData = new GeneratedVersionBuilder().Build(
            manifest,
            repositoryRoot);
        int byteCount = new RuntimeDataCSharpBuilder().WriteFiles(
            snapshot,
            currencyKindPath,
            versionData,
            outputDirectory);
        Console.WriteLine($"output-directory\t{outputDirectory}");
        Console.WriteLine("generated-files\t6");
        Console.WriteLine($"bytes\t{byteCount}");
        return 0;
    }

    private static void IncrementCount(Dictionary<string, int> counts, string key)
    {
        if (counts.TryGetValue(key, out int count))
        {
            counts[key] = count + 1;
        }
        else
        {
            counts.Add(key, 1);
        }
    }

    private static int CountRepeatedKeys(Dictionary<string, int> counts)
    {
        int result = 0;
        foreach (KeyValuePair<string, int> entry in counts)
        {
            if (entry.Value > 1)
            {
                result++;
            }
        }

        return result;
    }

    private static string FormatDate(bool hasDate, DateOnly date)
    {
        return hasDate ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string FormatDatePrecision(IsoDatePrecision precision)
    {
        if (precision == IsoDatePrecision.Year)
        {
            return "year";
        }

        if (precision == IsoDatePrecision.Month)
        {
            return "month";
        }

        if (precision == IsoDatePrecision.Day)
        {
            return "day";
        }

        return "unknown";
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static int ResolveCldrFraction(string path, string currencyCode)
    {
        CldrFractionData data = new CldrSupplementalReader().ReadFractionData(path);
        CldrFractionRule rule = data.ResolveRule(currencyCode);
        Console.WriteLine($"currency-code\t{rule.CurrencyCode}");
        Console.WriteLine($"digits\t{rule.Digits}\t{FormatProvenance(rule.DigitsExplicit)}");
        Console.WriteLine($"rounding\t{rule.Rounding}\t{FormatProvenance(rule.RoundingExplicit)}");
        Console.WriteLine($"cash-digits\t{rule.CashDigits}\t{FormatProvenance(rule.CashDigitsExplicit)}");
        Console.WriteLine($"cash-rounding\t{rule.CashRounding}\t{FormatProvenance(rule.CashRoundingExplicit)}");
        return 0;
    }

    private static string FormatProvenance(bool isExplicit)
    {
        return isExplicit ? "explicit" : "inherited";
    }

    private static int ReadCldrFractions(string path)
    {
        CldrFractionData data = new CldrSupplementalReader().ReadFractionData(path);
        int explicitDigitsCount = 0;
        int explicitRoundingCount = 0;
        int explicitCashDigitsCount = 0;
        int explicitCashRoundingCount = 0;

        foreach (CldrFractionRule rule in data.ExplicitRules)
        {
            if (rule.DigitsExplicit)
            {
                explicitDigitsCount++;
            }

            if (rule.RoundingExplicit)
            {
                explicitRoundingCount++;
            }

            if (rule.CashDigitsExplicit)
            {
                explicitCashDigitsCount++;
            }

            if (rule.CashRoundingExplicit)
            {
                explicitCashRoundingCount++;
            }
        }

        CldrFractionRule inheritedRule = data.ResolveRule("ZZZ");
        Console.WriteLine($"explicit-rules\t{data.ExplicitRules.Count}");
        Console.WriteLine($"explicit-digits\t{explicitDigitsCount}");
        Console.WriteLine($"explicit-rounding\t{explicitRoundingCount}");
        Console.WriteLine($"explicit-cash-digits\t{explicitCashDigitsCount}");
        Console.WriteLine($"explicit-cash-rounding\t{explicitCashRoundingCount}");
        Console.WriteLine($"default-digits\t{inheritedRule.Digits}");
        Console.WriteLine($"default-rounding\t{inheritedRule.Rounding}");
        Console.WriteLine($"default-cash-digits\t{inheritedRule.CashDigits}");
        Console.WriteLine($"default-cash-rounding\t{inheritedRule.CashRounding}");
        return 0;
    }

    private static int ReadListThree(string path)
    {
        IsoListThreeData data = new IsoListThreeReader().Read(path);
        int numericCodeCount = 0;
        int minorUnitCount = 0;
        int yearPrecisionCount = 0;
        int monthPrecisionCount = 0;
        int dayPrecisionCount = 0;
        int unknownPrecisionCount = 0;

        foreach (IsoListThreeRow row in data.Rows)
        {
            if (row.NumericCode != null)
            {
                numericCodeCount++;
            }

            if (row.HasMinorUnit)
            {
                minorUnitCount++;
            }

            if (row.WithdrawalDate.Precision == IsoDatePrecision.Year)
            {
                yearPrecisionCount++;
            }
            else if (row.WithdrawalDate.Precision == IsoDatePrecision.Month)
            {
                monthPrecisionCount++;
            }
            else if (row.WithdrawalDate.Precision == IsoDatePrecision.Day)
            {
                dayPrecisionCount++;
            }
            else
            {
                unknownPrecisionCount++;
            }
        }

        Console.WriteLine($"publication-date\t{data.PublicationDate}");
        Console.WriteLine($"rows\t{data.Rows.Count}");
        Console.WriteLine($"numeric-codes\t{numericCodeCount}");
        Console.WriteLine($"known-minor-units\t{minorUnitCount}");
        Console.WriteLine($"withdrawal-year\t{yearPrecisionCount}");
        Console.WriteLine($"withdrawal-month\t{monthPrecisionCount}");
        Console.WriteLine($"withdrawal-day\t{dayPrecisionCount}");
        Console.WriteLine($"withdrawal-unknown\t{unknownPrecisionCount}");
        return 0;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  CurrencyCodes.DataGenerator --help");
        Console.WriteLine("  CurrencyCodes.DataGenerator read-list-one <xml-path>");
        Console.WriteLine("  CurrencyCodes.DataGenerator read-list-three <xml-path>");
        Console.WriteLine("  CurrencyCodes.DataGenerator read-cldr-fractions <xml-path>");
        Console.WriteLine("  CurrencyCodes.DataGenerator resolve-cldr-fraction <xml-path> <currency-code>");
        Console.WriteLine("  CurrencyCodes.DataGenerator read-cldr-usage <xml-path>");
        Console.WriteLine("  CurrencyCodes.DataGenerator inspect-cldr-usage <xml-path> <territory-code> <currency-code>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator read-cldr-territory-codes <supplemental-data-path> <supplemental-metadata-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator inspect-cldr-territory-code <supplemental-data-path> <supplemental-metadata-path> <code>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator reconcile-territories <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <overlay-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator inspect-territory-reconciliation <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <overlay-path> <source-list> <source-entity-name>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator reconcile-cross-source <list-one-path> <list-three-path> <supplemental-data-path> <conflict-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator inspect-cross-source <list-one-path> <list-three-path> <supplemental-data-path> <conflict-path> <source-list> <alphabetic-code>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator validate-data <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator aggregate-list-one <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator inspect-active-currency <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path> <alphabetic-code>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator aggregate-list-three <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator inspect-historical-currency <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path> <alphabetic-code>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator semantic-diff <old-list-one-path> <old-list-three-path> <old-supplemental-data-path> <old-supplemental-metadata-path> <old-un-m49-path> <old-territory-overlay-path> <old-conflict-path> <new-list-one-path> <new-list-three-path> <new-supplemental-data-path> <new-supplemental-metadata-path> <new-un-m49-path> <new-territory-overlay-path> <new-conflict-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator emit-canonical-tables <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path> <output-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator emit-version-data <source-checksum-manifest-path> <repository-root> <output-path>");
        Console.WriteLine(
            "  CurrencyCodes.DataGenerator emit-runtime-data <list-one-path> <list-three-path> <supplemental-data-path> <supplemental-metadata-path> <un-m49-path> <territory-overlay-path> <conflict-path> <currency-kind-overlay-path> <source-checksum-manifest-path> <repository-root> <output-directory>");
    }
}
