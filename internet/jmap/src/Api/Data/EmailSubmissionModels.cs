using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents the submission of one Email for delivery to its recipients.
/// </summary>
public class EmailSubmission
{
    /// <summary>The immutable server-assigned submission identifier.</summary>
    public string? Id;

    /// <summary>The Identity associated with the submission.</summary>
    public string? IdentityId;

    /// <summary>The Email being submitted.</summary>
    public string? EmailId;

    /// <summary>The server-set Thread identifier of the submitted Email.</summary>
    public string? ThreadId;

    /// <summary>The explicit SMTP envelope, or null for an envelope derived from the Email.</summary>
    public EmailSubmissionEnvelope? Envelope;

    /// <summary>The UTC date-time at which the submission was or will be released.</summary>
    public string? SendAt;

    /// <summary>The current cancellation state.</summary>
    public JmapSubmissionUndoStatus UndoStatus;

    /// <summary>Whether the undoStatus property was present.</summary>
    public bool HasUndoStatus;

    /// <summary>Maps recipient addresses to their latest known delivery status.</summary>
    public Dictionary<string, EmailDeliveryStatus>? DeliveryStatus;

    /// <summary>The Delivery Status Notification blob identifiers in receipt order.</summary>
    public List<string>? DsnBlobIds;

    /// <summary>The Message Disposition Notification blob identifiers in receipt order.</summary>
    public List<string>? MdnBlobIds;

    /// <summary>Stores unrecognized EmailSubmission properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the SMTP return path and recipients for an Email submission.
/// </summary>
public class EmailSubmissionEnvelope
{
    /// <summary>The SMTP MAIL FROM address and parameters.</summary>
    public EmailSubmissionAddress? MailFrom;

    /// <summary>The SMTP RCPT TO addresses and parameters.</summary>
    public List<EmailSubmissionAddress> RcptTo = new List<EmailSubmissionAddress>();

    /// <summary>Stores unrecognized submission-envelope properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents an SMTP envelope address and its extension parameters.
/// </summary>
public class EmailSubmissionAddress
{
    /// <summary>The SMTP mailbox address, which may be empty for a return path.</summary>
    public string? Email;

    /// <summary>Maps SMTP extension parameter names to string or null JSON values.</summary>
    public Dictionary<string, JsonElement>? Parameters;

    /// <summary>Stores unrecognized submission-address properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes the latest known delivery and display status for one recipient.
/// </summary>
public class EmailDeliveryStatus
{
    /// <summary>The normalized SMTP reply string for the recipient.</summary>
    public string? SmtpReply;

    /// <summary>The extensible delivered-state token.</summary>
    public string? Delivered;

    /// <summary>The extensible displayed-state token.</summary>
    public string? Displayed;

    /// <summary>Stores unrecognized delivery-status properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Represents a recursive EmailSubmission query filter operator or condition.
/// </summary>
public class EmailSubmissionFilter
{
    /// <summary>The filter operator when this is an operator node.</summary>
    public JmapFilterOperator Operator;

    /// <summary>Whether this node contains an operator.</summary>
    public bool HasOperator;

    /// <summary>The child filters for an operator, or null for a condition node.</summary>
    public List<EmailSubmissionFilter>? Conditions;

    /// <summary>The permitted Identity identifiers for a matching submission.</summary>
    public List<string>? IdentityIds;

    /// <summary>The permitted Email identifiers for a matching submission.</summary>
    public List<string>? EmailIds;

    /// <summary>The permitted Thread identifiers for a matching submission.</summary>
    public List<string>? ThreadIds;

    /// <summary>The cancellation state that a matching submission must have.</summary>
    public JmapSubmissionUndoStatus UndoStatus;

    /// <summary>Whether the undoStatus condition was supplied.</summary>
    public bool HasUndoStatus;

    /// <summary>The exclusive upper bound for the sendAt value.</summary>
    public string? Before;

    /// <summary>The inclusive lower bound for the sendAt value.</summary>
    public string? After;

