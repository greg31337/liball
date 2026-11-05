using System.IO;

namespace avif.Tests;

public class CallerOutputTests
{
    [Test]
    public void EncodeResultsCopyAvifAndAv1DataAndLeaveStreamsOpen()
    {
        AvifImage image = CreateImage(3, 2, 47);
        AvifEncodeResult avif = Avif.Encode(image);
        Av1Encoder encoder = new Av1Encoder();
        Assert.That(encoder.AddFrame(image, 1), Is.EqualTo(AvifResultCode.Success));
        AvifEncodeResult av1 = encoder.Finish();

        AssertResultOutput(avif);
        AssertResultOutput(av1);

        encoder.Close();
        image.Close();
    }

    [Test]
    public void ReaderCopiesAndWritesBoxesItemsAndIndividualExtents()
    {
        AvifImage image = CreateImage(4, 3, 83);
        byte[] source = Avif.Encode(image).GetData();
        AvifReader reader = new AvifReader(source, new AvifDecoderOptions());
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        AvifDocument document = reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.");
        uint itemId = (document.GetFileInfo()
            ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        AvifImageItem item = document.FindItem(itemId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifItemExtent extent = item.GetExtent(0)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifBox box = document.GetBox(0)
            ?? throw new InvalidOperationException("Invalid internal state.");
        byte[] expectedItem = reader.GetEncodedItem(itemId);
        byte[] expectedExtent = source.AsSpan(
            (int)extent.GetResolvedOffset(),
            (int)extent.GetResolvedLength()).ToArray();
        byte[] expectedBox = source.AsSpan(
            (int)box.GetOffset(),
            (int)box.GetSize()).ToArray();
        byte[] itemBuffer = new byte[expectedItem.Length + 2];
        byte[] extentBuffer = new byte[expectedExtent.Length];
        byte[] boxBuffer = new byte[expectedBox.Length];
        MemoryStream itemStream = new MemoryStream();
        MemoryStream extentStream = new MemoryStream();
        MemoryStream boxStream = new MemoryStream();
        byte[] tooSmall = [201];

        Assert.Multiple(() =>
        {
            Assert.That(reader.CopyEncodedItemTo(itemId, itemBuffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemBuffer.AsSpan(0, expectedItem.Length).ToArray(),
                Is.EqualTo(expectedItem));
            Assert.That(reader.CopyEncodedItemTo(itemId, tooSmall),
                Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 201 }));
            Assert.That(reader.WriteEncodedItemTo(itemId, itemStream),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.CopyItemExtentTo(itemId, 0, extentBuffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.WriteItemExtentTo(itemId, 0, extentStream),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.CopyBoxDataTo(0, boxBuffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.WriteBoxDataTo(0, boxStream),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemStream.ToArray(), Is.EqualTo(expectedItem));
            Assert.That(extentBuffer, Is.EqualTo(expectedExtent));
            Assert.That(extentStream.ToArray(), Is.EqualTo(expectedExtent));
            Assert.That(boxBuffer, Is.EqualTo(expectedBox));
            Assert.That(boxStream.ToArray(), Is.EqualTo(expectedBox));
            Assert.That(itemStream.CanWrite, Is.True);
            Assert.That(extentStream.CanWrite, Is.True);
            Assert.That(boxStream.CanWrite, Is.True);
            Assert.That(reader.CopyBoxDataTo(-1, boxBuffer),
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(reader.CopyItemExtentTo(itemId, -1, extentBuffer),
                Is.EqualTo(AvifResultCode.NotFound));
        });

        reader.Close();
        image.Close();
        itemStream.Dispose();
        extentStream.Dispose();
        boxStream.Dispose();
    }

    [Test]
    public void ReaderCopiesAndWritesEncodedTrackSamples()
    {
        AvifImage image = CreateImage(2, 2, 113);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(image, 100));
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        byte[] source = writer.Finish().GetData();
        AvifReader reader = new AvifReader(source, new AvifDecoderOptions());
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        AvifTrack track = ((reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie()
            ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(trackId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifSample sample = track.GetSample(0)
            ?? throw new InvalidOperationException("Invalid internal state.");
        byte[] expected = source.AsSpan(
            (int)sample.GetOffset(),
            (int)sample.GetSize()).ToArray();
        byte[] buffer = new byte[expected.Length];
        MemoryStream stream = new MemoryStream();

        Assert.Multiple(() =>
        {
            Assert.That(reader.CopySampleTo(trackId, 0, buffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.WriteSampleTo(trackId, 0, stream),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(buffer, Is.EqualTo(expected));
            Assert.That(stream.ToArray(), Is.EqualTo(expected));
            Assert.That(stream.CanWrite, Is.True);
            Assert.That(reader.CopySampleTo(trackId, 1, buffer),
                Is.EqualTo(AvifResultCode.NotFound));
        });

        stream.Dispose();
        reader.Close();
        writer.Close();
        image.Close();
    }

    [Test]
    public void MetadataAndRewriterSupportCallerOwnedOutputDestinations()
    {
        byte[] payload = [4, 8, 15, 16, 23, 42];
        AvifMetadata metadata = new AvifMetadata(
            "mime",
            "application/example",
            payload);
        byte[] metadataBuffer = new byte[payload.Length];
        MemoryStream metadataStream = new MemoryStream();
        Assert.That(metadata.CopyDataTo(metadataBuffer),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(metadata.WriteDataTo(metadataStream),
            Is.EqualTo(AvifResultCode.Success));

        AvifImage image = CreateImage(2, 2, 31);
        byte[] source = Avif.Encode(image).GetData();
        AvifRewriter rewriter = AvifRewriter.Create();
        Assert.That(rewriter.Open(source), Is.EqualTo(AvifResultCode.Success));
        Assert.That(rewriter.CopyResultTo(new byte[source.Length]),
            Is.EqualTo(AvifResultCode.InvalidState));
        AvifEncodeResult result = rewriter.Finish();
        byte[] rewriteBuffer = new byte[result.GetDataLength()];
        byte[] tooSmall = [177];

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetDataLength(), Is.EqualTo(payload.Length));
            Assert.That(metadataBuffer, Is.EqualTo(payload));
            Assert.That(metadataStream.ToArray(), Is.EqualTo(payload));
            Assert.That(metadataStream.CanWrite, Is.True);
            Assert.That(rewriter.CopyResultTo(rewriteBuffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(rewriter.GetResultLength(), Is.EqualTo(rewriteBuffer.Length));
            Assert.That(rewriteBuffer, Is.EqualTo(result.GetData()));
            Assert.That(rewriter.CopyResultTo(tooSmall),
                Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 177 }));
        });

        rewriter.Close();
        image.Close();
        metadataStream.Dispose();
    }

    [Test]
    public void FailedEncodeResultDoesNotExposeOrWriteSuppliedBytes()
    {
        byte[] supplied = new byte[] { 1, 2, 3 };
        AvifEncodeResult result = new AvifEncodeResult(
            AvifResultCode.EncodeFailed,
            supplied);
        byte[] destination = new byte[] { 7, 8, 9 };
        MemoryStream stream = new MemoryStream();

        supplied[0] = 99;
        Assert.Multiple(() =>
        {
            Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.EncodeFailed));
            Assert.That(result.GetData(), Is.Empty);
            Assert.That(result.GetDataLength(), Is.Zero);
            Assert.That(
                result.CopyDataTo(destination),
                Is.EqualTo(AvifResultCode.EncodeFailed));
            Assert.That(destination, Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(
                result.WriteDataTo(stream),
                Is.EqualTo(AvifResultCode.EncodeFailed));
            Assert.That(stream.ToArray(), Is.Empty);
            Assert.That(stream.CanWrite, Is.True);
        });

        stream.Dispose();
    }

    static void AssertResultOutput(AvifEncodeResult result)
    {
        byte[] expected = result.GetData();
        byte[] buffer = new byte[expected.Length + 3];
        byte[] tooSmall = [199];
        MemoryStream stream = new MemoryStream();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.CopyDataTo(buffer),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(buffer.AsSpan(0, expected.Length).ToArray(),
                Is.EqualTo(expected));
            Assert.That(result.CopyDataTo(tooSmall),
                Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 199 }));
            Assert.That(result.WriteDataTo(stream),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(stream.ToArray(), Is.EqualTo(expected));
            Assert.That(stream.CanWrite, Is.True);
        });

        stream.Dispose();
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }
}
