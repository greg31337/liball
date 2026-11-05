namespace ttf;

/// <summary>Defines one public OpenType layout-table model.</summary>
public interface IFontLayoutTable
{
    /// <summary>Reports whether this is a GSUB table.</summary>
    /// <returns>True for GSUB; false for GPOS.</returns>
    bool IsSubstitution();

    /// <summary>Reports whether this is a GPOS table.</summary>
    /// <returns>True for GPOS; false for GSUB.</returns>
    bool IsPositioning();

    /// <summary>Gets the major version.</summary>
    /// <returns>The major version.</returns>
    int GetMajorVersion();

    /// <summary>Gets the minor version.</summary>
    /// <returns>The minor version.</returns>
    int GetMinorVersion();

    /// <summary>Gets the script count.</summary>
    /// <returns>The script count.</returns>
    int GetScriptCount();

    /// <summary>Gets one script.</summary>
    /// <param name="index">The zero-based script index.</param>
    /// <returns>The script model.</returns>
    FontLayoutScript GetScript(int index);

    /// <summary>Finds a script by tag.</summary>
    /// <param name="scriptTag">The big-endian script tag.</param>
    /// <returns>The script model, or null.</returns>
    FontLayoutScript? FindScript(int scriptTag);

    /// <summary>Gets the feature count.</summary>
    /// <returns>The feature count.</returns>
    int GetFeatureCount();

    /// <summary>Gets one feature.</summary>
    /// <param name="index">The zero-based feature index.</param>
    /// <returns>The feature model.</returns>
    FontLayoutFeature GetFeature(int index);

    /// <summary>Finds the first feature with a tag.</summary>
    /// <param name="featureTag">The big-endian feature tag.</param>
    /// <returns>The feature model, or null.</returns>
    FontLayoutFeature? FindFeature(int featureTag);

    /// <summary>Gets the lookup count.</summary>
    /// <returns>The lookup count.</returns>
    int GetLookupCount();

    /// <summary>Gets one available lookup.</summary>
    /// <param name="index">The zero-based lookup index.</param>
    /// <returns>The lookup model.</returns>
    FontLayoutLookup GetLookup(int index);

    /// <summary>Gets conditional feature substitutions.</summary>
    /// <returns>The FeatureVariations model, or null.</returns>
    FontLayoutFeatureVariations? GetFeatureVariations();

    /// <summary>Selects features and lookups for a UTF-16 text range.</summary>
    /// <param name="scriptTag">The requested script tag.</param>
    /// <param name="languageTag">The requested OpenType language tag, or zero.</param>
    /// <param name="settings">Caller feature values and UTF-16 ranges.</param>
    /// <param name="textLength">The nonnegative UTF-16 text length.</param>
    /// <returns>The resolved immutable selection.</returns>
    FontLayoutSelection Select(
        int scriptTag,
        int languageTag,
        FontLayoutFeatureSetting[] settings,
        int textLength);
}
