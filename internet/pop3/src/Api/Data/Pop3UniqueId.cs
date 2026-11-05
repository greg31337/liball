namespace pop3;

/// <summary>Associates a POP3 message number with its stable unique identifier.</summary>
public class Pop3UniqueId
{
    /// <summary>The one-based message number assigned for the transaction.</summary>
    public int MessageNumber;

    /// <summary>The stable unique identifier supplied by the maildrop.</summary>
    public string Value = "";
}
