namespace iso20022;

/// <summary>Builds deterministic inspectable plans between exact ISO 20022 message versions.</summary>
public class MigrationPlanner : IMigrationPlanner
{
    /// <summary>Initializes a deterministic migration planner.</summary>
    public MigrationPlanner()
    {
    }

    /// <summary>Plans source-to-target copy, conversion, mapping, default, and loss operations.</summary>
    /// <param name="request">The versions, difference, explicit mapping, fallback policy, and limits.</param>
    /// <returns>The ordered migration plan.</returns>
    public MigrationPlan PlanMigration(MigrationRequest? request)
    {
        if (request == null)
        {
            MigrationPlan missing = new MigrationPlan();
            missing.ErrorCode = "migration.request_missing";
            missing.ErrorMessage = "A migration planning request is required.";
            return missing;
        }

        return MigrationPlanBuilder.BuildPlan(request);
    }
}
