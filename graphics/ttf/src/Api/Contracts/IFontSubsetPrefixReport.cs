namespace ttf;

/// <summary>Defines inspection of subset-prefix inputs and selection.</summary>
public interface IFontSubsetPrefixReport
{
    /// <summary>Gets the six-letter prefix, excluding its following plus sign.</summary>
    /// <returns>The prefix, or an empty string for full-font output.</returns>
    string GetPrefix();

    /// <summary>Reports whether the caller's prefix override was used.</summary>
    /// <returns>True when the output uses the override.</returns>
    bool WasOverrideUsed();

    /// <summary>Reports whether the prefix was generated from the reported inputs.</summary>
    /// <returns>True when the deterministic hash selected the prefix.</returns>
    bool WasDeterministicallyGenerated();

    /// <summary>Gets the deterministic identity of the complete source face.</summary>
    /// <returns>The uppercase SHA-256 face identity.</returns>
    string GetSourceFaceIdentity();

    /// <summary>Gets the resolved variation-coordinate key used by the prefix hash.</summary>
    /// <returns>The canonical coordinate key.</returns>
    string GetCoordinateKey();

    /// <summary>Gets the unprefixed source PostScript name used by the prefix hash.</summary>
    /// <returns>The normalized source PostScript name.</returns>
    string GetSourcePostScriptName();

    /// <summary>Gets the effective policy snapshot used by the prefix hash.</summary>
    /// <returns>A defensive mutable copy of the effective options.</returns>
    FontSubsetOptions GetPolicyOptions();

    /// <summary>Copies the source glyph identifiers used by the prefix hash.</summary>
    /// <returns>A new sorted array of retained source glyph identifiers.</returns>
    int[] GetRetainedGlyphIds();
}
