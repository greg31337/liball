using System.Text;
using System.Xml;
using System.Xml.Linq;
using iso20022;

namespace iso20022.Tests;

public class MessageStreamBatchApiTests
{
    [Test]
    public void EmptyAndSingleItemBatchesHaveStablePullBoundaries()
    {
        using MemoryStream emptyBatchInput = Input("<Batch xmlns='urn:example:batch'/>");
        using MessageStreamReader emptyBatchReader = new MessageStreamReader(
            emptyBatchInput,
            WrapperOptions());

        string singleXml = "<Batch xmlns='urn:example:batch'>" +
            Document("pacs.008.001.10", "SINGLE-1") + "</Batch>";
        using MemoryStream singleInput = Input(singleXml);
        using MessageStreamReader singleReader = new MessageStreamReader(
            singleInput,
            WrapperOptions());
        MessageStreamItem? single = singleReader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(emptyBatchReader.ReadNextMessage(), Is.Null);
            Assert.That(emptyBatchReader.ReadNextMessage(), Is.Null);
            Assert.That(single?.Index, Is.Zero);
            Assert.That(single?.Result.Success, Is.True, single?.Result.ErrorMessage);
            Assert.That(single?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(singleReader.ReadNextMessage(), Is.Null);
            Assert.That(singleReader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void MixedVersionsAndBusinessNamespacesRemainDistinctAndOrdered()
    {
        string xml = "<Batch xmlns='urn:example:batch'>" +
            Document("pacs.008.001.09", "VERSION-9") +
            Document("pacs.008.001.10", "VERSION-10") +
            Document("camt.053.001.13", "STATEMENT-13") +
            "</Batch>";
        using MemoryStream input = Input(xml);
        using MessageStreamReader reader = new MessageStreamReader(input, WrapperOptions());

        List<string> identities = PullIdentities(reader);

        Assert.That(identities, Is.EqualTo(new[]
        {
            "pacs.008.001.09|" + Namespace("pacs.008.001.09"),
            "pacs.008.001.10|" + Namespace("pacs.008.001.10"),
            "camt.053.001.13|" + Namespace("camt.053.001.13")
        }));
    }

    [Test]
    public void InvalidRootAndTruncatedBatchFailuresOccurAtDeterministicPulls()
    {
        using MemoryStream invalidInput = Input("<Other xmlns='urn:example:batch'/>");
        using MessageStreamReader invalidReader = new MessageStreamReader(
            invalidInput,
            WrapperOptions());
        MessageStreamItem? invalid = invalidReader.ReadNextMessage();

        string truncatedXml = "<Batch xmlns='urn:example:batch'>" +
            Document("pacs.008.001.10", "COMPLETE-BEFORE-TRUNCATION");
        using MemoryStream truncatedInput = Input(truncatedXml);
        using MessageStreamReader truncatedReader = new MessageStreamReader(
            truncatedInput,
            WrapperOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalid?.Result.Success, Is.False);
            Assert.That(invalid?.Result.ErrorCode, Is.EqualTo("stream.wrapper_unsupported"));
            Assert.That(invalid?.Result.EndOfInput, Is.True);
            Assert.That(invalidReader.ReadNextMessage(), Is.Null);
            Assert.Throws<XmlException>(() => truncatedReader.ReadNextMessage());
        });
    }

    [Test]
    public void ReaderItemLimitReturnsOneTerminalFailureAfterAllowedItems()
    {
        string xml = "<Batch xmlns='urn:example:batch'>" +
            Document("pacs.008.001.10", "ALLOWED") +
            Document("camt.053.001.13", "OVER-LIMIT") +
            "</Batch>";
        StreamReadOptions options = WrapperOptions();
        options.MaxItems = 1;
        using MemoryStream input = Input(xml);
        using MessageStreamReader reader = new MessageStreamReader(input, options);

        MessageStreamItem? allowed = reader.ReadNextMessage();
        MessageStreamItem? limit = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(allowed?.Index, Is.Zero);
            Assert.That(allowed?.Result.Success, Is.True, allowed?.Result.ErrorMessage);
            Assert.That(limit?.Index, Is.EqualTo(1));
            Assert.That(limit?.Result.Success, Is.False);
            Assert.That(limit?.Result.ErrorCode, Is.EqualTo("stream.item_limit"));
            Assert.That(limit?.Result.EndOfInput, Is.True);
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void WriterFinalizesEmptyAndSingleItemBatchesExactlyOnce()
    {
        MemoryStream emptyOutput = new MemoryStream();
        using MessageStreamWriter emptyWriter = new MessageStreamWriter(
            emptyOutput,
            null,
            WriteOptions());
        MessageWriteResult emptyCompletion = emptyWriter.CompleteWriting();
        MessageWriteResult repeatedCompletion = emptyWriter.CompleteWriting();
        MessageWriteResult afterCompletion = emptyWriter.WriteNextMessage(
            Message("pacs.008.001.10", "TOO-LATE"));

        MemoryStream singleOutput = new MemoryStream();
        MessageStreamWriter singleWriter = new MessageStreamWriter(
            singleOutput,
            null,
            WriteOptions());
        MessageWriteResult singleWrite = singleWriter.WriteNextMessage(
            Message("pacs.008.001.10", "DISPOSE-FINALIZED"));
        singleWriter.Dispose();
        MessageWriteResult disposalCompletion = singleWriter.CompleteWriting();

        XDocument emptyDocument = XDocument.Parse(Encoding.UTF8.GetString(
            emptyOutput.ToArray()));
        XDocument singleDocument = XDocument.Parse(Encoding.UTF8.GetString(
            singleOutput.ToArray()));

        Assert.Multiple(() =>
        {
            Assert.That(emptyCompletion.Success, Is.True, emptyCompletion.ErrorMessage);
            Assert.That(repeatedCompletion, Is.SameAs(emptyCompletion));
            Assert.That(afterCompletion.Success, Is.False);
            Assert.That(afterCompletion.ErrorCode, Is.EqualTo("stream_writer.completed"));
            Assert.That(emptyDocument.Root?.Elements(), Is.Empty);
            Assert.That(singleWrite.Success, Is.True, singleWrite.ErrorMessage);
            Assert.That(disposalCompletion.Success,
                Is.True,
                disposalCompletion.ErrorMessage);
            Assert.That(singleDocument.Root?.Elements().Count(), Is.EqualTo(1));
            Assert.That(singleOutput.CanWrite, Is.True);
        });
    }

    [Test]
    public void WriterRoundTripsMixedVersionsAndBusinessNamespaces()
    {
        MemoryStream output = new MemoryStream();
        StreamWriteOptions writeOptions = WriteOptions();
        using MessageStreamWriter writer = new MessageStreamWriter(
            output,
            null,
            writeOptions);

        MessageWriteResult first = writer.WriteNextMessage(
            Message("pacs.008.001.09", "VERSION-9"));
        MessageWriteResult second = writer.WriteNextMessage(
            Message("pacs.008.001.10", "VERSION-10"));
        MessageWriteResult third = writer.WriteNextMessage(
            Message("camt.053.001.13", "STATEMENT-13"));
        MessageWriteResult completed = writer.CompleteWriting();

        using MemoryStream input = new MemoryStream(output.ToArray(), false);
        using MessageStreamReader reader = new MessageStreamReader(input, WrapperOptions());
        List<string> identities = PullIdentities(reader);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(third.Success, Is.True, third.ErrorMessage);
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(identities, Is.EqualTo(new[]
            {
                "pacs.008.001.09|" + Namespace("pacs.008.001.09"),
                "pacs.008.001.10|" + Namespace("pacs.008.001.10"),
                "camt.053.001.13|" + Namespace("camt.053.001.13")
            }));
        });
    }

