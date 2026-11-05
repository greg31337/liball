namespace sql;

/// <summary>Specifies how an AST node differs between two syntax trees.</summary>
public enum SqlAstDiffKind
{
    /// <summary>The node exists in both trees with the same structural content.</summary>
    Unchanged = 0,
    /// <summary>The node exists only in the modified tree.</summary>
    Added = 1,
    /// <summary>The node exists only in the original tree.</summary>
    Removed = 2,
    /// <summary>The path exists in both trees with different structural content.</summary>
    Replaced = 3
}
