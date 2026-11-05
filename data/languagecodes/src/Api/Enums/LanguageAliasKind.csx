namespace languagecodes;

/// <summary>Identifies the origin and meaning of a language code alias.</summary>
public enum LanguageAliasKind
{
    /// <summary>An ISO 639 Set 1 two-letter alias.</summary>
    Alpha2 = 0,
    /// <summary>An ISO 639 Set 2 terminology alias.</summary>
    Set2Terminology = 1,
    /// <summary>An ISO 639 Set 2 bibliographic alias.</summary>
    Set2Bibliographic = 2,
    /// <summary>An active ISO 639 Set 3 alias.</summary>
    Set3 = 3,
    /// <summary>An ISO 639 Set 5 alias.</summary>
    Set5 = 4,
    /// <summary>A retired ISO 639 Set 3 alias.</summary>
    RetiredSet3 = 5,
    /// <summary>A deprecated IANA alias.</summary>
    IanaDeprecated = 6,
    /// <summary>An IANA preferred value used as an alias.</summary>
    IanaPreferred = 7,
    /// <summary>An IANA grandfathered tag alias.</summary>
    Grandfathered = 8,
    /// <summary>An overlong ISO language-code alias.</summary>
    Overlong = 9
}
