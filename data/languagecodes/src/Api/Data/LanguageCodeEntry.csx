namespace languagecodes;

/// <summary>Contains one reconciled ISO 639 language, collection, group, or special entity.</summary>
public class LanguageCodeEntry
{
    /// <summary>The ISO 639 Set 1 two-letter code, when assigned.</summary>
    public string? Alpha2Code;

    /// <summary>The ISO 639 Set 2 terminology code, when assigned.</summary>
    public string? Set2TerminologyCode;

    /// <summary>The ISO 639 Set 2 bibliographic code, when assigned.</summary>
    public string? Set2BibliographicCode;

    /// <summary>The ISO 639 Set 3 code, when assigned.</summary>
    public string? Set3Code;

    /// <summary>The ISO 639 Set 5 code, when assigned.</summary>
    public string? Set5Code;

    /// <summary>Whether the entity belongs to ISO 639 Set 1.</summary>
    public bool InSet1;

    /// <summary>Whether the entity belongs to ISO 639 Set 2.</summary>
    public bool InSet2;

    /// <summary>Whether the entity belongs to ISO 639 Set 3.</summary>
    public bool InSet3;

    /// <summary>Whether the entity belongs to ISO 639 Set 5.</summary>
    public bool InSet5;

    /// <summary>The scope assigned to the entity.</summary>
    public LanguageScope Scope;

    /// <summary>The language type assigned to the entity.</summary>
    public LanguageType LanguageType;

    /// <summary>The assignment and lifecycle status of the entity.</summary>
    public LanguageCodeStatus Status;

    /// <summary>The reconciled authoritative reference name.</summary>
    public string? ReferenceName;

    /// <summary>All authoritative and registry-supplied names for the entity.</summary>
    public LanguageName[] Names = new LanguageName[0];

    /// <summary>All alternate codes and registered tags resolving to the entity.</summary>
    public LanguageAlias[] Aliases = new LanguageAlias[0];

    /// <summary>All direct macrolanguage and ISO 639 Set 5 relationships involving the entity.</summary>
    public LanguageRelationship[] Relationships = new LanguageRelationship[0];

    /// <summary>The retirement record when this entry represents a retired identifier.</summary>
    public LanguageRetirement? Retirement;

    /// <summary>The authoritative source comment associated with the entity.</summary>
    public string? Comment;
}
