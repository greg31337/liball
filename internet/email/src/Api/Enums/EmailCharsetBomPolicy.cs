namespace email;
/// <summary>
/// Specifies the supported charset BOM values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCharsetBomPolicy
{
    /// <summary>
    /// Represents the ignore choice or state.
    /// </summary>
    Ignore = 0,
    /// <summary>
    /// Represents the detect choice or state.
    /// </summary>
    Detect = 1,
    /// <summary>
    /// Represents the require match choice or state.
    /// </summary>
    RequireMatch = 2
}
