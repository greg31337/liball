namespace ttf;

/// <summary>Controls document-local PDF character-code allocation.</summary>
public enum PdfCidAllocationMode
{
    /// <summary>Assign dense CIDs in first-use order.</summary>
    Dense = 0,

    /// <summary>Use each source glyph identifier as its CID.</summary>
    PreserveSourceGlyphIdentifiers = 1
}
