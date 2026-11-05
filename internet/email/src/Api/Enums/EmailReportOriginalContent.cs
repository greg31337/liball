namespace email;
/// <summary>
/// Specifies the supported report original content values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailReportOriginalContent
{
    /// <summary>
    /// No report original content is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the headers choice or state.
    /// </summary>
    Headers = 1,
    /// <summary>
    /// Represents the full message choice or state.
    /// </summary>
    FullMessage = 2
}
