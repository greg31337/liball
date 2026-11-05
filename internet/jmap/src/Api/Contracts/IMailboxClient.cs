using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP Mailboxes.
/// </summary>
public interface IMailboxClient
{
    /// <summary>Gets Mailboxes by identifier or retrieves all Mailboxes.</summary>
    /// <param name="request">The Mailbox/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/get result.</returns>
    Task<MailboxGetResponse> GetMailboxesAsync(MailboxGetRequest request, CancellationToken cancellationToken);

    /// <summary>Gets Mailbox changes since a previous collection state.</summary>
    /// <param name="request">The Mailbox/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/changes result.</returns>
    Task<MailboxChangesResponse> GetMailboxChangesAsync(MailboxChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Gets and combines every Mailbox changes page needed to reach the current state.</summary>
    /// <param name="request">The initial Mailbox/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing one deduplicated aggregate changes result.</returns>
    Task<MailboxChangesResponse> GetAllMailboxChangesAsync(MailboxChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Queries Mailbox identifiers using filtering, sorting, and tree options.</summary>
    /// <param name="request">The Mailbox/query arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/query result.</returns>
    Task<MailboxQueryResponse> QueryMailboxesAsync(MailboxQueryRequest request, CancellationToken cancellationToken);

    /// <summary>Gets changes to a previous Mailbox query result.</summary>
    /// <param name="request">The Mailbox/queryChanges arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/queryChanges result.</returns>
    Task<MailboxQueryChangesResponse> GetMailboxQueryChangesAsync(MailboxQueryChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Applies a Mailbox/queryChanges result to a complete ordered query snapshot.</summary>
    /// <param name="snapshot">The query snapshot at the result's old query state.</param>
    /// <param name="changes">The queryChanges result to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    JmapQuerySnapshot ContinueMailboxQuery(JmapQuerySnapshot snapshot, MailboxQueryChangesResponse changes);

    /// <summary>Creates, updates, or destroys Mailboxes.</summary>
    /// <param name="request">The Mailbox/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/set result.</returns>
    Task<MailboxSetResponse> SetMailboxesAsync(MailboxSetRequest request, CancellationToken cancellationToken);
}
