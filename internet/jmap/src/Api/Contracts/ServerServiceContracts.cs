using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Provides the Session resource for an authenticated server request.
/// </summary>
public interface IJmapSessionService
{
    /// <summary>Gets the Session visible to the authenticated request.</summary>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Session resource.</returns>
    Task<JmapSession> GetServerSessionAsync(JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides JMAP Core behavior not owned by the dispatcher.
/// </summary>
public interface IJmapCoreService
{
    /// <summary>Echoes arbitrary Core/echo arguments.</summary>
    /// <param name="arguments">The arbitrary arguments object.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <returns>The echoed arguments.</returns>
    JsonElement ExecuteCoreEcho(JsonElement arguments, JmapRequestContext context);
}

/// <summary>
/// Provides persistent and temporary blob operations.
/// </summary>
public interface IBlobService
{
    /// <summary>Stores an uploaded binary stream.</summary>
    /// <param name="request">The upload data and metadata.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the stored blob metadata.</returns>
    Task<BlobUploadResponse> StoreUploadedBlobAsync(BlobUploadRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Lists unreferenced blobs attributed to the authenticated uploader in one account.</summary>
    /// <param name="accountId">The account containing the blobs.</param>
    /// <param name="context">The authenticated uploader context used to scope a per-user quota.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the uploader's unreferenced blobs.</returns>
    Task<System.Collections.Generic.List<JmapUnreferencedBlob>> ListUnreferencedBlobsAsync(string accountId, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Refreshes the upload time and retention time for a newly uploaded or deduplicated unreferenced blob.</summary>
    /// <param name="accountId">The account containing the blob.</param>
    /// <param name="blobId">The immutable blob identifier.</param>
    /// <param name="retainUntilUtc">The earliest normal expiration time in UTC.</param>
    /// <param name="context">The authenticated uploader context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when retention metadata is refreshed.</returns>
    Task RefreshUnreferencedBlobAsync(string accountId, string blobId, DateTimeOffset retainUntilUtc, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Evicts one unreferenced blob attributed to the authenticated uploader.</summary>
    /// <param name="accountId">The account containing the blob.</param>
    /// <param name="blobId">The immutable blob identifier.</param>
    /// <param name="context">The authenticated uploader context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when the blob is evicted.</returns>
    Task EvictUnreferencedBlobAsync(string accountId, string blobId, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Opens authorized immutable blob content, returning null for missing or inaccessible data.</summary>
    /// <param name="request">The requested blob and response metadata.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the download stream and safe metadata, or null when access must not be disclosed.</returns>
    Task<JmapDownloadContent?> OpenStoredBlobAsync(BlobDownloadRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Copies blobs between accounts.</summary>
    /// <param name="request">The Blob/copy arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Blob/copy result.</returns>
    Task<BlobCopyResponse> CopyStoredBlobsAsync(BlobCopyRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides credential-owned push-subscription persistence.
/// </summary>
public interface IPushService
{
    /// <summary>Gets push subscriptions owned by the request credentials.</summary>
    /// <param name="request">The PushSubscription/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the PushSubscription/get result.</returns>
    Task<PushSubscriptionGetResponse> GetStoredPushSubscriptionsAsync(PushSubscriptionGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies credential-owned push-subscription changes.</summary>
    /// <param name="request">The PushSubscription/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the PushSubscription/set result.</returns>
    Task<PushSubscriptionSetResponse> SetStoredPushSubscriptionsAsync(PushSubscriptionSetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides Mailbox persistence and query operations.
/// </summary>
public interface IMailboxService
{
    /// <summary>Gets Mailboxes.</summary>
    /// <param name="request">The Mailbox/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/get result.</returns>
    Task<MailboxGetResponse> GetStoredMailboxesAsync(MailboxGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets Mailbox changes.</summary>
    /// <param name="request">The Mailbox/changes arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/changes result.</returns>
    Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(MailboxChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Queries Mailboxes.</summary>
    /// <param name="request">The Mailbox/query arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/query result.</returns>
    Task<MailboxQueryResponse> QueryStoredMailboxesAsync(MailboxQueryRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets changes to a Mailbox query.</summary>
    /// <param name="request">The Mailbox/queryChanges arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/queryChanges result.</returns>
    Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(MailboxQueryChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies Mailbox changes.</summary>
    /// <param name="request">The Mailbox/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Mailbox/set result.</returns>
    Task<MailboxSetResponse> SetStoredMailboxesAsync(MailboxSetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides Thread read and change operations.
/// </summary>
public interface IThreadService
{
    /// <summary>Gets Threads.</summary>
    /// <param name="request">The Thread/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Thread/get result.</returns>
    Task<ThreadGetResponse> GetStoredThreadsAsync(ThreadGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets Thread changes.</summary>
    /// <param name="request">The Thread/changes arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Thread/changes result.</returns>
    Task<ThreadChangesResponse> GetStoredThreadChangesAsync(ThreadChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides Email persistence, query, import, and parse operations.
/// </summary>
public interface IEmailService
{
    /// <summary>Gets Emails.</summary>
    /// <param name="request">The Email/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/get result.</returns>
    Task<EmailGetResponse> GetStoredEmailsAsync(EmailGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets Email changes.</summary>
    /// <param name="request">The Email/changes arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/changes result.</returns>
    Task<EmailChangesResponse> GetStoredEmailChangesAsync(EmailChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Queries Emails.</summary>
    /// <param name="request">The Email/query arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/query result.</returns>
    Task<EmailQueryResponse> QueryStoredEmailsAsync(EmailQueryRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets changes to an Email query.</summary>
    /// <param name="request">The Email/queryChanges arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/queryChanges result.</returns>
    Task<EmailQueryChangesResponse> GetStoredEmailQueryChangesAsync(EmailQueryChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies Email changes.</summary>
    /// <param name="request">The Email/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/set result.</returns>
    Task<EmailSetResponse> SetStoredEmailsAsync(EmailSetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Copies Emails between accounts.</summary>
    /// <param name="request">The Email/copy arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/copy result.</returns>
    Task<EmailCopyResponse> CopyStoredEmailsAsync(EmailCopyRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Imports uploaded messages.</summary>
    /// <param name="request">The Email/import arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/import result.</returns>
    Task<EmailImportResponse> ImportStoredEmailsAsync(EmailImportRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Parses message blobs without storing Email objects.</summary>
    /// <param name="request">The Email/parse arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Email/parse result.</returns>
    Task<EmailParseResponse> ParseStoredEmailBlobsAsync(EmailParseRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides Email search-snippet generation.
/// </summary>
public interface ISearchSnippetService
{
    /// <summary>Gets highlighted search snippets.</summary>
    /// <param name="request">The SearchSnippet/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the SearchSnippet/get result.</returns>
    Task<SearchSnippetGetResponse> GetStoredSearchSnippetsAsync(SearchSnippetGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides Identity persistence operations.
/// </summary>
public interface IIdentityService
{
    /// <summary>Gets Identities.</summary>
    /// <param name="request">The Identity/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/get result.</returns>
    Task<IdentityGetResponse> GetStoredIdentitiesAsync(IdentityGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets Identity changes.</summary>
    /// <param name="request">The Identity/changes arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/changes result.</returns>
    Task<IdentityChangesResponse> GetStoredIdentityChangesAsync(IdentityChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies Identity changes.</summary>
    /// <param name="request">The Identity/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/set result.</returns>
    Task<IdentitySetResponse> SetStoredIdentitiesAsync(IdentitySetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides EmailSubmission persistence, query, and delivery operations.
/// </summary>
public interface IEmailSubmissionService
{
    /// <summary>Gets EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/get result.</returns>
    Task<EmailSubmissionGetResponse> GetStoredEmailSubmissionsAsync(EmailSubmissionGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets EmailSubmission changes.</summary>
    /// <param name="request">The EmailSubmission/changes arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/changes result.</returns>
    Task<EmailSubmissionChangesResponse> GetStoredEmailSubmissionChangesAsync(EmailSubmissionChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Queries EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/query arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/query result.</returns>
    Task<EmailSubmissionQueryResponse> QueryStoredEmailSubmissionsAsync(EmailSubmissionQueryRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Gets changes to an EmailSubmission query.</summary>
    /// <param name="request">The EmailSubmission/queryChanges arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/queryChanges result.</returns>
    Task<EmailSubmissionQueryChangesResponse> GetStoredEmailSubmissionQueryChangesAsync(EmailSubmissionQueryChangesRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies EmailSubmission changes and schedules delivery.</summary>
    /// <param name="request">The EmailSubmission/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the EmailSubmission/set result.</returns>
    Task<EmailSubmissionSetResponse> SetStoredEmailSubmissionsAsync(EmailSubmissionSetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Provides VacationResponse persistence operations.
/// </summary>
public interface IVacationResponseService
{
    /// <summary>Gets the singleton VacationResponse object.</summary>
    /// <param name="request">The VacationResponse/get arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the VacationResponse/get result.</returns>
    Task<VacationResponseGetResponse> GetStoredVacationResponseAsync(VacationResponseGetRequest request, JmapRequestContext context, CancellationToken cancellationToken);

    /// <summary>Applies changes to the singleton VacationResponse object.</summary>
    /// <param name="request">The VacationResponse/set arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the VacationResponse/set result.</returns>
    Task<VacationResponseSetResponse> SetStoredVacationResponseAsync(VacationResponseSetRequest request, JmapRequestContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Publishes coalesced state changes to EventSource and external push consumers.
/// </summary>
public interface IJmapStatePublisher
{
    /// <summary>Publishes one coalesced state change.</summary>
    /// <param name="publication">The state and event metadata to publish.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task representing publication.</returns>
    Task PublishStateChangeAsync(JmapStatePublication publication, CancellationToken cancellationToken);
}

/// <summary>
/// Supplies deterministic current time to protocol operations.
/// </summary>
public interface IJmapClock
{
    /// <summary>Gets the current UTC date and time.</summary>
    /// <returns>The current UTC date and time.</returns>
    DateTimeOffset GetCurrentUtcDateTime();
}

/// <summary>
/// Approves push callback destinations before the server connects to them.
/// </summary>
public interface IJmapPushUrlPolicy
{
    /// <summary>Determines whether a push callback URL may be contacted.</summary>
    /// <param name="url">The absolute callback URL.</param>
    /// <param name="context">The authenticated request context that owns the subscription.</param>
    /// <returns>True when the destination is permitted; otherwise, false.</returns>
    bool IsPushUrlAllowed(string url, JmapRequestContext context);
}

/// <summary>
/// Selects localized text for genuinely user-visible protocol fields.
/// </summary>
public interface IJmapStringLocalizer
{
    /// <summary>Gets localized text for a stable message key.</summary>
    /// <param name="key">The stable localization key.</param>
    /// <param name="acceptLanguage">The request's Accept-Language value.</param>
    /// <returns>The best available localized text.</returns>
    string LocalizeJmapString(string key, string? acceptLanguage);
}
