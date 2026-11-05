namespace jmap;

/// <summary>
/// Classifies standard JMAP method-level errors while allowing unknown wire values to be preserved separately.
/// </summary>
public enum JmapMethodErrorKind
{
    /// <summary>The method error type is not recognized by this version of the library.</summary>
    Unknown = 0,

    /// <summary>A required internal server resource is temporarily unavailable.</summary>
    ServerUnavailable = 1,

    /// <summary>An unexpected server failure occurred without changing server state.</summary>
    ServerFail = 2,

    /// <summary>Only some of the expected changes were applied.</summary>
    ServerPartialFail = 3,

    /// <summary>The server does not recognize the method name.</summary>
    UnknownMethod = 4,

    /// <summary>The method arguments are invalid or incomplete.</summary>
    InvalidArguments = 5,

    /// <summary>A reference to a previous method result could not be resolved.</summary>
    InvalidResultReference = 6,

    /// <summary>The method would violate an access-control policy.</summary>
    Forbidden = 7,

    /// <summary>The requested account does not exist or is inaccessible.</summary>
    AccountNotFound = 8,

    /// <summary>The requested account does not support the method or data type.</summary>
    AccountNotSupportedByMethod = 9,

    /// <summary>The method modifies an account that is read-only.</summary>
    AccountReadOnly = 10,

    /// <summary>The method call exceeds the server's object or action limit.</summary>
    RequestTooLarge = 11,

    /// <summary>The server cannot calculate changes from the supplied state.</summary>
    CannotCalculateChanges = 12,

    /// <summary>The supplied state precondition does not match the current state.</summary>
    StateMismatch = 13,

    /// <summary>The supplied query anchor is not present in the result set.</summary>
    AnchorNotFound = 14,

    /// <summary>The server cannot process the supplied filter.</summary>
    UnsupportedFilter = 15,

    /// <summary>The server cannot process the supplied sort criteria.</summary>
    UnsupportedSort = 16,

    /// <summary>The query has more changes than the requested maximum.</summary>
    TooManyChanges = 17,

    /// <summary>The source account for a copy operation does not exist or is inaccessible.</summary>
    FromAccountNotFound = 18,

    /// <summary>The source account does not support the copied data type.</summary>
    FromAccountNotSupportedByMethod = 19
}
