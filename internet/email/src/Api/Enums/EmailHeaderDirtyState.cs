namespace email;
/// <summary>
/// Specifies the supported header dirty values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderDirtyState
{
    /// <summary>
    /// Represents the clean choice or state.
    /// </summary>
    Clean = 0,
    /// <summary>
    /// Represents the name changed choice or state.
    /// </summary>
    NameChanged = 1,
    /// <summary>
    /// Represents the value changed choice or state.
    /// </summary>
    ValueChanged = 2,
    /// <summary>
    /// Represents the line ending changed choice or state.
    /// </summary>
    LineEndingChanged = 3,
    /// <summary>
    /// Represents the multiple changes choice or state.
    /// </summary>
    MultipleChanges = 4
}
