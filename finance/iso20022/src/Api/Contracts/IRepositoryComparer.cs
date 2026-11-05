namespace iso20022;

/// <summary>Compares complete normalized repository snapshots with explicit matching policy.</summary>
public interface IRepositoryComparer
{
    /// <summary>Compares two repository snapshots and returns deterministic graph differences.</summary>
    /// <param name="request">The source, target, overrides, matching policy, and limits.</param>
    /// <returns>The stable repository difference result.</returns>
    RepositoryDiff CompareRepositories(RepositoryComparisonRequest? request);
}
