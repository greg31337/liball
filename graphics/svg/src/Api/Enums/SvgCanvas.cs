namespace svg;

/// <summary>Defines stable SVG canvas recording failures.</summary>
public enum SvgCanvasError
{
    /// <summary>No canvas operation has failed.</summary>
    None = 0,
    /// <summary>An argument or mutable canvas setting was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The current save, layer, definition, or recording state cannot perform the requested operation.</summary>
    InvalidState = 2,
    /// <summary>The configured save-state depth was exceeded.</summary>
    SaveLimitExceeded = 3,
    /// <summary>The configured layer depth was exceeded.</summary>
    LayerLimitExceeded = 4,
    /// <summary>The current path builder failed.</summary>
    PathFailed = 5,
    /// <summary>An affine transformation failed.</summary>
    TransformFailed = 6,
    /// <summary>The underlying document builder was in an error state.</summary>
    DocumentBuilderFailed = 7,
    /// <summary>An editable-document mutation failed.</summary>
    MutationFailed = 8
}
