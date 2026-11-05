using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageStreamReaderDeterminismTests
{
    [Test]
    public void SeekableAndNonSeekablePartialInputsProduceTheSameOrderedOutcomes()
    {
        string xml = BatchWithInvalidAndValidItems();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using MemoryStream seekable = new MemoryStream(bytes, false);
        using ChunkedNonSeekableReadStream partial = new ChunkedNonSeekableReadStream(bytes, 3);

        List<string> seekableOutcomes = ReadAllOutcomes(seekable, ContinuingOptions());
        List<string> partialOutcomes = ReadAllOutcomes(partial, ContinuingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(partialOutcomes, Is.EqualTo(seekableOutcomes));
            Assert.That(partialOutcomes, Is.EqualTo(new[]
            {
                "0:False:detection.wrapper_empty:",
                "1:True::pacs.008.001.10",
                "2:True::camt.053.001.13"
            }));
            Assert.That(partial.ReadCount, Is.GreaterThan(3));
            Assert.That(seekable.CanRead, Is.True);
            Assert.That(partial.CanRead, Is.True);
        });
    }

    [Test]
    public void StopPolicyEndsAfterTheFirstInvalidItemOnAPartialInput()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BatchWithInvalidAndValidItems());
        using ChunkedNonSeekableReadStream input = new ChunkedNonSeekableReadStream(bytes, 5);
        using MessageStreamReader reader = new MessageStreamReader(input, WrapperOptions());

        MessageStreamItem? invalid = reader.ReadNextMessage();
        MessageStreamItem? end = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(invalid?.Index, Is.Zero);
            Assert.That(invalid?.Result.Success, Is.False);
            Assert.That(invalid?.Result.ErrorCode, Is.EqualTo("detection.wrapper_empty"));
            Assert.That(invalid?.Result.EndOfInput, Is.True);
            Assert.That(end, Is.Null);
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void CancellationBeforeReadingConsumesNoInputAndStopsTheReader()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BatchWithInvalidAndValidItems());
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        StreamReadOptions seekableOptions = WrapperOptions();
        seekableOptions.CancellationToken = cancellation.Token;
        StreamReadOptions partialOptions = WrapperOptions();
        partialOptions.CancellationToken = cancellation.Token;
        using MemoryStream seekableInput = new MemoryStream(bytes, false);
        using ChunkedNonSeekableReadStream partialInput =
            new ChunkedNonSeekableReadStream(bytes, 2);
        using MessageStreamReader seekableReader =
            new MessageStreamReader(seekableInput, seekableOptions);
        using MessageStreamReader partialReader =
            new MessageStreamReader(partialInput, partialOptions);

        Assert.Throws<OperationCanceledException>(() => seekableReader.ReadNextMessage());
        Assert.Throws<OperationCanceledException>(() => partialReader.ReadNextMessage());

        Assert.Multiple(() =>
        {
            Assert.That(seekableInput.Position, Is.Zero);
            Assert.That(partialInput.ReadCount, Is.Zero);
            Assert.That(seekableReader.ReadNextMessage(), Is.Null);
            Assert.That(partialReader.ReadNextMessage(), Is.Null);
            Assert.That(seekableInput.CanRead, Is.True);
            Assert.That(partialInput.CanRead, Is.True);
        });
    }

    [Test]
    public void CancellationRaisedByAPartialReadIsObservedBeforeAnItemIsReturned()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BatchWithInvalidAndValidItems());
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        StreamReadOptions options = ContinuingOptions();
        options.CancellationToken = cancellation.Token;
        using CancellingChunkedReadStream input =
            new CancellingChunkedReadStream(bytes, 7, cancellation);
        using MessageStreamReader reader = new MessageStreamReader(input, options);

        Assert.Throws<OperationCanceledException>(() => reader.ReadNextMessage());

        Assert.Multiple(() =>
        {
            Assert.That(input.ReadCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(reader.ReadNextMessage(), Is.Null);
            Assert.That(input.CanRead, Is.True);
        });
    }

    private static List<string> ReadAllOutcomes(Stream input, StreamReadOptions options)
    {
        List<string> outcomes = new List<string>();
        using MessageStreamReader reader = new MessageStreamReader(input, options);
        MessageStreamItem? item = reader.ReadNextMessage();
        while (item != null)
        {
            string identifier = string.Empty;
            if (item.Result.Document?.Metadata.MessageIdentifier != null)
            {
                identifier = item.Result.Document.Metadata.MessageIdentifier.Value;
            }

            outcomes.Add(item.Index + ":" + item.Result.Success + ":" +
                item.Result.ErrorCode + ":" + identifier);
            item = reader.ReadNextMessage();
        }

        return outcomes;
    }

    private static StreamReadOptions ContinuingOptions()
    {
        StreamReadOptions options = WrapperOptions();
        options.ContinueAfterInvalidItem = true;
        return options;
    }

    private static StreamReadOptions WrapperOptions()
    {
        StreamReadOptions options = new StreamReadOptions();
        options.WrapperNames.Add(new QualifiedNameData
        {
            NamespaceUri = "urn:example:batch",
            LocalName = "Batch"
        });
        return options;
    }

    private static string BatchWithInvalidAndValidItems()
    {
        return "<Batch xmlns='urn:example:batch'>" +
            "<Item><Unknown/></Item>" +
            "<Item>" + Document("pacs.008.001.10") + "</Item>" +
            "<Item>" + Document("camt.053.001.13") + "</Item>" +
            "</Batch>";
    }

    private static string Document(string identifier)
    {
        return "<Document xmlns='" + Iso20022Namespaces.SchemaUrnPrefix + identifier + "'>" +
            "<MsgId>MESSAGE-" + identifier + "</MsgId></Document>";
    }

    private class ChunkedNonSeekableReadStream : Stream
    {
        readonly MemoryStream inner;
        readonly int maximumChunkSize;

        internal ChunkedNonSeekableReadStream(byte[] bytes, int maximumChunkSize)
        {
            inner = new MemoryStream(bytes, false);
            this.maximumChunkSize = maximumChunkSize;
        }

        internal int ReadCount;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

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
            return inner.Read(buffer, offset, Math.Min(count, maximumChunkSize));
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
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class CancellingChunkedReadStream : ChunkedNonSeekableReadStream
    {
        readonly CancellationTokenSource cancellation;

        internal CancellingChunkedReadStream(
            byte[] bytes,
            int maximumChunkSize,
            CancellationTokenSource cancellation)
            : base(bytes, maximumChunkSize)
        {
            this.cancellation = cancellation;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = base.Read(buffer, offset, count);
            if (result > 0)
            {
                cancellation.Cancel();
            }

            return result;
        }
    }
}
