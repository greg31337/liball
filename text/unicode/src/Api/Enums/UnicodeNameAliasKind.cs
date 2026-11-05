namespace unicode;

/// <summary>
/// Identifies the kind of a Unicode character-name alias.
/// </summary>
public enum UnicodeNameAliasKind
{
    /// <summary>
    /// Corrects a defective character name that cannot be changed for stability reasons.
    /// </summary>
    Correction = 0,
    /// <summary>
    /// Supplies a commonly used name for a control character.
    /// </summary>
    Control = 1,
    /// <summary>
    /// Supplies an alternate character name.
    /// </summary>
    Alternate = 2,
    /// <summary>
    /// Preserves a documented name for a character that never existed.
    /// </summary>
    Figment = 3,
    /// <summary>
    /// Supplies a standard abbreviation.
    /// </summary>
    Abbreviation = 4,
}
