namespace email;
/// <summary>
/// Specifies the supported generation values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailGenerationKind
{
    /// <summary>
    /// Represents the date choice or state.
    /// </summary>
    Date = 0,
    /// <summary>
    /// Represents the message ID choice or state.
    /// </summary>
    MessageId = 1,
    /// <summary>
    /// Represents the content ID choice or state.
    /// </summary>
    ContentId = 2,
    /// <summary>
    /// Represents the MIME version choice or state.
    /// </summary>
    MimeVersion = 3,
    /// <summary>
    /// Represents the boundary choice or state.
    /// </summary>
    Boundary = 4
}
