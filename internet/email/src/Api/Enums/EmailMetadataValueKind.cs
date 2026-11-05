namespace email;
/// <summary>
/// Specifies the supported metadata value values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMetadataValueKind
{
    /// <summary>
    /// Represents the keywords choice or state.
    /// </summary>
    Keywords = 0,
    /// <summary>
    /// Represents the priority choice or state.
    /// </summary>
    Priority = 1,
    /// <summary>
    /// Represents the importance choice or state.
    /// </summary>
    Importance = 2,
    /// <summary>
    /// Represents the sensitivity choice or state.
    /// </summary>
    Sensitivity = 3,
    /// <summary>
    /// Represents the auto submitted choice or state.
    /// </summary>
    AutoSubmitted = 4,
    /// <summary>
    /// Represents the list management choice or state.
    /// </summary>
    ListManagement = 5,
    /// <summary>
    /// Represents the list unsubscribe post choice or state.
    /// </summary>
    ListUnsubscribePost = 6,
    /// <summary>
    /// Represents the resent field choice or state.
    /// </summary>
    ResentField = 7
}
