namespace svg;

/// <summary>Identifies a computed-style cache invalidation dependency.</summary>
public enum SvgStyleInvalidationKind
{
    /// <summary>Every cached style was invalidated.</summary>
    All = 0,
    /// <summary>Tree structure or selector relationships changed.</summary>
    Tree = 1,
    /// <summary>An ordinary element attribute changed.</summary>
    Attribute = 2,
    /// <summary>An ID or class selector input changed.</summary>
    ClassOrId = 3,
    /// <summary>A stylesheet changed.</summary>
    StyleSheet = 4,
    /// <summary>A custom property changed.</summary>
    CustomProperty = 5,
    /// <summary>Viewport inputs changed.</summary>
    Viewport = 6,
    /// <summary>Media inputs changed.</summary>
    Media = 7,
    /// <summary>Caller font inputs changed.</summary>
    Fonts = 8,
    /// <summary>Host interaction state changed.</summary>
    Interaction = 9
}
