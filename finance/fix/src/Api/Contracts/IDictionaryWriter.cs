using System.IO;

namespace fix;

/// <summary>Writes a canonical FIX dictionary to a stream.</summary>
public interface IDictionaryWriter
{
    /// <summary>Writes one dictionary using deterministic ordering.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="dictionary">The dictionary to write.</param>
    /// <param name="options">The writing options and resource limits.</param>
    /// <returns>The dictionary and ordered write diagnostics.</returns>
    DictionaryLoadResult WriteDictionary(Stream destination, FixDictionary dictionary, FixDictionaryOptions options);
}
