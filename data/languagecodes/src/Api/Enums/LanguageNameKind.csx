namespace languagecodes;

/// <summary>Identifies the role and source-language form of a language name.</summary>
public enum LanguageNameKind
{
    /// <summary>The authoritative reference name.</summary>
    Reference = 0,
    /// <summary>An ISO 639 Set 3 print name.</summary>
    Print = 1,
    /// <summary>An ISO 639 Set 3 inverted name.</summary>
    Inverted = 2,
    /// <summary>An official English name.</summary>
    English = 3,
    /// <summary>An official French name.</summary>
    French = 4,
    /// <summary>An IANA registry description.</summary>
    IanaDescription = 5
}
