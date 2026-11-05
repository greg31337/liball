namespace email;
/// <summary>
/// Specifies the supported maildir collision values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMaildirCollisionPolicy
{
    /// <summary>
    /// Represents the fail choice or state.
    /// </summary>
    Fail = 0,
    /// <summary>
    /// Represents the generate unique choice or state.
    /// </summary>
    GenerateUnique = 1,
    /// <summary>
    /// Represents the overwrite choice or state.
    /// </summary>
    Overwrite = 2
}
