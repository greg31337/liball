namespace sql;

/// <summary>Specifies the category of preserved unsupported SQL syntax.</summary>
public enum SqlUnsupportedSyntaxKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents a statement form that could not be classified.</summary>
    UnknownStatement = 1,
    /// <summary>Represents syntax rejected by the selected dialect.</summary>
    DialectFeature = 2,
    /// <summary>Represents text skipped during parser recovery.</summary>
    SkippedText = 3
}
