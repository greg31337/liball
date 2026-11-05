namespace languagecodes;

/// <summary>Identifies an identifier set in ISO 639:2023.</summary>
public enum Iso639Set
{
    /// <summary>No ISO 639 set is specified.</summary>
    None = 0,
    /// <summary>ISO 639 Set 1, containing two-letter identifiers.</summary>
    Set1 = 1,
    /// <summary>ISO 639 Set 2, containing three-letter identifiers for widely known languages and groups.</summary>
    Set2 = 2,
    /// <summary>ISO 639 Set 3, containing three-letter identifiers for comprehensive individual-language coverage.</summary>
    Set3 = 3,
    /// <summary>ISO 639 Set 5, containing three-letter identifiers for language families and groups.</summary>
    Set5 = 5
}
