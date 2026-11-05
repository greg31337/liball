using System;
using System.Collections.Generic;

namespace CountryCodes.DataGenerator.Staging;

internal static class StagingIntegrityValidator
{
    internal static void Validate(
        RawSourceData raw,
        GeneratorStagingData staged,
        SourceManifestInfo manifest)
    {
        RequireCount(raw.Debian.Countries.Count, staged.Countries.Count, "countries");
        RequireCount(raw.Debian.Subdivisions.Count, staged.Subdivisions.Count, "subdivisions");
        RequireCount(
            raw.Debian.HistoricalCountries.Count,
            staged.HistoricalCountries.Count,
            "historical countries");
        RequireCount(
            raw.UnM49.Countries.Count + raw.Cldr.Names.Count,
            staged.Names.Count,
            "localized names");
        RequireCount(raw.Cldr.Aliases.Count, staged.Aliases.Count, "aliases");
        RequireCount(raw.Cldr.Containment.Count, staged.Containment.Count, "containment rows");
        RequireCount(raw.UnM49.History.Count, staged.M49History.Count, "M49 history rows");
        RequireCount(raw.UnM49.Hierarchy.Count, staged.M49Hierarchy.Count, "M49 hierarchy rows");
        RequireCount(raw.Iana.Records.Count, staged.IanaRecords.Count, "IANA records");

        ValidateCountries(raw.Debian.Countries, staged.Countries, manifest);
        ValidateSubdivisions(raw.Debian.Subdivisions, staged.Subdivisions, manifest);
        ValidateHistorical(raw.Debian.HistoricalCountries, staged.HistoricalCountries, manifest);
        ValidateNames(raw, staged.Names, manifest);
        ValidateAliases(raw.Cldr.Aliases, staged.Aliases, manifest);
        ValidateContainment(raw.Cldr.Containment, staged.Containment, manifest);
        ValidateM49History(raw.UnM49.History, staged.M49History, manifest);
        ValidateM49Hierarchy(raw.UnM49.Hierarchy, staged.M49Hierarchy, manifest);
        ValidateIana(raw.Iana, staged.IanaRecords, manifest);
    }

