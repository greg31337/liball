using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a named set of Email objects in a JMAP account.
/// </summary>
public class Mailbox
{
    /// <summary>The immutable server-assigned Mailbox identifier.</summary>
    public string? Id;

    /// <summary>The user-visible Mailbox name.</summary>
    public string? Name;

    /// <summary>The parent Mailbox identifier, or null for a top-level Mailbox.</summary>
    public string? ParentId;

    /// <summary>The extensible lowercase Mailbox role, or null when no role is assigned.</summary>
    public string? Role;

    /// <summary>The client-visible ordering value among sibling Mailboxes.</summary>
    public long SortOrder;

    /// <summary>The number of Emails directly in this Mailbox.</summary>
    public long TotalEmails;

    /// <summary>The number of unread, non-draft Emails directly in this Mailbox.</summary>
    public long UnreadEmails;

    /// <summary>The number of Threads represented in this Mailbox.</summary>
    public long TotalThreads;

    /// <summary>The server-calculated number of unread Threads represented in this Mailbox.</summary>
    public long UnreadThreads;

    /// <summary>The current user's rights in relation to this Mailbox.</summary>
    public MailboxRights? MyRights;

    /// <summary>Whether the current user is subscribed to this Mailbox.</summary>
    public bool IsSubscribed;

    /// <summary>Whether <see cref="IsSubscribed"/> was explicitly supplied for Mailbox creation.</summary>
    public bool HasIsSubscribed;

    /// <summary>Stores unrecognized Mailbox properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes the current user's access rights for a Mailbox.
/// </summary>
public class MailboxRights
{
    /// <summary>Whether the user may read items through this Mailbox.</summary>
    public bool MayReadItems;

    /// <summary>Whether the user may add items to this Mailbox.</summary>
    public bool MayAddItems;

    /// <summary>Whether the user may remove items from this Mailbox.</summary>
    public bool MayRemoveItems;

    /// <summary>Whether the user may change the seen keyword on items in this Mailbox.</summary>
    public bool MaySetSeen;

    /// <summary>Whether the user may change other keywords on items in this Mailbox.</summary>
    public bool MaySetKeywords;

    /// <summary>Whether the user may create a child of this Mailbox.</summary>
    public bool MayCreateChild;

    /// <summary>Whether the user may rename or move this Mailbox.</summary>
    public bool MayRename;

    /// <summary>Whether the user may delete this Mailbox.</summary>
    public bool MayDelete;

    /// <summary>Whether messages may be submitted directly to this Mailbox.</summary>
    public bool MaySubmit;

    /// <summary>Stores unrecognized MailboxRights properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a recursive Mailbox query filter operator or condition.
/// </summary>
public class MailboxFilter
{
    /// <summary>The filter operator when this is an operator node.</summary>
    public JmapFilterOperator Operator;

    /// <summary>Whether this node contains an operator.</summary>
    public bool HasOperator;

    /// <summary>The child filters for an operator, or null for a condition node.</summary>
    public List<MailboxFilter>? Conditions;

    /// <summary>The parent identifier to match, including null for top-level Mailboxes.</summary>
    public string? ParentId;

    /// <summary>Whether the parentId condition was supplied.</summary>
    public bool HasParentId;

    /// <summary>The substring that the Mailbox name must contain.</summary>
    public string? Name;

    /// <summary>The role to match, including null for Mailboxes without a role.</summary>
    public string? Role;

    /// <summary>Whether the role condition was supplied.</summary>
    public bool HasRole;

    /// <summary>Whether matching Mailboxes must have a non-null role.</summary>
    public bool HasAnyRole;

    /// <summary>Whether the hasAnyRole condition was supplied.</summary>
    public bool HasHasAnyRole;

    /// <summary>The subscription value that matching Mailboxes must have.</summary>
    public bool IsSubscribed;

    /// <summary>Whether the isSubscribed condition was supplied.</summary>
    public bool HasIsSubscribed;

    /// <summary>Stores unrecognized Mailbox filter properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Mailbox/get.
/// </summary>
public class MailboxGetRequest
{
    /// <summary>The account from which Mailboxes are requested.</summary>
    public string? AccountId;

    /// <summary>The requested Mailbox identifiers, or null to request all Mailboxes.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized Mailbox/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Mailbox/get.
/// </summary>
public class MailboxGetResponse
{
    /// <summary>The account from which Mailboxes were read.</summary>
    public string? AccountId;

    /// <summary>The current state of the Mailbox collection.</summary>
    public string? State;

    /// <summary>The returned Mailboxes in server-selected order.</summary>
    public List<Mailbox> List = new List<Mailbox>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized Mailbox/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Mailbox/changes.
/// </summary>
public class MailboxChangesRequest
{
    /// <summary>The account whose Mailbox changes are requested.</summary>
    public string? AccountId;

    /// <summary>The Mailbox state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized Mailbox/changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Mailbox/changes.
/// </summary>
public class MailboxChangesResponse
{
    /// <summary>The account whose Mailboxes changed.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether more changes must be requested to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of Mailboxes created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of Mailboxes updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of Mailboxes destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>The count-only properties that may have changed, or null when broader changes are possible.</summary>
    public List<string>? UpdatedProperties;

    /// <summary>Stores unrecognized Mailbox/changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Mailbox/query.
/// </summary>
public class MailboxQueryRequest
{
    /// <summary>The account whose Mailboxes are queried.</summary>
    public string? AccountId;

    /// <summary>The recursive Mailbox filter, or null to include all Mailboxes.</summary>
    public MailboxFilter? Filter;

