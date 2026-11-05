namespace iso20022;

/// <summary>Builds deterministic inspectable migration plans between exact message versions.</summary>
public interface IMigrationPlanner
{
    /// <summary>Plans source-to-target copy, conversion, mapping, default, and loss operations.</summary>
    /// <param name="request">The versions, difference, explicit mapping, fallback policy, and limits.</param>
    /// <returns>The ordered migration plan.</returns>
    MigrationPlan PlanMigration(MigrationRequest? request);
}
