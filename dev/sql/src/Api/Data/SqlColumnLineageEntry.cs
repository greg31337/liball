using System;

namespace sql;

/// <summary>Maps one query output or DML target column to its value expression and source columns.</summary>
public class SqlColumnLineageEntry
{
    /// <summary>The syntax target category.</summary>
    public SqlColumnLineageTargetKind TargetKind;
    /// <summary>The established origin category.</summary>
    public SqlColumnLineageOriginKind OriginKind;
    /// <summary>The output or assigned column name, or an empty string when it is not known.</summary>
    public string OutputName;
    /// <summary>The explicit DML target column, or <c>null</c> for query outputs and implicit insert targets.</summary>
    public SqlName? TargetColumn;
    /// <summary>The resolved DML target table, or <c>null</c> for query outputs or unknown targets.</summary>
    public SqlCatalogTable? TargetTable;
    /// <summary>The result-column syntax node, or <c>null</c> for DML assignments.</summary>
    public SqlResultColumn? ResultColumn;
    /// <summary>The assignment syntax node, or <c>null</c> when the target has no explicit assignment node.</summary>
    public SqlAssignment? Assignment;
    /// <summary>The value expression, or <c>null</c> for wildcard expansions or unavailable expressions.</summary>
    public SqlExpression? Expression;
    /// <summary>The containing query, or <c>null</c> for DML-only targets.</summary>
    public SqlQuery? Query;
    /// <summary>The contributing source columns in resolution order.</summary>
    public SqlColumnLineageSource[] Sources;
    /// <summary>The half-open source span covered by the target.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a column-lineage entry.</summary>
    /// <param name="targetKind">The syntax target category.</param>
    /// <param name="originKind">The established origin category.</param>
    /// <param name="outputName">The output or assigned column name.</param>
    /// <param name="targetColumn">The explicit target column, or <c>null</c>.</param>
    /// <param name="targetTable">The resolved target table, or <c>null</c>.</param>
    /// <param name="resultColumn">The result-column node, or <c>null</c>.</param>
    /// <param name="assignment">The assignment node, or <c>null</c>.</param>
    /// <param name="expression">The value expression, or <c>null</c>.</param>
    /// <param name="query">The containing query, or <c>null</c>.</param>
    /// <param name="sources">The contributing source columns.</param>
    /// <param name="span">The source span associated with the target.</param>
    /// <exception cref="ArgumentNullException">outputName, sources, span, or a source is <c>null</c>.</exception>
    public SqlColumnLineageEntry(SqlColumnLineageTargetKind targetKind, SqlColumnLineageOriginKind originKind, string outputName, SqlName? targetColumn, SqlCatalogTable? targetTable, SqlResultColumn? resultColumn, SqlAssignment? assignment, SqlExpression? expression, SqlQuery? query, SqlColumnLineageSource[] sources, SqlTextSpan span)
    {
        if (outputName == null)
        {
            throw new ArgumentNullException("outputName");
        }

        ValidateSources(sources);
        Span = span ?? throw new ArgumentNullException("span");
        TargetKind = targetKind;
        OriginKind = originKind;
        OutputName = outputName;
        TargetColumn = targetColumn;
        TargetTable = targetTable;
        ResultColumn = resultColumn;
        Assignment = assignment;
        Expression = expression;
        Query = query;
        Sources = sources;
    }

    internal static void ValidateSources(SqlColumnLineageSource[] sources)
    {
        if (sources == null)
        {
            throw new ArgumentNullException("sources");
        }

        int index = 0;
        while (index < sources.Length)
        {
            if (sources[index] == null)
            {
                throw new ArgumentNullException("sources");
            }

            index = index + 1;
        }
    }
}
