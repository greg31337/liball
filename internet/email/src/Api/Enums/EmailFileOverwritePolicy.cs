namespace email;
/// <summary>
/// Specifies the supported file overwrite values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailFileOverwritePolicy
{
    /// <summary>
    /// Represents the fail choice or state.
    /// </summary>
    Fail = 0,
    /// <summary>
    /// Represents the overwrite choice or state.
    /// </summary>
    Overwrite = 1
}
