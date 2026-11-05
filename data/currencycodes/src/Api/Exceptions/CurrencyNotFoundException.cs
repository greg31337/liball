namespace currencycodes;

/// <summary>
/// Represents a failure to find a required currency or territory.
/// </summary>
public class CurrencyNotFoundException : System.Exception
{
    /// <summary>
    /// Initializes the exception with an invariant failure message supplied by the caller.
    /// </summary>
    /// <param name="message">The invariant failure message.</param>
    public CurrencyNotFoundException(string message) : base(message)
    {
    }
}
