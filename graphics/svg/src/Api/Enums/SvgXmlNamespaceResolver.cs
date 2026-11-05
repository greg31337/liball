namespace svg;

/// <summary>Defines stable XML namespace-processing failures.</summary>
public enum SvgXmlNamespaceError
{
    /// <summary>No namespace failure occurred.</summary>
    None = 0,
    /// <summary>An argument or validated token field was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A name was not a namespace-qualified name.</summary>
    InvalidQualifiedName = 2,
    /// <summary>A used namespace prefix had no in-scope declaration.</summary>
    UndeclaredPrefix = 3,
    /// <summary>A prefixed namespace declaration used an empty namespace name.</summary>
    EmptyPrefixedNamespace = 4,
    /// <summary>A reserved XML or XMLNS prefix was used incorrectly.</summary>
    ReservedPrefix = 5,
    /// <summary>A reserved XML or XMLNS namespace name was bound incorrectly.</summary>
    ReservedNamespace = 6,
    /// <summary>Two attributes had the same expanded name.</summary>
    DuplicateAttribute = 7
}
