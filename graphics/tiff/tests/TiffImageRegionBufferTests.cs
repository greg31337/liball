using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageRegionBufferTests
{
    [Test]
    public void ReadRegionToBuffer_ReadsSourceWindowAcrossStrips()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(5, 4, 1, new ulong[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        writer.WriteRawStrip(imageIndex, 1, new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(1, 1, 3, 2, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[] { 90, 90, 90, 90, 90, 90, 90, 90 };

        TiffRegionReadInfo info = image.GetRegionReadInfo(options);
        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(info.Success, Is.True);
        Assert.That(info.RowByteCount, Is.EqualTo(3));
        Assert.That(info.TotalByteCount, Is.EqualTo(6));
        Assert.That(info.SamplesPerPixel, Is.EqualTo(1));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 7, 8, 9, 12, 13, 14, 90, 90 }));
    }

    [Test]
    public void ReadRegionToBuffer_SelectsSourceSamplePlaneAndReportsItsLayout()
    {
        TiffWriter writer = CreateSeparateStripWriter();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(1, 1, 2, 1, TiffRegionOutput.Source, 1);
        byte[] destination = new byte[4];

        TiffRegionReadInfo info = image.GetRegionReadInfo(options);
        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(info.Success, Is.True);
        Assert.That(info.SampleIndex, Is.EqualTo(1));
        Assert.That(info.SamplesPerPixel, Is.EqualTo(1));
        Assert.That(info.BitsPerSample, Is.EqualTo(new int[] { 16 }));
        Assert.That(info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(info.RowByteCount, Is.EqualTo(4));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 18, 19, 20, 21 }));
    }

    [Test]
    public void ReadRegionToBuffer_NormalizesSeparateStripsToInterleavedSamples()
    {
        TiffWriter writer = CreateSeparateStripWriter();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(1, 0, 2, 2, TiffRegionOutput.Normalized, 0);
        byte[] destination = new byte[12];

        TiffRegionReadInfo info = image.GetRegionReadInfo(options);
        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(info.Success, Is.True);
        Assert.That(info.SamplesPerPixel, Is.EqualTo(2));
        Assert.That(info.BitsPerSample, Is.EqualTo(new int[] { 8, 16 }));
        Assert.That(info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Contiguous));
        Assert.That(info.RowByteCount, Is.EqualTo(6));
        Assert.That(info.TotalByteCount, Is.EqualTo(12));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 2, 12, 13, 3, 14, 15, 5, 18, 19, 6, 20, 21 }));
    }

    [Test]
    public void ReadRegionToBuffer_ReadsSourceWindowAcrossTilesAndEdgePadding()
    {
        TiffWriter writer = CreateContiguousTileWriter();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(2, 0, 3, 2, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[6];

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 3, 4, 5, 8, 9, 10 }));
    }

    [Test]
    public void ReadRegionToBuffer_NormalizesSeparateTilesToInterleavedSamples()
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
        TiffRegionReadOptions options = CreateRegionOptions(1, 0, 2, 2, TiffRegionOutput.Normalized, 0);
        byte[] destination = new byte[8];

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 2, 12, 5, 15, 4, 14, 7, 17 }));
    }

    [Test]
    public void ReadRegionToBuffer_DecodesOnlyRequiredCompressedStripRows()
    {
        byte[] compressedBytes = new byte[] { 120, 156, 99, 100, 98, 102, 97, 101, 3, 0, 0, 62, 0, 22 };
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 1, new ulong[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.Deflate)));
        writer.WriteRawStrip(imageIndex, 0, compressedBytes);
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(1, 1, 2, 1, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[2];

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 5, 6 }));
    }

    [Test]
    public void ReadRegionToBuffer_ZeroFillsSparseIntersectingTile()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(4, 1, 1, new ulong[] { 8 }, 2, 1, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[0]);
        writer.WriteRawTile(imageIndex, 1, new byte[] { 7, 8 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(1, 0, 2, 1, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[] { 9, 9 };

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 0, 7 }));
    }

    [Test]
    public void ReadRegionToBuffer_RejectsShortDestinationBeforeWriting()
    {
        TiffWriter writer = CreateContiguousTileWriter();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRegionReadOptions options = CreateRegionOptions(2, 0, 3, 2, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[] { 90, 91, 92, 93, 94 };

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 90, 91, 92, 93, 94 }));
    }

    [Test]
    public void ReadRegionToBuffer_DoesNotReadUnrelatedTilePayload()
    {
        TiffWriter writer = CreateContiguousTileWriter();
        TrackingSource source = new TrackingSource(writer.GetFileBytes());
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.LeaveOpen = true;
        TiffFile file = TiffApi.Factory.OpenReadSource(source, readOptions);
        ITiffImage image = file.GetImage(0);
        ulong[] tileOffsets = image.GetUnsignedTagArray(TiffTagId.TileOffsets);
        image.GetUnsignedTagArray(TiffTagId.TileByteCounts);
        source.ClearRequests();
        TiffRegionReadOptions options = CreateRegionOptions(0, 0, 2, 1, TiffRegionOutput.Source, 0);
        byte[] destination = new byte[2];

        TiffDiagnostics diagnostics = image.ReadRegionToBuffer(options, destination);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(source.HasReadOffset((long)tileOffsets[0]), Is.True);
        Assert.That(source.HasReadRange((long)tileOffsets[1], 6), Is.False);
    }

    static TiffWriter CreateSeparateStripWriter()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(3, 2, 2, new ulong[] { 8, 16 }, 2, TiffPlanarConfiguration.Separate, TiffCompression.None)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4, 5, 6 });
        writer.WriteRawStrip(imageIndex, 1, new byte[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 });
        writer.Flush();
        return writer;
    }

    static TiffWriter CreateContiguousTileWriter()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags(5, 2, 1, new ulong[] { 8 }, 3, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None)));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 1, 2, 3, 6, 7, 8 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 4, 5, 90, 9, 10, 91 });
        writer.Flush();
        return writer;
    }

    static TiffRegionReadOptions CreateRegionOptions(ulong column, ulong row, ulong width, ulong length, TiffRegionOutput output, int sampleIndex)
    {
        TiffRegionReadOptions options = new TiffRegionReadOptions();
        options.Region.Column = column;
        options.Region.Row = row;
        options.Region.Width = width;
        options.Region.Length = length;
        options.Output = output;
        options.SampleIndex = sampleIndex;
        return options;
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, int samplesPerPixel, ulong[] bitsPerSample, ulong rowsPerStrip, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, bitsPerSample),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)compression),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, (ulong)samplesPerPixel),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, rowsPerStrip),
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

    public class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public List<long> Offsets;
        public List<int> Counts;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            Offsets = new List<long>();
            Counts = new List<int>();
        }

        public bool GetIsOpen()
        {
            return IsOpen;
        }

        public long GetLength()
        {
            return Bytes.Length;
        }

        public int Read(long offset, Span<byte> destination)
        {
            Offsets.Add(offset);
            Counts.Add(destination.Length);
            Bytes.AsSpan((int)offset, destination.Length).CopyTo(destination);
            return destination.Length;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void ClearRequests()
        {
            Offsets.Clear();
            Counts.Clear();
        }

        public bool HasReadOffset(long offset)
        {
            int index = 0;
            while (index < Offsets.Count)
            {
                if (Offsets[index] == offset)
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }

        public bool HasReadRange(long offset, int count)
        {
            long end = offset + count;
            int index = 0;
            while (index < Offsets.Count)
            {
                long requestEnd = Offsets[index] + Counts[index];
                if (Offsets[index] < end && requestEnd > offset)
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }
    }
}
