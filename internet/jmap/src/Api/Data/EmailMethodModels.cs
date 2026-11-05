using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Contains the arguments for Email/get.
/// </summary>
public class EmailGetRequest
{
    /// <summary>The account from which Emails are requested.</summary>
    public string? AccountId;

    /// <summary>The requested Email identifiers, or null to request all Emails subject to server limits.</summary>
    public List<string>? Ids;

    /// <summary>The requested Email properties, or null for the Email-specific default property set.</summary>
    public List<string>? Properties;

    /// <summary>The properties projected for each returned EmailBodyPart.</summary>
    public List<string>? BodyProperties;

    /// <summary>Whether bodyValues includes textual parts from textBody.</summary>
    public bool FetchTextBodyValues;

    /// <summary>Whether the fetchTextBodyValues argument was present.</summary>
    public bool HasFetchTextBodyValues;

    /// <summary>Whether bodyValues includes textual parts from htmlBody.</summary>
    public bool FetchHtmlBodyValues;

    /// <summary>Whether the fetchHTMLBodyValues argument was present.</summary>
    public bool HasFetchHtmlBodyValues;

    /// <summary>Whether bodyValues includes every textual part from bodyStructure.</summary>
    public bool FetchAllBodyValues;

    /// <summary>Whether the fetchAllBodyValues argument was present.</summary>
    public bool HasFetchAllBodyValues;

    /// <summary>The maximum UTF-8 byte size of each body value, where zero means unlimited.</summary>
    public long MaxBodyValueBytes;

    /// <summary>Whether the maxBodyValueBytes argument was present.</summary>
    public bool HasMaxBodyValueBytes;

    /// <summary>Stores unrecognized Email/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/get.
/// </summary>
public class EmailGetResponse
{
    /// <summary>The account from which Emails were read.</summary>
    public string? AccountId;

    /// <summary>The current state of the Email collection.</summary>
    public string? State;

    /// <summary>The returned Emails in server-selected order.</summary>
    public List<Email> List = new List<Email>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized Email/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/changes.
/// </summary>
public class EmailChangesRequest
{
    /// <summary>The account whose Email changes are requested.</summary>
    public string? AccountId;

    /// <summary>The Email state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized Email/changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/changes.
/// </summary>
public class EmailChangesResponse
{
    /// <summary>The account whose Emails changed.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether more changes must be requested to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of Emails created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of Emails updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of Emails destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>Stores unrecognized Email/changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/set.
/// </summary>
public class EmailSetRequest
{
    /// <summary>The account whose Emails are changed.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to draft Emails to create.</summary>
    public Dictionary<string, Email>? Create;

    /// <summary>Maps Email identifiers to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The Email identifiers to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Stores unrecognized Email/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/set.
/// </summary>
public class EmailSetResponse
{
    /// <summary>The account whose Emails were changed.</summary>
    public string? AccountId;

    /// <summary>The Email state before applying changes.</summary>
    public string? OldState;

    /// <summary>The Email state after applying changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to created Email metadata.</summary>
    public Dictionary<string, Email>? Created;

    /// <summary>Maps successful update identifiers to returned properties or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers of Emails successfully destroyed.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized Email/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/copy.
/// </summary>
public class EmailCopyRequest
{
    /// <summary>The account from which Emails are copied.</summary>
    public string? FromAccountId;

    /// <summary>The required source state, or null to read the current source state.</summary>
    public string? IfFromInState;

    /// <summary>The destination account.</summary>
    public string? AccountId;

    /// <summary>The required destination state, or null to use the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to source Email ids and allowed property overrides.</summary>
    public Dictionary<string, Email> Create = new Dictionary<string, Email>();

    /// <summary>Whether successfully copied source Emails should then be destroyed.</summary>
    public bool OnSuccessDestroyOriginal;

    /// <summary>Whether the onSuccessDestroyOriginal argument was present.</summary>
    public bool HasOnSuccessDestroyOriginal;

    /// <summary>The state precondition passed to the implicit source-account Email/set call.</summary>
    public string? DestroyFromIfInState;

