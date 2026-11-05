namespace fix;

/// <summary>Resolves immutable dictionary snapshots by identity.</summary>
public interface IDictionaryProvider
{
    /// <summary>Resolves one transport or application dictionary.</summary>
    /// <param name="identity">The requested dictionary identity.</param>
    /// <returns>The resolved dictionary and ordered diagnostics.</returns>
    DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity);
}
