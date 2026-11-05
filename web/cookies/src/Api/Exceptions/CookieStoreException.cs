namespace cookies;

/// <summary>
/// Represents an operational failure in a cookie store.
/// </summary>
public class CookieStoreException : global::System.Exception
{
    /// <summary>
    /// Creates an exception with a safe diagnostic message.
    /// </summary>
    /// <param name="message">The safe diagnostic message.</param>
    public CookieStoreException(string message) : base(message)
    {
    }
}
