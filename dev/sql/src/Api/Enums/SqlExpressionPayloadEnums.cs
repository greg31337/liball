namespace sql;

/// <summary>Specifies an SQL distinctness predicate.</summary>
public enum SqlDistinctnessKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents <c>IS DISTINCT FROM</c>.</summary>
    IsDistinctFrom = 1,
    /// <summary>Represents <c>IS NOT DISTINCT FROM</c>.</summary>
    IsNotDistinctFrom = 2
}

/// <summary>Specifies the right-hand form of an <c>IN</c> predicate.</summary>
public enum SqlInTargetKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents a parenthesized expression list.</summary>
    List = 1,
    /// <summary>Represents a parenthesized subquery.</summary>
    Subquery = 2,
    /// <summary>Represents a table name.</summary>
    Table = 3,
    /// <summary>Represents a table-valued function call.</summary>
    Function = 4
}

/// <summary>Specifies the spelling used for a cast expression.</summary>
public enum SqlCastSyntaxKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents <c>CAST(value AS type)</c>.</summary>
    CastFunction = 1,
    /// <summary>Represents PostgreSQL <c>value::type</c> syntax.</summary>
    PostgreSql = 2
}

/// <summary>Specifies a non-standard operator expression category.</summary>
public enum SqlOperatorExpressionKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents a dialect-defined JSON operator.</summary>
    Json = 1,
    /// <summary>Represents a caller-defined operator.</summary>
    Custom = 2
}
