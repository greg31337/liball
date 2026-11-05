namespace pop3;

/// <summary>Represents an unavailable required TLS service or a TLS policy or handshake failure.</summary>
public class Pop3TlsException : System.Exception
{
    /// <summary>The configured transport security mode.</summary>
    public Pop3SecurityMode SecurityMode;

    /// <summary>The TLS operation that failed.</summary>
    public string Operation;

    /// <summary>Initializes a TLS exception with security policy and operation context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="securityMode">The configured transport security mode.</param>
    /// <param name="operation">The TLS operation that failed.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when none is available.</param>
    public Pop3TlsException(string message, Pop3SecurityMode securityMode, string operation, System.Exception? innerException) : base(message, innerException)
    {
        SecurityMode = securityMode;
        Operation = operation;
    }
}
