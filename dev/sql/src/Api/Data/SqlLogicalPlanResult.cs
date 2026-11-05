using System;

namespace sql;

/// <summary>Contains a relational logical plan and diagnostics for unresolved semantics.</summary>
public class SqlLogicalPlanResult
{
    /// <summary>The unchanged syntax tree converted by the operation.</summary>
    public SqlScript Script;
    /// <summary>The catalog used for semantic resolution.</summary>
    public SqlCatalog Catalog;
    /// <summary>The script-level logical-plan root.</summary>
    public SqlLogicalPlanNode Root;
    /// <summary>The diagnostics for unknown or unsupported plan nodes.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a logical-plan conversion result.</summary>
    /// <param name="script">The source script.</param>
    /// <param name="catalog">The semantic catalog.</param>
    /// <param name="root">The script-level plan root.</param>
    /// <param name="diagnostics">The conversion diagnostics.</param>
    /// <exception cref="ArgumentNullException">script, catalog, root, or diagnostics is <c>null</c>.</exception>
    public SqlLogicalPlanResult(SqlScript script, SqlCatalog catalog, SqlLogicalPlanNode root, SqlDiagnosticBag diagnostics)
    {
        Script = script ?? throw new ArgumentNullException("script");
        Catalog = catalog ?? throw new ArgumentNullException("catalog");
        Root = root ?? throw new ArgumentNullException("root");
        Diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
    }
}
