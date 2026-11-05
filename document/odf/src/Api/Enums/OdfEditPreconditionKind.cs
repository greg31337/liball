namespace odf;

/// <summary>Identifies a condition that must hold before replaying an operation.</summary>
public enum OdfEditPreconditionKind
{
    /// <summary>The precondition kind is absent or unsupported.</summary>
    Unknown = 0,
    /// <summary>The target node must exist.</summary>
    NodeExists = 1,
    /// <summary>The target node must not exist.</summary>
    NodeAbsent = 2,
    /// <summary>The target node must have the expected parent.</summary>
    ParentEquals = 3,
    /// <summary>The target node must have the expected child index.</summary>
    ChildIndexEquals = 4,
    /// <summary>The selected attribute must have the expected presence and value.</summary>
    AttributeEquals = 5,
    /// <summary>The target node must have the expected text content.</summary>
    TextEquals = 6
}
