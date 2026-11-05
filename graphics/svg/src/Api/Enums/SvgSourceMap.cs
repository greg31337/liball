namespace svg;

/// <summary>Defines how a node or attribute relates to authored source.</summary>
public enum SvgSourceMapKind
{
    /// <summary>The region is unchanged from its authored source.</summary>
    Original = 0,
    /// <summary>The region was created without authored source.</summary>
    Generated = 1,
    /// <summary>The region was authored but has since been changed or repositioned.</summary>
    Rewritten = 2
}
