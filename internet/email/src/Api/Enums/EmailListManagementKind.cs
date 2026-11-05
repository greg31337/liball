namespace email;
/// <summary>
/// Specifies the supported list management values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailListManagementKind
{
    /// <summary>
    /// Represents the help choice or state.
    /// </summary>
    Help = 0,
    /// <summary>
    /// Represents the unsubscribe choice or state.
    /// </summary>
    Unsubscribe = 1,
    /// <summary>
    /// Represents the subscribe choice or state.
    /// </summary>
    Subscribe = 2,
    /// <summary>
    /// Represents the post choice or state.
    /// </summary>
    Post = 3,
    /// <summary>
    /// Represents the owner choice or state.
    /// </summary>
    Owner = 4,
    /// <summary>
    /// Represents the archive choice or state.
    /// </summary>
    Archive = 5
}
