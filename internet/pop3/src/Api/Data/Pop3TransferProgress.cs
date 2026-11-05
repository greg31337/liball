namespace pop3;

/// <summary>Describes progress for a POP3 multiline transfer.</summary>
public class Pop3TransferProgress
{
    /// <summary>The operation represented by this progress report.</summary>
    public Pop3TransferKind Kind;

    /// <summary>The one-based message number, or <c>0</c> when no single message applies.</summary>
    public int MessageNumber;

    /// <summary>The number of payload bytes transferred so far.</summary>
    public long BytesTransferred;

    /// <summary>The expected payload size in bytes, or <c>-1</c> when unknown.</summary>
    public long ExpectedBytes = -1;

    /// <summary>Indicates whether the transfer completed successfully.</summary>
    public bool IsComplete;
}
