namespace languagecodes;

/// <summary>Contains one language-related record from the IANA Language Subtag Registry.</summary>
public class LanguageSubtagMetadata
{
    /// <summary>The subtag or complete-tag record type.</summary>
    public LanguageSubtagType Type;

    /// <summary>The registered subtag or complete tag.</summary>
    public string? Value;

    /// <summary>All registry descriptions in source order.</summary>
    public string[] Descriptions = new string[0];

    /// <summary>The registry date on which the record was added, in ISO <c>yyyy-MM-dd</c> form.</summary>
    public string? AddedDate;

    /// <summary>The registry deprecation date in ISO <c>yyyy-MM-dd</c> form, when deprecated.</summary>
    public string? DeprecatedDate;

    /// <summary>The authoritative preferred replacement value when one exists.</summary>
    public string? PreferredValue;

    /// <summary>All registry prefix constraints in source order.</summary>
    public string[] Prefixes = new string[0];

    /// <summary>The suppress-script value for a language subtag when registered.</summary>
    public string? SuppressScript;

    /// <summary>The associated macrolanguage subtag when registered.</summary>
    public string? Macrolanguage;

    /// <summary>The registry scope marker when present.</summary>
    public string? Scope;

    /// <summary>All registry comments in source order.</summary>
    public string[] Comments = new string[0];
}
