namespace email;
/// <summary>
/// Specifies the supported body storage values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodyStorageMode
{
    /// <summary>
    /// Represents the lazy choice or state.
    /// </summary>
    Lazy = 0,
    /// <summary>
    /// Represents the store choice or state.
    /// </summary>
    Store = 1,
    /// <summary>
    /// Represents the discard choice or state.
    /// </summary>
    Discard = 2
}
