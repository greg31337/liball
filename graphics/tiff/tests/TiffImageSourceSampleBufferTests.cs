using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageSourceSampleBufferTests
{
    [Test]
    public void ReadSourceRasterToBuffer_PreservesPackedBilevelRows()
    {
        TiffWriter writer = CreateStripWriter(8, 1, 1, new ulong[] { 1 }, new ulong[] { (ulong)TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { new byte[] { 170 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90, 91 };

        TiffRasterReadInfo info = image.GetSourceRasterReadInfo();
        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(destination);

        Assert.That(info.Success, Is.True);
        Assert.That(info.IsPacked, Is.True);
        Assert.That(info.BitsPerPixel, Is.EqualTo(1));
        Assert.That(info.RowBitCount, Is.EqualTo(8));
        Assert.That(info.RowByteCount, Is.EqualTo(1));
        Assert.That(info.TotalByteCount, Is.EqualTo(1));
        Assert.That(info.ByteOrder, Is.EqualTo(TiffByteOrder.Unknown));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 170, 91 }));
    }

    [Test]
    public void ReadSamplePlaneToBuffer_ExtractsMixedPackedSamplesAtExactBitWidths()
    {
        TiffWriter writer = CreateStripWriter(2, 1, 2, new ulong[] { 4, 12 }, new ulong[] { (ulong)TiffSampleFormat.UnsignedInteger, (ulong)TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { new byte[] { 161, 35, 180, 86 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] raster = new byte[4];
        byte[] firstPlane = new byte[1];
        byte[] secondPlane = new byte[3];

        TiffRasterReadInfo rasterInfo = image.GetSourceRasterReadInfo();
        TiffRasterReadInfo planeInfo = image.GetSamplePlaneReadInfo(1);
        TiffDiagnostics rasterDiagnostics = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics firstDiagnostics = image.ReadSamplePlaneToBuffer(0, firstPlane);
        TiffDiagnostics secondDiagnostics = image.ReadSamplePlaneToBuffer(1, secondPlane);

        Assert.That(rasterInfo.IsPacked, Is.True);
        Assert.That(rasterInfo.BitsPerSample, Is.EqualTo(new int[] { 4, 12 }));
        Assert.That(rasterInfo.RowBitCount, Is.EqualTo(32));
        Assert.That(planeInfo.IsPacked, Is.True);
        Assert.That(planeInfo.SampleIndex, Is.EqualTo(1));
        Assert.That(planeInfo.BitsPerPixel, Is.EqualTo(12));
        Assert.That(planeInfo.RowBitCount, Is.EqualTo(24));
        Assert.That(rasterDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(firstDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(secondDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[] { 161, 35, 180, 86 }));
        Assert.That(firstPlane, Is.EqualTo(new byte[] { 171 }));
        Assert.That(secondPlane, Is.EqualTo(new byte[] { 18, 52, 86 }));
    }

    [Test]
    public void ReadSourceRasterToBuffer_InterleavesPackedSeparatePlanes()
    {
        TiffWriter writer = CreateStripWriter(4, 1, 2, new ulong[] { 1, 1 }, new ulong[] { (ulong)TiffSampleFormat.UnsignedInteger, (ulong)TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Separate, TiffCompression.None, new byte[][] { new byte[] { 160 }, new byte[] { 192 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[1];

        TiffRasterReadInfo info = image.GetSourceRasterReadInfo();
        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(destination);

        Assert.That(info.SourcePlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Contiguous));
        Assert.That(info.RowBitCount, Is.EqualTo(8));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 216 }));
    }

    [Test]
    public void ReadSourceRasterToBuffer_ReportsAndCopiesMixedSignedAndFloatingSamples()
    {
        byte[] source = new byte[] { 254, 255, 0, 0, 192, 63, 7, 0, 0, 0, 16, 192 };
        TiffWriter writer = CreateStripWriter(2, 1, 2, new ulong[] { 16, 32 }, new ulong[] { (ulong)TiffSampleFormat.SignedInteger, (ulong)TiffSampleFormat.IeeeFloatingPoint }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { source });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] raster = new byte[12];
        byte[] signedPlane = new byte[4];
        byte[] floatingPlane = new byte[8];

        TiffRasterReadInfo info = image.GetSourceRasterReadInfo();
        TiffDiagnostics rasterDiagnostics = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics signedDiagnostics = image.ReadSamplePlaneToBuffer(0, signedPlane);
        TiffDiagnostics floatingDiagnostics = image.ReadSamplePlaneToBuffer(1, floatingPlane);

        Assert.That(info.IsPacked, Is.False);
        Assert.That(info.BitsPerSample, Is.EqualTo(new int[] { 16, 32 }));
        Assert.That(info.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.SignedInteger, TiffSampleFormat.IeeeFloatingPoint }));
        Assert.That(info.BitsPerPixel, Is.EqualTo(48));
        Assert.That(info.RowByteCount, Is.EqualTo(12));
        Assert.That(info.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(rasterDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(signedDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(floatingDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(source));
        Assert.That(signedPlane, Is.EqualTo(new byte[] { 254, 255, 7, 0 }));
        Assert.That(floatingPlane, Is.EqualTo(new byte[] { 0, 0, 192, 63, 0, 0, 16, 192 }));
    }

    [Test]
    public void ReadSourceRasterToBuffer_InterleavesByteAlignedSeparateSamples()
    {
        TiffWriter writer = CreateStripWriter(2, 1, 2, new ulong[] { 8, 16 }, new ulong[] { (ulong)TiffSampleFormat.UnsignedInteger, (ulong)TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Separate, TiffCompression.None, new byte[][] { new byte[] { 1, 2 }, new byte[] { 10, 11, 12, 13 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] raster = new byte[6];
        byte[] plane = new byte[4];

        TiffDiagnostics rasterDiagnostics = image.ReadSourceRasterToBuffer(raster);
        TiffDiagnostics planeDiagnostics = image.ReadSamplePlaneToBuffer(1, plane);

        Assert.That(rasterDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(planeDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(raster, Is.EqualTo(new byte[] { 1, 10, 11, 2, 12, 13 }));
        Assert.That(plane, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
    }

    [Test]
    public void ReadSamplePlaneToBuffer_StitchesContiguousPartialEdgeTiles()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(3, 2, 3, new ulong[] { 8, 8, 8 }, new ulong[] { 1, 1, 1 }, 2, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 1, 10, 100, 2, 20, 101, 4, 40, 103, 5, 50, 104 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 3, 30, 102, 90, 90, 90, 6, 60, 105, 91, 91, 91 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[6];

        TiffDiagnostics diagnostics = image.ReadSamplePlaneToBuffer(1, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
    }

    [Test]
    public void ReadSourceRasterToBuffer_DecodesCompressedSegments()
    {
        byte[] compressedBytes = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };
        TiffWriter writer = CreateStripWriter(3, 2, 1, new ulong[] { 8 }, new ulong[] { 1 }, TiffPlanarConfiguration.Contiguous, TiffCompression.Deflate, new byte[][] { compressedBytes });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[6];

        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void ReadSamplePlaneToBuffer_ZeroFillsSparsePlane()
    {
        TiffWriter writer = CreateStripWriter(3, 1, 1, new ulong[] { 8 }, new ulong[] { 1 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { new byte[0] });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 9, 9, 9 };

        TiffDiagnostics diagnostics = image.ReadSamplePlaneToBuffer(0, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 0 }));
    }

    [Test]
    public void ReadSourceRasterToBuffer_RejectsShortDestinationBeforeWriting()
    {
        TiffWriter writer = CreateStripWriter(3, 1, 1, new ulong[] { 8 }, new ulong[] { 1 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { new byte[] { 1, 2, 3 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90, 91 };

        TiffDiagnostics diagnostics = image.ReadSourceRasterToBuffer(destination);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 90, 91 }));
    }

    [Test]
    public void ReadSamplePlaneToBuffer_RejectsNegativeSampleIndexBeforeWriting()
    {
        TiffWriter writer = CreateStripWriter(3, 1, 1, new ulong[] { 8 }, new ulong[] { 1 }, TiffPlanarConfiguration.Contiguous, TiffCompression.None, new byte[][] { new byte[] { 1, 2, 3 } });
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        byte[] destination = new byte[] { 90, 91, 92 };

        TiffDiagnostics diagnostics = image.ReadSamplePlaneToBuffer(-1, destination);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 90, 91, 92 }));
    }

    static TiffWriter CreateStripWriter(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, ulong[] sampleFormats, TiffPlanarConfiguration planarConfiguration, TiffCompression compression, byte[][] strips)
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(width, length, samplesPerPixel, bitsPerSample, sampleFormats, planarConfiguration, compression)));
        int stripIndex = 0;
        while (stripIndex < strips.Length)
        {
            writer.WriteRawStrip(imageIndex, stripIndex, strips[stripIndex]);
            stripIndex = stripIndex + 1;
        }

        writer.Flush();
        return writer;
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, ulong[] sampleFormats, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
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
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)planarConfiguration),
            TiffTagValue.CreateUnsignedArray(TiffTagId.SampleFormat, TiffDataType.Short, sampleFormats)
        };
    }

    static TiffTagValue[] CreateTileTags(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, ulong[] sampleFormats, ulong tileWidth, ulong tileLength, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
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
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)planarConfiguration),
            TiffTagValue.CreateUnsignedArray(TiffTagId.SampleFormat, TiffDataType.Short, sampleFormats)
        };
    }
}
