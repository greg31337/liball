using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Describes an address or domain from which the user may send Email.
/// </summary>
public class Identity
{
    /// <summary>The immutable server-assigned Identity identifier.</summary>
    public string? Id;

    /// <summary>The default display name for the From header field.</summary>
    public string? Name;

    /// <summary>The immutable From address or wildcard domain address.</summary>
    public string? Email;

    /// <summary>The default Reply-To addresses, or null when none are configured.</summary>
    public List<EmailAddress>? ReplyTo;

    /// <summary>The default blind-copy addresses, or null when none are configured.</summary>
    public List<EmailAddress>? Bcc;

    /// <summary>The default plain-text signature.</summary>
    public string? TextSignature;

    /// <summary>The default HTML signature snippet.</summary>
    public string? HtmlSignature;

    /// <summary>Whether the current user may delete this Identity.</summary>
    public bool MayDelete;

    /// <summary>Stores unrecognized Identity properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Identity/get.
/// </summary>
public class IdentityGetRequest
{
    /// <summary>The account from which Identities are requested.</summary>
    public string? AccountId;

    /// <summary>The requested Identity identifiers, or null to request all Identities.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized Identity/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Identity/get.
/// </summary>
public class IdentityGetResponse
{
    /// <summary>The account from which Identities were read.</summary>
    public string? AccountId;

    /// <summary>The current state of the Identity collection.</summary>
    public string? State;

    /// <summary>The returned Identities in server-selected order.</summary>
    public List<Identity> List = new List<Identity>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized Identity/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Identity/changes.
/// </summary>
public class IdentityChangesRequest
{
    /// <summary>The account whose Identity changes are requested.</summary>
    public string? AccountId;

    /// <summary>The Identity state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized Identity/changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Identity/changes.
/// </summary>
public class IdentityChangesResponse
{
    /// <summary>The account whose Identities changed.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether more changes must be requested to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of Identities created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of Identities updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of Identities destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>Stores unrecognized Identity/changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Identity/set.
/// </summary>
public class IdentitySetRequest
{
    /// <summary>The account whose Identities are changed.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to Identities to create.</summary>
    public Dictionary<string, Identity>? Create;

    /// <summary>Maps Identity identifiers to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The Identity identifiers to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Stores unrecognized Identity/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Identity/set.
/// </summary>
public class IdentitySetResponse
{
    /// <summary>The account whose Identities were changed.</summary>
    public string? AccountId;

    /// <summary>The Identity state before applying changes.</summary>
    public string? OldState;

    /// <summary>The Identity state after applying changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to created Identity data.</summary>
    public Dictionary<string, Identity>? Created;

    /// <summary>Maps successful update identifiers to returned properties or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers of Identities successfully destroyed.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized Identity/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
