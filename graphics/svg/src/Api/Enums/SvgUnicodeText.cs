namespace svg;

/// <summary>Defines stable Unicode text analysis failures.</summary>
public enum SvgUnicodeTextError
{
    /// <summary>No Unicode text failure occurred.</summary>
    None = 0,
    /// <summary>The text, options, or a language span was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured scalar, cluster, or item-run bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Unicode text analysis was cancelled.</summary>
    Cancelled = 3
}
