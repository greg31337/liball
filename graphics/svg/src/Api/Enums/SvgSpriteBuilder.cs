namespace svg;

/// <summary>Defines stable sprite-builder failures.</summary>
public enum SvgSpriteBuilderError
{
    /// <summary>No sprite-builder operation failed.</summary>
    None = 0,
    /// <summary>An input document, fragment identifier, or option was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The configured symbol bound was exceeded.</summary>
    SymbolLimitExceeded = 2,
    /// <summary>The configured source-node bound was exceeded.</summary>
    NodeLimitExceeded = 3,
    /// <summary>A requested fragment was not present in its source document.</summary>
    FragmentNotFound = 4,
    /// <summary>Safe source optimization failed.</summary>
    OptimizationFailed = 5,
    /// <summary>Identifier, CSS, or local-reference isolation failed.</summary>
    IsolationFailed = 6,
    /// <summary>An editable-document mutation failed.</summary>
    MutationFailed = 7
}
