namespace svg;

/// <summary>Identifies a stable URI or IRI processing failure.</summary>
public enum SvgUriError
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured character bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The input does not match the generic URI-reference syntax.</summary>
    InvalidSyntax = 2,
    /// <summary>A percent escape was incomplete or malformed.</summary>
    InvalidPercentEscape = 3,
    /// <summary>The base reference was not suitable for relative resolution.</summary>
    InvalidBaseUri = 4,
    /// <summary>A file URI violated the authority or absolute-path rules.</summary>
    InvalidFileUri = 5,
    /// <summary>The input or normalized output exceeded the configured character bound.</summary>
    TooLong = 6
}
