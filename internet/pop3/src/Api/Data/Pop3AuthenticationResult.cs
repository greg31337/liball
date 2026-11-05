namespace pop3;

/// <summary>Describes the classified result of a POP3 authentication attempt.</summary>
public class Pop3AuthenticationResult
{
    /// <summary>The classified authentication outcome.</summary>
    public Pop3AuthenticationStatus Status;

    /// <summary>The authenticated identity, or <c>null</c> when authentication did not succeed.</summary>
    public string? AuthenticatedIdentity;

    /// <summary>The provider-defined maildrop key, or <c>null</c> when no maildrop is available.</summary>
    public string? MaildropKey;

    /// <summary>A suggested delay before retrying, in seconds, or <c>-1</c> when none is available.</summary>
    public int RetryAfterSeconds = -1;

    /// <summary>Safe human-readable failure text, or <c>null</c> when no text is supplied.</summary>
    public string? FailureText;
}
