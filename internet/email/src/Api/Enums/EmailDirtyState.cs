namespace email;
/// <summary>
/// Specifies the supported dirty values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDirtyState
{
    /// <summary>
    /// Represents the clean choice or state.
    /// </summary>
    Clean = 0,
    /// <summary>
    /// Represents the exact only choice or state.
    /// </summary>
    ExactOnly = 1,
    /// <summary>
    /// Represents the semantic only choice or state.
    /// </summary>
    SemanticOnly = 2,
    /// <summary>
    /// Represents the exact and semantic choice or state.
    /// </summary>
    ExactAndSemantic = 3
}
