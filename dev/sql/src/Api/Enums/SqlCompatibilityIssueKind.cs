namespace sql;

/// <summary>Specifies the category of a target-dialect compatibility issue.</summary>
public enum SqlCompatibilityIssueKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents a statement unavailable in the target dialect.</summary>
    UnsupportedStatement = 1,
    /// <summary>Represents a clause unavailable in the target dialect.</summary>
    UnsupportedClause = 2,
    /// <summary>Represents an expression form unavailable in the target dialect.</summary>
    UnsupportedExpression = 3,
    /// <summary>Represents a literal form unavailable in the target dialect.</summary>
    UnsupportedLiteral = 4,
    /// <summary>Represents identifier or string quoting unavailable in the target dialect.</summary>
    UnsupportedQuoting = 5,
    /// <summary>Represents an operator unavailable in the target dialect.</summary>
    UnsupportedOperator = 6,
    /// <summary>Represents preserved syntax whose compatibility cannot be established.</summary>
    UnsupportedSyntax = 7
}
