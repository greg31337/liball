namespace pop3;

/// <summary>Describes the visible size of a POP3 maildrop.</summary>
public class Pop3MailboxStatus
{
    /// <summary>The number of visible messages in the maildrop.</summary>
    public int MessageCount;

    /// <summary>The total transmitted size of the visible messages, in octets.</summary>
    public long TotalOctets;
}
