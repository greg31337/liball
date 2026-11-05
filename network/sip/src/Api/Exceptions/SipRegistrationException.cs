namespace sip;

/// <summary>Indicates that a SIP registration operation failed.</summary>
public class SipRegistrationException : System.Exception
{
    /// <summary>The registration state associated with the failure.</summary>
    public SipRegistrationState State;
    /// <summary>The address of record associated with the failure, when available.</summary>
    public SipUri? AddressOfRecord;

    /// <summary>Initializes a SIP registration failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="state">The registration state associated with the failure.</param>
    /// <param name="addressOfRecord">The address of record associated with the failure, when available.</param>
    public SipRegistrationException(string message, SipRegistrationState state, SipUri? addressOfRecord) : base(message)
    {
        State = state;
        AddressOfRecord = addressOfRecord;
    }
}
