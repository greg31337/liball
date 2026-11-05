namespace sql;

/// <summary>Defines optional conversion from SQL syntax trees to relational logical plans.</summary>
public interface ISqlLogicalPlanner
{
    /// <summary>Builds a conservative relational logical plan using catalog metadata.</summary>
    /// <param name="script">The syntax tree to convert.</param>
    /// <param name="catalog">The catalog used for semantic resolution.</param>
    /// <returns>The logical plan and diagnostics for unknown semantics.</returns>
    SqlLogicalPlanResult BuildPlan(SqlScript script, SqlCatalog catalog);
}
