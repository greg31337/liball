using System.IO;

namespace fix;

/// <summary>Loads a canonical FIX dictionary from a stream.</summary>
public interface IDictionaryLoader
{
    /// <summary>Loads and normalizes one dictionary source.</summary>
    /// <param name="source">The readable dictionary stream.</param>
    /// <param name="options">The loading options and resource limits.</param>
    /// <returns>The dictionary and ordered diagnostics.</returns>
    DictionaryLoadResult LoadDictionary(Stream source, FixDictionaryOptions options);
}
