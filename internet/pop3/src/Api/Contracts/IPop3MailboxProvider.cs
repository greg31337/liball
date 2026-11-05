namespace pop3;

/// <summary>Acquires exclusive provider-backed POP3 mailbox sessions.</summary>
public interface IPop3MailboxProvider
{
    /// <summary>Attempts to acquire the requested maildrop.</summary>
    /// <param name="request">The authenticated maildrop request.</param>
    /// <param name="cancellationToken">A token that can cancel provider work.</param>
    /// <returns>The classified acquisition result and optional mailbox session.</returns>
    Pop3MailboxOpenResult OpenMailbox(Pop3MailboxOpenRequest request, System.Threading.CancellationToken cancellationToken);
}
