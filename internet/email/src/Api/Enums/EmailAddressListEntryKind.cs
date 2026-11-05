namespace email;
/// <summary>
/// Specifies the supported address list entry values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailAddressListEntryKind
{
    /// <summary>
    /// Represents the mailbox choice or state.
    /// </summary>
    Mailbox = 0,
    /// <summary>
    /// Represents the group choice or state.
    /// </summary>
    Group = 1
}
