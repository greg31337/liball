namespace iso20022;

/// <summary>Identifies one explicit deterministic migration operation.</summary>
public enum MigrationActionKind
{
    /// <summary>The migration action is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>Copies source content without changing its meaning.</summary>
    Copy = 1,
    /// <summary>Copies content to a renamed target.</summary>
    Rename = 2,
    /// <summary>Moves content to a different target location.</summary>
    Move = 3,
    /// <summary>Converts content through an explicitly identified conversion.</summary>
    Convert = 4,
    /// <summary>Supplies an explicit target default.</summary>
    DefaultValue = 5,
    /// <summary>Splits one source value or component into several targets.</summary>
    Split = 6,
    /// <summary>Merges several source values or components into one target.</summary>
    Merge = 7,
    /// <summary>Maps a source code through an explicit code table.</summary>
    CodeMap = 8,
    /// <summary>Drops source content with an explicit data-loss decision.</summary>
    Drop = 9,
    /// <summary>Requires a caller-implemented rule identified by name.</summary>
    Custom = 10
}
