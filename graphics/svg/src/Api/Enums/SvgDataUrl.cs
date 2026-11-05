namespace svg;

/// <summary>Identifies a stable data URL parsing or encoding failure.</summary>
public enum SvgDataUrlError
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The input did not begin with the data scheme.</summary>
    InvalidScheme = 2,
    /// <summary>The required metadata and payload comma was missing.</summary>
    MissingComma = 3,
    /// <summary>The media type, parameter, or base64 marker was malformed.</summary>
    InvalidMediaType = 4,
    /// <summary>A percent escape was incomplete or malformed.</summary>
    InvalidPercentEscape = 5,
    /// <summary>The percent-encoded payload contained a disallowed raw character.</summary>
    InvalidPayload = 6,
    /// <summary>The base64 payload was malformed.</summary>
    InvalidBase64 = 7,
    /// <summary>The decoded payload exceeded the configured byte limit.</summary>
    DecodedSizeLimitExceeded = 8
}
