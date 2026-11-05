namespace svg;

/// <summary>Defines deterministic structural patch operation kinds.</summary>
public enum SvgPatchOperationKind
{
    /// <summary>No patch operation.</summary>
    None = 0,
    /// <summary>Insert a node at a parent child index.</summary>
    NodeInsert = 1,
    /// <summary>Remove a node at a stable path.</summary>
    NodeRemove = 2,
    /// <summary>Replace a node subtree at a stable path.</summary>
    NodeReplace = 3,
    /// <summary>Replace a character-like node value.</summary>
    NodeValue = 4,
    /// <summary>Insert an attribute at an ordered index.</summary>
    AttributeInsert = 5,
    /// <summary>Remove an attribute at an ordered index.</summary>
    AttributeRemove = 6,
    /// <summary>Replace an attribute at an ordered index.</summary>
    AttributeReplace = 7,
    /// <summary>Move an attribute between ordered indexes.</summary>
    AttributeMove = 8
}

/// <summary>Defines stable patch application failures.</summary>
public enum SvgPatchError
{
    /// <summary>No patch failure occurred.</summary>
    None = 0,
    /// <summary>A required argument or operation field was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The document version precondition failed.</summary>
    VersionConflict = 2,
    /// <summary>A stable path could not be resolved.</summary>
    PathConflict = 3,
    /// <summary>An expected node, attribute, value, index, or count did not match.</summary>
    PreconditionFailed = 4,
    /// <summary>An invariant-preserving mutation rejected an operation.</summary>
    MutationFailed = 5,
    /// <summary>The target document already has an active transaction.</summary>
    ActiveTransaction = 6
}
