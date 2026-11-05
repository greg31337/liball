using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace jmap;

/// <summary>
/// Configures protocol, streaming, concurrency, retention, and push limits for a JMAP server.
/// </summary>
public class JmapServerOptions
{
    /// <summary>The maximum accepted API request body size in octets.</summary>
    public long MaxRequestBytes = JmapProtocolLimits.SuggestedMinimumMaxSizeRequest;

    /// <summary>The maximum accepted upload body size in octets.</summary>
    public long MaxUploadBytes = JmapProtocolLimits.SuggestedMinimumMaxSizeUpload;

    /// <summary>The maximum permitted JSON nesting depth.</summary>
    public int MaxJsonDepth = 64;

    /// <summary>The maximum number of concurrently processed API requests.</summary>
    public int MaxConcurrentRequests = JmapProtocolLimits.SuggestedMinimumMaxConcurrentRequests;

    /// <summary>The maximum number of concurrently processed uploads.</summary>
    public int MaxConcurrentUploads = JmapProtocolLimits.SuggestedMinimumMaxConcurrentUpload;

    /// <summary>The maximum number of method calls accepted in one API request.</summary>
    public int MaxCallsInRequest = JmapProtocolLimits.SuggestedMinimumMaxCallsInRequest;

    /// <summary>The maximum number of objects accepted by one get operation.</summary>
    public int MaxObjectsInGet = JmapProtocolLimits.SuggestedMinimumMaxObjectsInGet;

    /// <summary>The maximum combined object actions accepted by one set operation.</summary>
    public int MaxObjectsInSet = JmapProtocolLimits.SuggestedMinimumMaxObjectsInSet;

    /// <summary>The quota for unreferenced blobs attributable to one user, in octets.</summary>
    public long UnreferencedBlobQuotaBytes = JmapProtocolLimits.SuggestedMinimumMaxSizeUpload;

    /// <summary>The minimum unreferenced blob retention time in seconds unless quota forces eviction.</summary>
    public int UnreferencedBlobMinimumLifetimeSeconds = JmapProtocolLimits.MinimumUnreferencedBlobLifetimeSeconds;

    /// <summary>The minimum EventSource ping interval accepted from a client, in seconds.</summary>
    public int MinimumEventSourcePingSeconds = JmapProtocolLimits.MaximumEventSourceMinimumPingIntervalSeconds;

    /// <summary>The maximum EventSource ping interval accepted from a client, in seconds.</summary>
    public int MaximumEventSourcePingSeconds = JmapProtocolLimits.MinimumEventSourceMaximumPingIntervalSeconds;

    /// <summary>The default push-message time-to-live in seconds.</summary>
    public int PushTimeToLiveSeconds = 86400;

    /// <summary>The maximum lifetime assigned to a push subscription, in seconds.</summary>
    public int MaximumPushSubscriptionLifetimeSeconds = JmapProtocolLimits.SuggestedPushSubscriptionMaximumLifetimeSeconds;

    /// <summary>Whether insecure HTTP endpoints are permitted for explicit local or test use.</summary>
    public bool AllowInsecureHttp;
}

/// <summary>
/// Collects caller-supplied backend services and server policy hooks.
/// </summary>
public class JmapServerServices
{
    /// <summary>Additional capability URIs accepted by the server.</summary>
    public List<string> VendorCapabilities = new List<string>();

    /// <summary>Application-defined method registrations dispatched as opaque JSON.</summary>
    public List<JmapVendorMethodRegistration> VendorMethods =
        new List<JmapVendorMethodRegistration>();

    /// <summary>Provides Session resources for authenticated requests.</summary>
    public IJmapSessionService? SessionService;

    /// <summary>Processes JMAP Core operations.</summary>
    public IJmapCoreService? CoreService;

    /// <summary>Stores, opens, and copies immutable blobs.</summary>
    public IBlobService? BlobService;

    /// <summary>Stores and updates push subscriptions.</summary>
    public IPushService? PushService;

    /// <summary>The HTTP client used for outbound push verification and state-change requests, or null to use the shared default client.</summary>
    public HttpClient? PushHttpClient;

    /// <summary>Processes Mailbox operations.</summary>
    public IMailboxService? MailboxService;

    /// <summary>Processes Thread operations.</summary>
    public IThreadService? ThreadService;

    /// <summary>Processes Email operations.</summary>
    public IEmailService? EmailService;

    /// <summary>Produces Email search snippets.</summary>
    public ISearchSnippetService? SearchSnippetService;

    /// <summary>Processes Identity operations.</summary>
    public IIdentityService? IdentityService;

    /// <summary>Processes EmailSubmission operations.</summary>
    public IEmailSubmissionService? EmailSubmissionService;

    /// <summary>Processes VacationResponse operations.</summary>
    public IVacationResponseService? VacationResponseService;

    /// <summary>Publishes state changes to connected and external push consumers.</summary>
    public IJmapStatePublisher? StatePublisher;

    /// <summary>Provides deterministic UTC time.</summary>
    public IJmapClock? Clock;

    /// <summary>Approves or rejects push callback destinations.</summary>
    public IJmapPushUrlPolicy? PushUrlPolicy;

