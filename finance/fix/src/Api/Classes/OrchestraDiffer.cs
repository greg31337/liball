namespace fix;

/// <summary>Compares complete Orchestra repository graphs deterministically.</summary>
public class OrchestraDiffer : IOrchestraDiffer
{
    /// <summary>Compares two repositories and reports stable semantic changes.</summary>
    /// <param name="beforeRepository">The earlier repository.</param>
    /// <param name="afterRepository">The later repository.</param>
    /// <returns>The ordered repository changes and comparison diagnostics.</returns>
    public OrchestraDiff DiffRepositories(OrchestraRepository beforeRepository, OrchestraRepository afterRepository)
    {
        return OrchestraDifference.CompareRepositories(beforeRepository, afterRepository);
    }
}
