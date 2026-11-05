using System;
using System.Collections.Generic;

namespace CountryCodes.DataGenerator;

internal sealed class RawSourceData
{
    internal RawSourceData(
        DebianIsoData debian,
        UnM49Data unM49,
        CldrData cldr,
        IanaRegistryData iana)
    {
        Debian = debian;
        UnM49 = unM49;
        Cldr = cldr;
        Iana = iana;
    }

    internal DebianIsoData Debian { get; }

    internal UnM49Data UnM49 { get; }

    internal CldrData Cldr { get; }

    internal IanaRegistryData Iana { get; }
}

internal sealed class DebianIsoData
{
    internal DebianIsoData(
        IReadOnlyList<DebianCountryRecord> countries,
        IReadOnlyList<DebianSubdivisionRecord> subdivisions,
        IReadOnlyList<DebianHistoricalRecord> historicalCountries)
    {
        Countries = countries;
        Subdivisions = subdivisions;
        HistoricalCountries = historicalCountries;
    }

    internal IReadOnlyList<DebianCountryRecord> Countries { get; }

    internal IReadOnlyList<DebianSubdivisionRecord> Subdivisions { get; }

    internal IReadOnlyList<DebianHistoricalRecord> HistoricalCountries { get; }
}

internal sealed class DebianCountryRecord
{
    internal DebianCountryRecord(
        string alpha2,
        string alpha3,
        string numeric,
        string name,
        string? officialName,
        string? commonName)
    {
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Numeric = numeric;
        Name = name;
        OfficialName = officialName;
        CommonName = commonName;
    }

    internal string Alpha2 { get; }

    internal string Alpha3 { get; }

    internal string Numeric { get; }

    internal string Name { get; }

    internal string? OfficialName { get; }

    internal string? CommonName { get; }
}

internal sealed class DebianSubdivisionRecord
{
    internal DebianSubdivisionRecord(string code, string name, string type, string? parent)
    {
        Code = code;
        Name = name;
        Type = type;
        Parent = parent;
    }

    internal string Code { get; }

    internal string Name { get; }

    internal string Type { get; }

    internal string? Parent { get; }
}

internal sealed class DebianHistoricalRecord
{
    internal DebianHistoricalRecord(
        string alpha2,
        string alpha3,
        string alpha4,
        string name,
        string? numeric,
        string? withdrawalDate,
        string? comment)
    {
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Alpha4 = alpha4;
        Name = name;
        Numeric = numeric;
        WithdrawalDate = withdrawalDate;
        Comment = comment;
    }

    internal string Alpha2 { get; }

    internal string Alpha3 { get; }

    internal string Alpha4 { get; }

    internal string Name { get; }

    internal string? Numeric { get; }

    internal string? WithdrawalDate { get; }

    internal string? Comment { get; }
}

internal sealed class UnM49Data
{
    internal UnM49Data(
        IReadOnlyList<UnM49CountryRecord> countries,
        IReadOnlyList<UnM49HistoryRecord> history,
        IReadOnlyList<UnM49HierarchyRecord> hierarchy)
    {
        Countries = countries;
        History = history;
        Hierarchy = hierarchy;
    }

    internal IReadOnlyList<UnM49CountryRecord> Countries { get; }

    internal IReadOnlyList<UnM49HistoryRecord> History { get; }

    internal IReadOnlyList<UnM49HierarchyRecord> Hierarchy { get; }
}

internal sealed class UnM49CountryRecord
{
    internal UnM49CountryRecord(string locale, string name, string m49Code, string alpha3)
    {
        Locale = locale;
        Name = name;
        M49Code = m49Code;
        Alpha3 = alpha3;
    }

    internal string Locale { get; }

    internal string Name { get; }

    internal string M49Code { get; }

    internal string Alpha3 { get; }
}

internal enum UnM49HistoryKind
{
    AddedOrChanged = 0,
    NameChanged = 1,
    NotInCurrentUse = 2,
    Reintroduced = 3,
    Removed = 4
}

internal sealed class UnM49HistoryRecord
{
    internal UnM49HistoryRecord(
        UnM49HistoryKind kind,
        string m49Code,
        string name,
        string? changeDate,
        string note)
    {
        Kind = kind;
        M49Code = m49Code;
        Name = name;
        ChangeDate = changeDate;
        Note = note;
    }

