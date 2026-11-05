namespace email;
/// <summary>
/// Specifies the supported body chunk values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodyChunkKind
{
    /// <summary>
    /// Represents the content choice or state.
    /// </summary>
    Content = 0,
    /// <summary>
    /// Represents the preamble choice or state.
    /// </summary>
    Preamble = 1,
    /// <summary>
    /// Represents the epilogue choice or state.
    /// </summary>
    Epilogue = 2
}
