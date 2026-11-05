using System.IO;

namespace fix;

/// <summary>Loads Orchestra 1.0 and 1.1 repositories into canonical dictionaries.</summary>
public class OrchestraDictionaryLoader : IDictionaryLoader
{
    /// <summary>Creates an Orchestra dictionary loader.</summary>
    public OrchestraDictionaryLoader()
    {
    }

    /// <summary>Loads the structural dictionary content of one Orchestra repository.</summary>
    /// <param name="source">The readable Orchestra repository stream.</param>
    /// <param name="options">The loading options and resource limits.</param>
    /// <returns>The canonical dictionary and ordered mapping diagnostics.</returns>
    public DictionaryLoadResult LoadDictionary(Stream source, FixDictionaryOptions options)
    {
        return OrchestraDictionaryParser.ParseRepositoryDictionary(source, options);
    }
}
