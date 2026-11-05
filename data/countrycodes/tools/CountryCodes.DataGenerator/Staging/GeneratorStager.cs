using System;
using System.Collections.Generic;

namespace CountryCodes.DataGenerator.Staging;

internal static class GeneratorStager
{
    private const string DebianSource = "debian-iso-codes";
    private const string UnM49Source = "un-m49";
    private const string CldrSource = "cldr-48-2";
    private const string IanaSource = "iana-language-subtag-registry";

    internal static GeneratorStagingData Stage(RawSourceData raw, SourceManifestInfo manifest)
    {
        GeneratorStagingData staged = new GeneratorStagingData(
            StageCountries(raw.Debian.Countries, manifest),
            StageSubdivisions(raw.Debian.Subdivisions, manifest),
            StageHistoricalCountries(raw.Debian.HistoricalCountries, manifest),
            StageNames(raw, manifest),
            StageAliases(raw.Cldr.Aliases, manifest),
            StageContainment(raw.Cldr.Containment, manifest),
            StageM49History(raw.UnM49.History, manifest),
            StageM49Hierarchy(raw.UnM49.Hierarchy, manifest),
            StageIana(raw.Iana, manifest));

        StagingIntegrityValidator.Validate(raw, staged, manifest);
        return staged;
    }

