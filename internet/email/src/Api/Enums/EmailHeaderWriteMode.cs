namespace email;
/// <summary>
/// Specifies the supported header write values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderWriteMode
{
    /// <summary>
    /// Represents the raw choice or state.
    /// </summary>
    Raw = 0,
    /// <summary>
    /// Represents the preserving choice or state.
    /// </summary>
    Preserving = 1,
    /// <summary>
    /// Represents the canonical choice or state.
    /// </summary>
    Canonical = 2
}
