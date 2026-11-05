namespace tftp;

/// <summary>
/// Represents rejected, malformed, or unacceptable TFTP option negotiation.
/// </summary>
public class TftpOptionNegotiationException : System.Exception
{
    /// <summary>
    /// The option name associated with the failure, or an empty string when no single option applies.
    /// </summary>
    public string OptionName;

    /// <summary>
    /// The option value associated with the failure, or an empty string when unavailable.
    /// </summary>
    public string OptionValue;

    /// <summary>
    /// Initializes an option-negotiation exception with option details.
    /// </summary>
    /// <param name="message">The option-negotiation failure message.</param>
    /// <param name="optionName">The associated option name, or an empty string.</param>
    /// <param name="optionValue">The associated option value, or an empty string.</param>
    public TftpOptionNegotiationException(
        string message,
        string optionName,
        string optionValue)
        : base(message)
    {
        OptionName = optionName;
        OptionValue = optionValue;
    }
}
