using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Contains the common arguments for a standard JMAP get method.
/// </summary>
public class JmapGetRequest
{
    /// <summary>The account identifier on which to operate.</summary>
    public string? AccountId;

    /// <summary>The requested object identifiers, or null to request all objects.</summary>
    public List<string>? Ids;

    /// <summary>The requested object properties, or null to request the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized or data-type-specific get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP get method.
/// </summary>
public class JmapGetResponse
{
    /// <summary>The account identifier used for the call.</summary>
    public string? AccountId;

    /// <summary>The current state of all objects of this data type in the account.</summary>
    public string? State;

    /// <summary>The returned objects in server-selected order.</summary>
    public List<JsonElement> List = new List<JsonElement>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized or data-type-specific get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common arguments for a standard JMAP changes method.
/// </summary>
public class JmapChangesRequest
{
    /// <summary>The account identifier on which to operate.</summary>
    public string? AccountId;

    /// <summary>The state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers to return when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized or data-type-specific changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP changes method.
/// </summary>
public class JmapChangesResponse
{
    /// <summary>The account identifier used for the call.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether another changes request is required to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of records created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of records updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of records destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>Stores unrecognized or data-type-specific changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents an unordered RFC 8620 PatchObject from JSON Pointer paths to replacement values.
/// </summary>
public class JmapPatchObject
{
    /// <summary>Maps patch paths with an implicit leading slash to their replacement or null values.</summary>
    public Dictionary<string, JsonElement> Values = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common arguments for a standard JMAP set method.
/// </summary>
public class JmapSetRequest
{
    /// <summary>The account identifier on which to operate.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to objects to create, or null when no creates are requested.</summary>
    public Dictionary<string, JsonElement>? Create;

    /// <summary>Maps object identifiers to patches, or null when no updates are requested.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The identifiers to destroy, or null when no destroys are requested.</summary>
    public List<string>? Destroy;

    /// <summary>Stores unrecognized or data-type-specific set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP set method.
/// </summary>
public class JmapSetResponse
{
    /// <summary>The account identifier used for the call.</summary>
    public string? AccountId;

    /// <summary>The state before the changes, or null when the server cannot determine it.</summary>
    public string? OldState;

    /// <summary>The state after the changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to server-populated object data.</summary>
    public Dictionary<string, JsonElement>? Created;

    /// <summary>Maps successful update identifiers to server-populated object data or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers successfully destroyed, or null when none succeeded.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized or data-type-specific set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common arguments for a standard JMAP copy method.
/// </summary>
public class JmapCopyRequest
{
    /// <summary>The account identifier from which records are copied.</summary>
    public string? FromAccountId;

    /// <summary>The required source state, or null to read the current source state.</summary>
    public string? IfFromInState;

    /// <summary>The destination account identifier.</summary>
    public string? AccountId;

    /// <summary>The required destination state, or null to apply changes to the current destination state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to source identifiers and any property overrides.</summary>
    public Dictionary<string, JsonElement> Create = new Dictionary<string, JsonElement>();

    /// <summary>Whether successfully copied source records should then be destroyed.</summary>
    public bool OnSuccessDestroyOriginal;

    /// <summary>Whether the onSuccessDestroyOriginal argument was present.</summary>
    public bool HasOnSuccessDestroyOriginal;

    /// <summary>The state precondition passed to the implicit source-account set call.</summary>
    public string? DestroyFromIfInState;

    /// <summary>Stores unrecognized or data-type-specific copy arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP copy method.
/// </summary>
public class JmapCopyResponse
{
    /// <summary>The source account identifier.</summary>
    public string? FromAccountId;

    /// <summary>The destination account identifier.</summary>
    public string? AccountId;

    /// <summary>The destination state before copying, or null when the server cannot determine it.</summary>
    public string? OldState;

