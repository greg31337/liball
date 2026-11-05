namespace cookies;

/// <summary>
/// Represents malformed cookie input encountered by a throwing parse operation.
/// </summary>
public class CookieParseException : global::System.Exception
{
    /// <summary>
    /// Creates an exception with a safe diagnostic message.
    /// </summary>
    /// <param name="message">The safe diagnostic message.</param>
    public CookieParseException(string message) : base(message)
    {
    }
}