    /// <summary>Stores unrecognized Email/copy arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/copy.
/// </summary>
public class EmailCopyResponse
{
    /// <summary>The account from which Emails were copied.</summary>
    public string? FromAccountId;

    /// <summary>The destination account.</summary>
    public string? AccountId;

    /// <summary>The destination Email state before copying.</summary>
    public string? OldState;

    /// <summary>The destination Email state after copying.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to copied Email metadata.</summary>
    public Dictionary<string, Email>? Created;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Stores unrecognized Email/copy result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes one uploaded RFC 5322 message to import into the mail store.
/// </summary>
public class EmailImport
{
    /// <summary>The identifier of the uploaded raw message blob.</summary>
    public string? BlobId;

    /// <summary>The non-empty set of Mailboxes to assign to the imported Email.</summary>
    public Dictionary<string, bool> MailboxIds = new Dictionary<string, bool>();

    /// <summary>The keywords to apply to the imported Email.</summary>
    public Dictionary<string, bool> Keywords = new Dictionary<string, bool>();

    /// <summary>The received date-time to assign, or null for the server-derived default.</summary>
    public string? ReceivedAt;

    /// <summary>Stores unrecognized EmailImport properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/import.
/// </summary>
public class EmailImportRequest
{
    /// <summary>The account into which Emails are imported.</summary>
    public string? AccountId;

    /// <summary>The required current Email state, or null to use the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to uploaded messages and their mutable metadata.</summary>
    public Dictionary<string, EmailImport> Emails = new Dictionary<string, EmailImport>();

    /// <summary>Stores unrecognized Email/import arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/import.
/// </summary>
public class EmailImportResponse
{
    /// <summary>The account into which Emails were imported.</summary>
    public string? AccountId;

    /// <summary>The Email state before importing, or null when it was unknown.</summary>
    public string? OldState;

    /// <summary>The Email state after importing.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to imported Email metadata.</summary>
    public Dictionary<string, Email>? Created;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Stores unrecognized Email/import result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/parse.
/// </summary>
public class EmailParseRequest
{
    /// <summary>The account providing access to the blobs.</summary>
    public string? AccountId;

    /// <summary>The identifiers of uploaded or referenced blobs to parse.</summary>
    public List<string> BlobIds = new List<string>();

    /// <summary>The requested Email properties, or null for the parse-specific defaults.</summary>
    public List<string>? Properties;

    /// <summary>The properties projected for each returned EmailBodyPart.</summary>
    public List<string>? BodyProperties;

    /// <summary>Whether bodyValues includes textual parts from textBody.</summary>
    public bool FetchTextBodyValues;

    /// <summary>Whether the fetchTextBodyValues argument was present.</summary>
    public bool HasFetchTextBodyValues;

    /// <summary>Whether bodyValues includes textual parts from htmlBody.</summary>
    public bool FetchHtmlBodyValues;

    /// <summary>Whether the fetchHTMLBodyValues argument was present.</summary>
    public bool HasFetchHtmlBodyValues;

    /// <summary>Whether bodyValues includes every textual part from bodyStructure.</summary>
    public bool FetchAllBodyValues;

    /// <summary>Whether the fetchAllBodyValues argument was present.</summary>
    public bool HasFetchAllBodyValues;

    /// <summary>The maximum UTF-8 byte size of each body value, where zero means unlimited.</summary>
    public long MaxBodyValueBytes;

    /// <summary>Whether the maxBodyValueBytes argument was present.</summary>
    public bool HasMaxBodyValueBytes;

    /// <summary>Stores unrecognized Email/parse arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/parse.
/// </summary>
public class EmailParseResponse
{
    /// <summary>The account used to access the blobs.</summary>
    public string? AccountId;

    /// <summary>Maps successfully parsed blob identifiers to Email representations.</summary>
    public Dictionary<string, Email>? Parsed;

    /// <summary>The identifiers of blobs that could not be parsed as messages.</summary>
    public List<string>? NotParsable;

    /// <summary>The identifiers of blobs that could not be found.</summary>
    public List<string>? NotFound;

    /// <summary>Stores unrecognized Email/parse result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
