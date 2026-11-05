namespace currencycodes;

/// <summary>
/// Represents invalid amount metadata, scale, increment, text, or conversion range.
/// </summary>
public class InvalidCurrencyAmountException : System.Exception
{
    /// <summary>
    /// Initializes the exception with an invariant failure message supplied by the caller.
    /// </summary>
    /// <param name="message">The invariant failure message.</param>
    public InvalidCurrencyAmountException(string message) : base(message)
    {
    }
}
