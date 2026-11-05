namespace ttf;

/// <summary>Represents one public GSUB or GPOS layout table.</summary>
public class FontLayoutTable : IFontLayoutTable
{
    SfntLayoutTable table;
    SfntGsubTable? substitution;
    SfntGposTable? positioning;
    FontLayoutFeatureVariations? featureVariations;

    internal FontLayoutTable(
        SfntLayoutTable table,
        SfntGsubTable? substitution,
        SfntGposTable? positioning)
    {
        this.table = table;
        this.substitution = substitution;
        this.positioning = positioning;
        featureVariations = table.GetFeatureVariationsOffset() == 0 ? null :
            new FontLayoutFeatureVariations(table, this);
    }

    /// <summary>Reports whether this is a GSUB table.</summary>
    /// <returns>True for GSUB; false for GPOS.</returns>
    public bool IsSubstitution() { return substitution != null; }

    /// <summary>Reports whether this is a GPOS table.</summary>
    /// <returns>True for GPOS; false for GSUB.</returns>
    public bool IsPositioning() { return positioning != null; }

    /// <summary>Gets the major version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }

    /// <summary>Gets the minor version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }

    /// <summary>Gets the script count.</summary>
    /// <returns>The script count.</returns>
    public int GetScriptCount()
    {
        return table.GetScriptList().GetScriptCount();
    }

    /// <summary>Gets one script.</summary>
    /// <param name="index">The zero-based script index.</param>
    /// <returns>The script model.</returns>
    public FontLayoutScript GetScript(int index)
    {
        if (index < 0 || index >= GetScriptCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontLayoutScript(
            table.GetScriptList().GetScript(index),
            this);
    }

    /// <summary>Finds a script by tag.</summary>
    /// <param name="scriptTag">The big-endian script tag.</param>
    /// <returns>The script model, or null.</returns>
    public FontLayoutScript? FindScript(int scriptTag)
    {
        SfntLayoutScript? script = table.GetScriptList().FindScript(scriptTag);
        return script == null ? null : new FontLayoutScript(script, this);
    }

    /// <summary>Gets the feature count.</summary>
    /// <returns>The feature count.</returns>
    public int GetFeatureCount()
    {
        return table.GetFeatureList().GetFeatureCount();
    }

    /// <summary>Gets one feature.</summary>
    /// <param name="index">The zero-based feature index.</param>
    /// <returns>The feature model.</returns>
    public FontLayoutFeature GetFeature(int index)
    {
        if (index < 0 || index >= GetFeatureCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        SfntLayoutFeature feature = table.GetFeatureList().GetFeature(index);
        int[] lookupIndices = new int[feature.GetLookupIndexCount()];
        int lookupIndex = 0;
        while (lookupIndex < lookupIndices.Length)
        {
            lookupIndices[lookupIndex] = feature.GetLookupIndex(lookupIndex);
            lookupIndex += 1;
        }
        return new FontLayoutFeature(
            index,
            feature.GetFeatureTag(),
            feature.GetFeatureParamsTableOffset(),
            lookupIndices,
            table.GetDataSlice());
    }

    /// <summary>Finds the first feature with a tag.</summary>
    /// <param name="featureTag">The big-endian feature tag.</param>
    /// <returns>The feature model, or null.</returns>
    public FontLayoutFeature? FindFeature(int featureTag)
    {
        int index = 0;
        while (index < GetFeatureCount())
        {
            if (table.GetFeatureList().GetFeature(index).GetFeatureTag() ==
                featureTag)
            {
                return GetFeature(index);
            }
            index += 1;
        }
        return null;
    }

    /// <summary>Gets the lookup count.</summary>
    /// <returns>The lookup count.</returns>
    public int GetLookupCount()
    {
        return table.GetLookupList().GetLookupCount();
    }

    /// <summary>Gets one available lookup.</summary>
    /// <param name="index">The zero-based lookup index.</param>
    /// <returns>The lookup model.</returns>
    public FontLayoutLookup GetLookup(int index)
    {
        if (index < 0 || index >= GetLookupCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        int effectiveType;
        if (substitution != null)
        {
            effectiveType = substitution.GetLookup(index).GetLookupType();
        }
        else if (positioning != null)
        {
            effectiveType = positioning.GetLookup(index).GetLookupType();
        }
        else
        {
            throw new System.InvalidOperationException(
                "The layout table kind is unavailable.");
        }
        return new FontLayoutLookup(
            index,
            table.GetLookupList().GetLookup(index),
            effectiveType);
    }

    /// <summary>Gets conditional feature substitutions.</summary>
    /// <returns>The FeatureVariations model, or null.</returns>
    public FontLayoutFeatureVariations? GetFeatureVariations()
    {
        return featureVariations;
    }

    /// <summary>Selects features and lookups for a UTF-16 text range.</summary>
    /// <param name="scriptTag">The requested script tag.</param>
    /// <param name="languageTag">The requested OpenType language tag, or zero.</param>
    /// <param name="settings">Caller feature values and UTF-16 ranges.</param>
    /// <param name="textLength">The nonnegative UTF-16 text length.</param>
    /// <returns>The resolved immutable selection.</returns>
    /// <exception cref="System.ArgumentNullException"><c>settings</c> is null.</exception>
    /// <exception cref="System.ArgumentException">A setting is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>textLength</c> is negative.
    /// </exception>
    public FontLayoutSelection Select(
        int scriptTag,
        int languageTag,
        FontLayoutFeatureSetting[] settings,
        int textLength)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }
        SfntUserFeature[] internalSettings =
            new SfntUserFeature[settings.Length];
        int index = 0;
        while (index < settings.Length)
        {
            if (settings[index] == null)
            {
                throw new System.ArgumentException(
                    "A feature setting cannot be null.",
                    "settings");
            }
            internalSettings[index] = settings[index].CreateInternal();
            index += 1;
        }
        return new FontLayoutSelection(
            SfntLayoutSelector.CreateSelection(
                table,
                scriptTag,
                languageTag,
                internalSettings,
                textLength),
            this);
    }
}
