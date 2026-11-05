namespace languagecodes;

internal enum ReconciledLanguageScope
{
    Unknown = 0,
    Individual = 1,
    Macrolanguage = 2,
    Collection = 3,
    Special = 4
}

internal enum ReconciledLanguageType
{
    Unknown = 0,
    Living = 1,
    Extinct = 2,
    Ancient = 3,
    Historical = 4,
    Constructed = 5,
    Special = 6
}

internal enum ReconciledEntityStatus
{
    Active = 0,
    Retired = 1
}

internal enum ReconciledGroupKind
{
    None = 0,
    Family = 1,
    LanguageGroup = 2,
    RemainderGroup = 3
}

internal enum ReconciledNameKind
{
    Reference = 0,
    Print = 1,
    Inverted = 2,
    English = 3,
    French = 4,
    IanaDescription = 5
}

internal enum ReconciledAliasKind
{
    Alpha2 = 0,
    Set2Terminology = 1,
    Set2Bibliographic = 2,
    Set3 = 3,
    Set5 = 4,
    RetiredSet3 = 5,
    IanaDeprecated = 6,
    IanaPreferred = 7,
    Grandfathered = 8,
    Overlong = 9
}

internal enum ReconciledSource
{
    Unknown = 0,
    Iso639Set1 = 1,
    Iso639Set2 = 2,
    Iso639Set3 = 3,
    Iso639Set5 = 4,
    Iana = 5
}

internal enum ReconciledRetirementReason
{
    Change = 0,
    Duplicate = 1,
    Nonexistent = 2,
    Split = 3,
    Merge = 4
}

internal enum ReconciledRelationshipKind
{
    MacrolanguageMember = 0,
    Set5ParentChild = 1
}

internal enum ReconciledRelationshipStatus
{
    Active = 0,
    Retired = 1
}

internal enum ReconciledIanaRecordType
{
    Language = 0,
    Extlang = 1,
    Grandfathered = 2,
    Redundant = 3
}

internal class ReconciledLanguageModel
{
    internal ReconciledLanguageEntity[]? Entities = new ReconciledLanguageEntity[0];
    internal ReconciledReservedRange[]? ReservedRanges = new ReconciledReservedRange[0];
    internal ReconciledRelationship[]? Relationships = new ReconciledRelationship[0];
    internal ReconciledIanaLink[]? IanaLinks = new ReconciledIanaLink[0];
}

internal class ReconciledLanguageEntity
{
    internal string? Key;
    internal string? Alpha2Code;
    internal string? Set2TerminologyCode;
    internal string? Set2BibliographicCode;
    internal string? Set3Code;
    internal string? Set5Code;
    internal bool InSet1;
    internal bool InSet2;
    internal bool InSet3;
    internal bool InSet5;
    internal ReconciledLanguageScope Scope;
    internal ReconciledLanguageType LanguageType;
    internal ReconciledEntityStatus Status;
    internal ReconciledGroupKind GroupKind;
    internal ReconciledLanguageName[]? Names = new ReconciledLanguageName[0];
    internal ReconciledLanguageAlias[]? Aliases = new ReconciledLanguageAlias[0];
    internal ReconciledLanguageRetirement? Retirement;
}

internal class ReconciledLanguageName
{
    internal string? Text;
    internal string? NameLanguage;
    internal ReconciledNameKind Kind;
    internal ReconciledSource Source;
    internal bool IsReferenceName;
}

internal class ReconciledLanguageAlias
{
    internal string? Value;
    internal ReconciledAliasKind Kind;
    internal string? PreferredValue;
    internal ReconciledSource Source;
    internal bool IsDeprecated;
}

internal class ReconciledLanguageRetirement
{
    internal string? RetiredCode;
    internal string? FormerReferenceName;
    internal ReconciledRetirementReason Reason;
    internal string[]? ReplacementCodes = new string[0];
    internal string? Remedy;
    internal global::System.DateOnly EffectiveDate;
}

internal class ReconciledReservedRange
{
    internal string? FirstCode;
    internal string? LastCode;
    internal ReconciledSource Source;
    internal string? Purpose;
}

internal class ReconciledRelationship
{
    internal string? ParentKey;
    internal string? ChildKey;
    internal ReconciledRelationshipKind Kind;
    internal ReconciledRelationshipStatus Status;
    internal ReconciledSource Source;
}

internal class ReconciledIanaLink
{
    internal string? EntityKey;
    internal ReconciledIanaRecordType RecordType;
    internal string? Value;
    internal string[]? Descriptions = new string[0];
    internal string[]? Prefixes = new string[0];
    internal string? PreferredValue;
    internal string? Macrolanguage;
    internal string? SuppressScript;
    internal string? Scope;
    internal string? AddedDate;
    internal string? DeprecatedDate;
}
