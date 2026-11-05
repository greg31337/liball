namespace sql;

/// <summary>Builds conservative relational logical plans for supported query and DML trees.</summary>
public class SqlLogicalPlannerService : ISqlLogicalPlanner
{
    /// <summary>Initializes a logical-plan conversion service.</summary>
    public SqlLogicalPlannerService()
    {
    }

    /// <summary>Builds a conservative relational logical plan using catalog metadata.</summary>
    /// <param name="script">The syntax tree to convert.</param>
    /// <param name="catalog">The catalog used for semantic resolution.</param>
    /// <returns>The logical plan and diagnostics for unknown semantics.</returns>
    public SqlLogicalPlanResult BuildPlan(SqlScript script, SqlCatalog catalog)
    {
        return SqlLogicalPlanConversion.BuildPlan(script, catalog);
    }
}
