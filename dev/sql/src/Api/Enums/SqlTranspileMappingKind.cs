namespace sql;

/// <summary>Specifies a lossless syntax mapping applied during dialect transpilation.</summary>
public enum SqlTranspileMappingKind
{
    /// <summary>Represents an identifier rewritten with target-dialect quoting.</summary>
    IdentifierQuoting = 0,
    /// <summary>Represents a cast rewritten with target-dialect syntax.</summary>
    CastSyntax = 1
}
