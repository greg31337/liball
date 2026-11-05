using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP Email submissions.
/// </summary>
public interface IEmailSubmissionClient
{
    /// <summary>Gets EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/get result.</returns>
    Task<EmailSubmissionGetResponse> GetEmailSubmissionsAsync(EmailSubmissionGetRequest request, CancellationToken cancellationToken);

    /// <summary>Gets EmailSubmission changes since a previous collection state.</summary>
    /// <param name="request">The EmailSubmission/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/changes result.</returns>
    Task<EmailSubmissionChangesResponse> GetEmailSubmissionChangesAsync(EmailSubmissionChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Gets and combines every EmailSubmission changes page needed to reach the current state.</summary>
    /// <param name="request">The initial EmailSubmission/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing one deduplicated aggregate changes result.</returns>
    Task<EmailSubmissionChangesResponse> GetAllEmailSubmissionChangesAsync(EmailSubmissionChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Queries EmailSubmission identifiers.</summary>
    /// <param name="request">The EmailSubmission/query arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/query result.</returns>
    Task<EmailSubmissionQueryResponse> QueryEmailSubmissionsAsync(EmailSubmissionQueryRequest request, CancellationToken cancellationToken);

    /// <summary>Gets changes to a previous EmailSubmission query result.</summary>
    /// <param name="request">The EmailSubmission/queryChanges arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/queryChanges result.</returns>
    Task<EmailSubmissionQueryChangesResponse> GetEmailSubmissionQueryChangesAsync(EmailSubmissionQueryChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Applies an EmailSubmission/queryChanges result to a complete ordered query snapshot.</summary>
    /// <param name="snapshot">The query snapshot at the result's old query state.</param>
    /// <param name="changes">The queryChanges result to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    JmapQuerySnapshot ContinueEmailSubmissionQuery(JmapQuerySnapshot snapshot, EmailSubmissionQueryChangesResponse changes);

    /// <summary>Creates, cancels, or destroys EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/set result.</returns>
    Task<EmailSubmissionSetResponse> SetEmailSubmissionsAsync(EmailSubmissionSetRequest request, CancellationToken cancellationToken);
}
