namespace pop3;

/// <summary>Describes the result of attempting to acquire a POP3 maildrop.</summary>
public class Pop3MailboxOpenResult
{
    /// <summary>The classified acquisition outcome.</summary>
    public Pop3AuthenticationStatus Status;

    /// <summary>The acquired exclusive mailbox session, or <c>null</c> when acquisition failed.</summary>
    public IPop3MailboxSession? Session;

    /// <summary>A suggested delay before retrying, in seconds, or <c>-1</c> when none is available.</summary>
    public int RetryAfterSeconds = -1;

    /// <summary>Safe human-readable failure text, or <c>null</c> when no text is supplied.</summary>
    public string? FailureText;
}
