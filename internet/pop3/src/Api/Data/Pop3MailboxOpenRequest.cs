namespace pop3;

/// <summary>Describes a request to acquire an exclusive POP3 maildrop session.</summary>
public class Pop3MailboxOpenRequest
{
    /// <summary>The authenticated identity requesting the maildrop.</summary>
    public string AuthenticatedIdentity = "";

    /// <summary>The provider-defined key identifying the requested maildrop.</summary>
    public string MaildropKey = "";

    /// <summary>The accepted connection context, or <c>null</c> when unavailable.</summary>
    public Pop3ServerConnection? Connection;
}
