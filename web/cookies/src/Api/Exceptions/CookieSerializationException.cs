namespace cookies;

/// <summary>
/// Represents an invalid cookie model that cannot be serialized safely.
/// </summary>
public class CookieSerializationException : global::System.Exception
{
    /// <summary>
    /// Creates an exception with a safe diagnostic message.
    /// </summary>
    /// <param name="message">The safe diagnostic message.</param>
    public CookieSerializationException(string message) : base(message)
    {
    }
}
