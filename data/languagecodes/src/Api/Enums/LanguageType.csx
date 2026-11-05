namespace languagecodes;

/// <summary>Identifies the ISO language type of an entity.</summary>
public enum LanguageType
{
    /// <summary>The language type is not specified by the source.</summary>
    Unknown = 0,
    /// <summary>A living language.</summary>
    Living = 1,
    /// <summary>An extinct language.</summary>
    Extinct = 2,
    /// <summary>An ancient language.</summary>
    Ancient = 3,
    /// <summary>A historical language.</summary>
    Historical = 4,
    /// <summary>A constructed language.</summary>
    Constructed = 5,
    /// <summary>A special-purpose language identifier.</summary>
    Special = 6
}
