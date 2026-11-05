namespace pop3;

/// <summary>Represents a POP3 connection, end-of-stream, timeout, or closed-session failure.</summary>
public class Pop3ConnectionException : System.Exception
{
    /// <summary>The operation that was in progress when the connection failed.</summary>
    public string Operation;

    /// <summary>The session state observed when the connection failed.</summary>
    public Pop3SessionState SessionState;

    /// <summary>Indicates whether the failure was caused by an unexpected end of stream.</summary>
    public bool IsEndOfStream;

    /// <summary>Indicates whether the failure was caused by a timeout.</summary>
    public bool IsTimeout;

    /// <summary>Initializes a connection exception with safe operation and session context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="sessionState">The session state when the failure occurred.</param>
    /// <param name="isEndOfStream">Whether an unexpected end of stream caused the failure.</param>
    /// <param name="isTimeout">Whether a timeout caused the failure.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when none is available.</param>
    public Pop3ConnectionException(string message, string operation, Pop3SessionState sessionState, bool isEndOfStream, bool isTimeout, System.Exception? innerException) : base(message, innerException)
    {
        Operation = operation;
        SessionState = sessionState;
        IsEndOfStream = isEndOfStream;
        IsTimeout = isTimeout;
    }
}