    /// <summary>Stores unrecognized EmailSubmission filter properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes one comparison in an EmailSubmission query sort order.
/// </summary>
public class EmailSubmissionComparator
{
    /// <summary>The EmailSubmission property to compare.</summary>
    public string? Property;

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
/// Contains the arguments for EmailSubmission/get.
/// </summary>
public class EmailSubmissionGetRequest
{
    /// <summary>The account from which submissions are requested.</summary>
    public string? AccountId;

    /// <summary>The requested submission identifiers, or null to request all submissions.</summary>
    public List<string>? Ids;

    /// <summary>The requested properties, or null for the default property set.</summary>
    public List<string>? Properties;

    /// <summary>Stores unrecognized EmailSubmission/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for EmailSubmission/get.
/// </summary>
public class EmailSubmissionGetResponse
{
    /// <summary>The account from which submissions were read.</summary>
    public string? AccountId;

    /// <summary>The current state of the EmailSubmission collection.</summary>
    public string? State;

    /// <summary>The returned EmailSubmissions in server-selected order.</summary>
    public List<EmailSubmission> List = new List<EmailSubmission>();

    /// <summary>The requested identifiers that were not found.</summary>
    public List<string> NotFound = new List<string>();

    /// <summary>Stores unrecognized EmailSubmission/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for EmailSubmission/changes.
/// </summary>
public class EmailSubmissionChangesRequest
{
    /// <summary>The account whose submission changes are requested.</summary>
    public string? AccountId;

    /// <summary>The EmailSubmission state from which to calculate changes.</summary>
    public string? SinceState;

    /// <summary>The maximum number of changed identifiers when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>Stores unrecognized EmailSubmission/changes arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for EmailSubmission/changes.
/// </summary>
public class EmailSubmissionChangesResponse
{
    /// <summary>The account whose submissions changed.</summary>
    public string? AccountId;

    /// <summary>The state from which changes were calculated.</summary>
    public string? OldState;

    /// <summary>The state after applying the returned changes.</summary>
    public string? NewState;

    /// <summary>Whether more changes must be requested to reach the current state.</summary>
    public bool HasMoreChanges;

    /// <summary>The identifiers of submissions created since the old state.</summary>
    public List<string> Created = new List<string>();

    /// <summary>The identifiers of submissions updated since the old state.</summary>
    public List<string> Updated = new List<string>();

    /// <summary>The identifiers of submissions destroyed since the old state.</summary>
    public List<string> Destroyed = new List<string>();

    /// <summary>Stores unrecognized EmailSubmission/changes result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for EmailSubmission/query.
/// </summary>
public class EmailSubmissionQueryRequest
{
    /// <summary>The account whose submissions are queried.</summary>
    public string? AccountId;

    /// <summary>The recursive submission filter, or null to include all submissions.</summary>
    public EmailSubmissionFilter? Filter;

    /// <summary>The ordered comparators, or null for stable server-defined ordering.</summary>
    public List<EmailSubmissionComparator>? Sort;

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

    /// <summary>Stores unrecognized EmailSubmission/query arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for EmailSubmission/query.
/// </summary>
public class EmailSubmissionQueryResponse
{
    /// <summary>The account whose submissions were queried.</summary>
    public string? AccountId;

    /// <summary>The current state of the ordered query result.</summary>
    public string? QueryState;

    /// <summary>Whether query changes can be calculated for this filter and sort.</summary>
    public bool CanCalculateChanges;

    /// <summary>The zero-based position of the first returned identifier.</summary>
    public long Position;

    /// <summary>The returned EmailSubmission identifiers in query order.</summary>
    public List<string> Ids = new List<string>();

    /// <summary>The total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The result limit enforced by the server.</summary>
    public long Limit;

    /// <summary>Whether the limit field was present.</summary>
    public bool HasLimit;

    /// <summary>Stores unrecognized EmailSubmission/query result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for EmailSubmission/queryChanges.
/// </summary>
public class EmailSubmissionQueryChangesRequest
{
    /// <summary>The account whose submission query changes are requested.</summary>
    public string? AccountId;

