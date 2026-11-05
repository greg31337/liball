namespace cookies;

/// <summary>
/// Represents a failure to evaluate cookie acceptance or return policy.
/// </summary>
public class CookiePolicyException : global::System.Exception
{
    /// <summary>
    /// Creates an exception with a safe diagnostic message.
    /// </summary>
    /// <param name="message">The safe diagnostic message.</param>
    public CookiePolicyException(string message) : base(message)
    {
    }
}
