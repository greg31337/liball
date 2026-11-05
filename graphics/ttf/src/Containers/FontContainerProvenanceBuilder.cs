namespace ttf;

internal static class FontContainerProvenanceBuilder
{
    internal static FontContainerProvenance Create(
        FontContainerKind sourceKind,
        FontData originalData,
        FontData sfntData,
        int[] faceOffsets,
        FontLoadOptions options,
        Woff1Font? woff1,
        Woff2Font? woff2)
    {
        if (originalData == null)
        {
            throw new System.ArgumentNullException("originalData");
        }

        if (sfntData == null)
        {
            throw new System.ArgumentNullException("sfntData");
        }

        if (faceOffsets == null)
        {
            throw new System.ArgumentNullException("faceOffsets");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        bool collectionHeader = false;
        uint collectionVersion = 0;
        bool collectionDsig = false;
        uint collectionDsigTag = 0;
        long collectionDsigOffset = 0;
        byte[] collectionDsigBytes = System.Array.Empty<byte>();

        if (ContainerDetector.Detect(sfntData.GetMemory().Span) ==
            ContainerSignatureKind.Collection)
        {
            SfntCollection collection = SfntCollectionParser.Parse(sfntData, options);
            SfntCollectionDsig dsig = collection.GetDsig();
            collectionHeader = true;
            collectionVersion = collection.GetVersion();
            collectionDsig = dsig.IsPresent();

            if (collectionDsig)
            {
                collectionDsigTag = dsig.GetTag();
                collectionDsigOffset = dsig.GetOffset();
                collectionDsigBytes = dsig.CopyData();
            }
        }

        bool woffHeader = false;
        uint woffFlavor = 0;
        int woffMajorVersion = 0;
        int woffMinorVersion = 0;
        bool woffMetadata = false;
        long woffMetadataOffset = 0;
        int woffMetadataCompressedLength = 0;
        int woffMetadataOriginalLength = 0;
        byte[] woffMetadataBytes = System.Array.Empty<byte>();
        bool woffPrivateData = false;
        long woffPrivateDataOffset = 0;
        byte[] woffPrivateDataBytes = System.Array.Empty<byte>();

        if (woff1 != null)
        {
            Woff1Metadata metadata = woff1.GetMetadata();
            Woff1PrivateData privateData = woff1.GetPrivateData();
            woffHeader = true;
            woffFlavor = woff1.GetFlavor();
            woffMajorVersion = woff1.GetMajorVersion();
            woffMinorVersion = woff1.GetMinorVersion();
            woffMetadata = metadata.IsPresent();

            if (woffMetadata)
            {
                woffMetadataOffset = metadata.GetCompressedOffset();
                woffMetadataCompressedLength = metadata.GetCompressedLength();
                woffMetadataOriginalLength = metadata.GetOriginalLength();
                woffMetadataBytes = metadata.CopyData();
            }

            woffPrivateData = privateData.IsPresent();

            if (woffPrivateData)
            {
                woffPrivateDataOffset = privateData.GetOffset();
                woffPrivateDataBytes = privateData.CopyData();
            }
        }
        else if (woff2 != null)
        {
            Woff2Metadata metadata = woff2.GetMetadata();
            Woff2PrivateData privateData = woff2.GetPrivateData();
            woffHeader = true;
            woffFlavor = woff2.GetFlavor();
            woffMajorVersion = woff2.GetMajorVersion();
            woffMinorVersion = woff2.GetMinorVersion();
            woffMetadata = metadata.IsPresent();

            if (woffMetadata)
            {
                woffMetadataOffset = metadata.GetCompressedOffset();
                woffMetadataCompressedLength = metadata.GetCompressedLength();
                woffMetadataOriginalLength = metadata.GetOriginalLength();
                woffMetadataBytes = metadata.CopyData();
            }

            woffPrivateData = privateData.IsPresent();

            if (woffPrivateData)
            {
                woffPrivateDataOffset = privateData.GetOffset();
                woffPrivateDataBytes = privateData.CopyData();
            }
        }

        return new FontContainerProvenance(
            sourceKind,
            originalData.GetLength(),
            sfntData.GetLength(),
            faceOffsets,
            options.GetMediaTypeHint(),
            collectionHeader,
            collectionVersion,
            collectionDsig,
            collectionDsigTag,
            collectionDsigOffset,
            collectionDsigBytes,
            woffHeader,
            woffFlavor,
            woffMajorVersion,
            woffMinorVersion,
            woffMetadata,
            woffMetadataOffset,
            woffMetadataCompressedLength,
            woffMetadataOriginalLength,
            woffMetadataBytes,
            woffPrivateData,
            woffPrivateDataOffset,
            woffPrivateDataBytes);
    }
}
