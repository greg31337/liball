namespace uri;

/// <summary>
/// Identifies standard and extension Web Link parameters.
/// </summary>
public enum WebLinkParameterKind
{
    /// <summary>An application-defined target attribute.</summary>
    Extension = 0,

    /// <summary>The forward <c>rel</c> relation list.</summary>
    Relation = 1,

    /// <summary>The deprecated reverse <c>rev</c> relation list.</summary>
    ReverseRelation = 2,

    /// <summary>The explicit link-context <c>anchor</c>.</summary>
    Anchor = 3,

    /// <summary>A repeatable <c>hreflang</c> language hint.</summary>
    HrefLang = 4,

    /// <summary>The <c>media</c> destination-medium hint.</summary>
    Media = 5,

    /// <summary>The plain <c>title</c> label.</summary>
    Title = 6,

    /// <summary>The RFC 8187 <c>title*</c> label.</summary>
    TitleExtended = 7,

    /// <summary>The <c>type</c> media type hint.</summary>
    Type = 8
}
