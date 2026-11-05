namespace svg;

/// <summary>Identifies whether a legacy SVG feature is an element or attribute.</summary>
public enum SvgLegacyFeatureType
{
    /// <summary>A legacy SVG element.</summary>
    Element = 0,
    /// <summary>A legacy unqualified or XLink attribute.</summary>
    Attribute = 1
}

/// <summary>Defines the library's semantic handling of a legacy SVG feature.</summary>
public enum SvgLegacyHandling
{
    /// <summary>The feature has explicit compatibility semantics.</summary>
    Implemented = 0,
    /// <summary>The feature round-trips without semantic processing.</summary>
    PreserveOnly = 1,
    /// <summary>The feature is retained but a current replacement is preferred.</summary>
    Deprecated = 2,
    /// <summary>The feature is recognized and preserved but unsupported.</summary>
    Unsupported = 3
}
