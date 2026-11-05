namespace rtp;

/// <summary>
/// Represents an invalid RTP library configuration value or combination.
/// </summary>
public class RtpConfigurationException : System.Exception
{
    /// <summary>
    /// Initializes an RTP configuration exception with an error message.
    /// </summary>
    /// <param name="message">The message that describes the configuration error.</param>
    public RtpConfigurationException(string message) : base(message)
    {
    }
}
