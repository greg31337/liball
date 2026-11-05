namespace ttf;

/// <summary>Defines one OpenType layout language system.</summary>
public interface IFontLayoutLanguageSystem
{
    /// <summary>Gets the language tag, or zero for a default language system.</summary>
    /// <returns>The big-endian tag or zero.</returns>
    int GetTag();

    /// <summary>Reports whether a required feature exists.</summary>
    /// <returns>True when present; otherwise false.</returns>
    bool HasRequiredFeature();

    /// <summary>Gets the required feature index.</summary>
    /// <returns>The feature index, or 65535 when absent.</returns>
    int GetRequiredFeatureIndex();

    /// <summary>Gets the required feature.</summary>
    /// <returns>The required feature, or null.</returns>
    FontLayoutFeature? GetRequiredFeature();

    /// <summary>Gets the optional feature count.</summary>
    /// <returns>The optional feature count.</returns>
    int GetOptionalFeatureCount();

    /// <summary>Gets one optional feature index.</summary>
    /// <param name="index">The zero-based optional-feature index.</param>
    /// <returns>The feature-list index.</returns>
    int GetOptionalFeatureIndex(int index);

    /// <summary>Gets one optional feature.</summary>
    /// <param name="index">The zero-based optional-feature index.</param>
    /// <returns>The feature model.</returns>
    FontLayoutFeature GetOptionalFeature(int index);
}
