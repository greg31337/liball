using System;

namespace sql;

/// <summary>Identifies one resolved or unresolved source-column reference contributing to a lineage target.</summary>
public class SqlColumnLineageSource
{
    /// <summary>The source name as written in the expression, or <c>null</c> for an expanded wildcard.</summary>
    public SqlName? Reference;
    /// <summary>The resolved catalog table, or <c>null</c>.</summary>
    public SqlCatalogTable? Table;
    /// <summary>The resolved catalog view, or <c>null</c>.</summary>
    public SqlCatalogView? View;
    /// <summary>The resolved catalog column, or <c>null</c>.</summary>
    public SqlCatalogColumn? Column;

    /// <summary>Initializes a source-column lineage reference.</summary>
    /// <param name="reference">The source name as written, or <c>null</c>.</param>
    /// <param name="table">The resolved table, or <c>null</c>.</param>
    /// <param name="view">The resolved view, or <c>null</c>.</param>
    /// <param name="column">The resolved column, or <c>null</c>.</param>
    /// <exception cref="ArgumentException">table and view are both non-null.</exception>
    public SqlColumnLineageSource(SqlName? reference, SqlCatalogTable? table, SqlCatalogView? view, SqlCatalogColumn? column)
    {
        if (table != null && view != null)
        {
            throw new ArgumentException("a lineage source cannot resolve to both a table and a view");
        }

        Reference = reference;
        Table = table;
        View = view;
        Column = column;
    }
}