    [Test]
    public void WriterRejectsInvalidItemsAndStopsAtTheCollectionLimit()
    {
        MemoryStream output = new MemoryStream();
        StreamWriteOptions options = WriteOptions();
        options.MessageOptions.Limits.MaxCollectionCount = 1;
        using MessageStreamWriter writer = new MessageStreamWriter(output, null, options);

        MessageWriteResult invalid = writer.WriteNextMessage(null);
        MessageWriteResult allowed = writer.WriteNextMessage(
            Message("pacs.008.001.10", "ALLOWED"));
        MessageWriteResult overLimit = writer.WriteNextMessage(
            Message("camt.053.001.13", "OVER-LIMIT"));
        MessageWriteResult completed = writer.CompleteWriting();
        XDocument document = XDocument.Parse(Encoding.UTF8.GetString(output.ToArray()));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode, Is.EqualTo("stream_writer.document_null"));
            Assert.That(allowed.Success, Is.True, allowed.ErrorMessage);
            Assert.That(overLimit.Success, Is.False);
            Assert.That(overLimit.ErrorCode, Is.EqualTo("stream_writer.item_limit"));
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(document.Root?.Elements().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void NonSeekableWriterSupportsPrecomputedTotalsAndRejectsDeferredTotals()
    {
        using NonSeekableBufferStream output = new NonSeekableBufferStream();
        StreamWriteOptions options = WriteOptions();
        options.HasPrecomputedMessageCount = true;
        options.PrecomputedMessageCount = 1;
        using MessageStreamWriter writer = new MessageStreamWriter(output, null, options);
        MessageWriteResult written = writer.WriteNextMessage(
            Message("pacs.008.001.10", "NON-SEEKABLE"));
        MessageWriteResult completed = writer.CompleteWriting();
        XDocument document = XDocument.Parse(Encoding.UTF8.GetString(output.ToArray()));

        using NonSeekableBufferStream deferredOutput = new NonSeekableBufferStream();
        StreamWriteOptions deferredOptions = WriteOptions();
        deferredOptions.UseDeferredTotals = true;
        using MessageStreamWriter deferredWriter = new MessageStreamWriter(
            deferredOutput,
            null,
            deferredOptions);
        MessageWriteResult deferred = deferredWriter.CompleteWriting();
        MessageWriteResult repeatedDeferred = deferredWriter.CompleteWriting();

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(written.BytesWritten, Is.Zero);
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(completed.BytesWritten, Is.Zero);
            Assert.That(document.Root?.Elements().Count(), Is.EqualTo(2));
            Assert.That(output.CanWrite, Is.True);
            Assert.That(deferred.Success, Is.False);
            Assert.That(deferred.ErrorCode,
                Is.EqualTo("stream_writer.deferred_seek_required"));
            Assert.That(repeatedDeferred, Is.SameAs(deferred));
            Assert.That(deferredOutput.ToArray(), Is.Empty);
        });
    }

    private static List<string> PullIdentities(MessageStreamReader reader)
    {
        List<string> identities = new List<string>();
        MessageStreamItem? item = reader.ReadNextMessage();
        while (item != null)
        {
            MessageMetadata? metadata = item.Result.Document?.Metadata;
            identities.Add((metadata?.MessageIdentifier?.Value ?? string.Empty) + "|" +
                (metadata?.NamespaceUri ?? string.Empty));
            item = reader.ReadNextMessage();
        }

        return identities;
    }

    private static MemoryStream Input(string xml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(xml), false);
    }

    private static StreamReadOptions WrapperOptions()
    {
        StreamReadOptions options = new StreamReadOptions();
        options.WrapperNames.Add(new QualifiedNameData
        {
            Prefix = "c",
            LocalName = "Batch",
            NamespaceUri = "urn:example:batch"
        });
        return options;
    }

    private static StreamWriteOptions WriteOptions()
    {
        StreamWriteOptions options = new StreamWriteOptions();
        options.EnvelopeKind = EnvelopeKind.CustomWrapper;
        options.CustomWrapperName = new QualifiedNameData
        {
            Prefix = "c",
            LocalName = "Batch",
            NamespaceUri = "urn:example:batch"
        };
        options.MessageOptions.FormattingMode = XmlFormattingMode.Compact;
        options.MessageOptions.WriteXmlDeclaration = false;
        options.MessageOptions.WriteByteOrderMark = false;
        return options;
    }

    private static MessageDocument Message(string identifier, string reference)
    {
        MessageReadResult result = new XmlMessageReader().ReadGenericText(
            Document(identifier, reference),
            new MessageReadOptions());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.Document ?? throw new AssertionException("Expected a message document.");
    }

    private static string Document(string identifier, string reference)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'>" +
            "<MsgId>" + reference + "</MsgId></Document>";
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class NonSeekableBufferStream : Stream
    {
        readonly MemoryStream inner = new MemoryStream();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            inner.Write(buffer, offset, count);
        }

        internal byte[] ToArray()
        {
            return inner.ToArray();
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
}
