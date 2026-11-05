namespace cookies;

/// <summary>
/// Represents invalid or corrupt lossless cookie snapshot data.
/// </summary>
public class CookieSnapshotException : global::System.Exception
{
    /// <summary>
    /// Creates an exception with a safe diagnostic message.
    /// </summary>
    /// <param name="message">The safe diagnostic message.</param>
    public CookieSnapshotException(string message) : base(message)
    {
    }
}
