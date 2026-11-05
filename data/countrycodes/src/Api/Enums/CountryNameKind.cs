namespace countrycodes;

/// <summary>
/// Identifies the role of a country or subdivision name.
/// </summary>
public enum CountryNameKind
{
    /// <summary>
    /// The ISO short name.
    /// </summary>
    IsoShort = 0,

    /// <summary>
    /// The ISO official name.
    /// </summary>
    IsoOfficial = 1,

    /// <summary>
    /// A commonly used name.
    /// </summary>
    Common = 2,

    /// <summary>
    /// A native or localized display name.
    /// </summary>
    NativeOrLocalized = 3,

    /// <summary>
    /// An alternate name used for lookup.
    /// </summary>
    Alias = 4,

    /// <summary>
    /// A deprecated name retained for compatibility.
    /// </summary>
    Deprecated = 5,

    /// <summary>
    /// A historical name.
    /// </summary>
    Historical = 6
}
