namespace pop3;

/// <summary>Describes one message in a stable POP3 transaction snapshot.</summary>
public class Pop3MessageInfo
{
    /// <summary>The one-based message number assigned for the transaction.</summary>
    public int MessageNumber;

    /// <summary>The transmitted size of the message representation, in octets.</summary>
    public long OctetCount;

    /// <summary>Indicates whether the message is currently marked for deletion.</summary>
    public bool IsDeleted;

    /// <summary>Indicates whether the size describes the UTF-8 message representation.</summary>
    public bool IsUtf8Representation;
}
