namespace http;

/// <summary>
/// Identifies the outcome of applying or removing HTTP content codings.
/// </summary>
public enum HttpContentCodingOutcome
{
    /// <summary>
    /// Content was encoded successfully.
    /// </summary>
    Encoded = 0,

    /// <summary>
    /// Content was decoded successfully.
    /// </summary>
    Decoded = 1,

    /// <summary>
    /// Identity processing returned the content unchanged.
    /// </summary>
    Identity = 2,

    /// <summary>
    /// A requested content coding is unknown or unavailable.
    /// </summary>
    Unknown = 3,

    /// <summary>
    /// The coded content is malformed.
    /// </summary>
    Malformed = 4,

    /// <summary>
    /// The transformed content exceeded a configured limit.
    /// </summary>
    LimitExceeded = 5
}
