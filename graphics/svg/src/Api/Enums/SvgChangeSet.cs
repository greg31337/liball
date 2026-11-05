namespace svg;

/// <summary>Defines recorded editable-document change kinds.</summary>
public enum SvgChangeKind
{
    /// <summary>No change was recorded.</summary>
    None = 0,
    /// <summary>A node was inserted.</summary>
    NodeInsert = 1,
    /// <summary>A node was removed.</summary>
    NodeRemove = 2,
    /// <summary>A node was moved.</summary>
    NodeMove = 3,
    /// <summary>A node was replaced.</summary>
    NodeReplace = 4,
    /// <summary>An attribute was inserted.</summary>
    AttributeInsert = 5,
    /// <summary>An attribute was removed.</summary>
    AttributeRemove = 6,
    /// <summary>An attribute was moved.</summary>
    AttributeMove = 7,
    /// <summary>An attribute was replaced.</summary>
    AttributeReplace = 8,
    /// <summary>Text content or adjacent text structure changed.</summary>
    Text = 9
}

/// <summary>Grades the rendering or semantic impact of a processing mutation.</summary>
public enum SvgProcessChangeSeverity
{
    /// <summary>No explicit impact classification was assigned.</summary>
    None = 0,
    /// <summary>The same content was represented using a different structure or resource form.</summary>
    Representation = 1,
    /// <summary>The mutation introduces a bounded geometric or numeric approximation.</summary>
    Approximation = 2,
    /// <summary>The mutation replaces authored semantics with derived output.</summary>
    Destructive = 3
}
