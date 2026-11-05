namespace sip;

/// <summary>Indicates that SIP target resolution failed.</summary>
public class SipResolutionException : System.Exception
{
    /// <summary>The resolution status associated with the failure.</summary>
    public SipResolutionStatus Status;
    /// <summary>The domain that could not be resolved, when available.</summary>
    public string? Domain;

    /// <summary>Initializes a SIP target resolution failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="status">The resolution status associated with the failure.</param>
    /// <param name="domain">The domain that could not be resolved, when available.</param>
    public SipResolutionException(string message, SipResolutionStatus status, string? domain) : base(message)
    {
        Status = status;
        Domain = domain;
    }
}
