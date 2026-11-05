using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class MessageRepeatingBlockReaderTests
{
    [Test]
    public void PullsSelectedTransactionsWithStableParentMessageContext()
    {
        string messageNamespace = Namespace("pacs.008.001.10");
        string xml = "<BizMsgEnvlp xmlns='" + Namespace("nvlp.001.001.02") + "'>" +
            "<AppHdr xmlns='" + Namespace("head.001.001.04") + "'>" +
            "<BizMsgIdr>BUSINESS-1</BizMsgIdr>" +
            "<MsgDefIdr>pacs.008.001.10</MsgDefIdr></AppHdr>" +
            "<Document xmlns='" + messageNamespace + "'><FIToFICstmrCdtTrf>" +
            "<GrpHdr><MsgId>GROUP-1</MsgId></GrpHdr>" +
            Transaction("TX-1") + Transaction("TX-2") + Transaction("TX-3") +
            "</FIToFICstmrCdtTrf></Document></BizMsgEnvlp>";
        StreamReadOptions options = new StreamReadOptions();
        options.MessageOptions.SelectedPaths.Add(new XmlPathData
        {
            Value = "/BizMsgEnvlp/Document/FIToFICstmrCdtTrf/CdtTrfTxInf"
        });
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        MessageStreamItem? first = reader.ReadNextMessage();
        MessageStreamItem? second = reader.ReadNextMessage();
        MessageStreamItem? third = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            AssertRepeatingItem(first, 0, "TX-1");
            AssertRepeatingItem(second, 1, "TX-2");
            AssertRepeatingItem(third, 2, "TX-3");
            Assert.That(first?.Context, Is.SameAs(second?.Context));
            Assert.That(second?.Context, Is.SameAs(third?.Context));
            Assert.That(first?.Context?.Layout,
                Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(first?.Context?.Header?.Identifier?.Value,
                Is.EqualTo("head.001.001.04"));
            Assert.That(first?.Context?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(first?.Context?.Metadata.NamespaceUri,
                Is.EqualTo(messageNamespace));
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void QualifiedPathSegmentsFilterSameLocalNamesByNamespace()
    {
        string messageNamespace = Namespace("camt.053.001.13");
        string extensionNamespace = "urn:example:extension";
        string xml = "<Document xmlns='" + messageNamespace +
            "' xmlns:x='" + extensionNamespace + "'><Body>" +
            "<Entry><Id>MESSAGE</Id></Entry>" +
            "<x:Entry><x:Id>EXTENSION</x:Id></x:Entry>" +
            "</Body></Document>";
        XmlPathData selected = new XmlPathData();
        selected.Segments.Add(Name(messageNamespace, "Document"));
        selected.Segments.Add(Name(messageNamespace, "Body"));
        selected.Segments.Add(Name(extensionNamespace, "Entry"));
        StreamReadOptions options = new StreamReadOptions();
        options.MessageOptions.SelectedPaths.Add(selected);
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        MessageStreamItem? item = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(item?.Result.Success, Is.True, item?.Result.ErrorMessage);
            Assert.That(item?.Path.Segments[2].NamespaceUri,
                Is.EqualTo(extensionNamespace));
            Assert.That(item?.Result.Document?.Payloads[0].GenericContent?.Name.NamespaceUri,
                Is.EqualTo(extensionNamespace));
            Assert.That(DescendantText(
                item?.Result.Document?.Payloads[0].GenericContent,
                "Id"), Is.EqualTo("EXTENSION"));
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void SelectedSubtreeMaterializationEnforcesConfiguredBounds()
    {
        string xml = "<Document xmlns='" + Namespace("tsmt.009.001.04") + "'>" +
            "<Body><Trade><A/><B/><C/><D/></Trade></Body></Document>";
        StreamReadOptions options = new StreamReadOptions();
        options.MessageOptions.SelectedPaths.Add(new XmlPathData
        {
            Value = "/Document/Body/Trade"
        });
        options.MessageOptions.Limits.MaxElementCount = 5;
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        XmlException? exception = Assert.Throws<XmlException>(() =>
            reader.ReadNextMessage());

        Assert.That(exception?.Message, Does.Contain("MaxElementCount"));
    }

    [Test]
    public void EverySelectedBlockIsReturnedExactlyOnceAcrossShortReads()
    {
        string messageNamespace = Namespace("camt.053.001.13");
        StringBuilder xml = new StringBuilder();
        xml.Append("<Document xmlns='");
        xml.Append(messageNamespace);
        xml.Append("'><Body>");
        int expectedIndex = 0;
        while (expectedIndex < 32)
        {
            xml.Append("<Entry><Id>ENTRY-");
            xml.Append(expectedIndex);
            xml.Append("</Id></Entry>");
            expectedIndex++;
        }

        xml.Append("</Body></Document>");
        StreamReadOptions options = SelectedEntryOptions();
        using TrackingChunkedReadStream input = new TrackingChunkedReadStream(
            Encoding.UTF8.GetBytes(xml.ToString()),
            7);
        using MessageStreamReader reader = new MessageStreamReader(input, options);
        List<string> identifiers = new List<string>();
        List<long> indexes = new List<long>();

        MessageStreamItem? item = reader.ReadNextMessage();
        while (item != null)
        {
            indexes.Add(item.Index);
            identifiers.Add(DescendantText(
                item.Result.Document?.Payloads[0].GenericContent,
                "Id"));
            item = reader.ReadNextMessage();
        }

        Assert.Multiple(() =>
        {
            Assert.That(indexes, Is.EqualTo(Enumerable.Range(0, 32).Select(value =>
                (long)value)));
            Assert.That(identifiers, Is.EqualTo(Enumerable.Range(0, 32).Select(value =>
                "ENTRY-" + value)));
            Assert.That(identifiers.Distinct().Count(), Is.EqualTo(32));
            Assert.That(reader.ReadNextMessage(), Is.Null);
            Assert.That(input.ReadCount, Is.GreaterThan(1));
        });
    }

    [Test]
    public void EarlyDisposalDoesNotCloseOrDrainTheCallerStream()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append("<Document xmlns='");
        xml.Append(Namespace("camt.053.001.13"));
        xml.Append("'><Body>");
        int index = 0;
        while (index < 100)
        {
            xml.Append("<Entry><Id>ENTRY-");
            xml.Append(index);
            xml.Append("</Id><Value>");
            xml.Append(new string('x', 40));
            xml.Append("</Value></Entry>");
            index++;
        }

        xml.Append("</Body></Document>");
        byte[] bytes = Encoding.UTF8.GetBytes(xml.ToString());
        using TrackingChunkedReadStream input = new TrackingChunkedReadStream(bytes, 11);
        MessageStreamReader reader = new MessageStreamReader(input, SelectedEntryOptions());

        MessageStreamItem? first = reader.ReadNextMessage();
        int readsBeforeDisposal = input.ReadCount;
        long bytesBeforeDisposal = input.BytesRead;
        reader.Dispose();
        MessageStreamItem? afterDisposal = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(first?.Result.Success, Is.True, first?.Result.ErrorMessage);
            Assert.That(DescendantText(
                first?.Result.Document?.Payloads[0].GenericContent,
                "Id"), Is.EqualTo("ENTRY-0"));
            Assert.That(bytesBeforeDisposal, Is.LessThan(bytes.Length));
            Assert.That(input.ReadCount, Is.EqualTo(readsBeforeDisposal));
            Assert.That(input.BytesRead, Is.EqualTo(bytesBeforeDisposal));
            Assert.That(input.CanRead, Is.True);
            Assert.That(afterDisposal, Is.Null);
        });
    }

    [Test]
    public void MaterializationFailureIsLocalizedToOneSelectedSubtree()
    {
        string xml = "<Document xmlns='" + Namespace("camt.053.001.13") + "'>" +
            "<Body>" +
            "<Entry><A/><B/><C/><D/></Entry>" +
            "<Entry><Id>RECOVERED</Id></Entry>" +
            "</Body></Document>";
        StreamReadOptions options = SelectedEntryOptions();
        options.MessageOptions.Limits.MaxElementCount = 4;
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        XmlException? failure = Assert.Throws<XmlException>(() =>
            reader.ReadNextMessage());
        MessageStreamItem? recovered = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(failure?.Message, Does.Contain("MaxElementCount"));
            Assert.That(recovered?.Index, Is.Zero);
            Assert.That(recovered?.Result.Success,
                Is.True,
                recovered?.Result.ErrorMessage);
            Assert.That(DescendantText(
                recovered?.Result.Document?.Payloads[0].GenericContent,
                "Id"), Is.EqualTo("RECOVERED"));
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    private static void AssertRepeatingItem(
        MessageStreamItem? item,
        int expectedIndex,
        string expectedTransaction)
    {
        Assert.Multiple(() =>
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item?.Index, Is.EqualTo(expectedIndex));
            Assert.That(item?.Result.Success, Is.True, item?.Result.ErrorMessage);
            Assert.That(item?.Path.Value,
                Does.EndWith("CdtTrfTxInf"));
            Assert.That(item?.Path.Segments, Has.Count.EqualTo(4));
            Assert.That(item?.Result.Document?.Payloads, Has.Count.EqualTo(1));
            Assert.That(item?.Result.Document?.Payloads[0].GenericContent?.Name.LocalName,
                Is.EqualTo("CdtTrfTxInf"));
            Assert.That(DescendantText(
                item?.Result.Document?.Payloads[0].GenericContent,
                "TxId"), Is.EqualTo(expectedTransaction));
        });
    }

    private static QualifiedNameData Name(string namespaceUri, string localName)
    {
        return new QualifiedNameData
        {
            NamespaceUri = namespaceUri,
            LocalName = localName
        };
    }

    private static StreamReadOptions SelectedEntryOptions()
    {
        StreamReadOptions options = new StreamReadOptions();
        options.MessageOptions.SelectedPaths.Add(new XmlPathData
        {
            Value = "/Document/Body/Entry"
        });
        return options;
    }

    private static string DescendantText(DynamicXmlNode? node, string localName)
    {
        if (node == null)
        {
            return string.Empty;
        }

        if (node.Kind == XmlNodeKind.Element && node.Name.LocalName == localName)
        {
            return node.Children.Count == 0 ? string.Empty : node.Children[0].Value;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            string value = DescendantText(node.Children[index], localName);
            if (value.Length > 0)
            {
                return value;
            }

            index++;
        }

        return string.Empty;
    }

    private static string Transaction(string identifier)
    {
        return "<CdtTrfTxInf><PmtId><TxId>" + identifier +
            "</TxId></PmtId></CdtTrfTxInf>";
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    private sealed class TrackingChunkedReadStream : Stream
    {
        readonly MemoryStream inner;
        readonly int maximumChunkSize;

        internal TrackingChunkedReadStream(byte[] bytes, int maximumChunkSize)
        {
            inner = new MemoryStream(bytes, false);
            this.maximumChunkSize = maximumChunkSize;
        }

        internal int ReadCount;

        internal long BytesRead;

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
            int read = inner.Read(buffer, offset, Math.Min(count, maximumChunkSize));
            ReadCount++;
            BytesRead = BytesRead + read;
            return read;
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
}
