using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents the singleton automatic vacation-response configuration for an account.
/// </summary>
public class VacationResponse
{
    /// <summary>The immutable identifier, which is <c>singleton</c>.</summary>
    public string? Id;

    /// <summary>Whether automatic vacation responses are enabled.</summary>
    public bool IsEnabled;

    /// <summary>The inclusive UTC start date, or null for immediate effect.</summary>
    public string? FromDate;

    /// <summary>The exclusive UTC end date, or null for indefinite effect.</summary>
    public string? ToDate;

    /// <summary>The response subject, or null for a server-generated subject.</summary>
    public string? Subject;

    /// <summary>The plain-text response body, or null for server-generated or HTML-only content.</summary>
    public string? TextBody;

    /// <summary>The HTML response body, or null for server-generated or plain-text-only content.</summary>
    public string? HtmlBody;

    /// <summary>Stores unrecognized VacationResponse properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for VacationResponse/get.
/// </summary>
public class VacationResponseGetRequest
{
    /// <summary>The account whose vacation response is requested.</summary>
    public string? AccountId;

    /// <summary>The requested singleton identifier, or null to request the singleton object.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized VacationResponse/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for VacationResponse/get.
/// </summary>
public class VacationResponseGetResponse
{
    /// <summary>The account containing the vacation response.</summary>
    public string? AccountId;

    /// <summary>The current state of the VacationResponse collection.</summary>
    public string? State;

    /// <summary>The returned singleton object.</summary>
    public List<VacationResponse> List = new List<VacationResponse>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized VacationResponse/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for VacationResponse/set.
/// </summary>
public class VacationResponseSetRequest
{
    /// <summary>The account whose vacation response is changed.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to attempted singleton creations.</summary>
    public Dictionary<string, VacationResponse>? Create;

    /// <summary>Maps the singleton identifier to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The identifiers to attempt to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Stores unrecognized VacationResponse/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for VacationResponse/set.
/// </summary>
public class VacationResponseSetResponse
{
    /// <summary>The account whose vacation response was changed.</summary>
    public string? AccountId;

    /// <summary>The state before applying changes.</summary>
    public string? OldState;

    /// <summary>The state after applying changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to created data.</summary>
    public Dictionary<string, VacationResponse>? Created;

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

    /// <summary>Stores unrecognized VacationResponse/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
