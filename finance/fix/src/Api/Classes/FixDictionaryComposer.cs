namespace fix;

/// <summary>Composes canonical dictionaries with deterministic overlay semantics.</summary>
public class FixDictionaryComposer : IDictionaryComposer
{
    /// <summary>Creates a canonical dictionary composer.</summary>
    public FixDictionaryComposer()
    {
    }

    /// <summary>Applies additions, replacements, and explicit removals from an overlay.</summary>
    /// <param name="baseDictionary">The base dictionary.</param>
    /// <param name="overlayDictionary">The overlay dictionary.</param>
    /// <param name="options">The composition options and resource limits.</param>
    /// <returns>A detached composed dictionary with ordered diagnostics.</returns>
    public DictionaryLoadResult ComposeDictionary(FixDictionary baseDictionary, FixDictionary overlayDictionary, FixDictionaryOptions options)
    {
        return DictionaryComposition.ComposeGraphs(baseDictionary, overlayDictionary, options);
    }

    /// <summary>Creates a deterministic semantic comparison of two dictionaries.</summary>
    /// <param name="beforeDictionary">The earlier dictionary.</param>
    /// <param name="afterDictionary">The later dictionary.</param>
    /// <returns>The dictionary identities and ordered semantic changes.</returns>
    public DictionaryDiff DiffDictionaries(FixDictionary beforeDictionary, FixDictionary afterDictionary)
    {
        return DictionaryDifference.CompareGraphs(beforeDictionary, afterDictionary);
    }
}
