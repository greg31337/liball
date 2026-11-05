namespace iso20022;

/// <summary>Identifies one explicit inspectable message-mapping operation.</summary>
public enum MappingActionKind
{
    /// <summary>The mapping action is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>Copies one source value to the corresponding target.</summary>
    Copy = 1,
    /// <summary>Copies one source value to a differently named target.</summary>
    Rename = 2,
    /// <summary>Moves one source value to a different target location.</summary>
    Move = 3,
    /// <summary>Applies an explicitly identified value conversion.</summary>
    Convert = 4,
    /// <summary>Writes a caller-supplied constant target value.</summary>
    Constant = 5,
    /// <summary>Splits one source value among several targets.</summary>
    Split = 6,
    /// <summary>Merges several source values into one target.</summary>
    Merge = 7,
    /// <summary>Maps a code through aligned explicit source and target values.</summary>
    CodeMap = 8,
    /// <summary>Intentionally omits a source value.</summary>
    Drop = 9,
    /// <summary>Requires a caller rule identified without embedding a delegate.</summary>
    Custom = 10
}
