using System.Buffers.Binary;
using System.IO;

namespace avif.Tests;

public class AvifReaderTests
{
    [Test]
    public void ByteSourceLimitIsAppliedBeforeReaderParsing()
    {
        AvifDecoderOptions options = new AvifDecoderOptions();
        options.Limits.MaxSourceBytes = 1;
        AvifReader reader = AvifReader.Create(new byte[2], options);

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(reader.Parse(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        reader.Close();
    }

    [Test]
    public void ParseHonorsCancellationBeforeConsumingOrPublishingInput()
    {
        AvifDecoderOptions byteOptions = new AvifDecoderOptions();
        byteOptions.Cancellation.Cancel();
        AvifReader byteReader = AvifReader.Create(new byte[8], byteOptions);

        AvifDecoderOptions streamOptions = new AvifDecoderOptions();
        streamOptions.Cancellation.Cancel();
        TrackingReadStream stream = new TrackingReadStream(new byte[8]);
        AvifReader streamReader = AvifReader.Create(stream, streamOptions);

        Assert.Multiple(() =>
        {
            Assert.That(byteReader.GetCode(),
                Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(byteReader.Parse(),
                Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(byteReader.GetDocument(), Is.Null);
            Assert.That(streamReader.Parse(),
                Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(stream.ReadCount, Is.Zero);
            Assert.That(stream.Closed, Is.False);
        });

        byteReader.Close();
        streamReader.Close();
        stream.Dispose();
    }

    [Test]
    public void ByteArrayReaderOwnsSourceAndSupportsParseDecodeExtractResetAndClose()
    {
        AvifImage sourceImage = CreateMonochromeImage(3, 2, 17);
        byte[] encoded = Avif.Encode(sourceImage).GetData();
        AvifReader reader = AvifReader.Create(encoded);
        System.Array.Clear(encoded);

        AvifDecodeResult beforeParse = reader.DecodePrimaryImage();
        AvifResultCode parse = reader.Parse();
        uint itemId = ((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        byte[] firstExtraction = reader.GetEncodedItem(itemId);
        byte firstByte = firstExtraction[0];
        firstExtraction[0] ^= 255;
        byte[] secondExtraction = reader.GetEncodedItem(itemId);
        AvifDecodeResult primary = reader.DecodePrimaryImage();
        AvifDecodeResult item = reader.DecodeItem(itemId);
        AvifDecodeResult missingFrame = reader.DecodeFrame(0);
        AvifDecodeResult missingLayer = reader.DecodeLayer(0);
        AvifFrameTiming? missingTiming = reader.GetFrameTiming(0);

        Assert.Multiple(() =>
        {
            Assert.That(beforeParse.GetCode(), Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetDocument(), Is.Not.Null);
            Assert.That(secondExtraction, Is.Not.Empty);
            Assert.That(secondExtraction[0], Is.EqualTo(firstByte));
            Assert.That(primary.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(item.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(missingFrame.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(missingLayer.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(missingTiming, Is.Null);
            Assert.That(reader.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertPlaneValue(primary.GetImage(), 17);
        AssertPlaneValue(item.GetImage(), 17);

        Assert.That(reader.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.GetDocument(), Is.Null);
        Assert.That((primary.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetCode(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.GetDocument(), Is.Null);
            Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.Reset(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.DecodeItem(itemId).GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));
        });

        sourceImage.Close();
        (primary.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (item.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void StreamReaderBuffersOnceAndNeverClosesCallerStream()
    {
        AvifImage image = CreateMonochromeImage(2, 2, 29);
        byte[] encoded = Avif.Encode(image).GetData();
        TrackingReadStream stream = new TrackingReadStream(encoded);
        AvifReader reader = AvifReader.Create(stream);

        AvifResultCode firstParse = reader.Parse();
        int readsAfterFirstParse = stream.ReadCount;
        reader.Reset();
        AvifResultCode secondParse = reader.Parse();
        AvifDecodeResult decoded = reader.DecodePrimaryImage();
        reader.Close();

        Assert.Multiple(() =>
        {
            Assert.That(firstParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(stream.ReadCount, Is.EqualTo(readsAfterFirstParse));
            Assert.That(stream.Closed, Is.False);
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(decoded.GetImage(), 29);

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        stream.Dispose();
    }

    [Test]
    public void ReaderDecodesGridItemsAndAddressedTiles()
    {
        AvifImage left = CreateMonochromeImage(3, 2, 41);
        AvifImage right = CreateMonochromeImage(3, 2, 83);
        byte[] gridSource = BuildGridSource(left, right);
        AvifReader reader = AvifReader.Create(gridSource);

        AvifResultCode parse = reader.Parse();
        AvifDecodeResult grid = reader.DecodeItem(3);
        AvifDecodeResult tile = reader.DecodeTile(3, 0, 1);
        AvifDecodeResult missingTile = reader.DecodeTile(3, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(grid.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((grid.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(6));
            Assert.That((grid.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(2));
            Assert.That(
                ((grid.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 41, 41, 41, 83, 83, 83 }));
            Assert.That(tile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((tile.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(3));
            Assert.That(missingTile.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertPlaneValue(tile.GetImage(), 83);

        left.Close();
        right.Close();
        (grid.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (tile.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
    }

    [Test]
    public void ReaderUsesLayerIndexBoundariesForProgressiveDecode()
    {
        AvifImage image = CreateMonochromeImage(2, 2, 61);
        byte[] layeredSource = BuildLayeredSource(image);
        AvifReader reader = AvifReader.Create(layeredSource);

        AvifResultCode parse = reader.Parse();
        AvifDecodeResult firstLayer = reader.DecodeLayer(0);
        AvifDecodeResult finalLayer = reader.DecodeLayer(1);
        AvifDecodeResult missingLayer = reader.DecodeLayer(2);

        Assert.Multiple(() =>
        {
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstLayer.GetCode(), Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(finalLayer.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(missingLayer.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertPlaneValue(finalLayer.GetImage(), 61);

        image.Close();
        (finalLayer.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
    }

    [Test]
    public void InvalidSourcesOptionsAndIdentifiersReturnStableCodes()
    {
        AvifReader? nullBytes = AvifReader.Create((byte[]?)null);
        AvifReader? nullStream = AvifReader.Create((Stream?)null);
        AvifReader? invalidOptions = AvifReader.Create(
            new byte[0], (AvifDecoderOptions?)null);
        AvifImage image = CreateMonochromeImage(1, 1, 7);
        AvifReader reader = AvifReader.Create(Avif.Encode(image).GetData());
        reader.Parse();

        Assert.Multiple(() =>
        {
            Assert.That(nullBytes.Parse(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(nullStream.Parse(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidOptions.Parse(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(reader.DecodeItem(0).GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(reader.DecodeItem(999).GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(reader.GetEncodedItem(0), Is.Empty);
            Assert.That(reader.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
        reader.Close();
    }

    static AvifImage CreateMonochromeImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < height)
        {
            (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertPlaneValue(AvifImage? image, byte value)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            Assert.That(
                plane.GetRowSpan(row).ToArray(),
                Is.EqualTo(Enumerable.Repeat(value, plane.GetWidth()).ToArray()));
            row++;
        }
    }

    static byte[] BuildGridSource(AvifImage left, AvifImage right)
    {
        CanonicalItemPayload leftItem = CreateCodedItem(1, Avif.Encode(left).GetData());
        CanonicalItemPayload rightItem = CreateCodedItem(2, Avif.Encode(right).GetData());
        byte[] gridPayload = new byte[] { 0, 0, 0, 1, 0, 6, 0, 2 };
        CanonicalItemPayload gridItem = CanonicalItemPayload.Create(
            3,
            "grid",
            string.Empty,
            false,
            gridPayload);
        gridItem.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildSpatialExtentsProperty(6, 2),
            false));
        CanonicalItemReference derived = new CanonicalItemReference(
            "dimg",
            3,
            new List<uint> { 1, 2 });
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 65536;
        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            3,
            new List<CanonicalItemPayload> { leftItem, rightItem, gridItem },
            new List<CanonicalItemReference> { derived },
            new List<CanonicalEntityGroup>(),
            options);
        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        return written.Data;
    }

    static byte[] BuildLayeredSource(AvifImage image)
    {
        CanonicalItemPayload item = CreateCodedItem(1, Avif.Encode(image).GetData());
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            item.Data,
            new AvifSecurityLimits(),
            0);
        int firstLayerSize = parsed.Obus[0].TotalSize;
        byte[] layeredIndex = new byte[15];
        BinaryPrimitives.WriteUInt32BigEndian(layeredIndex, 15);
        layeredIndex[4] = (byte)'a';
        layeredIndex[5] = (byte)'1';
        layeredIndex[6] = (byte)'l';
        layeredIndex[7] = (byte)'x';
        BinaryPrimitives.WriteUInt16BigEndian(
            layeredIndex.AsSpan(9, 2),
            (ushort)firstLayerSize);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            layeredIndex,
            false));
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 65536;
        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            new List<CanonicalItemPayload> { item },
            options);
        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        return written.Data;
    }

    static CanonicalItemPayload CreateCodedItem(uint itemId, byte[] encoded)
    {
        AvifReader sourceReader = AvifReader.Create(encoded);
        Assert.That(sourceReader.Parse(), Is.EqualTo(AvifResultCode.Success));
        AvifImageItem? sourceItem = (sourceReader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetItem(0);
        byte[] payload = sourceReader.GetEncodedItem((sourceItem ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            itemId,
            "av01",
            string.Empty,
            false,
            payload);
        int index = 0;
        while (index < sourceItem.GetPropertyCount())
        {
            AvifItemProperty? property = sourceItem.GetProperty(index);
            item.Properties.Add(new CanonicalItemPropertyAssociation(
                (property ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData(),
                property.IsEssential()));
            index++;
        }
        sourceReader.Close();
        return item;
    }

    static byte[] BuildSpatialExtentsProperty(uint width, uint height)
    {
        byte[] raw = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(raw, 20);
        raw[4] = (byte)'i';
        raw[5] = (byte)'s';
        raw[6] = (byte)'p';
        raw[7] = (byte)'e';
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(12, 4), width);
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(16, 4), height);
        return raw;
    }

    class TrackingReadStream : Stream
    {
        MemoryStream source;
        internal bool Closed;
        internal int ReadCount;

        internal TrackingReadStream(byte[] data)
        {
            source = new MemoryStream(data, false);
        }

        public override bool CanRead => !Closed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => source.Length;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Closed = true;
            source.Dispose();
            base.Dispose(disposing);
        }
    }
}
