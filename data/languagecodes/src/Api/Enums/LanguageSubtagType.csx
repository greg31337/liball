namespace languagecodes;

/// <summary>Identifies a component or complete-tag record in the IANA Language Subtag Registry.</summary>
public enum LanguageSubtagType
{
    /// <summary>A primary language subtag.</summary>
    Language = 0,
    /// <summary>An extended language subtag.</summary>
    Extlang = 1,
    /// <summary>A script subtag.</summary>
    Script = 2,
    /// <summary>A region subtag.</summary>
    Region = 3,
    /// <summary>A variant subtag.</summary>
    Variant = 4,
    /// <summary>An extension singleton and its following subtags.</summary>
    Extension = 5,
    /// <summary>A private-use singleton or subtag.</summary>
    PrivateUse = 6,
    /// <summary>A grandfathered complete language tag.</summary>
    Grandfathered = 7,
    /// <summary>A redundant complete language tag.</summary>
    Redundant = 8
}
