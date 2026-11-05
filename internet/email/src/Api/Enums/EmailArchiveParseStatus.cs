namespace email;
/// <summary>
/// Specifies the supported archive parse values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArchiveParseStatus
{
    /// <summary>
    /// Represents the success choice or state.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Represents the complete with errors choice or state.
    /// </summary>
    CompleteWithErrors = 1,
    /// <summary>
    /// Represents the partial choice or state.
    /// </summary>
    Partial = 2,
    /// <summary>
    /// Represents the failed choice or state.
    /// </summary>
    Failed = 3
}
