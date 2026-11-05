namespace svg;

/// <summary>Defines SVG path serialization styles.</summary>
public enum SvgPathFormatMode
{
    /// <summary>Writes explicit commands with canonical spacing and numbers.</summary>
    Canonical = 0,
    /// <summary>Writes compact numbers, separators, and implicit repeated commands.</summary>
    Minified = 1
}

/// <summary>Defines stable SVG path formatting failures.</summary>
public enum SvgPathFormatError
{
    /// <summary>No formatting failure occurred.</summary>
    None = 0,
    /// <summary>A path or options argument was null.</summary>
    InvalidArgument = 1,
    /// <summary>The path contained invalid mutable state.</summary>
    InvalidPath = 2,
    /// <summary>The formatting mode was unknown.</summary>
    InvalidMode = 3,
    /// <summary>The configured fractional precision was unsupported.</summary>
    InvalidPrecision = 4,
    /// <summary>A numeric payload could not be formatted.</summary>
    InvalidNumber = 5
}