    internal UnM49HistoryKind Kind { get; }

    internal string M49Code { get; }

    internal string Name { get; }

    internal string? ChangeDate { get; }

    internal string Note { get; }
}

internal sealed class UnM49HierarchyRecord
{
    internal UnM49HierarchyRecord(
        string locale,
        string globalCode,
        string globalName,
        string? regionCode,
        string? regionName,
        string? subregionCode,
        string? subregionName,
        string? intermediateRegionCode,
        string? intermediateRegionName,
        string countryName,
        string m49Code,
        string alpha2,
        string alpha3)
    {
        Locale = locale;
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

    internal string Locale { get; }

    internal string GlobalCode { get; }

    internal string GlobalName { get; }

    internal string? RegionCode { get; }

    internal string? RegionName { get; }

    internal string? SubregionCode { get; }

    internal string? SubregionName { get; }

    internal string? IntermediateRegionCode { get; }

    internal string? IntermediateRegionName { get; }

    internal string CountryName { get; }

    internal string M49Code { get; }

    internal string Alpha2 { get; }

    internal string Alpha3 { get; }
}

internal sealed class CldrData
{
    internal CldrData(
        string locale,
        IReadOnlyList<CldrNameRecord> names,
        IReadOnlyList<CldrAliasRecord> aliases,
        IReadOnlyList<CldrContainmentRecord> containment)
    {
        Locale = locale;
        Names = names;
        Aliases = aliases;
        Containment = containment;
    }

    internal string Locale { get; }

    internal IReadOnlyList<CldrNameRecord> Names { get; }

    internal IReadOnlyList<CldrAliasRecord> Aliases { get; }

    internal IReadOnlyList<CldrContainmentRecord> Containment { get; }
}

internal enum CldrNameTarget
{
    Territory = 0,
    Subdivision = 1
}

internal sealed class CldrNameRecord
{
    internal CldrNameRecord(
        CldrNameTarget target,
        string code,
        string value,
        string? alternate,
        string? draft)
    {
        Target = target;
        Code = code;
        Value = value;
        Alternate = alternate;
        Draft = draft;
    }

    internal CldrNameTarget Target { get; }

    internal string Code { get; }

    internal string Value { get; }

    internal string? Alternate { get; }

    internal string? Draft { get; }
}

internal enum CldrAliasTarget
{
    Territory = 0,
    Subdivision = 1
}

internal sealed class CldrAliasRecord
{
    internal CldrAliasRecord(
        CldrAliasTarget target,
        string code,
        IReadOnlyList<string> replacements,
        string reason)
    {
        Target = target;
        Code = code;
        Replacements = replacements;
        Reason = reason;
    }

    internal CldrAliasTarget Target { get; }

    internal string Code { get; }

    internal IReadOnlyList<string> Replacements { get; }

    internal string Reason { get; }
}

internal sealed class CldrContainmentRecord
{
    internal CldrContainmentRecord(string parent, IReadOnlyList<string> children)
    {
        Parent = parent;
        Children = children;
    }

    internal string Parent { get; }

    internal IReadOnlyList<string> Children { get; }
}

internal sealed class IanaRegistryData
{
    internal IanaRegistryData(string fileDate, IReadOnlyList<IanaRegistryRecord> records)
    {
        FileDate = fileDate;
        Records = records;
    }

    internal string FileDate { get; }

    internal IReadOnlyList<IanaRegistryRecord> Records { get; }
}

internal sealed class IanaRegistryRecord
{
    internal IanaRegistryRecord(int ordinal, IReadOnlyList<IanaRegistryField> fields)
    {
        Ordinal = ordinal;
        Fields = fields;
    }

    internal int Ordinal { get; }

    internal IReadOnlyList<IanaRegistryField> Fields { get; }
}

internal sealed class IanaRegistryField
{
    internal IanaRegistryField(string name, string value)
    {
        Name = name;
        Value = value;
    }

    internal string Name { get; }

    internal string Value { get; }
}

internal sealed class SourceParseException : Exception
{
    internal SourceParseException(string sourceName, string message)
        : base($"{sourceName}: {message}")
    {
    }

    internal SourceParseException(string sourceName, string message, Exception innerException)
        : base($"{sourceName}: {message}", innerException)
    {
    }
}
