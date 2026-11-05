using System;

namespace LanguageCodes.DataGenerator.Model;

internal sealed class IanaRegistryRecord
{
    internal IanaRegistryRecord(
        IanaRegistryRecordType recordType,
        string? subtag,
        string? tag,
        string[] descriptions,
        DateOnly addedDate,
        DateOnly? deprecatedDate,
        string? preferredValue,
        string[] prefixes,
        string? suppressScript,
        string? macrolanguage,
        string? scope,
        string[] comments)
    {
        RecordType = recordType;
        Subtag = subtag;
        Tag = tag;
        Descriptions = descriptions;
        AddedDate = addedDate;
        DeprecatedDate = deprecatedDate;
        PreferredValue = preferredValue;
        Prefixes = prefixes;
        SuppressScript = suppressScript;
        Macrolanguage = macrolanguage;
        Scope = scope;
        Comments = comments;
    }

    internal IanaRegistryRecordType RecordType { get; }

    internal string? Subtag { get; }

    internal string? Tag { get; }

    internal string[] Descriptions { get; }

    internal DateOnly AddedDate { get; }

    internal DateOnly? DeprecatedDate { get; }

    internal string? PreferredValue { get; }

    internal string[] Prefixes { get; }

    internal string? SuppressScript { get; }

    internal string? Macrolanguage { get; }

    internal string? Scope { get; }

    internal string[] Comments { get; }
}
