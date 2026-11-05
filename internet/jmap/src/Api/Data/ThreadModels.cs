using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a stable, received-date-ordered group of related Emails.
/// </summary>
public class Thread
{
    /// <summary>The immutable server-assigned Thread identifier.</summary>
    public string? Id;

    /// <summary>The Email identifiers in oldest-first received-date order.</summary>
    public List<string> EmailIds = new List<string>();

    /// <summary>Stores unrecognized Thread properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Thread/get.
/// </summary>
public class ThreadGetRequest
{
    /// <summary>The account from which Threads are requested.</summary>
    public string? AccountId;

    /// <summary>The requested Thread identifiers, or null to request all Threads.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized Thread/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Thread/get.
/// </summary>
public class ThreadGetResponse
{
    /// <summary>The account from which Threads were read.</summary>
    public string? AccountId;

    /// <summary>The current state of the Thread collection.</summary>
    public string? State;

    /// <summary>The returned Threads in server-selected order.</summary>
    public List<Thread> List = new List<Thread>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized Thread/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Thread/changes.
/// </summary>
public class ThreadChangesRequest
{
    /// <summary>The account whose Thread changes are requested.</summary>
    public string? AccountId;

    /// <summary>The Thread state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized Thread/changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Thread/changes.
/// </summary>
public class ThreadChangesResponse
{
    /// <summary>The account whose Threads changed.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether more changes must be requested to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of Threads created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of Threads updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of Threads destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>Stores unrecognized Thread/changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
