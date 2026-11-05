using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontContainerProvenanceTests
{
    [Test]
    public void ExposesScalarLengthsFaceOffsetAndCallerMediaTypeHint()
    {
        byte[] source = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1, 2, 3 })
            .Build();
        FontPackage package = new FontLoader().LoadFromBytes(
            source,
            InspectOptions("font/ttf"));
        FontContainerProvenance provenance = package.GetProvenance();

        Assert.Multiple(() =>
        {
            Assert.That(provenance, Is.SameAs(package.GetProvenance()));
            Assert.That(provenance.GetSourceKind(), Is.EqualTo(FontContainerKind.Sfnt));
            Assert.That(provenance.GetOriginalLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetReconstructedLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetFaceCount(), Is.EqualTo(1));
            Assert.That(provenance.GetFaceOffset(0), Is.Zero);
            Assert.That(provenance.GetMediaTypeHint(), Is.EqualTo("font/ttf"));
            Assert.That(provenance.HasCollectionHeader(), Is.False);
            Assert.That(provenance.HasCollectionDsig(), Is.False);
            Assert.That(provenance.HasWoffHeader(), Is.False);
            Assert.That(provenance.HasWoffMetadata(), Is.False);
            Assert.That(provenance.HasWoffPrivateData(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => provenance.GetFaceOffset(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => provenance.GetFaceOffset(1));
            Assert.Throws<InvalidOperationException>(() => provenance.GetCollectionVersion());
            Assert.Throws<InvalidOperationException>(() => provenance.GetWoffFlavor());
        });
    }

    [Test]
    public void ExposesCollectionVersionFaceOffsetsAndIndependentDsigCopies()
    {
        byte[] source = BuildVersionTwoCollection();
        FontContainerProvenance provenance = new FontLoader()
            .LoadFromBytes(source, InspectOptions(null))
            .GetProvenance();
        byte[] firstDsig = provenance.CopyCollectionDsigBytes();
        byte[] secondDsig = provenance.CopyCollectionDsigBytes();
        firstDsig[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(provenance.GetSourceKind(), Is.EqualTo(FontContainerKind.Collection));
            Assert.That(provenance.GetOriginalLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetReconstructedLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetFaceCount(), Is.EqualTo(1));
            Assert.That(provenance.GetFaceOffset(0), Is.EqualTo(28));
            Assert.That(provenance.GetMediaTypeHint(), Is.Null);
            Assert.That(provenance.HasCollectionHeader(), Is.True);
            Assert.That(provenance.GetCollectionVersion(), Is.EqualTo(0x00020000U));
            Assert.That(provenance.HasCollectionDsig(), Is.True);
            Assert.That(provenance.GetCollectionDsigTag(), Is.EqualTo((uint)FontTags.Dsig));
            Assert.That(provenance.GetCollectionDsigOffset(), Is.EqualTo(60));
            Assert.That(provenance.GetCollectionDsigLength(), Is.EqualTo(5));
            Assert.That(secondDsig, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
            Assert.That(firstDsig, Is.Not.SameAs(secondDsig));
            Assert.That(provenance.CopyCollectionDsigBytes(), Is.EqualTo(secondDsig));
            Assert.That(provenance.HasWoffHeader(), Is.False);
        });
    }

    [Test]
    public void ExposesWoff1HeaderMetadataAndPrivateBlockAsIndependentCopies()
    {
        const string metadataText = "<metadata version=\"1.0\"></metadata>";
        byte[] privateData = new byte[] { 7, 6, 5, 4 };
        byte[] source = new Woff1Builder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1, 2, 3 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .AddMetadata(metadataText)
            .AddPrivateData(privateData)
            .Build();
        FontPackage package = new FontLoader().LoadFromBytes(
            source,
            InspectOptions("font/woff"));
        FontContainerProvenance provenance = package.GetProvenance();
        byte[] metadataCopy = provenance.CopyWoffMetadataBytes();
        byte[] privateCopy = provenance.CopyWoffPrivateDataBytes();
        metadataCopy[0] = 0;
        privateCopy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(provenance.GetSourceKind(), Is.EqualTo(FontContainerKind.Woff1));
            Assert.That(provenance.GetOriginalLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetReconstructedLength(),
                Is.EqualTo(package.CopySfntBytes(0).Length));
            Assert.That(provenance.GetFaceOffset(0), Is.Zero);
            Assert.That(provenance.GetMediaTypeHint(), Is.EqualTo("font/woff"));
            Assert.That(provenance.HasWoffHeader(), Is.True);
            Assert.That(provenance.GetWoffFlavor(), Is.EqualTo(0x00010000U));
            Assert.That(provenance.GetWoffMajorVersion(), Is.EqualTo(1));
            Assert.That(provenance.GetWoffMinorVersion(), Is.Zero);
            Assert.That(provenance.HasWoffMetadata(), Is.True);
            Assert.That(provenance.GetWoffMetadataOffset(), Is.GreaterThan(0));
            Assert.That(provenance.GetWoffMetadataCompressedLength(), Is.GreaterThan(0));
            Assert.That(provenance.GetWoffMetadataOriginalLength(),
                Is.EqualTo(Encoding.UTF8.GetByteCount(metadataText)));
            Assert.That(Encoding.UTF8.GetString(provenance.CopyWoffMetadataBytes()),
                Is.EqualTo(metadataText));
            Assert.That(provenance.HasWoffPrivateData(), Is.True);
            Assert.That(provenance.GetWoffPrivateDataOffset(), Is.GreaterThan(0));
            Assert.That(provenance.GetWoffPrivateDataLength(), Is.EqualTo(privateData.Length));
            Assert.That(provenance.CopyWoffPrivateDataBytes(), Is.EqualTo(privateData));
        });
    }

    [Test]
    public void ExposesWoff2OptionalBlocksAndReconstructedCollectionMetadata()
    {
        byte[] metadata = Encoding.UTF8.GetBytes("<metadata version=\"1.0\"/>");
        byte[] privateData = new byte[] { 4, 3, 2, 1 };
        byte[] source = new Woff2Builder()
            .AddKnownTable(5, new byte[] { 1, 2, 3 })
            .AddCollectionFace(0x00010000U, 0)
            .SetCollectionVersion(0x00020000U)
            .AddMetadata(metadata)
            .AddPrivateData(privateData)
            .Build();
        FontContainerProvenance provenance = new FontLoader()
            .LoadFromBytes(source, InspectOptions("font/woff2"))
            .GetProvenance();

        Assert.Multiple(() =>
        {
            Assert.That(provenance.GetSourceKind(), Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(provenance.GetOriginalLength(), Is.EqualTo(source.Length));
            Assert.That(provenance.GetReconstructedLength(), Is.GreaterThan(0));
            Assert.That(provenance.GetFaceCount(), Is.EqualTo(1));
            Assert.That(provenance.GetFaceOffset(0), Is.EqualTo(28));
            Assert.That(provenance.GetMediaTypeHint(), Is.EqualTo("font/woff2"));
            Assert.That(provenance.HasCollectionHeader(), Is.True);
            Assert.That(provenance.GetCollectionVersion(), Is.EqualTo(0x00020000U));
            Assert.That(provenance.HasCollectionDsig(), Is.False);
            Assert.That(provenance.HasWoffHeader(), Is.True);
            Assert.That(provenance.GetWoffFlavor(), Is.EqualTo(0x74746366U));
            Assert.That(provenance.GetWoffMajorVersion(), Is.EqualTo(1));
            Assert.That(provenance.GetWoffMinorVersion(), Is.Zero);
            Assert.That(provenance.HasWoffMetadata(), Is.True);
            Assert.That(provenance.GetWoffMetadataOriginalLength(), Is.EqualTo(metadata.Length));
            Assert.That(provenance.CopyWoffMetadataBytes(), Is.EqualTo(metadata));
            Assert.That(provenance.HasWoffPrivateData(), Is.True);
            Assert.That(provenance.GetWoffPrivateDataLength(), Is.EqualTo(privateData.Length));
            Assert.That(provenance.CopyWoffPrivateDataBytes(), Is.EqualTo(privateData));
            Assert.Throws<InvalidOperationException>(() => provenance.GetCollectionDsigLength());
        });
    }

    static FontLoadOptions InspectOptions(string? mediaTypeHint)
    {
        FontApiFactory factory = new FontApiFactory();
        return factory.CreateConfiguredLoadOptionsWithMediaTypeHint(
            FontLoadMode.InspectOnly,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.ReportMismatch,
            factory.CreateInspectOnlyLimits(),
            CancellationToken.None,
            mediaTypeHint);
    }

    static byte[] BuildVersionTwoCollection()
    {
        int faceOffset = 28;
        int tableOffset = 56;
        int dsigOffset = 60;
        byte[] tableData = new byte[] { 1, 2, 3, 4 };
        byte[] dsigData = new byte[] { 9, 8, 7, 6, 5 };
        byte[] bytes = new byte[65];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x74746366U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0x00020000U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), (uint)faceOffset);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), (uint)FontTags.Dsig);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)dsigData.Length);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), (uint)dsigOffset);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(faceOffset, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(faceOffset + 4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(faceOffset + 6, 2), 16);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 12, 4),
            (uint)FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 16, 4),
            FontChecksum.CalculateTable(tableData));
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 20, 4),
            (uint)tableOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 24, 4),
            (uint)tableData.Length);
        tableData.CopyTo(bytes, tableOffset);
        dsigData.CopyTo(bytes, dsigOffset);
        return bytes;
    }
}
