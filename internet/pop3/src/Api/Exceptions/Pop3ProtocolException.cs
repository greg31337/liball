namespace pop3;

/// <summary>Represents malformed or contradictory POP3 peer protocol data.</summary>
public class Pop3ProtocolException : System.Exception
{
    /// <summary>The session state in which the invalid protocol data was observed.</summary>
    public Pop3SessionState SessionState;

    /// <summary>Safe diagnostic context for the invalid protocol element, or <c>null</c> when unavailable.</summary>
    public string? Context;

    /// <summary>Initializes a protocol exception with safe diagnostic context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="sessionState">The session state in which the failure occurred.</param>
    /// <param name="context">Safe protocol context, or <c>null</c> when unavailable.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when none is available.</param>
    public Pop3ProtocolException(string message, Pop3SessionState sessionState, string? context, System.Exception? innerException) : base(message, innerException)
    {
        SessionState = sessionState;
        Context = context;
    }
}
