namespace svg;

/// <summary>Defines the high-level XML and value formatting mode.</summary>
public enum SvgWriteMode
{
    /// <summary>Reuse eligible unchanged source regions when available.</summary>
    SourcePreserving = 0,
    /// <summary>Write normalized deterministic XML and value text.</summary>
    Canonical = 1,
    /// <summary>Write normalized XML with structural indentation.</summary>
    Pretty = 2,
    /// <summary>Write normalized compact XML and value text.</summary>
    Compact = 3
}

/// <summary>Defines XML declaration output policy.</summary>
public enum SvgXmlDeclarationPolicy
{
    /// <summary>Do not write an XML declaration.</summary>
    Omit = 0,
    /// <summary>Write a configured XML 1.0 declaration.</summary>
    Include = 1,
    /// <summary>Reuse an authored XML declaration when source text retains one.</summary>
    Preserve = 2
}

/// <summary>Defines document type output policy.</summary>
public enum SvgWriteDoctypePolicy
{
    /// <summary>Do not write document type nodes.</summary>
    Omit = 0,
    /// <summary>Write retained document type nodes without resolving entities.</summary>
    Preserve = 1,
    /// <summary>Fail when the document contains a document type node.</summary>
    Reject = 2
}

/// <summary>Defines empty-element tag output policy.</summary>
public enum SvgEmptyElementPolicy
{
    /// <summary>Use one self-closing tag for an element without children.</summary>
    SelfClosing = 0,
    /// <summary>Use separate start and end tags for an element without children.</summary>
    ExplicitEndTag = 1
}

/// <summary>Defines attribute quote selection.</summary>
public enum SvgAttributeQuoteStyle
{
    /// <summary>Use double quotation marks.</summary>
    Double = 0,
    /// <summary>Use single quotation marks.</summary>
    Single = 1,
    /// <summary>Choose the quotation mark requiring fewer escaped characters.</summary>
    MinimizeEscapes = 2
}

/// <summary>Defines deterministic attribute ordering.</summary>
public enum SvgWriteAttributeOrder
{
    /// <summary>Retain source attribute order after required namespace declarations.</summary>
    Source = 0,
    /// <summary>Sort namespace declarations by prefix and other attributes by expanded name.</summary>
    NamespaceThenExpandedName = 1
}

/// <summary>Defines stable XML serialization failures.</summary>
public enum SvgWriteError
{
    /// <summary>No write operation failed.</summary>
    None = 0,
    /// <summary>The document, node, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>An XML local name could not be written.</summary>
    InvalidName = 2,
    /// <summary>A character, comment, instruction, or document type was not valid XML.</summary>
    InvalidXml = 3,
    /// <summary>Two attributes had the same expanded name.</summary>
    DuplicateAttribute = 4,
    /// <summary>The configured document type policy rejected retained content.</summary>
    DoctypeRejected = 5,
    /// <summary>A node, attribute, depth, or output-character bound was exceeded.</summary>
    LimitExceeded = 6,
    /// <summary>The write operation was cancelled.</summary>
    Cancelled = 7,
    /// <summary>A caller-provided destination buffer was too small.</summary>
    DestinationTooSmall = 8
}
