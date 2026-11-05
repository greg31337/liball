namespace fix;

/// <summary>Describes the outcome of a decode operation.</summary>
public enum DecodeStatus
{
    /// <summary>Decoding completed successfully.</summary>
    Success = 0,
    /// <summary>More input is required.</summary>
    Incomplete = 1,
    /// <summary>The input is invalid.</summary>
    Invalid = 2,
    /// <summary>A configured decode limit was exceeded.</summary>
    LimitExceeded = 3
}
