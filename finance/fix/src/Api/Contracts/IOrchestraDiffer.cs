namespace fix;

/// <summary>Compares Orchestra repositories deterministically.</summary>
public interface IOrchestraDiffer
{
    /// <summary>Compares two repositories.</summary>
    /// <param name="beforeRepository">The earlier repository.</param>
    /// <param name="afterRepository">The later repository.</param>
    /// <returns>The ordered repository changes.</returns>
    OrchestraDiff DiffRepositories(OrchestraRepository beforeRepository, OrchestraRepository afterRepository);
}
