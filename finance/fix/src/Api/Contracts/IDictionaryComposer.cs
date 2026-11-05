namespace fix;

/// <summary>Composes and compares canonical FIX dictionaries.</summary>
public interface IDictionaryComposer
{
    /// <summary>Applies a deterministic overlay to a base dictionary.</summary>
    /// <param name="baseDictionary">The base dictionary.</param>
    /// <param name="overlayDictionary">The overlay dictionary.</param>
    /// <param name="options">The composition options and resource limits.</param>
    /// <returns>The composed dictionary and ordered diagnostics.</returns>
    DictionaryLoadResult ComposeDictionary(FixDictionary baseDictionary, FixDictionary overlayDictionary, FixDictionaryOptions options);
    /// <summary>Compares two canonical dictionaries.</summary>
    /// <param name="beforeDictionary">The earlier dictionary.</param>
    /// <param name="afterDictionary">The later dictionary.</param>
    /// <returns>The deterministic dictionary changes.</returns>
    DictionaryDiff DiffDictionaries(FixDictionary beforeDictionary, FixDictionary afterDictionary);
}
