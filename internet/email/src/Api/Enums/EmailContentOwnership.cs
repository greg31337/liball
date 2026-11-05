namespace email;
/// <summary>
/// Specifies the supported content ownership values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailContentOwnership
{
    /// <summary>
    /// Represents the borrowed choice or state.
    /// </summary>
    Borrowed = 0,
    /// <summary>
    /// Represents the owned choice or state.
    /// </summary>
    Owned = 1
}
