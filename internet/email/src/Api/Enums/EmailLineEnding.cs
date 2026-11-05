namespace email;
/// <summary>
/// Specifies the supported line ending values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailLineEnding
{
    /// <summary>
    /// No line ending is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the cr lf choice or state.
    /// </summary>
    CrLf = 1,
    /// <summary>
    /// Represents the lf choice or state.
    /// </summary>
    Lf = 2,
    /// <summary>
    /// Represents the cr choice or state.
    /// </summary>
    Cr = 3
}
