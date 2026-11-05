namespace fix;

/// <summary>Describes the outcome of an encode operation.</summary>
public enum EncodeStatus
{
    /// <summary>Encoding completed successfully.</summary>
    Success = 0,
    /// <summary>The destination cannot hold the encoded value.</summary>
    DestinationTooSmall = 1,
    /// <summary>The value or message is invalid.</summary>
    Invalid = 2,
    /// <summary>A configured encode limit was exceeded.</summary>
    LimitExceeded = 3
}
