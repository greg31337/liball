namespace iso20022;

/// <summary>Executes explicit ISO 20022 migration plans with complete reporting.</summary>
public class MessageMigrator : IMessageMigrator
{
    /// <summary>Initializes a deterministic message migrator.</summary>
    public MessageMigrator()
    {
    }

    /// <summary>Migrates one raw, generic, or typed message through an explicit plan.</summary>
    /// <param name="request">The source representation, plan, fallback authorization, and validation policy.</param>
    /// <returns>The migrated output and complete execution report.</returns>
    public MigrationResult MigrateMessage(MigrationRequest? request)
    {
        if (request == null)
        {
            MigrationResult missing = new MigrationResult();
            missing.ErrorCode = "migration.request_missing";
            missing.ErrorMessage = "A migration execution request is required.";
            return missing;
        }

        return MigrationExecutor.ExecuteMigration(request);
    }
}
