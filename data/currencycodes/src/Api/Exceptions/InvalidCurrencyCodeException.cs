namespace currencycodes;

/// <summary>
/// Represents a malformed, unassigned, or out-of-scope required currency code.
/// </summary>
public class InvalidCurrencyCodeException : System.Exception
{
    /// <summary>
    /// Initializes the exception with an invariant failure message supplied by the caller.
    /// </summary>
    /// <param name="message">The invariant failure message.</param>
    public InvalidCurrencyCodeException(string message) : base(message)
    {
    }
}
