namespace ttf;

/// <summary>Describes immutable container-level provenance for a loaded package.</summary>
/// <remarks>
/// Offsets for faces and collection DSIG data address the reconstructed SFNT or
/// TTC image. WOFF optional-block offsets address the original WOFF container.
/// Every returned byte array is an independent copy.
/// </remarks>
public class FontContainerProvenance : IFontContainerProvenance
{
    FontContainerKind sourceKind;
    int originalLength;
    int reconstructedLength;
    int[] faceOffsets;
    string? mediaTypeHint;
    bool collectionHeader;
    uint collectionVersion;
    bool collectionDsig;
    uint collectionDsigTag;
    long collectionDsigOffset;
    byte[] collectionDsigBytes;
    bool woffHeader;
    uint woffFlavor;
    int woffMajorVersion;
    int woffMinorVersion;
    bool woffMetadata;
    long woffMetadataOffset;
    int woffMetadataCompressedLength;
    int woffMetadataOriginalLength;
    byte[] woffMetadataBytes;
    bool woffPrivateData;
    long woffPrivateDataOffset;
    byte[] woffPrivateDataBytes;

    internal FontContainerProvenance(
        FontContainerKind sourceKind,
        int originalLength,
        int reconstructedLength,
        int[] faceOffsets,
        string? mediaTypeHint,
        bool collectionHeader,
        uint collectionVersion,
        bool collectionDsig,
        uint collectionDsigTag,
        long collectionDsigOffset,
        byte[] collectionDsigBytes,
        bool woffHeader,
        uint woffFlavor,
        int woffMajorVersion,
        int woffMinorVersion,
        bool woffMetadata,
        long woffMetadataOffset,
        int woffMetadataCompressedLength,
        int woffMetadataOriginalLength,
        byte[] woffMetadataBytes,
        bool woffPrivateData,
        long woffPrivateDataOffset,
        byte[] woffPrivateDataBytes)
    {
        if (faceOffsets == null)
        {
            throw new System.ArgumentNullException("faceOffsets");
        }

        if (collectionDsigBytes == null)
        {
            throw new System.ArgumentNullException("collectionDsigBytes");
        }

        if (woffMetadataBytes == null)
        {
            throw new System.ArgumentNullException("woffMetadataBytes");
        }

        if (woffPrivateDataBytes == null)
        {
            throw new System.ArgumentNullException("woffPrivateDataBytes");
        }

        this.sourceKind = sourceKind;
        this.originalLength = originalLength;
        this.reconstructedLength = reconstructedLength;
        this.faceOffsets = faceOffsets.AsSpan().ToArray();
        this.mediaTypeHint = mediaTypeHint;
        this.collectionHeader = collectionHeader;
        this.collectionVersion = collectionVersion;
        this.collectionDsig = collectionDsig;
        this.collectionDsigTag = collectionDsigTag;
        this.collectionDsigOffset = collectionDsigOffset;
        this.collectionDsigBytes = collectionDsigBytes.AsSpan().ToArray();
        this.woffHeader = woffHeader;
        this.woffFlavor = woffFlavor;
        this.woffMajorVersion = woffMajorVersion;
        this.woffMinorVersion = woffMinorVersion;
        this.woffMetadata = woffMetadata;
        this.woffMetadataOffset = woffMetadataOffset;
        this.woffMetadataCompressedLength = woffMetadataCompressedLength;
        this.woffMetadataOriginalLength = woffMetadataOriginalLength;
        this.woffMetadataBytes = woffMetadataBytes.AsSpan().ToArray();
        this.woffPrivateData = woffPrivateData;
        this.woffPrivateDataOffset = woffPrivateDataOffset;
        this.woffPrivateDataBytes = woffPrivateDataBytes.AsSpan().ToArray();
    }

    /// <summary>Gets the kind of the original container.</summary>
    /// <returns>The original container kind.</returns>
    public FontContainerKind GetSourceKind()
    {
        return sourceKind;
    }

