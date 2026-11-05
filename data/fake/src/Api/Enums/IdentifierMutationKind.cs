namespace fake;

/// <summary>
/// Selects a deliberate identifier edge-case mutation.
/// </summary>
public enum IdentifierMutationKind
{
    /// <summary>
    /// Changes a check digit while preserving the remaining identifier text.
    /// </summary>
    Checksum = 0,

    /// <summary>
    /// Removes the final Unicode scalar from the identifier.
    /// </summary>
    Length = 1,

    /// <summary>
    /// Replaces one identifier character with unsupported whitespace.
    /// </summary>
    Character = 2,

    /// <summary>
    /// Changes a required prefix, preserving the checksum for compact ISBN-13 input.
    /// </summary>
    Prefix = 3,

    /// <summary>
    /// Replaces the version nibble of canonical UUID text with an unsupported version.
    /// </summary>
    Version = 4,

    /// <summary>
    /// Changes valid ULID text to exceed its 128-bit textual range.
    /// </summary>
    Overflow = 5
}
