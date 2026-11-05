namespace svg;

/// <summary>Defines stable paint-definition builder failures.</summary>
public enum SvgPaintBuilderError
{
    /// <summary>No builder operation has failed.</summary>
    None = 0,
    /// <summary>An argument or mutable builder setting was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The current target or definition cannot perform the requested operation.</summary>
    InvalidState = 2,
    /// <summary>The document builder was already in an error state or failed during definition content generation.</summary>
    DocumentBuilderFailed = 3,
    /// <summary>An editable-document mutation failed.</summary>
    MutationFailed = 4
}
