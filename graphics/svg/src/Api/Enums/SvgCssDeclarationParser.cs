namespace svg;

/// <summary>Defines shorthand expansion failures.</summary>
public enum SvgCssDeclarationExpansionError
{
    /// <summary>No expansion failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The shorthand value did not match its grammar.</summary>
    InvalidValue = 2,
    /// <summary>The configured declaration bound was exceeded.</summary>
    DeclarationLimitExceeded = 3,
    /// <summary>A caller-defined shorthand has no built-in expansion algorithm.</summary>
    UnsupportedShorthand = 4
}
