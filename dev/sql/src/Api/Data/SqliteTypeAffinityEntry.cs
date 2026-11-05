namespace sql;

/// <summary>Associates a syntax or catalog target with its conservatively inferred SQLite type affinity.</summary>
public class SqliteTypeAffinityEntry
{
    /// <summary>The target category.</summary>
    public SqliteTypeAffinityTargetKind TargetKind;
    /// <summary>The inferred value affinity, or <see cref="SqliteTypeAffinityKind.Unknown"/>.</summary>
    public SqliteTypeAffinityKind Affinity;
    /// <summary>The target-column affinity for an assignment, or <see cref="SqliteTypeAffinityKind.Unknown"/>.</summary>
    public SqliteTypeAffinityKind TargetAffinity;
    /// <summary>The literal node, or <c>null</c>.</summary>
    public SqlLiteral? Literal;
    /// <summary>The catalog column, or <c>null</c>.</summary>
    public SqlCatalogColumn? CatalogColumn;
    /// <summary>The expression node, or <c>null</c>.</summary>
    public SqlExpression? Expression;
    /// <summary>The result-column node, or <c>null</c>.</summary>
    public SqlResultColumn? ResultColumn;
    /// <summary>The assignment node, or <c>null</c> for implicit insert and merge assignments.</summary>
    public SqlAssignment? Assignment;
    /// <summary>The related lineage mapping, or <c>null</c> for standalone nodes.</summary>
    public SqlColumnLineageEntry? Lineage;

    /// <summary>Initializes a SQLite type-affinity entry.</summary>
    /// <param name="targetKind">The target category.</param>
    /// <param name="affinity">The inferred value affinity.</param>
    /// <param name="targetAffinity">The assignment target affinity.</param>
    /// <param name="literal">The literal node, or <c>null</c>.</param>
    /// <param name="catalogColumn">The catalog column, or <c>null</c>.</param>
    /// <param name="expression">The expression node, or <c>null</c>.</param>
    /// <param name="resultColumn">The result-column node, or <c>null</c>.</param>
    /// <param name="assignment">The assignment node, or <c>null</c>.</param>
    /// <param name="lineage">The related lineage mapping, or <c>null</c>.</param>
    public SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind targetKind, SqliteTypeAffinityKind affinity, SqliteTypeAffinityKind targetAffinity, SqlLiteral? literal, SqlCatalogColumn? catalogColumn, SqlExpression? expression, SqlResultColumn? resultColumn, SqlAssignment? assignment, SqlColumnLineageEntry? lineage)
    {
        TargetKind = targetKind;
        Affinity = affinity;
        TargetAffinity = targetAffinity;
        Literal = literal;
        CatalogColumn = catalogColumn;
        Expression = expression;
        ResultColumn = resultColumn;
        Assignment = assignment;
        Lineage = lineage;
    }
}
