using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Maps changed data-type states for every affected JMAP account.
/// </summary>
public class StateChange
{
    /// <summary>The object type discriminator, which is <c>StateChange</c> on the wire.</summary>
    public string? Type = "StateChange";

    /// <summary>Maps account identifiers to data-type names and their current state strings.</summary>
    public Dictionary<string, Dictionary<string, string>> Changed = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>Stores unrecognized StateChange properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the client-generated RFC 8291 encryption key material for a push subscription.
/// </summary>
public class PushSubscriptionKeys
{
    /// <summary>The URL-safe base64-encoded P-256 ECDH public key.</summary>
    public string? P256dh;

    /// <summary>The URL-safe base64-encoded authentication secret.</summary>
    public string? Auth;

    /// <summary>Stores unrecognized push-key properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes a callback registration for JMAP push notifications.
/// </summary>
public class PushSubscription
{
    /// <summary>The server-assigned subscription identifier.</summary>
    public string? Id;

    /// <summary>The identifier for the client and device creating the subscription.</summary>
    public string? DeviceClientId;

    /// <summary>The HTTPS URL to which push messages are posted.</summary>
    public string? Url;

    /// <summary>The optional encryption keys, or null for unencrypted push messages.</summary>
    public PushSubscriptionKeys? Keys;

    /// <summary>The verification code used to activate the subscription.</summary>
    public string? VerificationCode;

    /// <summary>The UTC date-time after which the subscription expires.</summary>
    public string? Expires;

    /// <summary>The interested data-type names, or null to receive all types.</summary>
    public List<string>? Types;

    /// <summary>Stores unrecognized PushSubscription properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents the callback sent immediately after a push subscription is created.
/// </summary>
public class PushVerification
{
    /// <summary>The object type discriminator, which is <c>PushVerification</c> on the wire.</summary>
    public string? Type = "PushVerification";

    /// <summary>The identifier of the newly created push subscription.</summary>
    public string? PushSubscriptionId;

    /// <summary>The high-entropy code required to verify the subscription.</summary>
    public string? VerificationCode;

    /// <summary>Stores unrecognized PushVerification properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for PushSubscription/get.
/// </summary>
public class PushSubscriptionGetRequest
{
    /// <summary>The requested subscription identifiers, or null to request every subscription.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default non-private property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized PushSubscription/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for PushSubscription/get.
/// </summary>
public class PushSubscriptionGetResponse
{
    /// <summary>The matching push subscriptions.</summary>
    public List<PushSubscription> List = new List<PushSubscription>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized PushSubscription/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for PushSubscription/set.
/// </summary>
public class PushSubscriptionSetRequest
{
    /// <summary>Maps creation identifiers to subscriptions to create.</summary>
    public Dictionary<string, PushSubscription>? Create;

    /// <summary>Maps subscription identifiers to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The subscription identifiers to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Stores unrecognized PushSubscription/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for PushSubscription/set.
/// </summary>
public class PushSubscriptionSetResponse
{
    /// <summary>Maps successful creation identifiers to created subscriptions.</summary>
    public Dictionary<string, PushSubscription>? Created;

    /// <summary>Maps successful update identifiers to returned properties or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers successfully destroyed.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized PushSubscription/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Configures a connection to the JMAP EventSource endpoint.
/// </summary>
public class EventSourceOptions
{
    /// <summary>The interested data-type names, or null to request all types.</summary>
    public List<string>? Types;

    /// <summary>Whether the server should close the connection after sending a state event.</summary>
    public bool CloseAfterState;

    /// <summary>The requested keep-alive ping interval in seconds, where zero disables pings.</summary>
    public long PingIntervalSeconds;

    /// <summary>The last event identifier received, or null when establishing a fresh stream.</summary>
    public string? LastEventId;
}

/// <summary>
/// Represents one parsed record from a JMAP EventSource stream.
/// </summary>
public class JmapEvent
{
    /// <summary>The library classification of the record.</summary>
    public JmapEventKind Kind;

    /// <summary>The exact EventSource event name, or null when none was supplied.</summary>
    public string? Name;

    /// <summary>The event identifier, or null when the record did not set one.</summary>
    public string? Id;

    /// <summary>The complete event data after joining multiple data fields with line feeds.</summary>
    public string? Data;

    /// <summary>The comment text for a comment record.</summary>
    public string? Comment;

    /// <summary>The parsed StateChange value for a state event.</summary>
    public StateChange? StateChange;

    /// <summary>The ping interval announced by a ping event.</summary>
    public long PingIntervalSeconds;

    /// <summary>Whether the event contained a valid ping interval.</summary>
    public bool HasPingIntervalSeconds;

    /// <summary>The reconnection delay announced by an EventSource retry field, in milliseconds.</summary>
    public long RetryMilliseconds;

    /// <summary>Whether the record contained a valid reconnection delay.</summary>
    public bool HasRetryMilliseconds;
}
