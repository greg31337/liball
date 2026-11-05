using System.IO;

namespace fix;

/// <summary>Loads QuickFIX XML dictionaries into the canonical dictionary model.</summary>
public class QuickFixDictionaryLoader : IDictionaryLoader
{
    /// <summary>Creates a QuickFIX XML dictionary loader.</summary>
    public QuickFixDictionaryLoader()
    {
    }

    /// <summary>Loads and normalizes one QuickFIX XML dictionary source.</summary>
    /// <param name="source">The readable QuickFIX XML stream.</param>
    /// <param name="options">The loading options and resource limits.</param>
    /// <returns>The canonical dictionary and ordered diagnostics.</returns>
    public DictionaryLoadResult LoadDictionary(Stream source, FixDictionaryOptions options)
    {
        return QuickFixXmlParser.ParseDictionary(source, options);
    }
}
