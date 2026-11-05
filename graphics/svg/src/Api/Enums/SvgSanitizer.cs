namespace svg;

/// <summary>Defines stable secure-static sanitization failures.</summary>
public enum SvgSanitizationError
{
    /// <summary>Sanitization completed without an operational failure.</summary>
    None = 0,
    /// <summary>The source document or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured traversal or parser bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>An internal document mutation could not be completed.</summary>
    MutationFailed = 3
}

/// <summary>Identifies one deterministic sanitizer output change.</summary>
public enum SvgSanitizationChangeKind
{
    /// <summary>An element or non-element node subtree was removed.</summary>
    NodeRemoved = 0,
    /// <summary>An XML attribute was removed.</summary>
    AttributeRemoved = 1,
    /// <summary>An XML attribute value was replaced.</summary>
    AttributeChanged = 2,
    /// <summary>A broken local reference was removed or rewritten.</summary>
    ReferenceRepaired = 3,
    /// <summary>A complete CSS rule was removed.</summary>
    CssRuleRemoved = 4,
    /// <summary>One CSS declaration or recovered declaration fragment was removed.</summary>
    CssDeclarationRemoved = 5,
    /// <summary>A required namespace declaration was added.</summary>
    NamespaceAdded = 6,
    /// <summary>An unused namespace declaration was removed.</summary>
    NamespaceRemoved = 7
}
