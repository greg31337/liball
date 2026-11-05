namespace email;
/// <summary>
/// Specifies the supported token values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTokenKind
{
    /// <summary>
    /// No token is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the message start choice or state.
    /// </summary>
    MessageStart = 1,
    /// <summary>
    /// Represents the headers choice or state.
    /// </summary>
    Headers = 2,
    /// <summary>
    /// Represents the entity start choice or state.
    /// </summary>
    EntityStart = 3,
    /// <summary>
    /// Represents the body chunk choice or state.
    /// </summary>
    BodyChunk = 4,
    /// <summary>
    /// Represents the entity end choice or state.
    /// </summary>
    EntityEnd = 5,
    /// <summary>
    /// Represents the message end choice or state.
    /// </summary>
    MessageEnd = 6,
    /// <summary>
    /// Represents the error choice or state.
    /// </summary>
    Error = 7,
    /// <summary>
    /// Represents the end choice or state.
    /// </summary>
    End = 8
}
