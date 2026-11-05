using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlOutputWriterTests
{
    [Test]
    public void ByteCollectorCopiesBlocksAndCanBeCleared()
    {
        XmlByteCollector collector = new XmlByteCollector();
        byte[] source = [1, 2, 3];

        collector.WriteXmlBytes(source);
        source[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(collector.CountCollectedXmlBytes(), Is.EqualTo(3));
            Assert.That(collector.SnapshotCollectedXmlBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        collector.ClearCollectedXmlBytes();
        Assert.That(collector.CountCollectedXmlBytes(), Is.Zero);
        Assert.That(collector.SnapshotCollectedXmlBytes(), Is.Empty);
    }

    [Test]
    public void WritesEncodedStreamBlocksWithOneBomAndSplitSurrogateSupport()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream();
        XmlOutput output = new XmlOutput
        {
            Kind = XmlOutputKind.Stream,
            ByteStream = stream,
            EncodingName = "utf-16le",
            OwnsResource = true
        };
        XmlOutputWriter writer = new XmlOutputWriter(output, "utf-8", true, 3);

        writer.WriteXmlOutputText("A\uD83D");
        writer.FlushXmlOutput();
        writer.WriteXmlOutputText("\uDE00Z");
        writer.CompleteXmlOutput();

        byte[] expected = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("A\U0001F600Z")).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(stream.ToArray(), Is.EqualTo(expected));
            Assert.That(stream.WriteSizes, Has.All.LessThanOrEqualTo(3));
            Assert.That(stream.FlushCount, Is.EqualTo(2));
            Assert.That(stream.WasDisposed, Is.True);
            Assert.That(writer.InspectXmlOutputEncodingName(), Is.EqualTo("utf-16"));
            Assert.That(writer.InspectXmlOutputCompletion(), Is.True);
            Assert.That(writer.InspectXmlOutputFailure(), Is.False);
        });
    }

    [Test]
    public void WritesBoundedUtf8BlocksToByteSinks()
    {
        RecordingByteSink sink = new RecordingByteSink();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = sink };
        XmlOutputWriter writer = new XmlOutputWriter(output, null, true, 2);

        writer.WriteXmlOutputText("é-data");
        writer.CompleteXmlOutput();

        byte[] expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("é-data")).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(sink.Bytes, Is.EqualTo(expected));
            Assert.That(sink.BlockSizes, Has.All.LessThanOrEqualTo(2));
            Assert.That(sink.BlockSizes, Has.Count.GreaterThan(1));
            Assert.That(writer.InspectXmlOutputEncodingName(), Is.EqualTo("utf-8"));
        });
    }

    [Test]
    public void UsesTextWriterEncodingWithoutWritingABom()
    {
        TrackingStringWriter text = new TrackingStringWriter();
        XmlOutput output = new XmlOutput
        {
            Kind = XmlOutputKind.TextWriter,
            TextWriter = text,
            EncodingName = "utf-8",
            OwnsResource = true
        };
        XmlOutputWriter writer = new XmlOutputWriter(output, "utf-32", true, 4);

        writer.WriteXmlOutputText("content");
        writer.CompleteXmlOutput();

        Assert.Multiple(() =>
        {
            Assert.That(text.ToString(), Is.EqualTo("content"));
            Assert.That(text.ToString()[0], Is.Not.EqualTo('\uFEFF'));
            Assert.That(writer.InspectXmlOutputEncodingName(), Is.EqualTo("utf-16"));
            Assert.That(text.FlushCount, Is.EqualTo(1));
            Assert.That(text.WasDisposed, Is.True);
        });
    }

    [Test]
    public void CompletesEmptyByteOutputWithOnlyTheRequestedBom()
    {
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        XmlOutputWriter writer = new XmlOutputWriter(output, "utf-8", true, 8);

        writer.CompleteXmlOutput();

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Test]
    public void LeavesUnownedDestinationsOpenAfterCompletion()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream, OwnsResource = false };
        XmlOutputWriter writer = new XmlOutputWriter(output, "utf-8", false, 8);

        writer.WriteXmlOutputText("done");
        writer.CompleteXmlOutput();
        stream.WriteByte(1);

        Assert.That(stream.WasDisposed, Is.False);
        Assert.Throws<ObjectDisposedException>(() => writer.WriteXmlOutputText("again"));
    }

    [Test]
    public void CleansUpOwnedDestinationsAfterWriteAndEncodingFailures()
    {
        ThrowingWriteStream failingStream = new ThrowingWriteStream();
        XmlOutput failingOutput = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = failingStream, OwnsResource = true };
        XmlOutputWriter failingWriter = new XmlOutputWriter(failingOutput, "utf-8", false, 8);

        XmlSerializationException writeFailure = Assert.Throws<XmlSerializationException>(() => failingWriter.WriteXmlOutputText("value"))!;

        TrackingMemoryStream encodingStream = new TrackingMemoryStream();
        XmlOutput encodingOutput = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = encodingStream, EncodingName = "us-ascii", OwnsResource = true };
        XmlOutputWriter encodingWriter = new XmlOutputWriter(encodingOutput, null, false, 8);
        XmlSerializationException encodingFailure = Assert.Throws<XmlSerializationException>(() => encodingWriter.WriteXmlOutputText("é"))!;

        Assert.Multiple(() =>
        {
            Assert.That(writeFailure.Diagnostic!.Category, Is.EqualTo(XmlErrorCategory.Serialization));
            Assert.That(failingWriter.InspectXmlOutputFailure(), Is.True);
            Assert.That(failingStream.WasDisposed, Is.True);
            Assert.That(encodingWriter.InspectXmlOutputFailure(), Is.True);
            Assert.That(encodingStream.WasDisposed, Is.True);
            Assert.That(encodingFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0008));
        });
    }

    [Test]
    public void RejectsIncompleteTextAndInvalidDestinations()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream, OwnsResource = true };
        XmlOutputWriter writer = new XmlOutputWriter(output, "utf-8", false, 8);
        writer.WriteXmlOutputText("value\uD800");

        Assert.Throws<XmlSerializationException>(() => writer.CompleteXmlOutput());
        Assert.That(stream.WasDisposed, Is.True);
        Assert.Throws<ArgumentException>(() => new XmlOutputWriter(new XmlOutput { Kind = XmlOutputKind.Stream }, null, false, 8));
        Assert.Throws<ArgumentException>(() => new XmlOutputWriter(new XmlOutput { Kind = XmlOutputKind.TextWriter }, null, false, 8));
        Assert.Throws<ArgumentException>(() => new XmlOutputWriter(new XmlOutput { Kind = XmlOutputKind.ByteSink }, null, false, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlOutputWriter(new XmlOutput { Kind = (XmlOutputKind)99 }, null, false, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlOutputWriter(new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = new MemoryStream() }, null, false, 0));
        Assert.Throws<ArgumentException>(() => new XmlOutputWriter(new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = new MemoryStream(), EncodingName = "" }, null, false, 8));
    }

    private sealed class RecordingByteSink : IXmlByteSink
    {
        public readonly List<byte> Bytes = [];
        public readonly List<int> BlockSizes = [];

        public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
        {
            Bytes.AddRange(bytes.ToArray());
            BlockSizes.Add(bytes.Length);
        }
    }

    private class TrackingMemoryStream : MemoryStream
    {
        public readonly List<int> WriteSizes = [];
        public int FlushCount;
        public bool WasDisposed;

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteSizes.Add(count);
            base.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            FlushCount = FlushCount + 1;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingWriteStream : TrackingMemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("The destination failed.");
        }
    }

    private sealed class TrackingStringWriter : StringWriter
    {
        public int FlushCount;
        public bool WasDisposed;

        public override void Flush()
        {
            FlushCount = FlushCount + 1;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