    /// <summary>Gets the length of the original container.</summary>
    /// <returns>The original byte length.</returns>
    public int GetOriginalLength()
    {
        return originalLength;
    }

    /// <summary>Gets the length of the reconstructed SFNT or TTC image.</summary>
    /// <returns>The reconstructed byte length.</returns>
    public int GetReconstructedLength()
    {
        return reconstructedLength;
    }

    /// <summary>Gets the number of reconstructed face-directory offsets.</summary>
    /// <returns>The face count.</returns>
    public int GetFaceCount()
    {
        return faceOffsets.Length;
    }

    /// <summary>Gets one face-directory offset in the reconstructed SFNT or TTC image.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The reconstructed face-directory offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the face range.
    /// </exception>
    public int GetFaceOffset(int index)
    {
        if (index < 0 || index >= faceOffsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return faceOffsets[index];
    }

    /// <summary>Gets the caller-supplied media-type hint.</summary>
    /// <returns>The media-type hint, or null when none was supplied.</returns>
    public string? GetMediaTypeHint()
    {
        return mediaTypeHint;
    }

    /// <summary>Reports whether the reconstructed image has a TTC or OTC header.</summary>
    /// <returns>True when collection metadata is available; otherwise false.</returns>
    public bool HasCollectionHeader()
    {
        return collectionHeader;
    }

    /// <summary>Gets the fixed TTC or OTC header version.</summary>
    /// <returns>The collection header version.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The reconstructed image is not a collection.
    /// </exception>
    public uint GetCollectionVersion()
    {
        RequireCollectionHeader();
        return collectionVersion;
    }

    /// <summary>Reports whether the TTC or OTC header references a DSIG block.</summary>
    /// <returns>True when collection DSIG data is present; otherwise false.</returns>
    public bool HasCollectionDsig()
    {
        return collectionDsig;
    }

    /// <summary>Gets the collection DSIG tag.</summary>
    /// <returns>The DSIG tag.</returns>
    /// <exception cref="System.InvalidOperationException">No collection DSIG block is present.</exception>
    public uint GetCollectionDsigTag()
    {
        RequireCollectionDsig();
        return collectionDsigTag;
    }

    /// <summary>Gets the collection DSIG offset in the reconstructed image.</summary>
    /// <returns>The DSIG byte offset.</returns>
    /// <exception cref="System.InvalidOperationException">No collection DSIG block is present.</exception>
    public long GetCollectionDsigOffset()
    {
        RequireCollectionDsig();
        return collectionDsigOffset;
    }

    /// <summary>Gets the collection DSIG byte length.</summary>
    /// <returns>The DSIG byte length.</returns>
    /// <exception cref="System.InvalidOperationException">No collection DSIG block is present.</exception>
    public int GetCollectionDsigLength()
    {
        RequireCollectionDsig();
        return collectionDsigBytes.Length;
    }

    /// <summary>Copies the collection DSIG block.</summary>
    /// <returns>A new array containing the DSIG block.</returns>
    /// <exception cref="System.InvalidOperationException">No collection DSIG block is present.</exception>
    public byte[] CopyCollectionDsigBytes()
    {
        RequireCollectionDsig();
        return collectionDsigBytes.AsSpan().ToArray();
    }

    /// <summary>Reports whether WOFF header metadata is available.</summary>
    /// <returns>True for WOFF 1 and WOFF 2 sources; otherwise false.</returns>
    public bool HasWoffHeader()
    {
        return woffHeader;
    }

    /// <summary>Gets the WOFF flavor.</summary>
    /// <returns>The WOFF flavor.</returns>
    /// <exception cref="System.InvalidOperationException">The source is not WOFF.</exception>
    public uint GetWoffFlavor()
    {
        RequireWoffHeader();
        return woffFlavor;
    }

    /// <summary>Gets the WOFF major version.</summary>
    /// <returns>The WOFF major version.</returns>
    /// <exception cref="System.InvalidOperationException">The source is not WOFF.</exception>
    public int GetWoffMajorVersion()
    {
        RequireWoffHeader();
        return woffMajorVersion;
    }

    /// <summary>Gets the WOFF minor version.</summary>
    /// <returns>The WOFF minor version.</returns>
    /// <exception cref="System.InvalidOperationException">The source is not WOFF.</exception>
    public int GetWoffMinorVersion()
    {
        RequireWoffHeader();
        return woffMinorVersion;
    }

    /// <summary>Reports whether the WOFF source contains metadata.</summary>
    /// <returns>True when WOFF metadata is present; otherwise false.</returns>
    public bool HasWoffMetadata()
    {
        return woffMetadata;
    }

    /// <summary>Gets the compressed WOFF metadata offset in the original container.</summary>
    /// <returns>The compressed metadata offset.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF metadata is present.</exception>
    public long GetWoffMetadataOffset()
    {
        RequireWoffMetadata();
        return woffMetadataOffset;
    }

    /// <summary>Gets the compressed WOFF metadata byte length.</summary>
    /// <returns>The compressed metadata length.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF metadata is present.</exception>
    public int GetWoffMetadataCompressedLength()
    {
        RequireWoffMetadata();
        return woffMetadataCompressedLength;
    }

    /// <summary>Gets the decompressed WOFF metadata byte length.</summary>
    /// <returns>The decompressed metadata length.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF metadata is present.</exception>
    public int GetWoffMetadataOriginalLength()
    {
        RequireWoffMetadata();
        return woffMetadataOriginalLength;
    }

    /// <summary>Copies the decompressed WOFF metadata.</summary>
    /// <returns>A new array containing the decompressed metadata.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF metadata is present.</exception>
    public byte[] CopyWoffMetadataBytes()
    {
        RequireWoffMetadata();
        return woffMetadataBytes.AsSpan().ToArray();
    }

    /// <summary>Reports whether the WOFF source contains a private block.</summary>
    /// <returns>True when WOFF private data is present; otherwise false.</returns>
    public bool HasWoffPrivateData()
    {
        return woffPrivateData;
    }

    /// <summary>Gets the WOFF private-block offset in the original container.</summary>
    /// <returns>The private-block offset.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF private block is present.</exception>
    public long GetWoffPrivateDataOffset()
    {
        RequireWoffPrivateData();
        return woffPrivateDataOffset;
    }

    /// <summary>Gets the WOFF private-block byte length.</summary>
    /// <returns>The private-block byte length.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF private block is present.</exception>
    public int GetWoffPrivateDataLength()
    {
        RequireWoffPrivateData();
        return woffPrivateDataBytes.Length;
    }

    /// <summary>Copies the WOFF private block.</summary>
    /// <returns>A new array containing the private block.</returns>
    /// <exception cref="System.InvalidOperationException">No WOFF private block is present.</exception>
    public byte[] CopyWoffPrivateDataBytes()
    {
        RequireWoffPrivateData();
        return woffPrivateDataBytes.AsSpan().ToArray();
    }

    void RequireCollectionHeader()
    {
        if (!collectionHeader)
        {
            throw new System.InvalidOperationException(
                "The reconstructed font image is not a collection.");
        }
    }

    void RequireCollectionDsig()
    {
        if (!collectionDsig)
        {
            throw new System.InvalidOperationException(
                "The collection does not contain DSIG data.");
        }
    }

    void RequireWoffHeader()
    {
        if (!woffHeader)
        {
            throw new System.InvalidOperationException("The source container is not WOFF.");
        }
    }

    void RequireWoffMetadata()
    {
        if (!woffMetadata)
        {
            throw new System.InvalidOperationException(
                "The WOFF source does not contain metadata.");
        }
    }

    void RequireWoffPrivateData()
    {
        if (!woffPrivateData)
        {
            throw new System.InvalidOperationException(
                "The WOFF source does not contain private data.");
        }
    }
}
