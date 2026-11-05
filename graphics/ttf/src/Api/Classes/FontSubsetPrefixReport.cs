namespace ttf;

/// <summary>Reports the exact inputs and outcome of subset-prefix selection.</summary>
public class FontSubsetPrefixReport : IFontSubsetPrefixReport
{
    string prefix;
    bool overrideUsed;
    bool deterministicallyGenerated;
    string sourceFaceIdentity;
    string coordinateKey;
    string sourcePostScriptName;
    FontSubsetOptions policyOptions;
    int[] retainedGlyphIds;

    internal FontSubsetPrefixReport(
        FontInstance instance,
        FontSubsetOptions options,
        int[] oldToNewGlyphIds,
        string prefix,
        string sourcePostScriptName)
    {
        this.prefix = prefix;
        overrideUsed = prefix.Length != 0 &&
            options.GetSubsetPrefixOverride() != null;
        deterministicallyGenerated = prefix.Length != 0 && !overrideUsed;
        sourceFaceIdentity = FontSetFaceIdentity.Create(instance.GetFace());
        coordinateKey = instance.GetCoordinates().GetKey();
        this.sourcePostScriptName = sourcePostScriptName;
        policyOptions = CopyOptions(options);
        retainedGlyphIds = CollectRetainedGlyphIds(oldToNewGlyphIds);
    }

    /// <summary>Gets the six-letter prefix, excluding its following plus sign.</summary>
    /// <returns>The prefix, or an empty string for full-font output.</returns>
    public string GetPrefix() { return prefix; }

    /// <summary>Reports whether the caller's prefix override was used.</summary>
    /// <returns>True when the output uses the override.</returns>
    public bool WasOverrideUsed() { return overrideUsed; }

    /// <summary>Reports whether the prefix was generated from the reported inputs.</summary>
    /// <returns>True when the deterministic hash selected the prefix.</returns>
    public bool WasDeterministicallyGenerated()
    {
        return deterministicallyGenerated;
    }

    /// <summary>Gets the deterministic identity of the complete source face.</summary>
    /// <returns>The uppercase SHA-256 face identity.</returns>
    public string GetSourceFaceIdentity() { return sourceFaceIdentity; }

    /// <summary>Gets the resolved variation-coordinate key used by the prefix hash.</summary>
    /// <returns>The canonical coordinate key.</returns>
    public string GetCoordinateKey() { return coordinateKey; }

    /// <summary>Gets the unprefixed source PostScript name used by the prefix hash.</summary>
    /// <returns>The normalized source PostScript name.</returns>
    public string GetSourcePostScriptName() { return sourcePostScriptName; }

    /// <summary>Gets the effective policy snapshot used by the prefix hash.</summary>
    /// <returns>A defensive mutable copy of the effective options.</returns>
    public FontSubsetOptions GetPolicyOptions()
    {
        return CopyOptions(policyOptions);
    }

    /// <summary>Copies the source glyph identifiers used by the prefix hash.</summary>
    /// <returns>A new sorted array of retained source glyph identifiers.</returns>
    public int[] GetRetainedGlyphIds()
    {
        return CopyIntegers(retainedGlyphIds);
    }

    static int[] CollectRetainedGlyphIds(int[] oldToNewGlyphIds)
    {
        int count = 0;
        int index = 0;

        while (index < oldToNewGlyphIds.Length)
        {
            if (oldToNewGlyphIds[index] >= 0)
            {
                count += 1;
            }
            index += 1;
        }

        int[] result = new int[count];
        int outputIndex = 0;
        index = 0;

        while (index < oldToNewGlyphIds.Length)
        {
            if (oldToNewGlyphIds[index] >= 0)
            {
                result[outputIndex] = index;
                outputIndex += 1;
            }
            index += 1;
        }

        return result;
    }

    static FontSubsetOptions CopyOptions(FontSubsetOptions source)
    {
        FontSubsetOptions copy = FontSubsetOptions.Create(
            source.GetEmbeddingMode(),
            source.GetGlyphRetentionMode(),
            source.GetRecommendedGlyphPolicy(),
            source.GetHintPolicy(),
            source.GetLayoutPolicy(),
            source.GetVerticalPolicy(),
            source.GetColorPolicy(),
            source.GetMetadataPolicy(),
            source.GetUnknownTablePolicy(),
            source.GetRightsPolicy());
        copy.SetSubsetPrefixOverride(source.GetSubsetPrefixOverride());
        copy.SetOperationOptions(source.GetOperationOptions());
        return copy;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
