namespace fix;

/// <summary>Maps between Orchestra and the canonical FIX dictionary.</summary>
public interface IOrchestraMapper
{
    /// <summary>Maps an Orchestra repository to a canonical dictionary.</summary>
    /// <param name="repository">The source repository.</param>
    /// <param name="options">The dictionary mapping options.</param>
    /// <returns>The mapped dictionary and ordered diagnostics.</returns>
    DictionaryLoadResult MapRepositoryToDictionary(OrchestraRepository repository, FixDictionaryOptions options);
    /// <summary>Maps a canonical dictionary to an Orchestra repository.</summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="version">The target Orchestra schema version.</param>
    /// <returns>The mapped repository and ordered diagnostics.</returns>
    OrchestraReadResult MapDictionaryToRepository(FixDictionary dictionary, OrchestraVersion version);
}
