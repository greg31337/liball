namespace svg;

/// <summary>Defines stable filter-chain builder failures.</summary>
public enum SvgFilterBuilderError
{
    /// <summary>No builder operation has failed.</summary>
    None = 0,
    /// <summary>An argument or mutable builder setting was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The current target, filter, primitive, or child cannot perform the requested operation.</summary>
    InvalidState = 2,
    /// <summary>The configured primitive count was exceeded.</summary>
    PrimitiveLimitExceeded = 3,
    /// <summary>The destination document builder was already in an error state.</summary>
    DocumentBuilderFailed = 4,
    /// <summary>An editable-document mutation failed.</summary>
    MutationFailed = 5
}
