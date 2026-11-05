namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidMappingArtifact"/>.</summary>
public interface IPdfCidMappingArtifact
{
    /// <summary>Gets the descendant font's CIDSystemInfo values.</summary>
    /// <returns>The immutable Adobe-Identity-0 metadata.</returns>
    PdfCidSystemInfo GetCidSystemInfo();

    /// <summary>Gets the number of allocated document CIDs.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    int GetCidCount();

    /// <summary>Reports whether the descendant accepts a CIDToGIDMap entry.</summary>
    /// <returns>True for CIDFontType2; false for CIDFontType0.</returns>
    bool HasCidToGidMap();

    /// <summary>Reports whether <c>/CIDToGIDMap /Identity</c> is valid.</summary>
    /// <returns>True when every allocated CID equals its embedded GID.</returns>
    bool UsesIdentityCidToGidMap();

    /// <summary>Gets the predefined CIDToGIDMap name when one is used.</summary>
    /// <returns><c>Identity</c>, or an empty string for a stream or no entry.</returns>
    string GetCidToGidMapName();

    /// <summary>Gets the embedded glyph selected by one allocated CID.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The embedded glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The descendant does not accept a CIDToGIDMap entry.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    int GetMappedGlyphId(int cid);

    /// <summary>Copies the big-endian CIDToGIDMap stream data.</summary>
    /// <returns>
    /// Two bytes per allocated CID, or an empty array for Identity or no entry.
    /// </returns>
    byte[] CopyCidToGidMap();

    /// <summary>Reports whether legacy PDF/A-1 CIDSet data was requested.</summary>
    /// <returns>True when a CIDSet stream should be emitted.</returns>
    bool HasCidSet();

    /// <summary>Copies the high-bit-first CIDSet stream data.</summary>
    /// <returns>A new array, or an empty array when CIDSet is not requested.</returns>
    byte[] CopyCidSet();
}
