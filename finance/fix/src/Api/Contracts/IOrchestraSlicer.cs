namespace fix;

/// <summary>Selects a deterministic subset of an Orchestra repository.</summary>
public interface IOrchestraSlicer
{
    /// <summary>Slices a repository to selected messages and scenarios.</summary>
    /// <param name="repository">The source repository.</param>
    /// <param name="messageNames">The selected message names.</param>
    /// <param name="scenarios">The selected scenario names.</param>
    /// <returns>The sliced repository and ordered diagnostics.</returns>
    OrchestraReadResult SliceRepository(OrchestraRepository repository, string[] messageNames, string[] scenarios);
}
