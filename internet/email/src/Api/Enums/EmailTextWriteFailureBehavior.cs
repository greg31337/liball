namespace email;
/// <summary>
/// Specifies how text serialization reports an unsuccessful result.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTextWriteFailureBehavior
{
    /// <summary>
    /// Returns an unsuccessful result containing diagnostics.
    /// </summary>
    ReturnResult = 0,
    /// <summary>
    /// Throws an exception when the result would be unsuccessful.
    /// </summary>
    Throw = 1
}
