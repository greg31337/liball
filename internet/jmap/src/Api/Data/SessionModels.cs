using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Describes the capabilities, accounts, and endpoints available to an authenticated JMAP user.
/// </summary>
public class JmapSession
{
    /// <summary>Maps advertised capability URIs to their capability data.</summary>
    public Dictionary<string, JmapCapability> Capabilities = new Dictionary<string, JmapCapability>();

    /// <summary>Maps accessible account identifiers to account data.</summary>
    public Dictionary<string, JmapAccount> Accounts = new Dictionary<string, JmapAccount>();

    /// <summary>Maps capability URIs to the primary account identifier for that capability.</summary>
    public Dictionary<string, string> PrimaryAccounts = new Dictionary<string, string>();

    /// <summary>The username associated with the authenticated credentials.</summary>
    public string? Username;

    /// <summary>The URL used for JMAP API requests.</summary>
    public string? ApiUrl;

    /// <summary>The URI template used to download binary data.</summary>
    public string? DownloadUrl;

    /// <summary>The URI template used to upload binary data.</summary>
    public string? UploadUrl;

    /// <summary>The URI template used to connect to the EventSource endpoint.</summary>
    public string? EventSourceUrl;

    /// <summary>The current state of the Session resource.</summary>
    public string? State;

    /// <summary>Stores unrecognized top-level Session properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes one account accessible to the authenticated JMAP user.
/// </summary>
public class JmapAccount
{
    /// <summary>The user-visible account name.</summary>
    public string? Name;

    /// <summary>Whether the account belongs to the authenticated user.</summary>
    public bool IsPersonal;

    /// <summary>Whether the entire account is read-only.</summary>
    public bool IsReadOnly;

    /// <summary>Maps capability URIs to the capabilities available for this account.</summary>
    public Dictionary<string, JmapCapability> AccountCapabilities = new Dictionary<string, JmapCapability>();

    /// <summary>Stores unrecognized Account properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Holds typed data for a known capability or raw properties for an unknown capability.
/// </summary>
public class JmapCapability
{
    /// <summary>The JMAP Core capability data, when this represents the Core capability.</summary>
    public JmapCoreCapabilities? Core;

    /// <summary>The JMAP Mail capability data, when this represents an account Mail capability.</summary>
    public JmapMailCapabilities? Mail;

    /// <summary>The JMAP Submission capability data, when this represents an account Submission capability.</summary>
    public JmapSubmissionCapabilities? Submission;

    /// <summary>The Vacation Response capability data, when this represents that capability.</summary>
    public JmapVacationResponseCapabilities? VacationResponse;

    /// <summary>Stores raw members for an unknown capability object.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes server-wide limits advertised by the JMAP Core capability.
/// </summary>
public class JmapCoreCapabilities
{
    /// <summary>The maximum accepted size of one upload, in octets.</summary>
    public long MaxSizeUpload;

    /// <summary>The maximum number of concurrent upload requests.</summary>
    public long MaxConcurrentUpload;

    /// <summary>The maximum accepted size of one API request, in octets.</summary>
    public long MaxSizeRequest;

    /// <summary>The maximum number of concurrent API requests.</summary>
    public long MaxConcurrentRequests;

    /// <summary>The maximum number of method calls in one API request.</summary>
    public long MaxCallsInRequest;

    /// <summary>The maximum number of objects requested by one get call.</summary>
    public long MaxObjectsInGet;

    /// <summary>The maximum combined number of objects acted on by one set call.</summary>
    public long MaxObjectsInSet;

    /// <summary>The collation algorithm identifiers supported for query sorting.</summary>
    public List<string> CollationAlgorithms = new List<string>();

    /// <summary>Stores unrecognized Core capability properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes limits and permissions advertised by an account's JMAP Mail capability.
/// </summary>
public class JmapMailCapabilities
{
    /// <summary>The maximum number of Mailboxes assignable to one Email when present and not null.</summary>
    public long MaxMailboxesPerEmail;

    /// <summary>Whether the maxMailboxesPerEmail property was present.</summary>
    public bool HasMaxMailboxesPerEmail;

    /// <summary>Whether the present maxMailboxesPerEmail property was explicitly null.</summary>
    public bool IsMaxMailboxesPerEmailNull;

    /// <summary>The maximum Mailbox hierarchy depth when present and not null.</summary>
    public long MaxMailboxDepth;

    /// <summary>Whether the maxMailboxDepth property was present.</summary>
    public bool HasMaxMailboxDepth;

    /// <summary>Whether the present maxMailboxDepth property was explicitly null.</summary>
    public bool IsMaxMailboxDepthNull;

    /// <summary>The maximum UTF-8 size of a Mailbox name, in octets.</summary>
    public long MaxSizeMailboxName;

    /// <summary>The maximum total unencoded attachment size for one Email, in octets.</summary>
    public long MaxSizeAttachmentsPerEmail;

    /// <summary>The Email properties supported for query sorting.</summary>
    public List<string> EmailQuerySortOptions = new List<string>();

    /// <summary>Whether the user may create a top-level Mailbox in the account.</summary>
    public bool MayCreateTopLevelMailbox;

    /// <summary>Stores unrecognized Mail capability properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes delayed-send and SMTP extension support for an account's JMAP Submission capability.
/// </summary>
public class JmapSubmissionCapabilities
{
    /// <summary>The maximum supported delay before sending, in seconds.</summary>
    public long MaxDelayedSend;

    /// <summary>Maps supported SMTP EHLO extension names to their arguments.</summary>
    public Dictionary<string, List<string>> SubmissionExtensions = new Dictionary<string, List<string>>();

    /// <summary>Stores unrecognized Submission capability properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents the empty JMAP Vacation Response capability object and any extension properties.
/// </summary>
public class JmapVacationResponseCapabilities
{
    /// <summary>Stores unrecognized Vacation Response capability properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Configures direct or username-domain discovery of a JMAP Session resource.
/// </summary>
public class JmapDiscoveryOptions
{
    /// <summary>The directly supplied Session URL, or null to discover it from Username.</summary>
    public string? SessionUrl;

    /// <summary>The username whose domain is used for DNS SRV discovery.</summary>
    public string? Username;

    /// <summary>The maximum number of HTTP redirects followed during Session discovery.</summary>
    public int MaximumRedirects = 5;

    /// <summary>The DNS discovery timeout, in milliseconds.</summary>
    public int DnsTimeoutMilliseconds = 10000;

    /// <summary>Whether non-TLS endpoint URLs are permitted for explicit local or test configurations.</summary>
    public bool AllowInsecureHttp;
}

/// <summary>
/// Configures Session discovery, account selection, and Session cache behavior for a JMAP client.
/// </summary>
public class JmapClientOptions
{
    /// <summary>The options used to locate the JMAP Session resource.</summary>
    public JmapDiscoveryOptions DiscoveryOptions = new JmapDiscoveryOptions();

    /// <summary>The service locator used for direct or username-domain Session discovery, or null to use DNS discovery.</summary>
    public IJmapServiceLocator? ServiceLocator;

    /// <summary>An explicit default account identifier, or null to use a capability-specific primary account.</summary>
    public string? DefaultAccountId;

    /// <summary>The capability URI used to select a primary account when no explicit default is set.</summary>
    public string? DefaultAccountCapability = JmapCapabilities.Mail;

    /// <summary>Whether a changed API sessionState causes the cached Session to be refreshed before later operations.</summary>
    public bool RefreshSessionOnStateChange = true;
}