    /// <summary>The destination state after copying.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to server-populated copied object data.</summary>
    public Dictionary<string, JsonElement>? Created;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Stores unrecognized or data-type-specific copy result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a recursive JMAP filter operator or a type-specific filter condition.
/// </summary>
public class JmapFilterNode
{
    /// <summary>The filter operator when this is an operator node.</summary>
    public JmapFilterOperator Operator;

    /// <summary>Whether this node contains an operator.</summary>
    public bool HasOperator;

    /// <summary>The child nodes for an operator, or null for a condition node.</summary>
    public List<JmapFilterNode>? Conditions;

    /// <summary>The arbitrary type-specific properties for a condition node.</summary>
    public Dictionary<string, JsonElement> FilterProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes one comparison in a JMAP query sort order.
/// </summary>
public class JmapComparator
{
    /// <summary>The object property to compare.</summary>
    public string? Property;

    /// <summary>Whether values are sorted in ascending order.</summary>
    public bool IsAscending;

    /// <summary>Whether the isAscending property was present.</summary>
    public bool HasIsAscending;

    /// <summary>The collation algorithm identifier, or null for the server-selected default.</summary>
    public string? Collation;

    /// <summary>Stores additional comparator properties defined by a data type or extension.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common arguments for a standard JMAP query method.
/// </summary>
public class JmapQueryRequest
{
    /// <summary>The account identifier on which to operate.</summary>
    public string? AccountId;

    /// <summary>The recursive filter, or null to include all records.</summary>
    public JmapFilterNode? Filter;

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

    /// <summary>Stores unrecognized or data-type-specific query arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP query method.
/// </summary>
public class JmapQueryResponse
{
    /// <summary>The account identifier used for the call.</summary>
    public string? AccountId;

    /// <summary>The current state of the ordered query result.</summary>
    public string? QueryState;

    /// <summary>Whether query changes can be calculated for this filter and sort.</summary>
    public bool CanCalculateChanges;

    /// <summary>The zero-based position of the first returned identifier.</summary>
    public long Position;

    /// <summary>The returned result identifiers in query order.</summary>
    public List<string> Ids = new List<string>();

    /// <summary>The total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The result limit enforced by the server.</summary>
    public long Limit;

    /// <summary>Whether the limit field was present.</summary>
    public bool HasLimit;

    /// <summary>Stores unrecognized or data-type-specific query result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common arguments for a standard JMAP queryChanges method.
/// </summary>
public class JmapQueryChangesRequest
{
    /// <summary>The account identifier on which to operate.</summary>
    public string? AccountId;

    /// <summary>The filter used for the original query, or null for no filter.</summary>
    public JmapFilterNode? Filter;

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

    /// <summary>The highest-index cached identifier, or null when the cache boundary is unspecified.</summary>
    public string? UpToId;

    /// <summary>Whether the total result count is requested.</summary>
    public bool CalculateTotal;

    /// <summary>Whether the calculateTotal argument was present.</summary>
    public bool HasCalculateTotal;

    /// <summary>Stores unrecognized or data-type-specific queryChanges arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Identifies a record added at a particular index in a changed query result.
/// </summary>
public class JmapAddedItem
{
    /// <summary>The added record identifier.</summary>
    public string? Id;

    /// <summary>The zero-based index in the new query result.</summary>
    public long Index;

    /// <summary>Stores unrecognized added-item properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the common result fields for a standard JMAP queryChanges method.
/// </summary>
public class JmapQueryChangesResponse
{
    /// <summary>The account identifier used for the call.</summary>
    public string? AccountId;

    /// <summary>The query state from which changes were calculated.</summary>
    public string? OldQueryState;

    /// <summary>The query state after applying the returned changes.</summary>
    public string? NewQueryState;

    /// <summary>The current total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The identifiers removed from the query result.</summary>
    public List<string> Removed = new List<string>();

    /// <summary>The identifiers and indexes added to the query result, ordered by index.</summary>
    public List<JmapAddedItem> Added = new List<JmapAddedItem>();

    /// <summary>Stores unrecognized or data-type-specific queryChanges result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
