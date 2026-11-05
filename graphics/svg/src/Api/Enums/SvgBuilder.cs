namespace svg;

/// <summary>Defines stable document-builder failures.</summary>
public enum SvgBuilderError
{
    /// <summary>No builder operation has failed.</summary>
    None = 0,
    /// <summary>An operation received a missing or malformed argument.</summary>
    InvalidArgument = 1,
    /// <summary>The current builder position cannot perform the requested operation.</summary>
    InvalidState = 2,
    /// <summary>The requested element is not in a recognized current or legacy SVG element catalog.</summary>
    UnsupportedElement = 3,
    /// <summary>A style attribute or stylesheet is malformed.</summary>
    InvalidStyle = 4,
    /// <summary>The editable document rejected the requested structural mutation.</summary>
    MutationFailed = 5
}
