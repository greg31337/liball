namespace ttf;

/// <summary>Represents one resolved OpenType layout selection.</summary>
public class FontLayoutSelection : IFontLayoutSelection
{
    SfntLayoutSelection selection;
    FontLayoutTable owner;
    int[] availableFeatureIndices;

    internal FontLayoutSelection(
        SfntLayoutSelection selection,
        FontLayoutTable owner)
    {
        this.selection = selection;
        this.owner = owner;
        bool[] available = new bool[owner.GetFeatureCount()];
        SfntLayoutLanguageSystem? language = selection.GetLanguageSystem();
        if (language != null)
        {
            if (language.HasRequiredFeature())
            {
                available[language.GetRequiredFeatureIndex()] = true;
            }
            int featureIndex = 0;
            while (featureIndex < language.GetFeatureIndexCount())
            {
                available[language.GetFeatureIndex(featureIndex)] = true;
                featureIndex += 1;
            }
        }
        int count = 0;
        int index = 0;
        while (index < available.Length)
        {
            if (available[index]) { count += 1; }
            index += 1;
        }
        availableFeatureIndices = new int[count];
        int writeIndex = 0;
        index = 0;
        while (index < available.Length)
        {
            if (available[index])
            {
                availableFeatureIndices[writeIndex] = index;
                writeIndex += 1;
            }
            index += 1;
        }
    }

    /// <summary>Reports whether a script was resolved.</summary>
    /// <returns>True when resolved; otherwise false.</returns>
    public bool HasScript() { return selection.HasScript(); }

    /// <summary>Gets the resolved script tag.</summary>
    /// <returns>The script tag, or zero when unresolved.</returns>
    public int GetScriptTag()
    {
        SfntLayoutScript? script = selection.GetScript();
        return script == null ? 0 : script.GetScriptTag();
    }

    /// <summary>Reports whether the default script was used.</summary>
    /// <returns>True when fallback selected DFLT; otherwise false.</returns>
    public bool UsedDefaultScript() { return selection.UsedDefaultScript(); }

    /// <summary>Reports whether a language system was resolved.</summary>
    /// <returns>True when resolved; otherwise false.</returns>
    public bool HasLanguageSystem() { return selection.HasLanguageSystem(); }

    /// <summary>Gets the resolved language-system tag.</summary>
    /// <returns>The language tag, or zero for default or unresolved.</returns>
    public int GetLanguageTag()
    {
        SfntLayoutLanguageSystem? language = selection.GetLanguageSystem();
        return language == null ? 0 : language.GetLanguageTag();
    }

    /// <summary>Reports whether the default language system was used.</summary>
    /// <returns>True when default selection or fallback occurred.</returns>
    public bool UsedDefaultLanguageSystem()
    {
        return selection.UsedDefaultLanguageSystem();
    }

    /// <summary>Gets the available required and optional feature count.</summary>
    /// <returns>The feature count.</returns>
    public int GetAvailableFeatureCount()
    {
        return availableFeatureIndices.Length;
    }

    /// <summary>Gets one available feature.</summary>
    /// <param name="index">The zero-based available-feature index.</param>
    /// <returns>The feature model.</returns>
    public FontLayoutFeature GetAvailableFeature(int index)
    {
        if (index < 0 || index >= availableFeatureIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return owner.GetFeature(availableFeatureIndices[index]);
    }

    /// <summary>Reports whether a feature tag is available.</summary>
    /// <param name="featureTag">The big-endian feature tag.</param>
    /// <returns>True when available; otherwise false.</returns>
    public bool HasFeature(int featureTag)
    {
        return selection.HasFeature(featureTag);
    }

    /// <summary>Gets the selected lookup count.</summary>
    /// <returns>The lookup count.</returns>
    public int GetLookupCount() { return selection.GetLookupCount(); }

    /// <summary>Gets one selected lookup.</summary>
    /// <param name="index">The zero-based selected-lookup index.</param>
    /// <returns>The selected lookup and its ranges.</returns>
    public FontLayoutSelectedLookup GetLookup(int index)
    {
        if (index < 0 || index >= selection.GetLookupCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontLayoutSelectedLookup(selection.GetLookup(index), owner);
    }
}
