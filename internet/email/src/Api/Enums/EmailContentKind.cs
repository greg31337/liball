namespace email;
/// <summary>
/// Specifies the supported content values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailContentKind
{
    /// <summary>
    /// Represents the memory choice or state.
    /// </summary>
    Memory = 0,
    /// <summary>
    /// Represents the file choice or state.
    /// </summary>
    File = 1,
    /// <summary>
    /// Represents the stream choice or state.
    /// </summary>
    Stream = 2,
    /// <summary>
    /// Represents the temporary file choice or state.
    /// </summary>
    TemporaryFile = 3,
    /// <summary>
    /// Represents the slice choice or state.
    /// </summary>
    Slice = 4
}
