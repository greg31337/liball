namespace languagecodes;

/// <summary>Identifies the ISO scope of a language entity.</summary>
public enum LanguageScope
{
    /// <summary>The scope is not specified by the source.</summary>
    Unknown = 0,
    /// <summary>An individual language.</summary>
    Individual = 1,
    /// <summary>A macrolanguage.</summary>
    Macrolanguage = 2,
    /// <summary>A language collection, family, or group.</summary>
    Collection = 3,
    /// <summary>A special-purpose language identifier.</summary>
    Special = 4
}
