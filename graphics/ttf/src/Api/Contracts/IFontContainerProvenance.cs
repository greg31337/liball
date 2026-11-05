namespace ttf;

/// <summary>Defines immutable provenance operations for a loaded font container.</summary>
public interface IFontContainerProvenance
{
    /// <summary>Gets the kind of the original container.</summary>
    /// <returns>The original container kind.</returns>
    FontContainerKind GetSourceKind();

    /// <summary>Gets the length of the original container.</summary>
    /// <returns>The original byte length.</returns>
    int GetOriginalLength();

    /// <summary>Gets the length of the reconstructed SFNT or TTC image.</summary>
    /// <returns>The reconstructed byte length.</returns>
    int GetReconstructedLength();

    /// <summary>Gets the number of reconstructed face-directory offsets.</summary>
    /// <returns>The face count.</returns>
    int GetFaceCount();

    /// <summary>Gets one face-directory offset in the reconstructed SFNT or TTC image.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The reconstructed face-directory offset.</returns>
    int GetFaceOffset(int index);

    /// <summary>Gets the caller-supplied media-type hint.</summary>
    /// <returns>The media-type hint, or null when none was supplied.</returns>
    string? GetMediaTypeHint();

    /// <summary>Reports whether the reconstructed image has a TTC or OTC header.</summary>
    /// <returns>True when collection metadata is available; otherwise false.</returns>
    bool HasCollectionHeader();

    /// <summary>Gets the fixed TTC or OTC header version.</summary>
    /// <returns>The collection header version.</returns>
    uint GetCollectionVersion();

    /// <summary>Reports whether the TTC or OTC header references a DSIG block.</summary>
    /// <returns>True when collection DSIG data is present; otherwise false.</returns>
    bool HasCollectionDsig();

    /// <summary>Gets the collection DSIG tag.</summary>
    /// <returns>The DSIG tag.</returns>
    uint GetCollectionDsigTag();

    /// <summary>Gets the collection DSIG offset in the reconstructed image.</summary>
    /// <returns>The DSIG byte offset.</returns>
    long GetCollectionDsigOffset();

    /// <summary>Gets the collection DSIG byte length.</summary>
    /// <returns>The DSIG byte length.</returns>
    int GetCollectionDsigLength();

    /// <summary>Copies the collection DSIG block.</summary>
    /// <returns>A new array containing the DSIG block.</returns>
    byte[] CopyCollectionDsigBytes();

    /// <summary>Reports whether WOFF header metadata is available.</summary>
    /// <returns>True for WOFF 1 and WOFF 2 sources; otherwise false.</returns>
    bool HasWoffHeader();

    /// <summary>Gets the WOFF flavor.</summary>
    /// <returns>The WOFF flavor.</returns>
    uint GetWoffFlavor();

    /// <summary>Gets the WOFF major version.</summary>
    /// <returns>The WOFF major version.</returns>
    int GetWoffMajorVersion();

    /// <summary>Gets the WOFF minor version.</summary>
    /// <returns>The WOFF minor version.</returns>
    int GetWoffMinorVersion();

    /// <summary>Reports whether the WOFF source contains metadata.</summary>
    /// <returns>True when WOFF metadata is present; otherwise false.</returns>
    bool HasWoffMetadata();

    /// <summary>Gets the compressed WOFF metadata offset in the original container.</summary>
    /// <returns>The compressed metadata offset.</returns>
    long GetWoffMetadataOffset();

    /// <summary>Gets the compressed WOFF metadata byte length.</summary>
    /// <returns>The compressed metadata length.</returns>
    int GetWoffMetadataCompressedLength();

    /// <summary>Gets the decompressed WOFF metadata byte length.</summary>
    /// <returns>The decompressed metadata length.</returns>
    int GetWoffMetadataOriginalLength();

    /// <summary>Copies the decompressed WOFF metadata.</summary>
    /// <returns>A new array containing the decompressed metadata.</returns>
    byte[] CopyWoffMetadataBytes();

    /// <summary>Reports whether the WOFF source contains a private block.</summary>
    /// <returns>True when WOFF private data is present; otherwise false.</returns>
    bool HasWoffPrivateData();

    /// <summary>Gets the WOFF private-block offset in the original container.</summary>
    /// <returns>The private-block offset.</returns>
    long GetWoffPrivateDataOffset();

    /// <summary>Gets the WOFF private-block byte length.</summary>
    /// <returns>The private-block byte length.</returns>
    int GetWoffPrivateDataLength();

    /// <summary>Copies the WOFF private block.</summary>
    /// <returns>A new array containing the private block.</returns>
    byte[] CopyWoffPrivateDataBytes();
}