    /// <summary>The filter used for the original query, or null for no filter.</summary>
    public EmailSubmissionFilter? Filter;

    /// <summary>The comparators used for the original query, or null for server-defined ordering.</summary>
    public List<EmailSubmissionComparator>? Sort;

    /// <summary>The query state from which to calculate changes.</summary>
    public string? SinceQueryState;

    /// <summary>The maximum number of changes when present and not null.</summary>
    public long MaxChanges;

    /// <summary>Whether the maxChanges argument was present.</summary>
    public bool HasMaxChanges;

    /// <summary>Whether the present maxChanges argument was explicitly null.</summary>
    public bool IsMaxChangesNull;

    /// <summary>The highest-index cached submission identifier, or null when unspecified.</summary>
    public string? UpToId;

    /// <summary>Whether the total result count is requested.</summary>
    public bool CalculateTotal;

    /// <summary>Whether the calculateTotal argument was present.</summary>
    public bool HasCalculateTotal;

    /// <summary>Stores unrecognized EmailSubmission/queryChanges arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for EmailSubmission/queryChanges.
/// </summary>
public class EmailSubmissionQueryChangesResponse
{
    /// <summary>The account whose submission query changed.</summary>
    public string? AccountId;

    /// <summary>The query state from which changes were calculated.</summary>
    public string? OldQueryState;

    /// <summary>The query state after applying the returned changes.</summary>
    public string? NewQueryState;

    /// <summary>The current total result count when it was requested.</summary>
    public long Total;

    /// <summary>Whether the total field was present.</summary>
    public bool HasTotal;

    /// <summary>The submission identifiers removed from the query result.</summary>
    public List<string> Removed = new List<string>();

    /// <summary>The submission identifiers and indexes added to the changed result.</summary>
    public List<JmapAddedItem> Added = new List<JmapAddedItem>();

    /// <summary>Stores unrecognized EmailSubmission/queryChanges result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for EmailSubmission/set.
/// </summary>
public class EmailSubmissionSetRequest
{
    /// <summary>The account whose submissions are changed.</summary>
    public string? AccountId;

    /// <summary>The required current state, or null to apply changes to the current state.</summary>
    public string? IfInState;

    /// <summary>Maps creation identifiers to submissions to create.</summary>
    public Dictionary<string, EmailSubmission>? Create;

    /// <summary>Maps submission identifiers to patches to apply.</summary>
    public Dictionary<string, JmapPatchObject>? Update;

    /// <summary>The submission identifiers to destroy.</summary>
    public List<string>? Destroy;

    /// <summary>Maps successful submission identifiers or creation references to Email patches.</summary>
    public Dictionary<string, JmapPatchObject>? OnSuccessUpdateEmail;

    /// <summary>The successful submission identifiers or creation references whose Emails are destroyed.</summary>
    public List<string>? OnSuccessDestroyEmail;

    /// <summary>Stores unrecognized EmailSubmission/set arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for EmailSubmission/set.
/// </summary>
public class EmailSubmissionSetResponse
{
    /// <summary>The account whose submissions were changed.</summary>
    public string? AccountId;

    /// <summary>The EmailSubmission state before applying changes.</summary>
    public string? OldState;

    /// <summary>The EmailSubmission state after applying changes.</summary>
    public string? NewState;

    /// <summary>Maps successful creation identifiers to created submission data.</summary>
    public Dictionary<string, EmailSubmission>? Created;

    /// <summary>Maps successful update identifiers to returned properties or explicit null values.</summary>
    public Dictionary<string, JsonElement>? Updated;

    /// <summary>The identifiers of submissions successfully destroyed.</summary>
    public List<string>? Destroyed;

    /// <summary>Maps failed creation identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotCreated;

    /// <summary>Maps failed update identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotUpdated;

    /// <summary>Maps failed destruction identifiers to their SetError values.</summary>
    public Dictionary<string, JmapSetError>? NotDestroyed;

    /// <summary>Stores unrecognized EmailSubmission/set result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