    /// <summary>The ordered comparators, or null for stable server-defined ordering.</summary>
    public List<JmapComparator>? Sort;

    /// <summary>The requested zero-based or end-relative result position.</summary>
    public long Position;

    /// <summary>Whether the position argument was present.</summary>
    public bool HasPosition;

    /// <summary>The result identifier used as an anchor, or null when no anchor is used.</summary>
    public string? Anchor;

    /// <summary>The requested offset relative to the anchor.</summary>
    public long AnchorOffset;

    /// <summary>Whether the anchorOffset argument was present.</summary>
    public bool HasAnchorOffset;

    /// <summary>The maximum result count when present and not null.</summary>
    public long Limit;

    /// <summary>Whether the limit argument was present.</summary>
    public bool HasLimit;

    /// <summary>Whether the present limit argument was explicitly null.</summary>
    public bool IsLimitNull;

    /// <summary>Whether the total result count is requested.</summary>
    public bool CalculateTotal;

    /// <summary>Whether the calculateTotal argument was present.</summary>
    public bool HasCalculateTotal;

    /// <summary>Whether ancestors must precede descendants in the sorted results.</summary>
    public bool SortAsTree;

    /// <summary>Whether the sortAsTree argument was present.</summary>
    public bool HasSortAsTree;

    /// <summary>Whether a Mailbox is included only when all of its ancestors match the filter.</summary>
    public bool FilterAsTree;

    /// <summary>Whether the filterAsTree argument was present.</summary>
    public bool HasFilterAsTree;

    /// <summary>Stores unrecognized Mailbox/query arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Mailbox/query.
/// </summary>
public class MailboxQueryResponse
{
    /// <summary>The account whose Mailboxes were queried.</summary>
    public string? AccountId;

    /// <summary>The current state of the ordered query result.</summary>
    public string? QueryState;

    /// <summary>Whether query changes can be calculated for this filter and sort.</summary>
    public bool CanCalculateChanges;

    /// <summary>The zero-based position of the first returned identifier.</summary>
    public long Position;

    /// <summary>The returned Mailbox identifiers in query order.</summary>
    public List<string> Ids = new List<string>();

    /// <summary>The total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The result limit enforced by the server.</summary>
    public long Limit;

    /// <summary>Whether the limit field was present.</summary>
    public bool HasLimit;

    /// <summary>Stores unrecognized Mailbox/query result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Mailbox/queryChanges.
/// </summary>
public class MailboxQueryChangesRequest
{
    /// <summary>The account whose Mailbox query changes are requested.</summary>
    public string? AccountId;

    /// <summary>The filter used for the original query, or null for no filter.</summary>
    public MailboxFilter? Filter;

    /// <summary>The comparators used for the original query, or null for server-defined ordering.</summary>
    public List<JmapComparator>? Sort;

    /// <summary>The query state from which to calculate changes.</summary>
    public string? SinceQueryState;

    /// <summary>The maximum number of changes when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>The highest-index cached Mailbox identifier, or null when unspecified.</summary>
    public string? UpToId;

    /// <summary>Whether the total result count is requested.</summary>
    public bool CalculateTotal;

    /// <summary>Whether the calculateTotal argument was present.</summary>
    public bool HasCalculateTotal;

    /// <summary>Whether ancestors must precede descendants in the changed query.</summary>
    public bool SortAsTree;

    /// <summary>Whether the sortAsTree argument was present.</summary>
    public bool HasSortAsTree;

    /// <summary>Whether each matching Mailbox must have matching ancestors in the changed query.</summary>
    public bool FilterAsTree;

    /// <summary>Whether the filterAsTree argument was present.</summary>
    public bool HasFilterAsTree;

    /// <summary>Stores unrecognized Mailbox/queryChanges arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Mailbox/queryChanges.
/// </summary>
public class MailboxQueryChangesResponse
{
    /// <summary>The account whose Mailbox query changed.</summary>
    public string? AccountId;

    /// <summary>The query state from which changes were calculated.</summary>
    public string? OldQueryState;

    /// <summary>The query state after applying the returned changes.</summary>
    public string? NewQueryState;

    /// <summary>The current total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The Mailbox identifiers removed from the query result.</summary>
    public List<string> Removed = new List<string>();

    /// <summary>The Mailbox identifiers and indexes added to the changed result.</summary>
    public List<JmapAddedItem> Added = new List<JmapAddedItem>();

    /// <summary>Stores unrecognized Mailbox/queryChanges result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Mailbox/set.
/// </summary>
public class MailboxSetRequest
{
    /// <summary>The account whose Mailboxes are changed.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to Mailboxes to create.</summary>
    public Dictionary<string, Mailbox>? Create;

    /// <summary>Maps Mailbox identifiers to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The Mailbox identifiers to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Whether Emails are removed when their Mailbox is destroyed.</summary>
    public bool OnDestroyRemoveEmails;

    /// <summary>Whether the onDestroyRemoveEmails argument was present.</summary>
    public bool HasOnDestroyRemoveEmails;

    /// <summary>Stores unrecognized Mailbox/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Mailbox/set.
/// </summary>
public class MailboxSetResponse
{
    /// <summary>The account whose Mailboxes were changed.</summary>
    public string? AccountId;

    /// <summary>The Mailbox state before applying changes.</summary>
    public string? OldState;

    /// <summary>The Mailbox state after applying changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to created Mailbox data.</summary>
    public Dictionary<string, Mailbox>? Created;

    /// <summary>Maps successful update identifiers to returned properties or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers of Mailboxes successfully destroyed.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized Mailbox/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
