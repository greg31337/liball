namespace email;
/// <summary>
/// Specifies the supported priority values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailPriorityKind
{
    /// <summary>
    /// Represents the non urgent choice or state.
    /// </summary>
    NonUrgent = 0,
    /// <summary>
    /// Represents the normal choice or state.
    /// </summary>
    Normal = 1,
    /// <summary>
    /// Represents the urgent choice or state.
    /// </summary>
    Urgent = 2,
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 3
}
