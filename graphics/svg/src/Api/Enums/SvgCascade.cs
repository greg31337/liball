namespace svg;

/// <summary>Identifies how a declaration entered the cascade.</summary>
public enum SvgCascadeSourceKind
{
    /// <summary>A declaration from a stylesheet rule.</summary>
    StyleSheet = 0,
    /// <summary>A declaration mapped from an SVG presentation attribute.</summary>
    PresentationAttribute = 1,
    /// <summary>A declaration from an element style attribute.</summary>
    StyleAttribute = 2,
    /// <summary>A declaration from the built-in SVG user-agent defaults.</summary>
    UserAgentDefault = 3
}

/// <summary>Identifies how a cascaded value was obtained.</summary>
public enum SvgCascadedValueKind
{
    /// <summary>The value came from a winning declaration.</summary>
    Declaration = 0,
    /// <summary>The property uses its catalog initial value.</summary>
    Initial = 1,
    /// <summary>The property copied its parent cascaded value.</summary>
    Inherited = 2
}
