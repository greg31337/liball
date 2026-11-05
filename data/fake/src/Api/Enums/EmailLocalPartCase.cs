namespace fake;

/// <summary>
/// Selects a valid ASCII email local-part shape.
/// </summary>
public enum EmailLocalPartCase
{
    /// <summary>
    /// Uses two ordinary lowercase dot-atom segments.
    /// </summary>
    Typical = 0,

    /// <summary>
    /// Includes a plus-tag within dot-atom syntax.
    /// </summary>
    PlusTagged = 1,

    /// <summary>
    /// Includes an apostrophe permitted by RFC 5322 <c>atext</c> syntax.
    /// </summary>
    Apostrophe = 2,

    /// <summary>
    /// Uses a quoted local part containing a space.
    /// </summary>
    QuotedSpace = 3,

    /// <summary>
    /// Uses a quoted local part with escaped quote and backslash characters.
    /// </summary>
    QuotedSpecial = 4,

    /// <summary>
    /// Uses the 64-octet SMTP local-part boundary.
    /// </summary>
    MaximumLength = 5
}
