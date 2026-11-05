using System.Collections.Generic;

namespace CountryCodes.DataGenerator.Staging;

internal sealed class GeneratorStagingData
{
    internal GeneratorStagingData(
        IReadOnlyList<StagedCountry> countries,
        IReadOnlyList<StagedSubdivision> subdivisions,
        IReadOnlyList<StagedHistoricalCountry> historicalCountries,
        IReadOnlyList<StagedName> names,
        IReadOnlyList<StagedAlias> aliases,
        IReadOnlyList<StagedContainment> containment,
        IReadOnlyList<StagedM49History> m49History,
        IReadOnlyList<StagedM49Hierarchy> m49Hierarchy,
        IReadOnlyList<StagedIanaRecord> ianaRecords)
    {
        Countries = countries;
        Subdivisions = subdivisions;
        HistoricalCountries = historicalCountries;
        Names = names;
        Aliases = aliases;
        Containment = containment;
        M49History = m49History;
        M49Hierarchy = m49Hierarchy;
        IanaRecords = ianaRecords;
    }

    internal IReadOnlyList<StagedCountry> Countries { get; }

    internal IReadOnlyList<StagedSubdivision> Subdivisions { get; }

    internal IReadOnlyList<StagedHistoricalCountry> HistoricalCountries { get; }

    internal IReadOnlyList<StagedName> Names { get; }

    internal IReadOnlyList<StagedAlias> Aliases { get; }

    internal IReadOnlyList<StagedContainment> Containment { get; }

    internal IReadOnlyList<StagedM49History> M49History { get; }

    internal IReadOnlyList<StagedM49Hierarchy> M49Hierarchy { get; }

    internal IReadOnlyList<StagedIanaRecord> IanaRecords { get; }
}

internal sealed class StagingOrigin
{
    internal StagingOrigin(
        string sourceId,
        string sourceRevision,
        string upstreamKey,
        string? locale,
        string effectiveDate)
    {
        SourceId = sourceId;
        SourceRevision = sourceRevision;
        UpstreamKey = upstreamKey;
        Locale = locale;
        EffectiveDate = effectiveDate;
    }

    internal string SourceId { get; }

    internal string SourceRevision { get; }

    internal string UpstreamKey { get; }

    internal string? Locale { get; }

    internal string EffectiveDate { get; }
}

internal sealed class FieldProvenance
{
    internal FieldProvenance(StagingOrigin origin, string sourceField)
    {
        Origin = origin;
        SourceField = sourceField;
    }

    internal StagingOrigin Origin { get; }

    internal string SourceField { get; }
}

internal sealed class StagedText
{
    internal StagedText(string value, FieldProvenance provenance)
    {
        Value = value;
        Provenance = provenance;
    }

    internal string Value { get; }

    internal FieldProvenance Provenance { get; }
}

internal sealed class StagedCountry
{
    internal StagedCountry(
        StagingOrigin origin,
        StagedText alpha2,
        StagedText alpha3,
        StagedText numeric,
        StagedText name,
        StagedText? officialName,
        StagedText? commonName)
    {
        Origin = origin;
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Numeric = numeric;
        Name = name;
        OfficialName = officialName;
        CommonName = commonName;
    }

    internal StagingOrigin Origin { get; }

    internal StagedText Alpha2 { get; }

    internal StagedText Alpha3 { get; }

    internal StagedText Numeric { get; }

    internal StagedText Name { get; }

    internal StagedText? OfficialName { get; }

    internal StagedText? CommonName { get; }
}

internal sealed class StagedSubdivision
{
    internal StagedSubdivision(
        StagingOrigin origin,
        StagedText code,
        StagedText countryCode,
        StagedText suffix,
        StagedText name,
        StagedText type,
        StagedText? parent)
    {
        Origin = origin;
        Code = code;
        CountryCode = countryCode;
        Suffix = suffix;
        Name = name;
        Type = type;
        Parent = parent;
    }

    internal StagingOrigin Origin { get; }

    internal StagedText Code { get; }

    internal StagedText CountryCode { get; }

    internal StagedText Suffix { get; }

    internal StagedText Name { get; }

    internal StagedText Type { get; }

    internal StagedText? Parent { get; }
}

internal sealed class StagedHistoricalCountry
{
    internal StagedHistoricalCountry(
        StagingOrigin origin,
        StagedText alpha2,
        StagedText alpha3,
        StagedText alpha4,
        StagedText name,
        StagedText? numeric,
        StagedText? withdrawalDate,
        StagedText? comment)
    {
        Origin = origin;
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Alpha4 = alpha4;
        Name = name;
        Numeric = numeric;
        WithdrawalDate = withdrawalDate;
        Comment = comment;
    }

    internal StagingOrigin Origin { get; }

    internal StagedText Alpha2 { get; }

    internal StagedText Alpha3 { get; }

    internal StagedText Alpha4 { get; }

