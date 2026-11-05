namespace email;
/// <summary>
/// Specifies the supported related reference values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailRelatedReferenceKind
{
    /// <summary>
    /// Represents the content ID choice or state.
    /// </summary>
    ContentId = 0,
    /// <summary>
    /// Represents the content location choice or state.
    /// </summary>
    ContentLocation = 1,
    /// <summary>
    /// Represents the external choice or state.
    /// </summary>
    External = 2
}
