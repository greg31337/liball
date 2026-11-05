namespace odf;

/// <summary>Identifies a replayable collaboration operation.</summary>
public enum OdfOperationKind
{
    /// <summary>The operation kind is absent or unsupported.</summary>
    Unknown = 0,
    /// <summary>Insert a captured XML subtree.</summary>
    InsertNode = 1,
    /// <summary>Remove an XML subtree.</summary>
    RemoveNode = 2,
    /// <summary>Move an XML subtree to another parent or position.</summary>
    MoveNode = 3,
    /// <summary>Set an XML attribute value.</summary>
    SetAttribute = 4,
    /// <summary>Remove an XML attribute.</summary>
    RemoveAttribute = 5,
    /// <summary>Replace a node's text content.</summary>
    SetText = 6
}
