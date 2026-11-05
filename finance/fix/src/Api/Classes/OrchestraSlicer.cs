namespace fix;

/// <summary>Selects dependency-closed subsets of Orchestra repositories.</summary>
public class OrchestraSlicer : IOrchestraSlicer
{
    /// <summary>Selects messages or categories and scenarios with their required definitions.</summary>
    /// <param name="repository">The source Orchestra repository.</param>
    /// <param name="messageNames">Message names, message types, or category names to select; an empty array selects all.</param>
    /// <param name="scenarios">Scenario names to select; an empty array selects every available scenario.</param>
    /// <returns>The independent sliced repository and ordered diagnostics.</returns>
    public OrchestraReadResult SliceRepository(OrchestraRepository repository, string[] messageNames, string[] scenarios)
    {
        return OrchestraSlicing.SliceRepository(repository, messageNames, scenarios);
    }
}
