namespace iso20022;

/// <summary>Executes explicit migration plans while reporting unmapped data and target validation.</summary>
public interface IMessageMigrator
{
    /// <summary>Migrates one raw, generic, or typed message through an explicit plan.</summary>
    /// <param name="request">The source representation, plan, fallback authorization, and validation policy.</param>
    /// <returns>The migrated output and complete execution report.</returns>
    MigrationResult MigrateMessage(MigrationRequest? request);
}