    internal StagedText Name { get; }

    internal StagedText? Numeric { get; }

    internal StagedText? WithdrawalDate { get; }

    internal StagedText? Comment { get; }
}

internal enum StagedNameTarget
{
    UnM49Country = 0,
    CldrTerritory = 1,
    CldrSubdivision = 2
}

internal sealed class StagedName
{
    internal StagedName(
        StagingOrigin origin,
        StagedNameTarget target,
        StagedText entityKey,
        StagedText value,
        StagedText? alpha3,
        StagedText? alternate,
        StagedText? draft)
    {
        Origin = origin;
        Target = target;
        EntityKey = entityKey;
        Value = value;
        Alpha3 = alpha3;
        Alternate = alternate;
        Draft = draft;
    }

    internal StagingOrigin Origin { get; }

    internal StagedNameTarget Target { get; }

    internal StagedText EntityKey { get; }

    internal StagedText Value { get; }

    internal StagedText? Alpha3 { get; }

    internal StagedText? Alternate { get; }

    internal StagedText? Draft { get; }
}

internal enum StagedAliasTarget
{
    Territory = 0,
    Subdivision = 1
}

internal sealed class StagedAlias
{
    internal StagedAlias(
        StagingOrigin origin,
        StagedAliasTarget target,
        StagedText code,
        IReadOnlyList<StagedText> replacements,
        StagedText reason)
    {
        Origin = origin;
        Target = target;
        Code = code;
        Replacements = replacements;
        Reason = reason;
    }

    internal StagingOrigin Origin { get; }

    internal StagedAliasTarget Target { get; }

    internal StagedText Code { get; }

    internal IReadOnlyList<StagedText> Replacements { get; }

    internal StagedText Reason { get; }
}

internal sealed class StagedContainment
{
    internal StagedContainment(
        StagingOrigin origin,
        StagedText parent,
        IReadOnlyList<StagedText> children)
    {
        Origin = origin;
        Parent = parent;
        Children = children;
    }

    internal StagingOrigin Origin { get; }

    internal StagedText Parent { get; }

    internal IReadOnlyList<StagedText> Children { get; }
}

internal sealed class StagedM49History
{
    internal StagedM49History(
        StagingOrigin origin,
        UnM49HistoryKind kind,
        StagedText m49Code,
        StagedText name,
        StagedText? changeDate,
        StagedText note)
    {
        Origin = origin;
        Kind = kind;
        M49Code = m49Code;
        Name = name;
        ChangeDate = changeDate;
        Note = note;
    }

    internal StagingOrigin Origin { get; }

    internal UnM49HistoryKind Kind { get; }

    internal StagedText M49Code { get; }

    internal StagedText Name { get; }

    internal StagedText? ChangeDate { get; }

    internal StagedText Note { get; }
}

internal sealed class StagedM49Hierarchy
{
    internal StagedM49Hierarchy(
        StagingOrigin origin,
        StagedText globalCode,
        StagedText globalName,
        StagedText? regionCode,
        StagedText? regionName,
        StagedText? subregionCode,
        StagedText? subregionName,
        StagedText? intermediateRegionCode,
        StagedText? intermediateRegionName,
        StagedText countryName,
        StagedText m49Code,
        StagedText alpha2,
        StagedText alpha3)
    {
        Origin = origin;
        GlobalCode = globalCode;
        GlobalName = globalName;
        RegionCode = regionCode;
        RegionName = regionName;
        SubregionCode = subregionCode;
        SubregionName = subregionName;
        IntermediateRegionCode = intermediateRegionCode;
        IntermediateRegionName = intermediateRegionName;
        CountryName = countryName;
        M49Code = m49Code;
        Alpha2 = alpha2;
        Alpha3 = alpha3;
    }

    internal StagingOrigin Origin { get; }

    internal StagedText GlobalCode { get; }

    internal StagedText GlobalName { get; }

    internal StagedText? RegionCode { get; }

    internal StagedText? RegionName { get; }

    internal StagedText? SubregionCode { get; }

    internal StagedText? SubregionName { get; }

    internal StagedText? IntermediateRegionCode { get; }

    internal StagedText? IntermediateRegionName { get; }

    internal StagedText CountryName { get; }

    internal StagedText M49Code { get; }

    internal StagedText Alpha2 { get; }

    internal StagedText Alpha3 { get; }
}

internal sealed class StagedIanaRecord
{
    internal StagedIanaRecord(
        StagingOrigin origin,
        IReadOnlyList<StagedNamedField> fields)
    {
        Origin = origin;
        Fields = fields;
    }

    internal StagingOrigin Origin { get; }

    internal IReadOnlyList<StagedNamedField> Fields { get; }
}

internal sealed class StagedNamedField
{
    internal StagedNamedField(string name, StagedText value)
    {
        Name = name;
        Value = value;
    }

    internal string Name { get; }

    internal StagedText Value { get; }
}
