namespace sip;

/// <summary>Indicates that a SIP dialog operation failed.</summary>
public class SipDialogException : System.Exception
{
    /// <summary>The related dialog identifier, when available.</summary>
    public SipDialogId? DialogId;
    /// <summary>The dialog state associated with the failure.</summary>
    public SipDialogState State;

    /// <summary>Initializes a SIP dialog failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="dialogId">The related dialog identifier, when available.</param>
    /// <param name="state">The dialog state associated with the failure.</param>
    public SipDialogException(string message, SipDialogId? dialogId, SipDialogState state) : base(message)
    {
        DialogId = dialogId;
        State = state;
    }
}
