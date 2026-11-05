namespace pop3;

/// <summary>Represents a POP3 mailbox snapshot, retrieval, or commit failure.</summary>
public class Pop3MailboxException : System.Exception
{
    /// <summary>The mailbox operation that failed.</summary>
    public string Operation;

    /// <summary>The one-based message number involved, or <c>0</c> when no single message applies.</summary>
    public int MessageNumber;

    /// <summary>Initializes a mailbox exception with safe operation and message context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="operation">The mailbox operation that failed.</param>
    /// <param name="messageNumber">The one-based message number, or <c>0</c> when none applies.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when none is available.</param>
    public Pop3MailboxException(string message, string operation, int messageNumber, System.Exception? innerException) : base(message, innerException)
    {
        Operation = operation;
        MessageNumber = messageNumber;
    }
}
