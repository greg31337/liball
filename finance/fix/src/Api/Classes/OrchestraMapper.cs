namespace fix;

/// <summary>Maps between loss-aware Orchestra repositories and canonical FIX dictionaries.</summary>
public class OrchestraMapper : IOrchestraMapper
{
    /// <summary>Maps one Orchestra repository to a canonical dictionary.</summary>
    /// <param name="repository">The source Orchestra repository.</param>
    /// <param name="options">The dictionary normalization and reference options.</param>
    /// <returns>The mapped canonical dictionary and ordered diagnostics.</returns>
    public DictionaryLoadResult MapRepositoryToDictionary(OrchestraRepository repository, FixDictionaryOptions options)
    {
        return OrchestraMapping.MapRepositoryToDictionary(repository, options);
    }

    /// <summary>Maps one canonical dictionary to an Orchestra repository.</summary>
    /// <param name="dictionary">The source canonical dictionary.</param>
    /// <param name="version">The target Orchestra schema generation.</param>
    /// <returns>The mapped Orchestra repository and ordered diagnostics.</returns>
    public OrchestraReadResult MapDictionaryToRepository(FixDictionary dictionary, OrchestraVersion version)
    {
        return OrchestraMapping.MapDictionaryToRepository(dictionary, version);
    }
}
