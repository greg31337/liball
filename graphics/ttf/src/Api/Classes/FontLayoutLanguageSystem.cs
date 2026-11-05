namespace ttf;

/// <summary>Represents one OpenType layout language system.</summary>
public class FontLayoutLanguageSystem : IFontLayoutLanguageSystem
{
    SfntLayoutLanguageSystem language;
    FontLayoutTable owner;

    internal FontLayoutLanguageSystem(
        SfntLayoutLanguageSystem language,
        FontLayoutTable owner)
    {
        this.language = language;
        this.owner = owner;
    }

    /// <summary>Gets the language tag, or zero for a default language system.</summary>
    /// <returns>The big-endian tag or zero.</returns>
    public int GetTag() { return language.GetLanguageTag(); }

    /// <summary>Reports whether a required feature exists.</summary>
    /// <returns>True when present; otherwise false.</returns>
    public bool HasRequiredFeature() { return language.HasRequiredFeature(); }

    /// <summary>Gets the required feature index.</summary>
    /// <returns>The feature index, or 65535 when absent.</returns>
    public int GetRequiredFeatureIndex()
    {
        return language.GetRequiredFeatureIndex();
    }

    /// <summary>Gets the required feature.</summary>
    /// <returns>The required feature, or null.</returns>
    public FontLayoutFeature? GetRequiredFeature()
    {
        return language.HasRequiredFeature() ?
            owner.GetFeature(language.GetRequiredFeatureIndex()) : null;
    }

    /// <summary>Gets the optional feature count.</summary>
    /// <returns>The optional feature count.</returns>
    public int GetOptionalFeatureCount()
    {
        return language.GetFeatureIndexCount();
    }

    /// <summary>Gets one optional feature index.</summary>
    /// <param name="index">The zero-based optional-feature index.</param>
    /// <returns>The feature-list index.</returns>
    public int GetOptionalFeatureIndex(int index)
    {
        if (index < 0 || index >= language.GetFeatureIndexCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return language.GetFeatureIndex(index);
    }

    /// <summary>Gets one optional feature.</summary>
    /// <param name="index">The zero-based optional-feature index.</param>
    /// <returns>The feature model.</returns>
    public FontLayoutFeature GetOptionalFeature(int index)
    {
        return owner.GetFeature(GetOptionalFeatureIndex(index));
    }

    internal SfntLayoutLanguageSystem GetInternal()
    {
        return language;
    }
}