    /// <summary>Localizes genuinely user-visible strings.</summary>
    public IJmapStringLocalizer? StringLocalizer;
}

/// <summary>
/// Carries authenticated, transport-neutral context for one server request.
/// </summary>
public class JmapRequestContext
{
    /// <summary>The authenticated user identifier.</summary>
    public string? UserId;

    /// <summary>The stable credential identifier used to own private resources such as push subscriptions.</summary>
    public string? CredentialId;

    /// <summary>Whether the credentials used for this request have been revoked.</summary>
    public bool CredentialIsRevoked;

    /// <summary>The UTC expiry of the credentials when they are time limited.</summary>
    public DateTimeOffset CredentialExpiresUtc;

    /// <summary>Whether <see cref="CredentialExpiresUtc"/> contains a credential expiry.</summary>
    public bool HasCredentialExpiresUtc;

    /// <summary>The request's Authorization value when authentication is delegated to a service.</summary>
    public string? Authorization;

    /// <summary>The request's Accept-Language value.</summary>
    public string? AcceptLanguage;

    /// <summary>The remote peer address supplied by the host.</summary>
    public string? RemoteAddress;

    /// <summary>Whether the host received the request over a secure transport.</summary>
    public bool IsSecure;

    /// <summary>Stores host-provided request context values.</summary>
    public Dictionary<string, string> Values = new Dictionary<string, string>();
}

/// <summary>
/// Represents a host-neutral HTTP-style request to a JMAP server endpoint.
/// </summary>
public class JmapEndpointRequest
{
    /// <summary>The authenticated request context.</summary>
    public JmapRequestContext Context = new JmapRequestContext();

    /// <summary>The HTTP method supplied by the host.</summary>
    public string? Method;

    /// <summary>The absolute or host-relative request URL.</summary>
    public string? Url;

    /// <summary>The request Content-Type value.</summary>
    public string? ContentType;

    /// <summary>The request body stream, or null for an endpoint without a body.</summary>
    public Stream? Body;

    /// <summary>The request body size in octets when known.</summary>
    public long ContentLength;

    /// <summary>Whether the request body size is known.</summary>
    public bool HasContentLength;

    /// <summary>The request headers using case-insensitive semantics supplied by the host adapter.</summary>
    public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The decoded route variables such as accountId and blobId.</summary>
    public Dictionary<string, string> RouteValues = new Dictionary<string, string>();

    /// <summary>The decoded query parameters such as EventSource options.</summary>
    public Dictionary<string, string> QueryParameters = new Dictionary<string, string>();
}

/// <summary>
/// Represents a host-neutral HTTP-style response from a JMAP server endpoint.
/// </summary>
public class JmapEndpointResponse
{
    /// <summary>The HTTP status code.</summary>
    public int StatusCode;

    /// <summary>The response Content-Type value.</summary>
    public string? ContentType;

    /// <summary>A buffered response body, or null when the body is absent or streamed.</summary>
    public byte[]? BodyBytes;

    /// <summary>A streaming response body, or null when the body is absent or buffered.</summary>
    public Stream? BodyStream;

    /// <summary>The response body size in octets when known.</summary>
    public long ContentLength;

    /// <summary>Whether the response body size is known.</summary>
    public bool HasContentLength;

    /// <summary>The response headers to apply through the host.</summary>
    public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Contains authorized immutable blob content returned by a backend service.
/// </summary>
public class JmapDownloadContent
{
    /// <summary>The stream containing the raw immutable blob bytes.</summary>
    public Stream? Content;

    /// <summary>The safe media type to return.</summary>
    public string? MediaType;

    /// <summary>The safe response filename.</summary>
    public string? FileName;

    /// <summary>The blob size in octets when known.</summary>
    public long Size;

    /// <summary>Whether the blob size is known.</summary>
    public bool HasSize;

    /// <summary>The immutable private cache lifetime in seconds.</summary>
    public int CacheLifetimeSeconds = 31536000;
}

/// <summary>
/// Describes unreferenced blob storage attributed to the authenticated uploader.
/// </summary>
public class JmapUnreferencedBlob
{
    /// <summary>The account containing the unreferenced blob.</summary>
    public string? AccountId;

    /// <summary>The immutable identifier of the unreferenced blob.</summary>
    public string? BlobId;

    /// <summary>The size of the unreferenced blob in octets.</summary>
    public long Size;

    /// <summary>The UTC time at which the blob was most recently uploaded.</summary>
    public DateTimeOffset UploadedAtUtc;
}

/// <summary>
/// Describes an account state change for EventSource and external push publication.
/// </summary>
public class JmapStatePublication
{
    /// <summary>The event identifier representing the complete visible server state.</summary>
    public string? EventId;

    /// <summary>The coalesced account-to-data-type state changes.</summary>
    public StateChange StateChange = new StateChange();

    /// <summary>The interested data types, or null when publication is not pre-filtered.</summary>
    public List<string>? Types;

    /// <summary>Whether the publication represents newly delivered email and may include EmailDelivery state.</summary>
    public bool HasNewEmailDelivery;
}
