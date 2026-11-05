using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP Emails.
/// </summary>
public interface IEmailClient
{
    /// <summary>Gets Emails with optional header and body projection.</summary>
    /// <param name="request">The Email/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/get result.</returns>
    Task<EmailGetResponse> GetEmailsAsync(EmailGetRequest request, CancellationToken cancellationToken);

    /// <summary>Gets Email changes since a previous collection state.</summary>
    /// <param name="request">The Email/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/changes result.</returns>
    Task<EmailChangesResponse> GetEmailChangesAsync(EmailChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Gets and combines every Email changes page needed to reach the current state.</summary>
    /// <param name="request">The initial Email/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing one deduplicated aggregate changes result.</returns>
    Task<EmailChangesResponse> GetAllEmailChangesAsync(EmailChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Queries Emails using typed recursive filters and comparators.</summary>
    /// <param name="request">The Email/query arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/query result.</returns>
    Task<EmailQueryResponse> QueryEmailsAsync(EmailQueryRequest request, CancellationToken cancellationToken);

    /// <summary>Gets changes to a previous Email query result.</summary>
    /// <param name="request">The Email/queryChanges arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/queryChanges result.</returns>
    Task<EmailQueryChangesResponse> GetEmailQueryChangesAsync(EmailQueryChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Applies an Email/queryChanges result to a complete ordered query snapshot.</summary>
    /// <param name="snapshot">The query snapshot at the result's old query state.</param>
    /// <param name="changes">The queryChanges result to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    JmapQuerySnapshot ContinueEmailQuery(JmapQuerySnapshot snapshot, EmailQueryChangesResponse changes);

    /// <summary>Creates drafts, updates mutable metadata, or destroys Emails.</summary>
    /// <param name="request">The Email/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/set result.</returns>
    Task<EmailSetResponse> SetEmailsAsync(EmailSetRequest request, CancellationToken cancellationToken);

    /// <summary>Copies Emails between accounts.</summary>
    /// <param name="request">The Email/copy arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/copy result.</returns>
    Task<EmailCopyResponse> CopyEmailsAsync(EmailCopyRequest request, CancellationToken cancellationToken);

    /// <summary>Imports uploaded RFC 5322 messages into an account.</summary>
    /// <param name="request">The Email/import arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/import result.</returns>
    Task<EmailImportResponse> ImportEmailsAsync(EmailImportRequest request, CancellationToken cancellationToken);

    /// <summary>Parses message blobs without adding them to the mail store.</summary>
    /// <param name="request">The Email/parse arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/parse result.</returns>
    Task<EmailParseResponse> ParseEmailsAsync(EmailParseRequest request, CancellationToken cancellationToken);
}