    private static void ValidateCountries(
        IReadOnlyList<DebianCountryRecord> raw,
        IReadOnlyList<StagedCountry> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            DebianCountryRecord source = raw[index];
            StagedCountry target = staged[index];
            ValidateOrigin(target.Origin, manifest, "debian-iso-codes", source.Alpha2, null, null);
            RequirePreserved(source.Numeric, target.Numeric.Value, "country numeric", index);
            RequirePreserved(source.Name, target.Name.Value, "country name", index);
            RequireOptionalPreserved(
                source.OfficialName,
                target.OfficialName,
                "country official name",
                index);
            RequireOptionalPreserved(source.CommonName, target.CommonName, "country common name", index);
            ValidateFields(
                target.Origin,
                target.Alpha2,
                target.Alpha3,
                target.Numeric,
                target.Name,
                target.OfficialName,
                target.CommonName);
        }
    }

    private static void ValidateSubdivisions(
        IReadOnlyList<DebianSubdivisionRecord> raw,
        IReadOnlyList<StagedSubdivision> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            DebianSubdivisionRecord source = raw[index];
            StagedSubdivision target = staged[index];
            ValidateOrigin(target.Origin, manifest, "debian-iso-codes", source.Code, null, null);
            RequirePreserved(source.Name, target.Name.Value, "subdivision name", index);
            RequirePreserved(source.Type, target.Type.Value, "subdivision type", index);
            ValidateFields(
                target.Origin,
                target.Code,
                target.CountryCode,
                target.Suffix,
                target.Name,
                target.Type,
                target.Parent);
        }
    }

    private static void ValidateHistorical(
        IReadOnlyList<DebianHistoricalRecord> raw,
        IReadOnlyList<StagedHistoricalCountry> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            DebianHistoricalRecord source = raw[index];
            StagedHistoricalCountry target = staged[index];
            ValidateOrigin(
                target.Origin,
                manifest,
                "debian-iso-codes",
                source.Alpha4,
                null,
                source.WithdrawalDate);
            RequirePreserved(source.Name, target.Name.Value, "historical name", index);
            RequireOptionalPreserved(source.Numeric, target.Numeric, "historical numeric", index);
            RequireOptionalPreserved(
                source.WithdrawalDate,
                target.WithdrawalDate,
                "historical withdrawal date",
                index);
            RequireOptionalPreserved(source.Comment, target.Comment, "historical comment", index);
            ValidateFields(
                target.Origin,
                target.Alpha2,
                target.Alpha3,
                target.Alpha4,
                target.Name,
                target.Numeric,
                target.WithdrawalDate,
                target.Comment);
        }
    }

    private static void ValidateNames(
        RawSourceData raw,
        IReadOnlyList<StagedName> staged,
        SourceManifestInfo manifest)
    {
        int stagedIndex = 0;
        foreach (UnM49CountryRecord source in raw.UnM49.Countries)
        {
            StagedName target = staged[stagedIndex];
            ValidateOrigin(
                target.Origin,
                manifest,
                "un-m49",
                source.M49Code,
                IdentifierStagingNormalizer.LowerAscii(source.Locale),
                null);
            RequirePreserved(source.M49Code, target.EntityKey.Value, "M49 country code", stagedIndex);
            RequirePreserved(source.Name, target.Value.Value, "M49 country name", stagedIndex);
            ValidateFields(
                target.Origin,
                target.EntityKey,
                target.Value,
                target.Alpha3,
                target.Alternate,
                target.Draft);
            stagedIndex += 1;
        }

        foreach (CldrNameRecord source in raw.Cldr.Names)
        {
            StagedName target = staged[stagedIndex];
            ValidateOrigin(
                target.Origin,
                manifest,
                "cldr-48-2",
                source.Code,
                IdentifierStagingNormalizer.LowerAscii(raw.Cldr.Locale),
                null);
            RequirePreserved(source.Value, target.Value.Value, "CLDR display name", stagedIndex);
            RequireOptionalPreserved(source.Alternate, target.Alternate, "CLDR alternate", stagedIndex);
            RequireOptionalPreserved(source.Draft, target.Draft, "CLDR draft", stagedIndex);
            ValidateFields(
                target.Origin,
                target.EntityKey,
                target.Value,
                target.Alpha3,
                target.Alternate,
                target.Draft);
            stagedIndex += 1;
        }
    }

    private static void ValidateAliases(
        IReadOnlyList<CldrAliasRecord> raw,
        IReadOnlyList<StagedAlias> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            CldrAliasRecord source = raw[index];
            StagedAlias target = staged[index];
            ValidateOrigin(target.Origin, manifest, "cldr-48-2", source.Code, null, null);
            RequireCount(source.Replacements.Count, target.Replacements.Count, "alias replacements");
            RequirePreserved(source.Reason, target.Reason.Value, "alias reason", index);
            ValidateFields(target.Origin, target.Code, target.Reason);
            foreach (StagedText replacement in target.Replacements)
            {
                ValidateField(target.Origin, replacement);
            }
        }
    }

    private static void ValidateContainment(
        IReadOnlyList<CldrContainmentRecord> raw,
        IReadOnlyList<StagedContainment> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            CldrContainmentRecord source = raw[index];
            StagedContainment target = staged[index];
            ValidateOrigin(target.Origin, manifest, "cldr-48-2", source.Parent, null, null);
            ValidateFields(target.Origin, target.Parent);
            foreach (StagedText child in target.Children)
            {
                ValidateField(target.Origin, child);
            }
        }
    }

    private static void ValidateM49History(
        IReadOnlyList<UnM49HistoryRecord> raw,
        IReadOnlyList<StagedM49History> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            UnM49HistoryRecord source = raw[index];
            StagedM49History target = staged[index];
            ValidateOrigin(
                target.Origin,
                manifest,
                "un-m49",
                $"{source.Kind}:{source.M49Code}:{index}",
                "en",
                source.ChangeDate);
            RequirePreserved(source.M49Code, target.M49Code.Value, "M49 history code", index);
            RequirePreserved(source.Name, target.Name.Value, "M49 history name", index);
            RequireOptionalPreserved(
                source.ChangeDate,
                target.ChangeDate,
                "M49 history change date",
                index);
            RequirePreserved(source.Note, target.Note.Value, "M49 history note", index);
            ValidateFields(
                target.Origin,
                target.M49Code,
                target.Name,
                target.ChangeDate,
                target.Note);
        }
    }

    private static void ValidateM49Hierarchy(
        IReadOnlyList<UnM49HierarchyRecord> raw,
        IReadOnlyList<StagedM49Hierarchy> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Count; index += 1)
        {
            UnM49HierarchyRecord source = raw[index];
            StagedM49Hierarchy target = staged[index];
            ValidateOrigin(
                target.Origin,
                manifest,
                "un-m49",
                source.M49Code,
                IdentifierStagingNormalizer.LowerAscii(source.Locale),
                null);
            RequirePreserved(source.GlobalCode, target.GlobalCode.Value, "M49 global code", index);
            RequirePreserved(source.GlobalName, target.GlobalName.Value, "M49 global name", index);
            RequireOptionalPreserved(source.RegionCode, target.RegionCode, "M49 region code", index);
            RequireOptionalPreserved(source.RegionName, target.RegionName, "M49 region name", index);
            RequireOptionalPreserved(
                source.SubregionCode,
                target.SubregionCode,
                "M49 subregion code",
                index);
            RequireOptionalPreserved(
                source.SubregionName,
                target.SubregionName,
                "M49 subregion name",
                index);
            RequireOptionalPreserved(
                source.IntermediateRegionCode,
                target.IntermediateRegionCode,
                "M49 intermediate code",
                index);
            RequireOptionalPreserved(
                source.IntermediateRegionName,
                target.IntermediateRegionName,
                "M49 intermediate name",
                index);
            RequirePreserved(source.CountryName, target.CountryName.Value, "M49 country name", index);
            RequirePreserved(source.M49Code, target.M49Code.Value, "M49 hierarchy code", index);
            ValidateFields(
                target.Origin,
                target.GlobalCode,
                target.GlobalName,
                target.RegionCode,
                target.RegionName,
                target.SubregionCode,
                target.SubregionName,
                target.IntermediateRegionCode,
                target.IntermediateRegionName,
                target.CountryName,
                target.M49Code,
                target.Alpha2,
                target.Alpha3);
        }
    }

    private static void ValidateIana(
        IanaRegistryData raw,
        IReadOnlyList<StagedIanaRecord> staged,
        SourceManifestInfo manifest)
    {
        for (int index = 0; index < raw.Records.Count; index += 1)
        {
            IanaRegistryRecord source = raw.Records[index];
            StagedIanaRecord target = staged[index];
            ValidateOrigin(
                target.Origin,
                manifest,
                "iana-language-subtag-registry",
                CreateIanaUpstreamKey(source),
                null,
                raw.FileDate);
            RequireCount(source.Fields.Count, target.Fields.Count, "IANA fields");
            ValidateOrigin(target.Origin);
            for (int fieldIndex = 0; fieldIndex < source.Fields.Count; fieldIndex += 1)
            {
                IanaRegistryField sourceField = source.Fields[fieldIndex];
                StagedNamedField targetField = target.Fields[fieldIndex];
                RequirePreserved(sourceField.Name, targetField.Name, "IANA field name", fieldIndex);
                RequirePreserved(
                    sourceField.Value,
                    targetField.Value.Value,
                    "IANA field value",
                    fieldIndex);
                ValidateField(target.Origin, targetField.Value);
            }
        }
    }

    private static void ValidateFields(StagingOrigin origin, params StagedText?[] fields)
    {
        ValidateOrigin(origin);
        foreach (StagedText? field in fields)
        {
            if (field != null)
            {
                ValidateField(origin, field);
            }
        }
    }

    private static void ValidateField(StagingOrigin origin, StagedText field)
    {
        if (!ReferenceEquals(origin, field.Provenance.Origin))
        {
            throw new StagingException("A staged field lost its row provenance.");
        }

        if (field.Provenance.SourceField.Length == 0)
        {
            throw new StagingException("A staged field has no source-field provenance.");
        }
    }

    private static void ValidateOrigin(StagingOrigin origin)
    {
        if (origin.SourceId.Length == 0 ||
            origin.SourceRevision.Length == 0 ||
            origin.UpstreamKey.Length == 0 ||
            origin.EffectiveDate.Length == 0)
        {
            throw new StagingException("A staged row has incomplete origin provenance.");
        }

        if (origin.Locale != null &&
            !string.Equals(
                origin.Locale,
                IdentifierStagingNormalizer.LowerAscii(origin.Locale),
                StringComparison.Ordinal))
        {
            throw new StagingException($"Staged locale '{origin.Locale}' is not normalized.");
        }
    }

    private static void ValidateOrigin(
        StagingOrigin origin,
        SourceManifestInfo manifest,
        string sourceId,
        string upstreamKey,
        string? locale,
        string? effectiveDate)
    {
        SourceSnapshotInfo source = manifest.GetSource(sourceId);
        string expectedEffectiveDate = string.IsNullOrEmpty(effectiveDate)
            ? manifest.EffectiveDate
            : effectiveDate;
        if (!string.Equals(origin.SourceId, source.Id, StringComparison.Ordinal) ||
            !string.Equals(origin.SourceRevision, source.Revision, StringComparison.Ordinal) ||
            !string.Equals(origin.UpstreamKey, upstreamKey, StringComparison.Ordinal) ||
            !string.Equals(origin.Locale, locale, StringComparison.Ordinal) ||
            !string.Equals(origin.EffectiveDate, expectedEffectiveDate, StringComparison.Ordinal))
        {
            throw new StagingException($"Staged provenance does not match upstream key '{upstreamKey}'.");
        }
    }

    private static string CreateIanaUpstreamKey(IanaRegistryRecord record)
    {
        string? type = FindIanaField(record, "Type");
        string? identifier = FindIanaField(record, "Subtag") ?? FindIanaField(record, "Tag");
        return type == null || identifier == null
            ? $"record:{record.Ordinal}"
            : type + ":" + identifier;
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

    private static void RequireCount(int expected, int actual, string context)
    {
        if (expected != actual)
        {
            throw new StagingException(
                $"Staging changed the {context} count: expected {expected}, found {actual}.");
        }
    }

    private static void RequirePreserved(
        string expected,
        string actual,
        string context,
        int index)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new StagingException($"Staging changed {context} at index {index}.");
        }
    }

    private static void RequireOptionalPreserved(
        string? expected,
        StagedText? actual,
        string context,
        int index)
    {
        if (expected == null && actual == null)
        {
            return;
        }

        if (expected == null ||
            actual == null ||
            !string.Equals(expected, actual.Value, StringComparison.Ordinal))
        {
            throw new StagingException($"Staging changed {context} at index {index}.");
        }
    }
}
