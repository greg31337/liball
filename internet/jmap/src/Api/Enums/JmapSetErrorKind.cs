namespace jmap;

/// <summary>
/// Classifies standard JMAP per-record set errors while allowing unknown wire values to be preserved separately.
/// </summary>
public enum JmapSetErrorKind
{
    /// <summary>The set error type is not recognized by this version of the library.</summary>
    Unknown = 0,

    /// <summary>The record action would violate an access-control policy.</summary>
    Forbidden = 1,

    /// <summary>The record action would exceed an account quota.</summary>
    OverQuota = 2,

    /// <summary>The record would exceed the maximum supported size.</summary>
    TooLarge = 3,

    /// <summary>The server's record-creation rate limit was reached.</summary>
    RateLimit = 4,

    /// <summary>The referenced record does not exist.</summary>
    NotFound = 5,

    /// <summary>The supplied patch object is invalid.</summary>
    InvalidPatch = 6,

    /// <summary>An update was ignored because the same record will be destroyed.</summary>
    WillDestroy = 7,

    /// <summary>One or more record properties are invalid.</summary>
    InvalidProperties = 8,

    /// <summary>The requested create or destroy is invalid for a singleton type.</summary>
    Singleton = 9,

    /// <summary>An equivalent record already exists in the target account.</summary>
    AlreadyExists = 10,

    /// <summary>The Mailbox cannot be destroyed while it has a child Mailbox.</summary>
    MailboxHasChild = 11,

    /// <summary>The Mailbox cannot be destroyed while it contains Email objects.</summary>
    MailboxHasEmail = 12,

    /// <summary>A blob referenced by the record does not exist.</summary>
    BlobNotFound = 13,

    /// <summary>The Email would exceed the server's keyword limit.</summary>
    TooManyKeywords = 14,

    /// <summary>The Email would exceed the server's Mailbox limit.</summary>
    TooManyMailboxes = 15,

    /// <summary>The user may not use the requested From header address.</summary>
    ForbiddenFrom = 16,

    /// <summary>The Email selected for submission is invalid.</summary>
    InvalidEmail = 17,

    /// <summary>The submission contains more recipients than the server allows.</summary>
    TooManyRecipients = 18,

    /// <summary>The submission does not contain any recipients.</summary>
    NoRecipients = 19,

    /// <summary>One or more submission recipients are invalid.</summary>
    InvalidRecipients = 20,

    /// <summary>The user may not use the requested envelope From address.</summary>
    ForbiddenMailFrom = 21,

    /// <summary>The user is not currently permitted to submit Email.</summary>
    ForbiddenToSend = 22,

    /// <summary>The pending submission can no longer be canceled.</summary>
    CannotUnsend = 23
}
