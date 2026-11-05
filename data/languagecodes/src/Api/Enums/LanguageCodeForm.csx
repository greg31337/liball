namespace languagecodes;

/// <summary>Identifies the recognized form and source of a language code.</summary>
public enum LanguageCodeForm
{
    /// <summary>No recognized code form.</summary>
    Unknown = 0,
    /// <summary>An ISO 639 Set 1 two-letter code.</summary>
    Alpha2 = 1,
    /// <summary>An ISO 639 Set 2 terminology code.</summary>
    Set2Terminology = 2,
    /// <summary>An ISO 639 Set 2 bibliographic code.</summary>
    Set2Bibliographic = 3,
    /// <summary>An active ISO 639 Set 3 code.</summary>
    Set3 = 4,
    /// <summary>An ISO 639 Set 5 code.</summary>
    Set5 = 5,
    /// <summary>A retired ISO 639 Set 3 code.</summary>
    RetiredSet3 = 6,
    /// <summary>An IANA language subtag.</summary>
    IanaLanguage = 7,
    /// <summary>An IANA extlang subtag.</summary>
    IanaExtlang = 8,
    /// <summary>An IANA grandfathered language tag.</summary>
    GrandfatheredTag = 9,
    /// <summary>An IANA redundant language tag.</summary>
    RedundantTag = 10,
    /// <summary>A reserved private-use language code.</summary>
    PrivateUse = 11
}