    private static IReadOnlyList<StagedCountry> StageCountries(
        IReadOnlyList<DebianCountryRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedCountry> staged = new List<StagedCountry>(records.Count);
        foreach (DebianCountryRecord record in records)
        {
            StagingOrigin origin = CreateOrigin(
                manifest,
                DebianSource,
                record.Alpha2,
                null,
                null);
            staged.Add(new StagedCountry(
                origin,
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha2), "alpha_2"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha3), "alpha_3"),
                Field(origin, record.Numeric, "numeric"),
                Field(origin, record.Name, "name"),
                OptionalField(origin, record.OfficialName, "official_name"),
                OptionalField(origin, record.CommonName, "common_name")));
        }

        return staged;
    }

    private static IReadOnlyList<StagedSubdivision> StageSubdivisions(
        IReadOnlyList<DebianSubdivisionRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedSubdivision> staged = new List<StagedSubdivision>(records.Count);
        foreach (DebianSubdivisionRecord record in records)
        {
            string code = IdentifierStagingNormalizer.UpperAscii(record.Code);
            int separator = code.IndexOf('-');
            string countryCode = separator < 0 ? string.Empty : code.Substring(0, separator);
            string suffix = separator < 0 ? code : code.Substring(separator + 1);
            StagingOrigin origin = CreateOrigin(
                manifest,
                DebianSource,
                record.Code,
                null,
                null);
            staged.Add(new StagedSubdivision(
                origin,
                Field(origin, code, "code"),
                Field(origin, countryCode, "code.country"),
                Field(origin, suffix, "code.suffix"),
                Field(origin, record.Name, "name"),
                Field(origin, record.Type, "type"),
                OptionalNormalizedField(origin, record.Parent, "parent", false)));
        }

        return staged;
    }

    private static IReadOnlyList<StagedHistoricalCountry> StageHistoricalCountries(
        IReadOnlyList<DebianHistoricalRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedHistoricalCountry> staged = new List<StagedHistoricalCountry>(records.Count);
        foreach (DebianHistoricalRecord record in records)
        {
            StagingOrigin origin = CreateOrigin(
                manifest,
                DebianSource,
                record.Alpha4,
                null,
                record.WithdrawalDate);
            staged.Add(new StagedHistoricalCountry(
                origin,
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha2), "alpha_2"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha3), "alpha_3"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha4), "alpha_4"),
                Field(origin, record.Name, "name"),
                OptionalField(origin, record.Numeric, "numeric"),
                OptionalField(origin, record.WithdrawalDate, "withdrawal_date"),
                OptionalField(origin, record.Comment, "comment")));
        }

        return staged;
    }

    private static IReadOnlyList<StagedName> StageNames(
        RawSourceData raw,
        SourceManifestInfo manifest)
    {
        List<StagedName> staged = new List<StagedName>(
            raw.UnM49.Countries.Count + raw.Cldr.Names.Count);
        foreach (UnM49CountryRecord record in raw.UnM49.Countries)
        {
            string locale = IdentifierStagingNormalizer.LowerAscii(record.Locale);
            StagingOrigin origin = CreateOrigin(
                manifest,
                UnM49Source,
                record.M49Code,
                locale,
                null);
            staged.Add(new StagedName(
                origin,
                StagedNameTarget.UnM49Country,
                Field(origin, record.M49Code, "m49_code"),
                Field(origin, record.Name, "name"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha3), "iso_alpha3"),
                null,
                null));
        }

        string cldrLocale = IdentifierStagingNormalizer.LowerAscii(raw.Cldr.Locale);
        foreach (CldrNameRecord record in raw.Cldr.Names)
        {
            bool isSubdivision = record.Target == CldrNameTarget.Subdivision;
            string entityKey = isSubdivision
                ? IdentifierStagingNormalizer.NormalizeSubdivision(record.Code)
                : IdentifierStagingNormalizer.UpperAscii(record.Code);
            StagingOrigin origin = CreateOrigin(
                manifest,
                CldrSource,
                record.Code,
                cldrLocale,
                null);
            staged.Add(new StagedName(
                origin,
                isSubdivision
                    ? StagedNameTarget.CldrSubdivision
                    : StagedNameTarget.CldrTerritory,
                Field(origin, entityKey, "type"),
                Field(origin, record.Value, "value"),
                null,
                OptionalField(origin, record.Alternate, "alt"),
                OptionalField(origin, record.Draft, "draft")));
        }

        return staged;
    }

    private static IReadOnlyList<StagedAlias> StageAliases(
        IReadOnlyList<CldrAliasRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedAlias> staged = new List<StagedAlias>(records.Count);
        foreach (CldrAliasRecord record in records)
        {
            bool isSubdivision = record.Target == CldrAliasTarget.Subdivision;
            StagingOrigin origin = CreateOrigin(
                manifest,
                CldrSource,
                record.Code,
                null,
                null);
            List<StagedText> replacements = new List<StagedText>(record.Replacements.Count);
            foreach (string replacement in record.Replacements)
            {
                string normalizedReplacement = isSubdivision
                    ? IdentifierStagingNormalizer.NormalizeSubdivision(replacement)
                    : IdentifierStagingNormalizer.UpperAscii(replacement);
                replacements.Add(Field(origin, normalizedReplacement, "replacement"));
            }

            string normalizedCode = isSubdivision
                ? IdentifierStagingNormalizer.NormalizeSubdivision(record.Code)
                : IdentifierStagingNormalizer.UpperAscii(record.Code);
            staged.Add(new StagedAlias(
                origin,
                isSubdivision ? StagedAliasTarget.Subdivision : StagedAliasTarget.Territory,
                Field(origin, normalizedCode, "type"),
                replacements,
                Field(origin, record.Reason, "reason")));
        }

        return staged;
    }

    private static IReadOnlyList<StagedContainment> StageContainment(
        IReadOnlyList<CldrContainmentRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedContainment> staged = new List<StagedContainment>(records.Count);
        foreach (CldrContainmentRecord record in records)
        {
            StagingOrigin origin = CreateOrigin(
                manifest,
                CldrSource,
                record.Parent,
                null,
                null);
            List<StagedText> children = new List<StagedText>(record.Children.Count);
            foreach (string child in record.Children)
            {
                children.Add(Field(
                    origin,
                    IdentifierStagingNormalizer.NormalizeSubdivision(child),
                    "contains"));
            }

            staged.Add(new StagedContainment(
                origin,
                Field(
                    origin,
                    IdentifierStagingNormalizer.NormalizeSubdivision(record.Parent),
                    "type"),
                children));
        }

        return staged;
    }

    private static IReadOnlyList<StagedM49History> StageM49History(
        IReadOnlyList<UnM49HistoryRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedM49History> staged = new List<StagedM49History>(records.Count);
        int ordinal = 0;
        foreach (UnM49HistoryRecord record in records)
        {
            string upstreamKey = $"{record.Kind}:{record.M49Code}:{ordinal}";
            StagingOrigin origin = CreateOrigin(
                manifest,
                UnM49Source,
                upstreamKey,
                "en",
                record.ChangeDate);
            staged.Add(new StagedM49History(
                origin,
                record.Kind,
                Field(origin, record.M49Code, "numerical_code"),
                Field(origin, record.Name, "country_or_area"),
                OptionalField(origin, record.ChangeDate, "date_of_change"),
                Field(origin, record.Note, "note")));
            ordinal += 1;
        }

        return staged;
    }

    private static IReadOnlyList<StagedM49Hierarchy> StageM49Hierarchy(
        IReadOnlyList<UnM49HierarchyRecord> records,
        SourceManifestInfo manifest)
    {
        List<StagedM49Hierarchy> staged = new List<StagedM49Hierarchy>(records.Count);
        foreach (UnM49HierarchyRecord record in records)
        {
            string locale = IdentifierStagingNormalizer.LowerAscii(record.Locale);
            StagingOrigin origin = CreateOrigin(
                manifest,
                UnM49Source,
                record.M49Code,
                locale,
                null);
            staged.Add(new StagedM49Hierarchy(
                origin,
                Field(origin, record.GlobalCode, "global_code"),
                Field(origin, record.GlobalName, "global_name"),
                OptionalField(origin, record.RegionCode, "region_code"),
                OptionalField(origin, record.RegionName, "region_name"),
                OptionalField(origin, record.SubregionCode, "subregion_code"),
                OptionalField(origin, record.SubregionName, "subregion_name"),
                OptionalField(origin, record.IntermediateRegionCode, "intermediate_region_code"),
                OptionalField(origin, record.IntermediateRegionName, "intermediate_region_name"),
                Field(origin, record.CountryName, "country_or_area"),
                Field(origin, record.M49Code, "m49_code"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha2), "iso_alpha2"),
                Field(origin, IdentifierStagingNormalizer.UpperAscii(record.Alpha3), "iso_alpha3")));
        }

        return staged;
    }

    private static IReadOnlyList<StagedIanaRecord> StageIana(
        IanaRegistryData registry,
        SourceManifestInfo manifest)
    {
        List<StagedIanaRecord> staged = new List<StagedIanaRecord>(registry.Records.Count);
        foreach (IanaRegistryRecord record in registry.Records)
        {
            string upstreamKey = CreateIanaUpstreamKey(record);
            StagingOrigin origin = CreateOrigin(
                manifest,
                IanaSource,
                upstreamKey,
                null,
                registry.FileDate);
            List<StagedNamedField> fields = new List<StagedNamedField>(record.Fields.Count);
            foreach (IanaRegistryField field in record.Fields)
            {
                fields.Add(new StagedNamedField(
                    field.Name,
                    Field(origin, field.Value, field.Name)));
            }

            staged.Add(new StagedIanaRecord(origin, fields));
        }

        return staged;
    }

    private static string CreateIanaUpstreamKey(IanaRegistryRecord record)
    {
        string? type = FindIanaField(record, "Type");
        string? identifier = FindIanaField(record, "Subtag") ?? FindIanaField(record, "Tag");
        if (type == null || identifier == null)
        {
            return $"record:{record.Ordinal}";
        }

        return type + ":" + identifier;
    }

    private static string? FindIanaField(IanaRegistryRecord record, string fieldName)
    {
        foreach (IanaRegistryField field in record.Fields)
        {
            if (string.Equals(field.Name, fieldName, StringComparison.Ordinal))
            {
                return field.Value;
            }
        }

        return null;
    }

    private static StagingOrigin CreateOrigin(
        SourceManifestInfo manifest,
        string sourceId,
        string upstreamKey,
        string? locale,
        string? effectiveDate)
    {
        SourceSnapshotInfo source = manifest.GetSource(sourceId);
        string resolvedEffectiveDate = string.IsNullOrEmpty(effectiveDate)
            ? manifest.EffectiveDate
            : effectiveDate;
        return new StagingOrigin(
            source.Id,
            source.Revision,
            upstreamKey,
            locale,
            resolvedEffectiveDate);
    }

    private static StagedText Field(StagingOrigin origin, string value, string sourceField)
    {
        return new StagedText(value, new FieldProvenance(origin, sourceField));
    }

    private static StagedText? OptionalField(
        StagingOrigin origin,
        string? value,
        string sourceField)
    {
        return value == null ? null : Field(origin, value, sourceField);
    }

    private static StagedText? OptionalNormalizedField(
        StagingOrigin origin,
        string? value,
        string sourceField,
        bool isCldrSubdivision)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = isCldrSubdivision
            ? IdentifierStagingNormalizer.NormalizeSubdivision(value)
            : IdentifierStagingNormalizer.UpperAscii(value);
        return Field(origin, normalized, sourceField);
    }
}
