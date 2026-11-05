namespace jmap;

/// <summary>
/// Defines error tokens registered by JMAP Core and JMAP Mail.
/// </summary>
public static class JmapErrorCodes
{
    /// <summary>The URI prefix applied to request-level problem types.</summary>
    public const string ProblemTypePrefix = "urn:ietf:params:jmap:error:";

    /// <summary>The unknown-capability error token.</summary>
    public const string UnknownCapability = "unknownCapability";

    /// <summary>The not-JSON error token.</summary>
    public const string NotJson = "notJSON";

    /// <summary>The not-request error token.</summary>
    public const string NotRequest = "notRequest";

    /// <summary>The request-limit error token.</summary>
    public const string Limit = "limit";

    /// <summary>The complete unknown-capability problem type URI.</summary>
    public const string UnknownCapabilityProblemType = "urn:ietf:params:jmap:error:unknownCapability";

    /// <summary>The complete not-JSON problem type URI.</summary>
    public const string NotJsonProblemType = "urn:ietf:params:jmap:error:notJSON";

    /// <summary>The complete not-request problem type URI.</summary>
    public const string NotRequestProblemType = "urn:ietf:params:jmap:error:notRequest";

    /// <summary>The complete request-limit problem type URI.</summary>
    public const string LimitProblemType = "urn:ietf:params:jmap:error:limit";

    /// <summary>The temporarily unavailable method error.</summary>
    public const string ServerUnavailable = "serverUnavailable";

    /// <summary>The unexpected server failure method error.</summary>
    public const string ServerFail = "serverFail";

    /// <summary>The partial server failure method error.</summary>
    public const string ServerPartialFail = "serverPartialFail";

    /// <summary>The unknown-method method error.</summary>
    public const string UnknownMethod = "unknownMethod";

    /// <summary>The invalid-arguments method error.</summary>
    public const string InvalidArguments = "invalidArguments";

    /// <summary>The invalid-result-reference method error.</summary>
    public const string InvalidResultReference = "invalidResultReference";

    /// <summary>The access-forbidden method or set error.</summary>
    public const string Forbidden = "forbidden";

    /// <summary>The account-not-found method error.</summary>
    public const string AccountNotFound = "accountNotFound";

    /// <summary>The account-not-supported-by-method method error.</summary>
    public const string AccountNotSupportedByMethod = "accountNotSupportedByMethod";

    /// <summary>The account-read-only method error.</summary>
    public const string AccountReadOnly = "accountReadOnly";

    /// <summary>The request-too-large method error.</summary>
    public const string RequestTooLarge = "requestTooLarge";

    /// <summary>The cannot-calculate-changes method error.</summary>
    public const string CannotCalculateChanges = "cannotCalculateChanges";

    /// <summary>The state-mismatch method error.</summary>
    public const string StateMismatch = "stateMismatch";

    /// <summary>The anchor-not-found method error.</summary>
    public const string AnchorNotFound = "anchorNotFound";

    /// <summary>The unsupported-filter method error.</summary>
    public const string UnsupportedFilter = "unsupportedFilter";

    /// <summary>The unsupported-sort method error.</summary>
    public const string UnsupportedSort = "unsupportedSort";

    /// <summary>The too-many-changes method error.</summary>
    public const string TooManyChanges = "tooManyChanges";

    /// <summary>The source-account-not-found method error.</summary>
    public const string FromAccountNotFound = "fromAccountNotFound";

    /// <summary>The source-account-not-supported-by-method method error.</summary>
    public const string FromAccountNotSupportedByMethod = "fromAccountNotSupportedByMethod";

    /// <summary>The duplicate-record set error.</summary>
    public const string AlreadyExists = "alreadyExists";

    /// <summary>The over-quota set error.</summary>
    public const string OverQuota = "overQuota";

    /// <summary>The object-too-large set error.</summary>
    public const string TooLarge = "tooLarge";

    /// <summary>The creation-rate-limit set error.</summary>
    public const string RateLimit = "rateLimit";

    /// <summary>The record-not-found set error.</summary>
    public const string NotFound = "notFound";

    /// <summary>The invalid-patch set error.</summary>
    public const string InvalidPatch = "invalidPatch";

    /// <summary>The update-will-destroy set error.</summary>
    public const string WillDestroy = "willDestroy";

    /// <summary>The invalid-properties set error.</summary>
    public const string InvalidProperties = "invalidProperties";

    /// <summary>The singleton-type set error.</summary>
    public const string Singleton = "singleton";

    /// <summary>The mailbox-has-child set error.</summary>
    public const string MailboxHasChild = "mailboxHasChild";

    /// <summary>The mailbox-has-email set error.</summary>
    public const string MailboxHasEmail = "mailboxHasEmail";

    /// <summary>The referenced-blob-not-found set error.</summary>
    public const string BlobNotFound = "blobNotFound";

    /// <summary>The too-many-keywords set error.</summary>
    public const string TooManyKeywords = "tooManyKeywords";

    /// <summary>The too-many-mailboxes set error.</summary>
    public const string TooManyMailboxes = "tooManyMailboxes";

    /// <summary>The forbidden-from-address set error.</summary>
    public const string ForbiddenFrom = "forbiddenFrom";

    /// <summary>The invalid-email submission set error.</summary>
    public const string InvalidEmail = "invalidEmail";

    /// <summary>The too-many-recipients submission set error.</summary>
    public const string TooManyRecipients = "tooManyRecipients";

    /// <summary>The no-recipients submission set error.</summary>
    public const string NoRecipients = "noRecipients";

    /// <summary>The invalid-recipients submission set error.</summary>
    public const string InvalidRecipients = "invalidRecipients";

    /// <summary>The forbidden-envelope-from submission set error.</summary>
    public const string ForbiddenMailFrom = "forbiddenMailFrom";

    /// <summary>The forbidden-to-send submission set error.</summary>
    public const string ForbiddenToSend = "forbiddenToSend";

    /// <summary>The cannot-unsend submission set error.</summary>
    public const string CannotUnsend = "cannotUnsend";
}
