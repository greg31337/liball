using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a recursive Email query filter operator or condition.
/// </summary>
public class EmailFilter
{
    /// <summary>The filter operator when this is an operator node.</summary>
    public JmapFilterOperator Operator;

    /// <summary>Whether this node contains an operator.</summary>
    public bool HasOperator;

    /// <summary>The child filters for an operator, or null for a condition node.</summary>
    public List<EmailFilter>? Conditions;

    /// <summary>The Mailbox that matching Emails must belong to.</summary>
    public string? InMailbox;

    /// <summary>Mailbox identifiers that cannot be the sole Mailboxes of a matching Email.</summary>
    public List<string>? InMailboxOtherThan;

    /// <summary>The exclusive upper bound for the Email receivedAt value.</summary>
    public string? Before;

    /// <summary>The inclusive lower bound for the Email receivedAt value.</summary>
    public string? After;

    /// <summary>The inclusive minimum raw Email size.</summary>
    public long MinSize;

    /// <summary>Whether the minSize condition was supplied.</summary>
    public bool HasMinSize;

    /// <summary>The exclusive maximum raw Email size.</summary>
    public long MaxSize;

    /// <summary>Whether the maxSize condition was supplied.</summary>
    public bool HasMaxSize;

    /// <summary>The keyword every Email in the Thread must have.</summary>
    public string? AllInThreadHaveKeyword;

    /// <summary>The keyword at least one Email in the Thread must have.</summary>
    public string? SomeInThreadHaveKeyword;

    /// <summary>The keyword no Email in the Thread may have.</summary>
    public string? NoneInThreadHaveKeyword;

    /// <summary>The keyword the Email must have.</summary>
    public string? HasKeyword;

    /// <summary>The keyword the Email must not have.</summary>
    public string? NotKeyword;

    /// <summary>The attachment-presence value that matching Emails must have.</summary>
    public bool HasAttachment;

    /// <summary>Whether the hasAttachment condition was supplied.</summary>
    public bool HasHasAttachment;

    /// <summary>Text to find across address, subject, and body content.</summary>
    public string? Text;

    /// <summary>Text to find in the From header field.</summary>
    public string? From;

    /// <summary>Text to find in the To header field.</summary>
    public string? To;

    /// <summary>Text to find in the Cc header field.</summary>
    public string? Cc;

    /// <summary>Text to find in the Bcc header field.</summary>
    public string? Bcc;

    /// <summary>Text to find in the Subject header field.</summary>
    public string? Subject;

    /// <summary>Text to find in searchable body parts.</summary>
    public string? Body;

    /// <summary>A one-element header-name test or a two-element header-name-and-value test.</summary>
    public List<string>? Header;

    /// <summary>Stores unrecognized Email filter properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes one comparison in an Email query sort order.
/// </summary>
public class EmailComparator
{
    /// <summary>The Email property or Thread keyword aggregate to compare.</summary>
    public string? Property;

    /// <summary>The keyword used by a keyword-based comparator.</summary>
    public string? Keyword;

    /// <summary>Whether values are sorted in ascending order.</summary>
    public bool IsAscending;

    /// <summary>Whether the isAscending property was present.</summary>
    public bool HasIsAscending;

    /// <summary>The collation algorithm identifier, or null for the server-selected default.</summary>
    public string? Collation;

    /// <summary>Stores additional comparator properties defined by an extension.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/query.
/// </summary>
public class EmailQueryRequest
{
    /// <summary>The account whose Emails are queried.</summary>
    public string? AccountId;

    /// <summary>The recursive Email filter, or null to include all Emails.</summary>
    public EmailFilter? Filter;

    /// <summary>The ordered comparators, or null for stable server-defined ordering.</summary>
    public List<EmailComparator>? Sort;

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

    /// <summary>Whether all but the first Email in each Thread are removed after sorting.</summary>
    public bool CollapseThreads;

    /// <summary>Whether the collapseThreads argument was present.</summary>
    public bool HasCollapseThreads;

    /// <summary>Stores unrecognized Email/query arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/query.
/// </summary>
public class EmailQueryResponse
{
    /// <summary>The account whose Emails were queried.</summary>
    public string? AccountId;

    /// <summary>The current state of the ordered query result.</summary>
    public string? QueryState;

    /// <summary>Whether query changes can be calculated for this filter and sort.</summary>
    public bool CanCalculateChanges;

    /// <summary>The zero-based position of the first returned identifier.</summary>
    public long Position;

    /// <summary>The returned Email identifiers in query order.</summary>
    public List<string> Ids = new List<string>();

    /// <summary>The total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The result limit enforced by the server.</summary>
    public long Limit;

    /// <summary>Whether the limit field was present.</summary>
    public bool HasLimit;

    /// <summary>Stores unrecognized Email/query result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for Email/queryChanges.
/// </summary>
public class EmailQueryChangesRequest
{
    /// <summary>The account whose Email query changes are requested.</summary>
    public string? AccountId;

    /// <summary>The filter used for the original query, or null for no filter.</summary>
    public EmailFilter? Filter;

    /// <summary>The comparators used for the original query, or null for server-defined ordering.</summary>
    public List<EmailComparator>? Sort;

    /// <summary>The query state from which to calculate changes.</summary>
    public string? SinceQueryState;

    /// <summary>The maximum number of changes when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>The highest-index cached Email identifier, or null when unspecified.</summary>
    public string? UpToId;

    /// <summary>Whether the total result count is requested.</summary>
    public bool CalculateTotal;

    /// <summary>Whether the calculateTotal argument was present.</summary>
    public bool HasCalculateTotal;

    /// <summary>Whether the original query collapsed Emails by Thread.</summary>
    public bool CollapseThreads;

    /// <summary>Whether the collapseThreads argument was present.</summary>
    public bool HasCollapseThreads;

    /// <summary>Stores unrecognized Email/queryChanges arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for Email/queryChanges.
/// </summary>
public class EmailQueryChangesResponse
{
    /// <summary>The account whose Email query changed.</summary>
    public string? AccountId;

    /// <summary>The query state from which changes were calculated.</summary>
    public string? OldQueryState;

    /// <summary>The query state after applying the returned changes.</summary>
    public string? NewQueryState;

    /// <summary>The current total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The Email identifiers removed from the query result.</summary>
    public List<string> Removed = new List<string>();

    /// <summary>The Email identifiers and indexes added to the changed result.</summary>
    public List<JmapAddedItem> Added = new List<JmapAddedItem>();

    /// <summary>Stores unrecognized Email/queryChanges result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
