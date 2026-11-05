namespace languagecodes;

/// <summary>Identifies the field or matching rule that produced a language match.</summary>
public enum LanguageMatchKind
{
    /// <summary>No match was produced.</summary>
    None = 0,
    /// <summary>An ISO 639 Set 1 two-letter code matched.</summary>
    Alpha2 = 1,
    /// <summary>An ISO 639 Set 2 terminology code matched.</summary>
    Set2Terminology = 2,
    /// <summary>An ISO 639 Set 2 bibliographic code matched.</summary>
    Set2Bibliographic = 3,
    /// <summary>An active ISO 639 Set 3 code matched.</summary>
    Set3 = 4,
    /// <summary>An ISO 639 Set 5 code matched.</summary>
    Set5 = 5,
    /// <summary>A retired ISO 639 Set 3 code matched.</summary>
    RetiredSet3 = 6,
    /// <summary>An IANA alias matched.</summary>
    IanaAlias = 7,
    /// <summary>A name matched exactly.</summary>
    ExactName = 8,
    /// <summary>A normalized name matched.</summary>
    NormalizedName = 9,
    /// <summary>A reserved private-use code matched.</summary>
    PrivateUse = 10
}
