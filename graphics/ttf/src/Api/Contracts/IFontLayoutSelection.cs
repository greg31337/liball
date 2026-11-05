namespace ttf;

/// <summary>Defines one resolved OpenType layout selection.</summary>
public interface IFontLayoutSelection
{
    /// <summary>Reports whether a script was resolved.</summary>
    /// <returns>True when resolved; otherwise false.</returns>
    bool HasScript();

    /// <summary>Gets the resolved script tag.</summary>
    /// <returns>The script tag, or zero when unresolved.</returns>
    int GetScriptTag();

    /// <summary>Reports whether the default script was used.</summary>
    /// <returns>True when fallback selected DFLT; otherwise false.</returns>
    bool UsedDefaultScript();

    /// <summary>Reports whether a language system was resolved.</summary>
    /// <returns>True when resolved; otherwise false.</returns>
    bool HasLanguageSystem();

    /// <summary>Gets the resolved language-system tag.</summary>
    /// <returns>The language tag, or zero for default or unresolved.</returns>
    int GetLanguageTag();

    /// <summary>Reports whether the default language system was used.</summary>
    /// <returns>True when default selection or fallback occurred.</returns>
    bool UsedDefaultLanguageSystem();

    /// <summary>Gets the available required and optional feature count.</summary>
    /// <returns>The feature count.</returns>
    int GetAvailableFeatureCount();

    /// <summary>Gets one available feature.</summary>
    /// <param name="index">The zero-based available-feature index.</param>
    /// <returns>The feature model.</returns>
    FontLayoutFeature GetAvailableFeature(int index);

    /// <summary>Reports whether a feature tag is available.</summary>
    /// <param name="featureTag">The big-endian feature tag.</param>
    /// <returns>True when available; otherwise false.</returns>
    bool HasFeature(int featureTag);

    /// <summary>Gets the selected lookup count.</summary>
    /// <returns>The lookup count.</returns>
    int GetLookupCount();

    /// <summary>Gets one selected lookup.</summary>
    /// <param name="index">The zero-based selected-lookup index.</param>
    /// <returns>The selected lookup and its ranges.</returns>
    FontLayoutSelectedLookup GetLookup(int index);
}
