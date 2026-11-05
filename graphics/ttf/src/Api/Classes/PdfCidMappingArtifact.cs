namespace ttf;

/// <summary>Contains CID font mapping and legacy CIDSet data.</summary>
/// <remarks>
/// CIDFontType2 descendants use either the predefined <c>Identity</c> mapping
/// or the returned big-endian stream. CIDFontType0 descendants select glyphs
/// through their CFF data and therefore have no CIDToGIDMap entry. CIDSet data
/// is emitted only for the legacy PDF/A-1 profile.
/// </remarks>
public class PdfCidMappingArtifact : IPdfCidMappingArtifact
{
    PdfCidSystemInfo systemInfo;
    int cidCount;
    int[] mappedGlyphIds;
    bool hasCidToGidMap;
    bool usesIdentity;
    byte[] mapBytes;
    bool hasCidSet;
    byte[] cidSetBytes;

    internal PdfCidMappingArtifact(
        int cidCount,
        int[] mappedGlyphIds,
        bool hasCidToGidMap,
        bool usesIdentity,
        byte[] mapBytes,
        bool hasCidSet,
        byte[] cidSetBytes)
    {
        systemInfo = new PdfCidSystemInfo();
        this.cidCount = cidCount;
        this.mappedGlyphIds = CopyIntegers(mappedGlyphIds);
        this.hasCidToGidMap = hasCidToGidMap;
        this.usesIdentity = usesIdentity;
        this.mapBytes = CopyBytes(mapBytes);
        this.hasCidSet = hasCidSet;
        this.cidSetBytes = CopyBytes(cidSetBytes);
    }

    /// <summary>Gets the descendant font's CIDSystemInfo values.</summary>
    /// <returns>The immutable Adobe-Identity-0 metadata.</returns>
    public PdfCidSystemInfo GetCidSystemInfo()
    {
        return systemInfo;
    }

    /// <summary>Gets the number of allocated document CIDs.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    public int GetCidCount()
    {
        return cidCount;
    }

    /// <summary>Reports whether the descendant accepts a CIDToGIDMap entry.</summary>
    /// <returns>True for CIDFontType2; false for CIDFontType0.</returns>
    public bool HasCidToGidMap()
    {
        return hasCidToGidMap;
    }

    /// <summary>Reports whether <c>/CIDToGIDMap /Identity</c> is valid.</summary>
    /// <returns>True when every allocated CID equals its embedded GID.</returns>
    public bool UsesIdentityCidToGidMap()
    {
        return hasCidToGidMap && usesIdentity;
    }

    /// <summary>Gets the predefined CIDToGIDMap name when one is used.</summary>
    /// <returns><c>Identity</c>, or an empty string for a stream or no entry.</returns>
    public string GetCidToGidMapName()
    {
        return UsesIdentityCidToGidMap() ? "Identity" : string.Empty;
    }

    /// <summary>Gets the embedded glyph selected by one allocated CID.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The embedded glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The descendant does not accept a CIDToGIDMap entry.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    public int GetMappedGlyphId(int cid)
    {
        if (!hasCidToGidMap)
        {
            throw new System.InvalidOperationException(
                "CIDFontType0 does not use a CIDToGIDMap entry.");
        }

        if (cid < 0 || cid >= mappedGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("cid");
        }

        return mappedGlyphIds[cid];
    }

    /// <summary>Copies the big-endian CIDToGIDMap stream data.</summary>
    /// <returns>
    /// Two bytes per allocated CID, or an empty array for Identity or no entry.
    /// </returns>
    public byte[] CopyCidToGidMap()
    {
        return CopyBytes(mapBytes);
    }

    /// <summary>Reports whether legacy PDF/A-1 CIDSet data was requested.</summary>
    /// <returns>True when a CIDSet stream should be emitted.</returns>
    public bool HasCidSet()
    {
        return hasCidSet;
    }

    /// <summary>Copies the high-bit-first CIDSet stream data.</summary>
    /// <returns>A new array, or an empty array when CIDSet is not requested.</returns>
    public byte[] CopyCidSet()
    {
        return CopyBytes(cidSetBytes);
    }

    static byte[] CopyBytes(byte[] values)
    {
        byte[] copy = new byte[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
