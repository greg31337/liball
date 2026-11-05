using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Composes Session caching, account selection, request building, transport, and typed JMAP methods.
/// </summary>
public class JmapClient : IJmapClient, IBlobClient, IPushClient, IMailboxClient, IThreadClient,
    IEmailClient, ISearchSnippetClient, IIdentityClient, IEmailSubmissionClient,
    IVacationResponseClient
{
    internal const string SingleCallId = "c0";
    internal JmapClientOptions Options;
    internal IJmapTransport Transport;
    internal SessionCache Sessions;
    internal AccountSelector Accounts = new AccountSelector();
    internal ResponseMapper Responses = new ResponseMapper();
    internal CoreEchoMethod Echo = new CoreEchoMethod();
    internal BlobCopyMethod BlobCopies = new BlobCopyMethod();
    internal PushSubscriptionMethod PushSubscriptions = new PushSubscriptionMethod();
    internal MailboxMethod Mailboxes = new MailboxMethod();
    internal ThreadMethod Threads = new ThreadMethod();
    internal EmailMethod Emails = new EmailMethod();
    internal SearchSnippetMethod Snippets = new SearchSnippetMethod();
    internal IdentityMethod Identities = new IdentityMethod();
    internal EmailSubmissionMethod Submissions = new EmailSubmissionMethod();
    internal VacationResponseMethod Vacations = new VacationResponseMethod();
    internal QueryChangesContinuation QueryContinuations = new QueryChangesContinuation();

    /// <summary>Creates a client over the supplied configuration and transport.</summary>
    /// <param name="options">The client configuration.</param>
    /// <param name="transport">The Session, API, and binary transport.</param>
    public JmapClient(JmapClientOptions options, IJmapTransport transport)
    {
        Options = options;
        Transport = transport;
        Sessions = new SessionCache(options, transport);
    }

    /// <summary>Gets the configuration used by this client.</summary>
    /// <returns>The client configuration.</returns>
    public JmapClientOptions GetClientOptions()
    {
        return Options;
    }

    /// <summary>Gets the cached Session, or null before discovery.</summary>
    /// <returns>The cached Session, or null.</returns>
    public JmapSession? GetSession()
    {
        return Sessions.GetSession();
    }

    /// <summary>Discovers and caches a Session resource.</summary>
    /// <param name="options">The discovery configuration.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the discovered Session.</returns>
    public Task<JmapSession> DiscoverSessionAsync(
        JmapDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        return Sessions.DiscoverSessionAsync(options, cancellationToken);
    }

    /// <summary>Refreshes the configured Session resource.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the refreshed Session.</returns>
    public Task<JmapSession> RefreshSessionAsync(CancellationToken cancellationToken)
    {
        return Sessions.RefreshSessionAsync(cancellationToken);
    }

    /// <summary>Gets the primary account mapped to a capability.</summary>
    /// <param name="capability">The account capability URI.</param>
    /// <returns>The primary account identifier, or null.</returns>
    public string? GetPrimaryAccountId(string capability)
    {
        JmapSession? session = Sessions.GetSession();
        if (session == null)
        {
            return null;
        }

        return Accounts.FindPrimaryAccountId(session, capability);
    }

    /// <summary>Executes a complete low-level JMAP request.</summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the complete response.</returns>
    public Task<JmapResponse> ExecuteBatchAsync(
        JmapRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ExecuteBatch(request, cancellationToken), cancellationToken);
    }

    /// <summary>Executes one arbitrary standard or vendor method call.</summary>
    /// <param name="call">The raw method call.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the correlated call result.</returns>
    public Task<JmapCallResult> ExecuteRawCallAsync(
        JmapRawMethodCall call,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ExecuteRawCall(call, cancellationToken), cancellationToken);
    }

    /// <summary>Echoes an arbitrary JSON object through Core/echo.</summary>
    /// <param name="arguments">The object to echo.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the echoed object.</returns>
    public Task<JsonElement> ExecuteEchoAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ExecuteEcho(arguments, cancellationToken), cancellationToken);
    }

    /// <summary>Finds the first response with a call identifier.</summary>
    /// <param name="response">The response to search.</param>
    /// <param name="callId">The originating call identifier.</param>
    /// <returns>The raw and classified call result.</returns>
    public JmapCallResult FindCallResult(JmapResponse response, string callId)
    {
        return Responses.FindRawResult(response, callId);
    }

    /// <summary>Uploads binary content to an account.</summary>
    /// <param name="request">The upload request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing uploaded blob metadata.</returns>
    public Task<BlobUploadResponse> UploadBlobAsync(
        BlobUploadRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => UploadBlob(request, cancellationToken), cancellationToken);
    }

    /// <summary>Downloads binary content from an account.</summary>
    /// <param name="request">The download request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the streaming response.</returns>
    public Task<BlobDownloadResponse> DownloadBlobAsync(
        BlobDownloadRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => DownloadBlob(request, cancellationToken), cancellationToken);
    }

    /// <summary>Copies binary content between accounts.</summary>
    /// <param name="request">The Blob/copy arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the copy result.</returns>
    public Task<BlobCopyResponse> CopyBlobAsync(
        BlobCopyRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => CopyBlob(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets push subscriptions for the current credentials.</summary>
    /// <param name="request">The PushSubscription/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing matching subscriptions.</returns>
    public Task<PushSubscriptionGetResponse> GetPushSubscriptionsAsync(
        PushSubscriptionGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetPushSubscriptions(request, cancellationToken), cancellationToken);
    }

    /// <summary>Creates, updates, or destroys push subscriptions.</summary>
    /// <param name="request">The PushSubscription/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<PushSubscriptionSetResponse> SetPushSubscriptionsAsync(
        PushSubscriptionSetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetPushSubscriptions(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Mailboxes.</summary>
    /// <param name="request">The Mailbox/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the Mailboxes.</returns>
    public Task<MailboxGetResponse> GetMailboxesAsync(
        MailboxGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetMailboxes(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Mailbox changes.</summary>
    /// <param name="request">The Mailbox/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the changes.</returns>
    public Task<MailboxChangesResponse> GetMailboxChangesAsync(
        MailboxChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetMailboxChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets and combines all Mailbox changes pages.</summary>
    /// <param name="request">The initial Mailbox/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a deduplicated aggregate result.</returns>
    public Task<MailboxChangesResponse> GetAllMailboxChangesAsync(
        MailboxChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetAllMailboxChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Queries Mailboxes.</summary>
    /// <param name="request">The Mailbox/query arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the query result.</returns>
    public Task<MailboxQueryResponse> QueryMailboxesAsync(
        MailboxQueryRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => QueryMailboxes(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets changes to a Mailbox query.</summary>
    /// <param name="request">The Mailbox/queryChanges arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing query changes.</returns>
    public Task<MailboxQueryChangesResponse> GetMailboxQueryChangesAsync(
        MailboxQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetMailboxQueryChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Applies Mailbox query changes to an ordered query snapshot.</summary>
    /// <param name="snapshot">The snapshot at the old query state.</param>
    /// <param name="changes">The query changes to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    public JmapQuerySnapshot ContinueMailboxQuery(
        JmapQuerySnapshot snapshot,
        MailboxQueryChangesResponse changes)
    {
        if (changes == null)
        {
            throw ValidationFailure("Mailbox query changes are required.", "changes");
        }

        return QueryContinuations.ContinueQuery(
            snapshot, changes.AccountId, changes.OldQueryState, changes.NewQueryState,
            changes.Removed, changes.Added);
    }

    /// <summary>Creates, updates, or destroys Mailboxes.</summary>
    /// <param name="request">The Mailbox/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<MailboxSetResponse> SetMailboxesAsync(
        MailboxSetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetMailboxes(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Threads.</summary>
    /// <param name="request">The Thread/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the Threads.</returns>
    public Task<ThreadGetResponse> GetThreadsAsync(
        ThreadGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetThreads(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Thread changes.</summary>
    /// <param name="request">The Thread/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the changes.</returns>
    public Task<ThreadChangesResponse> GetThreadChangesAsync(
        ThreadChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetThreadChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets and combines all Thread changes pages.</summary>
    /// <param name="request">The initial Thread/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a deduplicated aggregate result.</returns>
    public Task<ThreadChangesResponse> GetAllThreadChangesAsync(
        ThreadChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetAllThreadChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Emails.</summary>
    /// <param name="request">The Email/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the Emails.</returns>
    public Task<EmailGetResponse> GetEmailsAsync(
        EmailGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Email changes.</summary>
    /// <param name="request">The Email/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the changes.</returns>
    public Task<EmailChangesResponse> GetEmailChangesAsync(
        EmailChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmailChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets and combines all Email changes pages.</summary>
    /// <param name="request">The initial Email/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a deduplicated aggregate result.</returns>
    public Task<EmailChangesResponse> GetAllEmailChangesAsync(
        EmailChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetAllEmailChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Queries Emails.</summary>
    /// <param name="request">The Email/query arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the query result.</returns>
    public Task<EmailQueryResponse> QueryEmailsAsync(
        EmailQueryRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => QueryEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets changes to an Email query.</summary>
    /// <param name="request">The Email/queryChanges arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing query changes.</returns>
    public Task<EmailQueryChangesResponse> GetEmailQueryChangesAsync(
        EmailQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmailQueryChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Applies Email query changes to an ordered query snapshot.</summary>
    /// <param name="snapshot">The snapshot at the old query state.</param>
    /// <param name="changes">The query changes to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    public JmapQuerySnapshot ContinueEmailQuery(
        JmapQuerySnapshot snapshot,
        EmailQueryChangesResponse changes)
    {
        if (changes == null)
        {
            throw ValidationFailure("Email query changes are required.", "changes");
        }

        return QueryContinuations.ContinueQuery(
            snapshot, changes.AccountId, changes.OldQueryState, changes.NewQueryState,
            changes.Removed, changes.Added);
    }

    /// <summary>Creates, updates, or destroys Emails.</summary>
    /// <param name="request">The Email/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<EmailSetResponse> SetEmailsAsync(
        EmailSetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Copies Emails between accounts.</summary>
    /// <param name="request">The Email/copy arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the copy result.</returns>
    public Task<EmailCopyResponse> CopyEmailsAsync(
        EmailCopyRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => CopyEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Imports uploaded messages.</summary>
    /// <param name="request">The Email/import arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the import result.</returns>
    public Task<EmailImportResponse> ImportEmailsAsync(
        EmailImportRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ImportEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Parses uploaded message blobs.</summary>
    /// <param name="request">The Email/parse arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing parsed messages.</returns>
    public Task<EmailParseResponse> ParseEmailsAsync(
        EmailParseRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ParseEmails(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets search snippets for Emails.</summary>
    /// <param name="request">The SearchSnippet/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing snippets.</returns>
    public Task<SearchSnippetGetResponse> GetSearchSnippetsAsync(
        SearchSnippetGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetSearchSnippets(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets sending Identities.</summary>
    /// <param name="request">The Identity/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing Identities.</returns>
    public Task<IdentityGetResponse> GetIdentitiesAsync(
        IdentityGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetIdentities(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets Identity changes.</summary>
    /// <param name="request">The Identity/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing changes.</returns>
    public Task<IdentityChangesResponse> GetIdentityChangesAsync(
        IdentityChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetIdentityChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets and combines all Identity changes pages.</summary>
    /// <param name="request">The initial Identity/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a deduplicated aggregate result.</returns>
    public Task<IdentityChangesResponse> GetAllIdentityChangesAsync(
        IdentityChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetAllIdentityChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Creates, updates, or destroys Identities.</summary>
    /// <param name="request">The Identity/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<IdentitySetResponse> SetIdentitiesAsync(
        IdentitySetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetIdentities(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing submissions.</returns>
    public Task<EmailSubmissionGetResponse> GetEmailSubmissionsAsync(
        EmailSubmissionGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmailSubmissions(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets EmailSubmission changes.</summary>
    /// <param name="request">The EmailSubmission/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing changes.</returns>
    public Task<EmailSubmissionChangesResponse> GetEmailSubmissionChangesAsync(
        EmailSubmissionChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmailSubmissionChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets and combines all EmailSubmission changes pages.</summary>
    /// <param name="request">The initial EmailSubmission/changes arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a deduplicated aggregate result.</returns>
    public Task<EmailSubmissionChangesResponse> GetAllEmailSubmissionChangesAsync(
        EmailSubmissionChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetAllEmailSubmissionChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Queries EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/query arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the query result.</returns>
    public Task<EmailSubmissionQueryResponse> QueryEmailSubmissionsAsync(
        EmailSubmissionQueryRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => QueryEmailSubmissions(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets changes to an EmailSubmission query.</summary>
    /// <param name="request">The EmailSubmission/queryChanges arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing query changes.</returns>
    public Task<EmailSubmissionQueryChangesResponse> GetEmailSubmissionQueryChangesAsync(
        EmailSubmissionQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetEmailSubmissionQueryChanges(request, cancellationToken), cancellationToken);
    }

    /// <summary>Applies EmailSubmission query changes to an ordered query snapshot.</summary>
    /// <param name="snapshot">The snapshot at the old query state.</param>
    /// <param name="changes">The query changes to apply.</param>
    /// <returns>A detached snapshot at the new query state.</returns>
    public JmapQuerySnapshot ContinueEmailSubmissionQuery(
        JmapQuerySnapshot snapshot,
        EmailSubmissionQueryChangesResponse changes)
    {
        if (changes == null)
        {
            throw ValidationFailure("EmailSubmission query changes are required.", "changes");
        }

        return QueryContinuations.ContinueQuery(
            snapshot, changes.AccountId, changes.OldQueryState, changes.NewQueryState,
            changes.Removed, changes.Added);
    }

    /// <summary>Creates, updates, or destroys EmailSubmission records.</summary>
    /// <param name="request">The EmailSubmission/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<EmailSubmissionSetResponse> SetEmailSubmissionsAsync(
        EmailSubmissionSetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetEmailSubmissions(request, cancellationToken), cancellationToken);
    }

    /// <summary>Gets the singleton vacation response.</summary>
    /// <param name="request">The VacationResponse/get arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the vacation response.</returns>
    public Task<VacationResponseGetResponse> GetVacationResponseAsync(
        VacationResponseGetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => GetVacationResponse(request, cancellationToken), cancellationToken);
    }

    /// <summary>Updates the singleton vacation response.</summary>
    /// <param name="request">The VacationResponse/set arguments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the set result.</returns>
    public Task<VacationResponseSetResponse> SetVacationResponseAsync(
        VacationResponseSetRequest request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SetVacationResponse(request, cancellationToken), cancellationToken);
    }

    internal JmapResponse ExecuteBatch(JmapRequest request, CancellationToken cancellationToken)
    {
        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        return SendBatch(session, request, cancellationToken);
    }

    internal JmapResponse SendBatch(
        JmapSession session,
        JmapRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.Using == null || request.MethodCalls == null ||
            request.UnknownProperties == null)
        {
            throw ValidationFailure("A complete JMAP request is required.", "request");
        }

        if (session == null || session.ApiUrl == null || session.ApiUrl.Length == 0)
        {
            throw ProtocolFailure("The cached Session has no usable API URL.", "apiUrl");
        }

        cancellationToken.ThrowIfCancellationRequested();
        JmapResponse response = Transport.SendTransportApiRequestAsync(
            session.ApiUrl, request, cancellationToken).GetAwaiter().GetResult();
        if (response == null || response.MethodResponses == null || response.UnknownProperties == null)
        {
            throw ProtocolFailure("The API transport returned an invalid JMAP response.", "response");
        }

        Sessions.ObserveResponseSessionState(response.SessionState);
        return response;
    }

    internal JmapCallResult ExecuteRawCall(
        JmapRawMethodCall call,
        CancellationToken cancellationToken)
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddRequestCapability(JmapCapabilities.Core);
        string callId = builder.AddRawMethodCall(call);
        JmapResponse response = ExecuteBatch(builder.BuildJmapRequest(), cancellationToken);
        return Responses.FindRawResult(response, callId);
    }

    internal JsonElement ExecuteEcho(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Core/echo arguments must be a JSON object.", "arguments");
        }

        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.CoreEcho;
        invocation.Arguments = arguments.Clone();
        invocation.CallId = SingleCallId;
        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Core, cancellationToken);
        return Echo.MapResponse(response, SingleCallId);
    }

    internal JmapResponse ExecuteInvocation(
        JmapSession session,
        JmapInvocation invocation,
        string capability,
        CancellationToken cancellationToken)
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            invocation.Name == null ? "" : invocation.Name,
            invocation.Arguments,
            RequiredCapabilities(capability),
            invocation.CallId);
        return SendBatch(session, builder.BuildJmapRequest(), cancellationToken);
    }

    internal List<string> RequiredCapabilities(string capability)
    {
        List<string> result = new List<string> { JmapCapabilities.Core };
        if (capability != JmapCapabilities.Core)
        {
            result.Add(capability);
        }

        return result;
    }

    internal BlobUploadResponse UploadBlob(
        BlobUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("A blob upload request is required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = Accounts.SelectAccountId(
            session, Options, request.AccountId, null, null, true);
        if (session.UploadUrl == null || session.UploadUrl.Length == 0)
        {
            throw ProtocolFailure("The cached Session has no usable upload URL.", "uploadUrl");
        }

        cancellationToken.ThrowIfCancellationRequested();
        BlobUploadResponse response = Transport.SendTransportUploadAsync(
            session.UploadUrl, request, cancellationToken).GetAwaiter().GetResult();
        ValidateUploadResponse(response, request);
        return response;
    }

    internal BlobDownloadResponse DownloadBlob(
        BlobDownloadRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("A blob download request is required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = Accounts.SelectAccountId(
            session, Options, request.AccountId, null, null, false);
        if (session.DownloadUrl == null || session.DownloadUrl.Length == 0)
        {
            throw ProtocolFailure("The cached Session has no usable download URL.", "downloadUrl");
        }

        cancellationToken.ThrowIfCancellationRequested();
        BlobDownloadResponse response = Transport.SendTransportDownloadAsync(
            session.DownloadUrl, request, cancellationToken).GetAwaiter().GetResult();
        ValidateDownloadResponse(response);
        return response;
    }

    internal BlobCopyResponse CopyBlob(BlobCopyRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Blob/copy arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.FromAccountId = Accounts.SelectAccountId(
            session, Options, request.FromAccountId, null, JmapMethods.BlobCopy, false);
        request.AccountId = Accounts.SelectAccountId(
            session, Options, request.AccountId, null, JmapMethods.BlobCopy, true);
        JmapInvocation invocation = BlobCopies.CreateInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Core, cancellationToken);
        return BlobCopies.MapResponse(response, SingleCallId, request);
    }

    internal void ValidateUploadResponse(BlobUploadResponse? response, BlobUploadRequest request)
    {
        if (response == null || response.AccountId != request.AccountId ||
            !Accounts.Values.IsId(response.BlobId) || response.Type == null || response.Type.Length == 0 ||
            !Accounts.Values.IsValidUnicode(response.Type) || !Accounts.Values.IsUnsignedInt(response.Size) ||
            response.UnknownProperties == null)
        {
            throw ProtocolFailure("The upload transport returned invalid blob metadata.", "uploadResponse");
        }
    }

    internal void ValidateDownloadResponse(BlobDownloadResponse? response)
    {
        if (response == null || response.Content == null || response.MediaType == null ||
            response.MediaType.Length == 0 || !Accounts.Values.IsValidUnicode(response.MediaType) ||
            response.HasContentLength && !Accounts.Values.IsUnsignedInt(response.ContentLength) ||
            response.FileName != null && !Accounts.Values.IsValidUnicode(response.FileName) ||
            response.CacheControl != null && !Accounts.Values.IsValidUnicode(response.CacheControl))
        {
            throw ProtocolFailure("The download transport returned invalid content metadata.",
                "downloadResponse");
        }
    }

    internal PushSubscriptionGetResponse GetPushSubscriptions(
        PushSubscriptionGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("PushSubscription/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        JmapInvocation invocation = PushSubscriptions.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Core, cancellationToken);
        return PushSubscriptions.MapGetResponse(response, SingleCallId, request);
    }

    internal PushSubscriptionSetResponse SetPushSubscriptions(
        PushSubscriptionSetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("PushSubscription/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        JmapInvocation invocation = PushSubscriptions.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Core, cancellationToken);
        return PushSubscriptions.MapSetResponse(response, SingleCallId, request);
    }

    internal MailboxGetResponse GetMailboxes(
        MailboxGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Mailbox/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.MailboxGet, false);
        JmapInvocation invocation = Mailboxes.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Mailboxes.MapGetResponse(response, SingleCallId, request);
    }

    internal MailboxChangesResponse GetMailboxChanges(
        MailboxChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Mailbox/changes arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.MailboxChanges, false);
        JmapInvocation invocation = Mailboxes.CreateChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Mailboxes.MapChangesResponse(response, SingleCallId, request);
    }

    internal MailboxChangesResponse GetAllMailboxChanges(
        MailboxChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.UnknownProperties == null)
        {
            throw ValidationFailure("Mailbox/changes arguments are required.", "request");
        }

        MailboxChangesRequest pageRequest = CloneMailboxChangesRequest(request);
        ChangesContinuation continuation = new ChangesContinuation();
        bool hasMoreChanges = true;
        while (hasMoreChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MailboxChangesResponse page = GetMailboxChanges(pageRequest, cancellationToken);
            continuation.AddPage(
                page.AccountId, page.OldState, page.NewState, page.HasMoreChanges,
                page.Created, page.Updated, page.Destroyed, page.UnknownProperties);
            continuation.AddUpdatedProperties(page.UpdatedProperties);
            hasMoreChanges = page.HasMoreChanges;
            pageRequest.SinceState = page.NewState;
        }

        MailboxChangesResponse result = new MailboxChangesResponse();
        result.AccountId = continuation.AccountId;
        result.OldState = continuation.OldState;
        result.NewState = continuation.NewState;
        result.HasMoreChanges = continuation.HasMoreChanges;
        continuation.CopyIds(result.Created, result.Updated, result.Destroyed);
        if (continuation.HasKnownUpdatedProperties)
        {
            result.UpdatedProperties = new List<string>(continuation.UpdatedProperties);
        }

        result.UnknownProperties = new Dictionary<string, JsonElement>(continuation.UnknownProperties);
        return result;
    }

    internal MailboxQueryResponse QueryMailboxes(
        MailboxQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Mailbox/query arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.MailboxQuery, false);
        JmapInvocation invocation = Mailboxes.CreateQueryInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Mailboxes.MapQueryResponse(response, SingleCallId, request);
    }

    internal MailboxQueryChangesResponse GetMailboxQueryChanges(
        MailboxQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Mailbox/queryChanges arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(
            session, request.AccountId, JmapMethods.MailboxQueryChanges, false);
        JmapInvocation invocation = Mailboxes.CreateQueryChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Mailboxes.MapQueryChangesResponse(response, SingleCallId, request);
    }

    internal MailboxSetResponse SetMailboxes(
        MailboxSetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Mailbox/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.MailboxSet, true);
        JmapInvocation invocation = Mailboxes.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Mailboxes.MapSetResponse(response, SingleCallId, request);
    }

    internal ThreadGetResponse GetThreads(
        ThreadGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Thread/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.ThreadGet, false);
        JmapInvocation invocation = Threads.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Threads.MapGetResponse(response, SingleCallId, request);
    }

    internal ThreadChangesResponse GetThreadChanges(
        ThreadChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Thread/changes arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.ThreadChanges, false);
        JmapInvocation invocation = Threads.CreateChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Threads.MapChangesResponse(response, SingleCallId, request);
    }

    internal ThreadChangesResponse GetAllThreadChanges(
        ThreadChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.UnknownProperties == null)
        {
            throw ValidationFailure("Thread/changes arguments are required.", "request");
        }

        ThreadChangesRequest pageRequest = CloneThreadChangesRequest(request);
        ChangesContinuation continuation = new ChangesContinuation();
        bool hasMoreChanges = true;
        while (hasMoreChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThreadChangesResponse page = GetThreadChanges(pageRequest, cancellationToken);
            continuation.AddPage(
                page.AccountId, page.OldState, page.NewState, page.HasMoreChanges,
                page.Created, page.Updated, page.Destroyed, page.UnknownProperties);
            hasMoreChanges = page.HasMoreChanges;
            pageRequest.SinceState = page.NewState;
        }

        ThreadChangesResponse result = new ThreadChangesResponse();
        result.AccountId = continuation.AccountId;
        result.OldState = continuation.OldState;
        result.NewState = continuation.NewState;
        result.HasMoreChanges = continuation.HasMoreChanges;
        continuation.CopyIds(result.Created, result.Updated, result.Destroyed);
        result.UnknownProperties = new Dictionary<string, JsonElement>(continuation.UnknownProperties);
        return result;
    }

    internal EmailGetResponse GetEmails(EmailGetRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailGet, false);
        JmapInvocation invocation = Emails.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapGetResponse(response, SingleCallId, request);
    }

    internal EmailChangesResponse GetEmailChanges(
        EmailChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/changes arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailChanges, false);
        JmapInvocation invocation = Emails.CreateChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapChangesResponse(response, SingleCallId, request);
    }

    internal EmailChangesResponse GetAllEmailChanges(
        EmailChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.UnknownProperties == null)
        {
            throw ValidationFailure("Email/changes arguments are required.", "request");
        }

        EmailChangesRequest pageRequest = CloneEmailChangesRequest(request);
        ChangesContinuation continuation = new ChangesContinuation();
        bool hasMoreChanges = true;
        while (hasMoreChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmailChangesResponse page = GetEmailChanges(pageRequest, cancellationToken);
            continuation.AddPage(
                page.AccountId, page.OldState, page.NewState, page.HasMoreChanges,
                page.Created, page.Updated, page.Destroyed, page.UnknownProperties);
            hasMoreChanges = page.HasMoreChanges;
            pageRequest.SinceState = page.NewState;
        }

        EmailChangesResponse result = new EmailChangesResponse();
        result.AccountId = continuation.AccountId;
        result.OldState = continuation.OldState;
        result.NewState = continuation.NewState;
        result.HasMoreChanges = continuation.HasMoreChanges;
        continuation.CopyIds(result.Created, result.Updated, result.Destroyed);
        result.UnknownProperties = new Dictionary<string, JsonElement>(continuation.UnknownProperties);
        return result;
    }

    internal EmailQueryResponse QueryEmails(
        EmailQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/query arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailQuery, false);
        JmapInvocation invocation = Emails.CreateQueryInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapQueryResponse(response, SingleCallId, request);
    }

    internal EmailQueryChangesResponse GetEmailQueryChanges(
        EmailQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/queryChanges arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(
            session, request.AccountId, JmapMethods.EmailQueryChanges, false);
        JmapInvocation invocation = Emails.CreateQueryChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapQueryChangesResponse(response, SingleCallId, request);
    }

    internal EmailSetResponse SetEmails(EmailSetRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailSet, true);
        JmapInvocation invocation = Emails.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapSetResponse(response, SingleCallId, request);
    }

    internal EmailCopyResponse CopyEmails(EmailCopyRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/copy arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.FromAccountId = SelectMailAccount(
            session, request.FromAccountId, JmapMethods.EmailCopy, false);
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailCopy, true);
        JmapInvocation invocation = Emails.CreateCopyInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapCopyResponse(response, SingleCallId, request);
    }

    internal EmailImportResponse ImportEmails(
        EmailImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/import arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailImport, true);
        JmapInvocation invocation = Emails.CreateImportInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapImportResponse(response, SingleCallId, request);
    }

    internal EmailParseResponse ParseEmails(
        EmailParseRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Email/parse arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(session, request.AccountId, JmapMethods.EmailParse, false);
        JmapInvocation invocation = Emails.CreateParseInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Emails.MapParseResponse(response, SingleCallId, request);
    }

    internal SearchSnippetGetResponse GetSearchSnippets(
        SearchSnippetGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("SearchSnippet/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectMailAccount(
            session, request.AccountId, JmapMethods.SearchSnippetGet, false);
        JmapInvocation invocation = Snippets.CreateInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Mail, cancellationToken);
        return Snippets.MapResponse(response, SingleCallId, request);
    }

    internal IdentityGetResponse GetIdentities(
        IdentityGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Identity/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.IdentityGet, false);
        JmapInvocation invocation = Identities.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Identities.MapGetResponse(response, SingleCallId, request);
    }

    internal IdentityChangesResponse GetIdentityChanges(
        IdentityChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Identity/changes arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.IdentityChanges, false);
        JmapInvocation invocation = Identities.CreateChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Identities.MapChangesResponse(response, SingleCallId, request);
    }

    internal IdentityChangesResponse GetAllIdentityChanges(
        IdentityChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.UnknownProperties == null)
        {
            throw ValidationFailure("Identity/changes arguments are required.", "request");
        }

        IdentityChangesRequest pageRequest = CloneIdentityChangesRequest(request);
        ChangesContinuation continuation = new ChangesContinuation();
        bool hasMoreChanges = true;
        while (hasMoreChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IdentityChangesResponse page = GetIdentityChanges(pageRequest, cancellationToken);
            continuation.AddPage(
                page.AccountId, page.OldState, page.NewState, page.HasMoreChanges,
                page.Created, page.Updated, page.Destroyed, page.UnknownProperties);
            hasMoreChanges = page.HasMoreChanges;
            pageRequest.SinceState = page.NewState;
        }

        IdentityChangesResponse result = new IdentityChangesResponse();
        result.AccountId = continuation.AccountId;
        result.OldState = continuation.OldState;
        result.NewState = continuation.NewState;
        result.HasMoreChanges = continuation.HasMoreChanges;
        continuation.CopyIds(result.Created, result.Updated, result.Destroyed);
        result.UnknownProperties = new Dictionary<string, JsonElement>(continuation.UnknownProperties);
        return result;
    }

    internal IdentitySetResponse SetIdentities(
        IdentitySetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("Identity/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.IdentitySet, true);
        JmapInvocation invocation = Identities.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Identities.MapSetResponse(response, SingleCallId, request);
    }

    internal EmailSubmissionGetResponse GetEmailSubmissions(
        EmailSubmissionGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("EmailSubmission/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.EmailSubmissionGet, false);
        JmapInvocation invocation = Submissions.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Submissions.MapGetResponse(response, SingleCallId, request);
    }

    internal EmailSubmissionChangesResponse GetEmailSubmissionChanges(
        EmailSubmissionChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("EmailSubmission/changes arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.EmailSubmissionChanges, false);
        JmapInvocation invocation = Submissions.CreateChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Submissions.MapChangesResponse(response, SingleCallId, request);
    }

    internal EmailSubmissionChangesResponse GetAllEmailSubmissionChanges(
        EmailSubmissionChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.UnknownProperties == null)
        {
            throw ValidationFailure("EmailSubmission/changes arguments are required.", "request");
        }

        EmailSubmissionChangesRequest pageRequest = CloneEmailSubmissionChangesRequest(request);
        ChangesContinuation continuation = new ChangesContinuation();
        bool hasMoreChanges = true;
        while (hasMoreChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmailSubmissionChangesResponse page = GetEmailSubmissionChanges(
                pageRequest, cancellationToken);
            continuation.AddPage(
                page.AccountId, page.OldState, page.NewState, page.HasMoreChanges,
                page.Created, page.Updated, page.Destroyed, page.UnknownProperties);
            hasMoreChanges = page.HasMoreChanges;
            pageRequest.SinceState = page.NewState;
        }

        EmailSubmissionChangesResponse result = new EmailSubmissionChangesResponse();
        result.AccountId = continuation.AccountId;
        result.OldState = continuation.OldState;
        result.NewState = continuation.NewState;
        result.HasMoreChanges = continuation.HasMoreChanges;
        continuation.CopyIds(result.Created, result.Updated, result.Destroyed);
        result.UnknownProperties = new Dictionary<string, JsonElement>(continuation.UnknownProperties);
        return result;
    }

    internal EmailSubmissionQueryResponse QueryEmailSubmissions(
        EmailSubmissionQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("EmailSubmission/query arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.EmailSubmissionQuery, false);
        JmapInvocation invocation = Submissions.CreateQueryInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Submissions.MapQueryResponse(response, SingleCallId, request);
    }

    internal EmailSubmissionQueryChangesResponse GetEmailSubmissionQueryChanges(
        EmailSubmissionQueryChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("EmailSubmission/queryChanges arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.EmailSubmissionQueryChanges, false);
        JmapInvocation invocation = Submissions.CreateQueryChangesInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Submissions.MapQueryChangesResponse(response, SingleCallId, request);
    }

    internal EmailSubmissionSetResponse SetEmailSubmissions(
        EmailSubmissionSetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("EmailSubmission/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectSubmissionAccount(
            session, request.AccountId, JmapMethods.EmailSubmissionSet, true);
        JmapInvocation invocation = Submissions.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.Submission, cancellationToken);
        return Submissions.MapSetResponse(response, SingleCallId, request);
    }

    internal VacationResponseGetResponse GetVacationResponse(
        VacationResponseGetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("VacationResponse/get arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectVacationAccount(
            session, request.AccountId, JmapMethods.VacationResponseGet, false);
        JmapInvocation invocation = Vacations.CreateGetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.VacationResponse, cancellationToken);
        return Vacations.MapGetResponse(response, SingleCallId, request);
    }

    internal VacationResponseSetResponse SetVacationResponse(
        VacationResponseSetRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw ValidationFailure("VacationResponse/set arguments are required.", "request");
        }

        JmapSession session = Sessions.GetUsableSessionAsync(cancellationToken).GetAwaiter().GetResult();
        request.AccountId = SelectVacationAccount(
            session, request.AccountId, JmapMethods.VacationResponseSet, true);
        JmapInvocation invocation = Vacations.CreateSetInvocation(request, SingleCallId);
        JmapResponse response = ExecuteInvocation(
            session, invocation, JmapCapabilities.VacationResponse, cancellationToken);
        return Vacations.MapSetResponse(response, SingleCallId, request);
    }

    internal MailboxChangesRequest CloneMailboxChangesRequest(MailboxChangesRequest source)
    {
        MailboxChangesRequest result = new MailboxChangesRequest();
        result.AccountId = source.AccountId;
        result.SinceState = source.SinceState;
        result.MaxChanges = source.MaxChanges;
        result.HasMaxChanges = source.HasMaxChanges;
        result.IsMaxChangesNull = source.IsMaxChangesNull;
        result.UnknownProperties = new Dictionary<string, JsonElement>(source.UnknownProperties);
        return result;
    }

    internal ThreadChangesRequest CloneThreadChangesRequest(ThreadChangesRequest source)
    {
        ThreadChangesRequest result = new ThreadChangesRequest();
        result.AccountId = source.AccountId;
        result.SinceState = source.SinceState;
        result.MaxChanges = source.MaxChanges;
        result.HasMaxChanges = source.HasMaxChanges;
        result.IsMaxChangesNull = source.IsMaxChangesNull;
        result.UnknownProperties = new Dictionary<string, JsonElement>(source.UnknownProperties);
        return result;
    }

    internal EmailChangesRequest CloneEmailChangesRequest(EmailChangesRequest source)
    {
        EmailChangesRequest result = new EmailChangesRequest();
        result.AccountId = source.AccountId;
        result.SinceState = source.SinceState;
        result.MaxChanges = source.MaxChanges;
        result.HasMaxChanges = source.HasMaxChanges;
        result.IsMaxChangesNull = source.IsMaxChangesNull;
        result.UnknownProperties = new Dictionary<string, JsonElement>(source.UnknownProperties);
        return result;
    }

    internal IdentityChangesRequest CloneIdentityChangesRequest(IdentityChangesRequest source)
    {
        IdentityChangesRequest result = new IdentityChangesRequest();
        result.AccountId = source.AccountId;
        result.SinceState = source.SinceState;
        result.MaxChanges = source.MaxChanges;
        result.HasMaxChanges = source.HasMaxChanges;
        result.IsMaxChangesNull = source.IsMaxChangesNull;
        result.UnknownProperties = new Dictionary<string, JsonElement>(source.UnknownProperties);
        return result;
    }

    internal EmailSubmissionChangesRequest CloneEmailSubmissionChangesRequest(
        EmailSubmissionChangesRequest source)
    {
        EmailSubmissionChangesRequest result = new EmailSubmissionChangesRequest();
        result.AccountId = source.AccountId;
        result.SinceState = source.SinceState;
        result.MaxChanges = source.MaxChanges;
        result.HasMaxChanges = source.HasMaxChanges;
        result.IsMaxChangesNull = source.IsMaxChangesNull;
        result.UnknownProperties = new Dictionary<string, JsonElement>(source.UnknownProperties);
        return result;
    }

    internal string SelectMailAccount(
        JmapSession session,
        string? accountId,
        string methodName,
        bool requireWritable)
    {
        return Accounts.SelectAccountId(
            session, Options, accountId, JmapCapabilities.Mail, methodName, requireWritable);
    }

    internal string SelectSubmissionAccount(
        JmapSession session,
        string? accountId,
        string methodName,
        bool requireWritable)
    {
        return Accounts.SelectAccountId(
            session, Options, accountId, JmapCapabilities.Submission, methodName, requireWritable);
    }

    internal string SelectVacationAccount(
        JmapSession session,
        string? accountId,
        string methodName,
        bool requireWritable)
    {
        return Accounts.SelectAccountId(
            session, Options, accountId, JmapCapabilities.VacationResponse, methodName,
            requireWritable);
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message, string path)
    {
        return new JmapProtocolException(message, path, default(JsonElement));
    }
}
