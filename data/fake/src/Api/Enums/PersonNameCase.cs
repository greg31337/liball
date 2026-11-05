namespace fake;

/// <summary>
/// Selects a valid unusual person-name structure supplied by locale data.
/// </summary>
public enum PersonNameCase
{
    /// <summary>
    /// Uses one name without separate given and family components.
    /// </summary>
    Mononym = 0,

    /// <summary>
    /// Includes a family-name particle.
    /// </summary>
    Particle = 1,

    /// <summary>
    /// Includes a patronymic component.
    /// </summary>
    Patronymic = 2,

    /// <summary>
    /// Includes multiple space-separated name parts.
    /// </summary>
    MultiPart = 3,

    /// <summary>
    /// Includes an apostrophe within a name component.
    /// </summary>
    Apostrophe = 4,

    /// <summary>
    /// Includes a hyphen within a name component.
    /// </summary>
    Hyphenated = 5,

    /// <summary>
    /// Includes letters with diacritics.
    /// </summary>
    Diacritic = 6,

    /// <summary>
    /// Uses a right-to-left script.
    /// </summary>
    RightToLeft = 7,

    /// <summary>
    /// Omits components that are optional in its locale-defined form.
    /// </summary>
    OptionalComponentOmitted = 8,

    /// <summary>
    /// Uses an unusually long but valid name.
    /// </summary>
    Long = 9
}
