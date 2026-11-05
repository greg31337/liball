using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageScanlineBufferTests
{
    [Test]
    public void ReadScanlineToBuffer_ReadsStrippedRowAndLeavesExtraDestinationBytesUnchanged()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 1, new ulong[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90, 90, 90, 90, 90 };

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(image.GetScanlineSize(), Is.EqualTo(3));
        Assert.That(destination, Is.EqualTo(new byte[] { 4, 5, 6, 90, 90 }));
    }

    [Test]
    public void ReadSampleScanlineToBuffer_SelectsSeparateStripWithItsOwnBitDepth()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2, 2, new ulong[] { 8, 16 }, TiffPlanarConfiguration.Separate, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.WriteRawStrip(imageIndex, 1, new byte[] { 10, 11, 12, 13, 14, 15, 16, 17 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[4];

        TiffDiagnostics diagnostics = image.ReadSampleScanlineToBuffer(1, 1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(image.GetScanlineSize(), Is.EqualTo(2));
        Assert.That(image.GetSampleScanlineSize(1), Is.EqualTo(4));
        Assert.That(destination, Is.EqualTo(new byte[] { 14, 15, 16, 17 }));
    }

    [Test]
    public void ReadScanlineToBuffer_StitchesTiledRowAndOmitsRightEdgePadding()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(5, 2, 1, new ulong[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 1, 2, 3, 6, 7, 8 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 4, 5, 90, 9, 10, 91 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[5];

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(image.GetScanlineSize(), Is.EqualTo(5));
        Assert.That(destination, Is.EqualTo(new byte[] { 6, 7, 8, 9, 10 }));
    }

    [Test]
    public void ReadSampleScanlineToBuffer_StitchesSeparateTilePlane()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(4, 2, 2, new ulong[] { 8, 8 }, 2, 2, TiffPlanarConfiguration.Separate, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 5, 6, 7, 8 });
        writer.WriteRawTile(imageIndex, 2, new byte[] { 11, 12, 13, 14 });
        writer.WriteRawTile(imageIndex, 3, new byte[] { 15, 16, 17, 18 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[4];

        TiffDiagnostics diagnostics = image.ReadSampleScanlineToBuffer(1, 1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 13, 14, 17, 18 }));
    }

    [Test]
    public void ReadScanlineToBuffer_DecodesCompressedStripBeforeCopyingRow()
    {
        byte[] compressedBytes = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 1, new ulong[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Deflate)));
        writer.WriteRawStrip(imageIndex, 0, compressedBytes);
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[3];

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void ReadScanlineToBuffer_ZeroFillsSparseStrip()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 1, new ulong[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[0]);
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 9, 9, 9 };

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 0 }));
    }

    [Test]
    public void ReadScanlineToBuffer_ZeroFillsSparseTileBesideStoredTile()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(4, 1, 1, new ulong[] { 8 }, 2, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[0]);
        writer.WriteRawTile(imageIndex, 1, new byte[] { 7, 8 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 9, 9, 9, 9 };

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(0, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 7, 8 }));
    }

    [Test]
    public void ReadScanlineToBuffer_RejectsShortDestinationWithoutChangingIt()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 1, new ulong[] { 8 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90, 91 };

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(0, destination);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 90, 91 }));
    }

    [Test]
    public void ReadScanlineToBuffer_RejectsSubByteTiledRowsWithoutChangingDestination()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(8, 1, 1, new ulong[] { 1 }, 8, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 170 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90 };

        TiffDiagnostics diagnostics = image.ReadScanlineToBuffer(0, destination);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 90 }));
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, bitsPerSample),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)compression),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, (ulong)samplesPerPixel),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)planarConfiguration)
        };
    }

    static TiffTagValue[] CreateTileTags(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, ulong tileWidth, ulong tileLength, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, bitsPerSample),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)compression),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, (ulong)samplesPerPixel),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, tileWidth),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, tileLength),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)planarConfiguration)
        };
    }
}
