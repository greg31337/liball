namespace pop3;

/// <summary>Represents a locally requested operation that is illegal in the current POP3 state.</summary>
public class Pop3StateException : System.Exception
{
    /// <summary>The operation rejected by the local state guard.</summary>
    public string Operation;

    /// <summary>The session state in which the operation was rejected.</summary>
    public Pop3SessionState SessionState;

    /// <summary>Initializes a state exception with operation and session context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="operation">The rejected operation.</param>
    /// <param name="sessionState">The session state in which the operation was rejected.</param>
    public Pop3StateException(string message, string operation, Pop3SessionState sessionState) : base(message)
    {
        Operation = operation;
        SessionState = sessionState;
    }
}
