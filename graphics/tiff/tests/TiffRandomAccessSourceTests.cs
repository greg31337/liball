using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRandomAccessSourceTests
{
    [Test]
    public void OpenReadSource_DefersExternalTagAndStripReads()
    {
        byte[] writtenBytes = CreateClassicFileBytes();
        byte[] sourceBytes = new byte[writtenBytes.Length + 1024 * 1024];
        writtenBytes.CopyTo(sourceBytes, 0);
        TrackingSource source = new TrackingSource(sourceBytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffFile file = TiffApi.Factory.OpenReadSource(source, options);
        TiffImage image = file.GetImage(0);
        TiffIfdEntry description = image.GetTag(TiffTagId.ImageDescription)!;
        TiffIfdEntry stripOffset = image.GetTag(TiffTagId.StripOffsets)!;

        Assert.That(file.GetSuccess(), Is.True);
        Assert.That(file.GetSource(), Is.SameAs(source));
        Assert.That(image.GetSource(), Is.SameAs(source));
        Assert.That(source.HasReadOffset((long)description.ValueOffset), Is.False);
        Assert.That(source.HasReadOffset((long)stripOffset.ValueOffset), Is.False);
        Assert.That(source.LargestRequest, Is.LessThan(sourceBytes.Length));
        Assert.That(source.TotalRequested, Is.LessThan(sourceBytes.Length));

        TiffTagDefaultResult descriptionResult = image.GetTagDefaulted(TiffTagId.ImageDescription);

        Assert.That(descriptionResult.Found, Is.True);
        Assert.That(source.HasReadOffset((long)description.ValueOffset), Is.True);
        Assert.That(source.HasReadOffset((long)stripOffset.ValueOffset), Is.False);

        TiffRawStripReadResult strip = image.ReadRawStrip(0);

        Assert.That(strip.Success, Is.True);
        Assert.That(strip.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(source.HasReadOffset((long)stripOffset.ValueOffset), Is.True);
    }

    [Test]
    public void StreamSource_UsesConstructionPositionAsOriginAndRestoresPosition()
    {
        byte[] bytes = CreateClassicFileBytes();
        byte[] prefixedBytes = new byte[bytes.Length + 5];
        bytes.CopyTo(prefixedBytes, 5);
        MemoryStream stream = new MemoryStream(prefixedBytes);
        stream.Position = 5;
        TiffStreamSource source = TiffApi.Factory.CreateStreamSource(stream);
        byte[] destination = new byte[4];

        int read = source.Read(0, destination);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(destination, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2a, 0x00 }));
        Assert.That(stream.Position, Is.EqualTo(5));
        Assert.That(source.GetLength(), Is.EqualTo(bytes.Length));
    }

    [Test]
    public void OpenReadSource_DefersTilePayloadRead()
    {
        byte[] bytes = CreateTiledFileBytes();
        TrackingSource source = new TrackingSource(bytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffImage image = TiffApi.Factory.OpenReadSource(source, options).GetImage(0);
        TiffIfdEntry tileOffset = image.GetTag(TiffTagId.TileOffsets)!;

        Assert.That(source.HasReadOffset((long)tileOffset.ValueOffset), Is.False);

        TiffRawTileReadResult tile = image.ReadRawTile(0);

        Assert.That(tile.Success, Is.True);
        Assert.That(tile.Bytes, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
        Assert.That(source.HasReadOffset((long)tileOffset.ValueOffset), Is.True);
    }

    [Test]
    public void FileSource_ReadsWithBuiltInRandomAccessApi()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        try
        {
            byte[] bytes = CreateClassicFileBytes();
            File.WriteAllBytes(path, bytes);
            TiffFileSource source = TiffApi.Factory.CreateFileSource(path);
            byte[] destination = new byte[4];

            int read = source.Read(0, destination);

            Assert.That(read, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2a, 0x00 }));
            Assert.That(source.GetLength(), Is.EqualTo(bytes.Length));
            source.Close();
            Assert.That(source.GetIsOpen(), Is.False);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void OpenReadSource_HonorsCallerSourceOwnership()
    {
        TrackingSource closedSource = new TrackingSource(CreateClassicFileBytes());
        TrackingSource retainedSource = new TrackingSource(CreateClassicFileBytes());
        TiffReadOptions retainedOptions = new TiffReadOptions();
        retainedOptions.LeaveOpen = true;
        TiffFile closedFile = TiffApi.Factory.OpenReadSource(closedSource, new TiffReadOptions());
        TiffFile retainedFile = TiffApi.Factory.OpenReadSource(retainedSource, retainedOptions);

        closedFile.Close();
        retainedFile.Close();

        Assert.That(closedSource.GetIsOpen(), Is.False);
        Assert.That(retainedSource.GetIsOpen(), Is.True);
    }

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "random access description")
        };
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    static byte[] CreateTiledFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, 2)
        };
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 9, 8, 7, 6 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    public class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public int LargestRequest;
        public long TotalRequested;
        public List<long> Offsets;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            LargestRequest = 0;
            TotalRequested = 0;
            Offsets = new List<long>();
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
            if (destination.Length > LargestRequest)
            {
                LargestRequest = destination.Length;
            }

            TotalRequested = TotalRequested + destination.Length;
            int count = destination.Length;
            long remaining = Bytes.Length - offset;
            if (remaining < count)
            {
                count = (int)remaining;
            }

            Bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public bool HasReadOffset(long offset)
        {
            return Offsets.Contains(offset);
        }
    }
}
