using raw;

namespace raw.Tests;

public class TiffImageDataTests
{
    [Test]
    public void StrippedChunkyDescriptorRetainsImageAndStorageTags()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 7 }),
            UnsignedEntry(258, TiffFieldType.Short, new ulong[] { 12, 12, 12 }),
            UnsignedEntry(259, TiffFieldType.Short, new ulong[] { 1 }),
            UnsignedEntry(262, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(266, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(273, TiffFieldType.Long, new ulong[] { 100, 200, 300 }),
            UnsignedEntry(274, TiffFieldType.Short, new ulong[] { 6 }),
            UnsignedEntry(277, TiffFieldType.Short, new ulong[] { 3 }),
            UnsignedEntry(278, TiffFieldType.Long, new ulong[] { 3 }),
            UnsignedEntry(279, TiffFieldType.Long, new ulong[] { 30, 30, 10 }),
            UnsignedEntry(284, TiffFieldType.Short, new ulong[] { 1 }),
            UnsignedEntry(317, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(338, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(339, TiffFieldType.Short, new ulong[] { 1, 2, 3 }));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Width, Is.EqualTo(10));
            Assert.That(descriptor.Height, Is.EqualTo(7));
            Assert.That(descriptor.SamplesPerPixel, Is.EqualTo(3));
            Assert.That(descriptor.BitsPerSample, Is.EqualTo(new ulong[] { 12, 12, 12 }));
            Assert.That(descriptor.Compression, Is.EqualTo(RawCompression.None));
            Assert.That(descriptor.Photometric, Is.EqualTo(RawPhotometricInterpretation.Rgb));
            Assert.That(descriptor.FillOrder, Is.EqualTo(2));
            Assert.That(descriptor.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(descriptor.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Chunky));
            Assert.That(descriptor.Predictor, Is.EqualTo(TiffPredictor.Horizontal));
            Assert.That(descriptor.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.FloatingPoint }));
            Assert.That(descriptor.ExtraSamples, Is.EqualTo(new ulong[] { 2 }));
            Assert.That(descriptor.StorageKind, Is.EqualTo(TiffStorageKind.Strips));
            Assert.That(descriptor.Blocks.Length, Is.EqualTo(3));
            Assert.That(descriptor.Blocks.Select(block => block.Offset), Is.EqualTo(new ulong[] { 100, 200, 300 }));
            Assert.That(descriptor.Blocks.Select(block => block.ByteCount), Is.EqualTo(new ulong[] { 30, 30, 10 }));
            Assert.That(descriptor.Blocks.Select(block => block.Tiled), Is.All.False);
            Assert.That(descriptor.Blocks.Select(block => block.Plane), Is.All.EqualTo(0));
        });
    }

    [Test]
    public void TiledPlanarDescriptorMapsBlocksToPlanesAndSubfileFlags()
    {
        ulong[] offsets = Enumerable.Range(0, 27).Select(index => (ulong)(1000 + index * 100)).ToArray();
        ulong[] counts = Enumerable.Repeat(64UL, 27).ToArray();
        TiffIfd ifd = CreateIfd("IFD0/SubIFD0",
            UnsignedEntry(254, TiffFieldType.Long, new ulong[] { 7 }),
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 17 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 9 }),
            UnsignedEntry(258, TiffFieldType.Short, new ulong[] { 16 }),
            UnsignedEntry(259, TiffFieldType.Short, new ulong[] { 32946 }),
            UnsignedEntry(262, TiffFieldType.Short, new ulong[] { 32803 }),
            UnsignedEntry(277, TiffFieldType.Short, new ulong[] { 3 }),
            UnsignedEntry(284, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(317, TiffFieldType.Short, new ulong[] { 3 }),
            UnsignedEntry(322, TiffFieldType.Long, new ulong[] { 8 }),
            UnsignedEntry(323, TiffFieldType.Long, new ulong[] { 4 }),
            UnsignedEntry(324, TiffFieldType.Long8, offsets),
            UnsignedEntry(325, TiffFieldType.Long8, counts),
            UnsignedEntry(339, TiffFieldType.Short, new ulong[] { 3 }));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Path, Is.EqualTo("IFD0/SubIFD0"));
            Assert.That(descriptor.StorageKind, Is.EqualTo(TiffStorageKind.Tiles));
            Assert.That(descriptor.Compression, Is.EqualTo(RawCompression.Deflate));
            Assert.That(descriptor.Photometric, Is.EqualTo(RawPhotometricInterpretation.ColorFilterArray));
            Assert.That(descriptor.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Planar));
            Assert.That(descriptor.Predictor, Is.EqualTo(TiffPredictor.FloatingPoint));
            Assert.That(descriptor.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.FloatingPoint }));
            Assert.That(descriptor.Blocks.Length, Is.EqualTo(27));
            Assert.That(descriptor.Blocks[0].Plane, Is.EqualTo(0));
            Assert.That(descriptor.Blocks[8].IndexInPlane, Is.EqualTo(8));
            Assert.That(descriptor.Blocks[9].Plane, Is.EqualTo(1));
            Assert.That(descriptor.Blocks[9].IndexInPlane, Is.EqualTo(0));
            Assert.That(descriptor.Blocks[18].Plane, Is.EqualTo(2));
            Assert.That(descriptor.Blocks.Select(block => block.Tiled), Is.All.True);
            Assert.That(descriptor.ReducedResolution, Is.True);
            Assert.That(descriptor.Page, Is.True);
            Assert.That(descriptor.TransparencyMask, Is.True);
        });
    }

    [Test]
    public void PlanarStripsUseRowsPerStripToIdentifyPlanes()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(255, TiffFieldType.Short, new ulong[] { 3 }),
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 5 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 7 }),
            UnsignedEntry(273, TiffFieldType.Long, new ulong[] { 100, 110, 120, 200, 210, 220 }),
            UnsignedEntry(277, TiffFieldType.Short, new ulong[] { 2 }),
            UnsignedEntry(278, TiffFieldType.Long, new ulong[] { 3 }),
            UnsignedEntry(279, TiffFieldType.Long, new ulong[] { 10, 10, 5, 10, 10, 5 }),
            UnsignedEntry(284, TiffFieldType.Short, new ulong[] { 2 }));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Blocks.Select(block => block.Plane), Is.EqualTo(new int[] { 0, 0, 0, 1, 1, 1 }));
            Assert.That(descriptor.Blocks.Select(block => block.IndexInPlane), Is.EqualTo(new int[] { 0, 1, 2, 0, 1, 2 }));
            Assert.That(descriptor.OldSubfileType, Is.EqualTo(3));
            Assert.That(descriptor.Page, Is.True);
        });
    }

    [Test]
    public void DefaultsDescribeMinimalImageWithoutInventingPhotometricOrStorage()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(256, TiffFieldType.Short, new ulong[] { 1 }),
            UnsignedEntry(257, TiffFieldType.Short, new ulong[] { 1 }));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.BitsPerSample, Is.EqualTo(new ulong[] { 1 }));
            Assert.That(descriptor.SamplesPerPixel, Is.EqualTo(1));
            Assert.That(descriptor.CompressionCode, Is.EqualTo(1));
            Assert.That(descriptor.Compression, Is.EqualTo(RawCompression.None));
            Assert.That(descriptor.PhotometricCode, Is.EqualTo(ulong.MaxValue));
            Assert.That(descriptor.Photometric, Is.EqualTo(RawPhotometricInterpretation.Unknown));
            Assert.That(descriptor.FillOrder, Is.EqualTo(1));
            Assert.That(descriptor.Orientation, Is.EqualTo(RawOrientation.Normal));
            Assert.That(descriptor.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Chunky));
            Assert.That(descriptor.Predictor, Is.EqualTo(TiffPredictor.None));
            Assert.That(descriptor.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger }));
            Assert.That(descriptor.RowsPerStrip, Is.EqualTo(uint.MaxValue));
            Assert.That(descriptor.StorageKind, Is.EqualTo(TiffStorageKind.None));
            Assert.That(descriptor.Blocks, Is.Empty);
        });
    }

    [Test]
    public void MismatchedStripAndTileArraysAreRetainedForLaterValidation()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(273, TiffFieldType.Long, new ulong[] { 100, 200 }),
            UnsignedEntry(279, TiffFieldType.Long, new ulong[] { 25 }),
            UnsignedEntry(324, TiffFieldType.Long, new ulong[] { 300 }),
            UnsignedEntry(325, TiffFieldType.Long, new ulong[0]));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.StorageKind, Is.EqualTo(TiffStorageKind.Mixed));
            Assert.That(descriptor.StripOffsets, Is.EqualTo(new ulong[] { 100, 200 }));
            Assert.That(descriptor.StripByteCounts, Is.EqualTo(new ulong[] { 25 }));
            Assert.That(descriptor.TileOffsets, Is.EqualTo(new ulong[] { 300 }));
            Assert.That(descriptor.TileByteCounts, Is.Empty);
            Assert.That(descriptor.Blocks.Length, Is.EqualTo(1));
            Assert.That(descriptor.Blocks[0].Offset, Is.EqualTo(100));
        });
    }

    [Test]
    public void UnknownCodesRemainExactAndMapToUnknownMeanings()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(259, TiffFieldType.Short, new ulong[] { 7 }),
            UnsignedEntry(262, TiffFieldType.Short, new ulong[] { 999 }),
            UnsignedEntry(274, TiffFieldType.Short, new ulong[] { 999 }),
            UnsignedEntry(284, TiffFieldType.Short, new ulong[] { 999 }),
            UnsignedEntry(317, TiffFieldType.Short, new ulong[] { 999 }),
            UnsignedEntry(339, TiffFieldType.Short, new ulong[] { 999 }));

        TiffImageDescriptor descriptor = Describe(ifd);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.CompressionCode, Is.EqualTo(7));
            Assert.That(descriptor.Compression, Is.EqualTo(RawCompression.Unknown));
            Assert.That(descriptor.PhotometricCode, Is.EqualTo(999));
            Assert.That(descriptor.Photometric, Is.EqualTo(RawPhotometricInterpretation.Unknown));
            Assert.That(descriptor.OrientationCode, Is.EqualTo(999));
            Assert.That(descriptor.Orientation, Is.EqualTo(RawOrientation.Unknown));
            Assert.That(descriptor.PlanarConfigurationCode, Is.EqualTo(999));
            Assert.That(descriptor.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Unknown));
            Assert.That(descriptor.PredictorCode, Is.EqualTo(999));
            Assert.That(descriptor.Predictor, Is.EqualTo(TiffPredictor.Unknown));
            Assert.That(descriptor.SampleFormatCodes, Is.EqualTo(new ulong[] { 999 }));
            Assert.That(descriptor.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.Unknown }));
        });
    }

    [Test]
    public void CompressionAndPhotometricMappingsCoverSupportedCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TiffImageData.GetCompression(1), Is.EqualTo(RawCompression.None));
            Assert.That(TiffImageData.GetCompression(5), Is.EqualTo(RawCompression.Lzw));
            Assert.That(TiffImageData.GetCompression(6), Is.EqualTo(RawCompression.BaselineJpeg));
            Assert.That(TiffImageData.GetCompression(8), Is.EqualTo(RawCompression.AdobeDeflate));
            Assert.That(TiffImageData.GetCompression(32773), Is.EqualTo(RawCompression.PackBits));
            Assert.That(TiffImageData.GetCompression(32946), Is.EqualTo(RawCompression.Deflate));
            Assert.That(TiffImageData.GetCompression(7), Is.EqualTo(RawCompression.Unknown));
            Assert.That(TiffImageData.GetPhotometric(0), Is.EqualTo(RawPhotometricInterpretation.WhiteIsZero));
            Assert.That(TiffImageData.GetPhotometric(1), Is.EqualTo(RawPhotometricInterpretation.BlackIsZero));
            Assert.That(TiffImageData.GetPhotometric(2), Is.EqualTo(RawPhotometricInterpretation.Rgb));
            Assert.That(TiffImageData.GetPhotometric(3), Is.EqualTo(RawPhotometricInterpretation.Palette));
            Assert.That(TiffImageData.GetPhotometric(6), Is.EqualTo(RawPhotometricInterpretation.YCbCr));
            Assert.That(TiffImageData.GetPhotometric(32803), Is.EqualTo(RawPhotometricInterpretation.ColorFilterArray));
            Assert.That(TiffImageData.GetPhotometric(34892), Is.EqualTo(RawPhotometricInterpretation.LinearRaw));
        });
    }

    [Test]
    public void GraphDiscoveryReturnsEveryImageIfdInTraversalOrder()
    {
        TiffIfd first = CreateIfd("IFD0", UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 100 }), UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 80 }));
        TiffIfd metadata = CreateIfd("IFD0/Exif", UnsignedEntry(40962, TiffFieldType.Long, new ulong[] { 100 }));
        TiffIfd preview = CreateIfd("IFD0/SubIFD0", UnsignedEntry(254, TiffFieldType.Long, new ulong[] { 1 }), UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 40 }), UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 30 }));
        TiffIfd second = CreateIfd("IFD1", UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 20 }), UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 10 }));
        TiffIfdGraph graph = new TiffIfdGraph(CreateHeader(), new TiffIfd[] { first, metadata, preview, second }, new TiffIfdLink[0]);

        TiffImageDescriptor[] images = TiffImageData.Describe(graph, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(images.Select(image => image.Path), Is.EqualTo(new string[] { "IFD0", "IFD0/SubIFD0", "IFD1" }));
            Assert.That(images.Select(image => image.ImageIndex), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(images[1].ReducedResolution, Is.True);
        });
    }

    [Test]
    public void ParserAttachesMultiImageDescriptorsWithoutReadingPayloads()
    {
        byte[] source = new byte[80];
        source[0] = 0x49;
        source[1] = 0x49;
        EndianWriter.WriteUInt16(source.AsSpan(2, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(4, 4), 8, RawByteOrder.LittleEndian);
        WriteDimensionIfd(source, 8, 48, 640, 480);
        WriteDimensionIfd(source, 48, 0, 160, 120);

        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits());
        TiffIfdGraph graph = TiffParser.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Images.Length, Is.EqualTo(2));
            Assert.That(graph.Images.Select(image => image.Path), Is.EqualTo(new string[] { "IFD0", "IFD1" }));
            Assert.That(graph.Images.Select(image => image.Width), Is.EqualTo(new ulong[] { 640, 160 }));
            Assert.That(graph.Images.Select(image => image.Height), Is.EqualTo(new ulong[] { 480, 120 }));
            Assert.That(input.GetPosition(), Is.EqualTo(8));
        });
    }

    [Test]
    public void StripAndTileCountLimitIsAppliedBeforeBlockAllocation()
    {
        TiffIfd ifd = CreateIfd("IFD0",
            UnsignedEntry(256, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(257, TiffFieldType.Long, new ulong[] { 10 }),
            UnsignedEntry(273, TiffFieldType.Long, new ulong[] { 100, 200 }),
            UnsignedEntry(279, TiffFieldType.Long, new ulong[] { 10, 10 }),
            UnsignedEntry(324, TiffFieldType.Long, new ulong[] { 300 }),
            UnsignedEntry(325, TiffFieldType.Long, new ulong[] { 10 }));
        TiffIfdGraph graph = new TiffIfdGraph(CreateHeader(), new TiffIfd[] { ifd }, new TiffIfdLink[0]);
        RawLimits limits = new RawLimits();
        limits.MaxStripOrTileCount = 2;

        RawLimitException? exception = Assert.Throws<RawLimitException>(() => TiffImageData.Describe(graph, limits));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(exception.Offset, Is.EqualTo(ifd.AbsoluteOffset));
        });
    }

    static TiffImageDescriptor Describe(TiffIfd ifd)
    {
        TiffIfdGraph graph = new TiffIfdGraph(CreateHeader(), new TiffIfd[] { ifd }, new TiffIfdLink[0]);
        return TiffImageData.Describe(graph, new RawLimits())[0];
    }

    static TiffHeader CreateHeader()
    {
        return new TiffHeader(RawByteOrder.LittleEndian, false, 42, 4, 8, 0, 8);
    }

    static TiffIfd CreateIfd(string path, params TiffEntry[] entries)
    {
        return new TiffIfd(path, 8, 8, 0, entries, 0);
    }

    static TiffEntry UnsignedEntry(ushort tag, TiffFieldType fieldType, ulong[] values)
    {
        int elementSize = TiffFieldTypes.GetElementSize(fieldType);
        byte[] rawBytes = new byte[values.Length * elementSize];
        for (int index = 0; index < values.Length; index++)
        {
            int offset = index * elementSize;
            if (fieldType == TiffFieldType.Byte)
            {
                rawBytes[offset] = (byte)values[index];
            }
            else if (fieldType == TiffFieldType.Short)
            {
                EndianWriter.WriteUInt16(rawBytes.AsSpan(offset, 2), (ushort)values[index], RawByteOrder.LittleEndian);
            }
            else if (fieldType == TiffFieldType.Long || fieldType == TiffFieldType.Ifd)
            {
                EndianWriter.WriteUInt32(rawBytes.AsSpan(offset, 4), (uint)values[index], RawByteOrder.LittleEndian);
            }
            else
            {
                EndianWriter.WriteUInt64(rawBytes.AsSpan(offset, 8), values[index], RawByteOrder.LittleEndian);
            }
        }
        return new TiffEntry(tag, fieldType, (ulong)values.Length, rawBytes, new byte[4], rawBytes.Length <= 4, 0, RawByteOrder.LittleEndian, 8);
    }

    static void WriteDimensionIfd(byte[] destination, int offset, uint nextOffset, ushort width, ushort height)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), 2, RawByteOrder.LittleEndian);
        WriteInlineShortEntry(destination, offset + 2, 256, width);
        WriteInlineShortEntry(destination, offset + 14, 257, height);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 26, 4), nextOffset, RawByteOrder.LittleEndian);
    }

    static void WriteInlineShortEntry(byte[] destination, int offset, ushort tag, ushort value)
    {
        EndianWriter.WriteUInt16(destination.AsSpan(offset, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 2, 2), (ushort)TiffFieldType.Short, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(destination.AsSpan(offset + 4, 4), 1, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(destination.AsSpan(offset + 8, 2), value, RawByteOrder.LittleEndian);
    }
}
