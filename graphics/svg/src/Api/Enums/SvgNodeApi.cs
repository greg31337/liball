namespace svg;

/// <summary>Defines stable editable-document mutation failures.</summary>
public enum SvgMutationError
{
    /// <summary>No mutation failure occurred.</summary>
    None = 0,
    /// <summary>A required mutation argument was missing.</summary>
    InvalidArgument = 1,
    /// <summary>The requested parent cannot contain child nodes.</summary>
    InvalidParent = 2,
    /// <summary>A document node cannot be inserted as a child.</summary>
    InvalidChild = 3,
    /// <summary>The requested index is outside the valid insertion or item range.</summary>
    IndexOutOfRange = 4,
    /// <summary>The requested child is already attached and must be moved explicitly.</summary>
    NodeAlreadyAttached = 5,
    /// <summary>The requested attribute is already attached and must be moved explicitly.</summary>
    AttributeAlreadyAttached = 6,
    /// <summary>The requested node is not a child of the supplied parent.</summary>
    NodeNotChild = 7,
    /// <summary>The requested attribute is not owned by the supplied element.</summary>
    AttributeNotOwned = 8,
    /// <summary>The mutation would create a node ancestry cycle.</summary>
    Cycle = 9,
    /// <summary>The mutation would create more than one document element.</summary>
    MultipleDocumentElements = 10,
    /// <summary>The target element already has an attribute with the same expanded name.</summary>
    DuplicateAttribute = 11,
    /// <summary>Existing public links or ownership fields are inconsistent.</summary>
    InvalidOwnership = 12
}
