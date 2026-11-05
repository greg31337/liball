using System.IO;

namespace fix;

/// <summary>Writes canonical dictionaries using deterministic QuickFIX XML.</summary>
public class QuickFixDictionaryWriter : IDictionaryWriter
{
    /// <summary>Creates a QuickFIX XML dictionary writer.</summary>
    public QuickFixDictionaryWriter()
    {
    }

    /// <summary>Writes one canonical dictionary using deterministic ordering.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="dictionary">The dictionary to write.</param>
    /// <param name="options">The writing options and resource limits.</param>
    /// <returns>The dictionary and ordered write diagnostics.</returns>
    public DictionaryLoadResult WriteDictionary(Stream destination, FixDictionary dictionary, FixDictionaryOptions options)
    {
        return QuickFixXmlEmitter.WriteDictionaryXml(destination, dictionary, options);
    }
}
