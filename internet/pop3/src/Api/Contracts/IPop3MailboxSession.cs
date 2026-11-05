namespace pop3;

/// <summary>Provides an exclusive transaction-scoped view of a POP3 maildrop.</summary>
public interface IPop3MailboxSession
{
    /// <summary>Gets the current visible mailbox status.</summary>
    /// <returns>The visible message count and total octet size.</returns>
    Pop3MailboxStatus GetStatus();

    /// <summary>Gets stable transaction metadata for the maildrop messages.</summary>
    /// <returns>The messages in stable transaction order.</returns>
    Pop3MessageInfo[] GetMessages();

    /// <summary>Gets stable unique identifiers for the maildrop messages.</summary>
    /// <returns>The identifiers in stable transaction order.</returns>
    Pop3UniqueId[] GetUniqueIds();

    /// <summary>Writes one complete message representation to a caller-owned stream.</summary>
    /// <param name="messageNumber">The one-based transaction message number.</param>
    /// <param name="useUtf8Representation">Whether the UTF-8 representation is required.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    void WriteMessage(int messageNumber, bool useUtf8Representation, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Writes message headers and the requested number of body lines.</summary>
    /// <param name="messageNumber">The one-based transaction message number.</param>
    /// <param name="bodyLineCount">The nonnegative number of body lines to write.</param>
    /// <param name="useUtf8Representation">Whether the UTF-8 representation is required.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    void WriteTop(int messageNumber, int bodyLineCount, bool useUtf8Representation, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Marks one message for deletion within the current transaction.</summary>
    /// <param name="messageNumber">The one-based transaction message number.</param>
    void MarkDeleted(int messageNumber);

    /// <summary>Clears all deletion marks and returns the restored visible status.</summary>
    /// <returns>The restored mailbox status.</returns>
    Pop3MailboxStatus ResetDeletions();

    /// <summary>Atomically commits all marked deletions during UPDATE.</summary>
    /// <param name="cancellationToken">A token that can cancel provider work before the atomic commit begins.</param>
    void CommitUpdate(System.Threading.CancellationToken cancellationToken);

    /// <summary>Rolls back all uncommitted deletion marks.</summary>
    void RollbackChanges();

    /// <summary>Releases the exclusive maildrop session and its provider resources.</summary>
    void ReleaseSession();
}
