using System.Globalization;
using System.Text;
using System.Xml.Linq;
using iso20022;

namespace iso20022.Tests;

public class MessageStreamWriterTests
{
    [Test]
    public void WritesBusinessFileHeaderPrecomputedCountAndMessagesIncrementally()
    {
        MemoryStream output = new MemoryStream();
        StreamWriteOptions options = new StreamWriteOptions
        {
            EnvelopeKind = EnvelopeKind.BusinessFileEnvelope,
            HasPrecomputedMessageCount = true,
            PrecomputedMessageCount = 2,
            MessageOptions = CompactOptions()
        };
        IMessageStreamWriter writer = new MessageStreamWriter(
            output,
            BusinessFileHeader(),
            options);

        MessageWriteResult first = writer.WriteNextMessage(
            Message("pacs.008.001.10", "BUSINESS-1", "PAYMENT-1", 10.25m, 2));
        MessageWriteResult second = writer.WriteNextMessage(
            Message("semt.018.001.14", "BUSINESS-2", "SECURITIES-1", 5.50m, 2));
        MessageWriteResult completed = writer.CompleteWriting();
        ((IDisposable)writer).Dispose();
        string xml = Encoding.UTF8.GetString(output.ToArray());

        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(output.ToArray(), false),
            new StreamReadOptions());
        MessageStreamItem? firstRead = reader.ReadNextMessage();
        MessageStreamItem? secondRead = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(first.BytesWritten, Is.GreaterThan(0));
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(completed.BytesWritten, Is.EqualTo(output.Length));
            Assert.That(xml, Does.StartWith("<BizFileEnvlp"));
            Assert.That(xml, Does.Contain("<BizFileIdr>FILE-1</BizFileIdr>"));
            Assert.That(xml, Does.Contain("<TtlNbOfBizMsgs>2</TtlNbOfBizMsgs>"));
            Assert.That(Count(xml, "<Pyld>"), Is.EqualTo(2));
            Assert.That(Count(xml, "<BizMsgEnvlp"), Is.EqualTo(2));
            Assert.That(xml.IndexOf("PyldData", StringComparison.Ordinal),
                Is.LessThan(xml.IndexOf("ApplSpcfcs", StringComparison.Ordinal)));
            Assert.That(firstRead?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(secondRead?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("semt.018.001.14"));
            Assert.That(reader.ReadNextMessage(), Is.Null);
            Assert.That(output.CanWrite, Is.True);
        });
    }

    [Test]
    public void WritesCustomHeaderAndVerifiesPrecomputedCountAndControlTotal()
    {
        MemoryStream output = new MemoryStream();
        StreamWriteOptions options = CustomOptions();
        options.HasPrecomputedMessageCount = true;
        options.PrecomputedMessageCount = 2;
        options.HasPrecomputedControlTotal = true;
        options.PrecomputedControlTotal = new IsoDecimalValue
        {
            Value = 15.75m,
            Scale = 2
        };
        using MessageStreamWriter writer = new MessageStreamWriter(
            output,
            CustomHeader(),
            options);

        MessageWriteResult first = writer.WriteNextMessage(
            Message("pacs.008.001.10", "BUSINESS-3", "PAYMENT-2", 10.25m, 2));
        MessageWriteResult second = writer.WriteNextMessage(
            Message("fxtr.008.001.07", "BUSINESS-4", "FX-2", 5.50m, 2));
        MessageWriteResult completed = writer.CompleteWriting();
        string xml = Encoding.UTF8.GetString(output.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(xml, Does.StartWith("<c:Batch"));
            Assert.That(xml, Does.Contain("<c:BatchId>BATCH-1</c:BatchId>"));
            Assert.That(xml, Does.Contain("<c:MessageCount>2</c:MessageCount>"));
            Assert.That(xml, Does.Contain("<c:ControlTotal>15.75</c:ControlTotal>"));
            Assert.That(Count(xml, "<AppHdr"), Is.EqualTo(2));
            Assert.That(Count(xml, "<Document"), Is.EqualTo(2));
        });
    }

    [Test]
    public void PatchesDeferredCountAndControlTotalAfterStreamingItems()
    {
        MemoryStream output = new MemoryStream();
        StreamWriteOptions options = CustomOptions();
        options.UseDeferredTotals = true;
        using MessageStreamWriter writer = new MessageStreamWriter(
            output,
            CustomHeader(),
            options);

        MessageWriteResult first = writer.WriteNextMessage(
            Message("caaa.001.001.14", "BUSINESS-5", "CARD-1", 1.20m, 2));
        MessageWriteResult second = writer.WriteNextMessage(
            Message("tsmt.009.001.04", "BUSINESS-6", "TRADE-1", 2.345m, 3));
        MessageWriteResult completed = writer.CompleteWriting();
        string xml = Encoding.UTF8.GetString(output.ToArray());
        XDocument parsed = XDocument.Parse(xml);
        XElement totals = parsed.Descendants().Single(element =>
            element.Name.LocalName == "StreamTotals");
        string count = totals.Elements().Single(element =>
            element.Name.LocalName == "MessageCount").Value;
        string total = totals.Elements().Single(element =>
            element.Name.LocalName == "ControlTotal").Value;

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(second.Success, Is.True, second.ErrorMessage);
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(long.Parse(count, CultureInfo.InvariantCulture), Is.EqualTo(2));
            Assert.That(decimal.Parse(total, CultureInfo.InvariantCulture),
                Is.EqualTo(3.545m));
            Assert.That(count, Has.Length.EqualTo(18));
            Assert.That(total, Has.Length.EqualTo(72));
            Assert.That(xml, Does.EndWith("</c:Batch>"));
        });
    }

    [Test]
    public void ReportsPrecomputedTotalsThatDoNotMatchWrittenMessages()
    {
        StreamWriteOptions options = CustomOptions();
        options.HasPrecomputedMessageCount = true;
        options.PrecomputedMessageCount = 2;
        options.HasPrecomputedControlTotal = true;
        options.PrecomputedControlTotal = new IsoDecimalValue
        {
            Value = 99m,
            Scale = 0
        };
        MemoryStream output = new MemoryStream();
        using MessageStreamWriter writer = new MessageStreamWriter(
            output,
            CustomHeader(),
            options);

        MessageWriteResult item = writer.WriteNextMessage(
            Message("pacs.008.001.10", "BUSINESS-7", "PAYMENT-3", 10m, 0));
        MessageWriteResult completed = writer.CompleteWriting();

        Assert.Multiple(() =>
        {
            Assert.That(item.Success, Is.True, item.ErrorMessage);
            Assert.That(completed.Success, Is.False);
            Assert.That(completed.ErrorCode,
                Is.EqualTo("stream_writer.message_count_mismatch"));
            Assert.That(XDocument.Parse(Encoding.UTF8.GetString(output.ToArray())).Root,
                Is.Not.Null);
        });
    }

    [Test]
    public void ConstructorAndOptionBoundaryFailuresAreStable()
    {
        using MemoryStream readOnly = new MemoryStream([], false);
        MessageStreamWriter noOptions = new MessageStreamWriter(new MemoryStream(), null, null);
        MessageStreamWriter notWritable = new MessageStreamWriter(readOnly, null,
            new StreamWriteOptions());
        Assert.That(noOptions.WriteNextMessage(null).ErrorCode,
            Is.EqualTo("stream_writer.options_null"));
        Assert.That(notWritable.WriteNextMessage(null).ErrorCode,
            Is.EqualTo("stream_writer.output_not_writable"));

        StreamWriteOptions missingMessageOptions = new StreamWriteOptions
            { MessageOptions = null! };
        StreamWriteOptions badEnvelope = new StreamWriteOptions
            { EnvelopeKind = EnvelopeKind.StandaloneDocument };
        StreamWriteOptions badWrapper = new StreamWriteOptions
            { EnvelopeKind = EnvelopeKind.CustomWrapper };
        StreamWriteOptions negativeCount = new StreamWriteOptions
        {
            EnvelopeKind = EnvelopeKind.BusinessFileEnvelope,
            HasPrecomputedMessageCount = true,
            PrecomputedMessageCount = -1
        };
        StreamWriteOptions missingTotal = new StreamWriteOptions
        {
            EnvelopeKind = EnvelopeKind.BusinessFileEnvelope,
            HasPrecomputedControlTotal = true,
            PrecomputedControlTotal = null!
        };
        using MemoryStream compressedBytes = new MemoryStream();
        using System.IO.Compression.GZipStream nonSeekable = new System.IO.Compression.GZipStream(
            compressedBytes, System.IO.Compression.CompressionMode.Compress, true);
        StreamWriteOptions deferred = new StreamWriteOptions
        {
            EnvelopeKind = EnvelopeKind.BusinessFileEnvelope,
            UseDeferredTotals = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(MessageStreamWriter.ValidateOptions(
                missingMessageOptions, new MemoryStream())?.ErrorCode,
                Is.EqualTo("stream_writer.message_options_invalid"));
            Assert.That(MessageStreamWriter.ValidateOptions(
                badEnvelope, new MemoryStream())?.ErrorCode,
                Is.EqualTo("stream_writer.envelope_kind_invalid"));
            Assert.That(MessageStreamWriter.ValidateOptions(
                badWrapper, new MemoryStream())?.ErrorCode,
                Is.EqualTo("stream_writer.wrapper_name_invalid"));
            Assert.That(MessageStreamWriter.ValidateOptions(
                deferred, nonSeekable)?.ErrorCode,
                Is.EqualTo("stream_writer.deferred_seek_required"));
            Assert.That(MessageStreamWriter.ValidateOptions(
                negativeCount, new MemoryStream())?.ErrorCode,
                Is.EqualTo("stream_writer.message_count_invalid"));
            Assert.That(MessageStreamWriter.ValidateOptions(
                missingTotal, new MemoryStream())?.ErrorCode,
                Is.EqualTo("stream_writer.control_total_invalid"));
            Assert.That(MessageStreamWriter.IsValidCustomWrapperName(null), Is.False);
            Assert.That(MessageStreamWriter.IsValidCustomWrapperName(
                new QualifiedNameData { LocalName = "bad:name" }), Is.False);
            Assert.That(MessageStreamWriter.IsValidCustomWrapperName(
                new QualifiedNameData { LocalName = "Wrapper", Prefix = "p" }), Is.False);
            Assert.That(MessageStreamWriter.IsValidCustomWrapperName(
                new QualifiedNameData { LocalName = "Wrapper", Prefix = "xml",
                    NamespaceUri = "urn:test" }), Is.False);
            Assert.That(MessageStreamWriter.IsValidCustomWrapperName(
                new QualifiedNameData { LocalName = "Wrapper", Prefix = "p",
                    NamespaceUri = "urn:test" }), Is.True);
        });

        MessageStreamWriter disposed = new MessageStreamWriter(
            new MemoryStream(), null, new StreamWriteOptions());
        disposed.Dispose();
        disposed.Dispose();
        Assert.That(disposed.WriteNextMessage(null).ErrorCode,
            Is.EqualTo("stream_writer.disposed"));
    }

    private static StreamWriteOptions CustomOptions()
    {
        return new StreamWriteOptions
        {
            EnvelopeKind = EnvelopeKind.CustomWrapper,
            CustomWrapperName = new QualifiedNameData
            {
                Prefix = "c",
                LocalName = "Batch",
                NamespaceUri = "urn:example:batch"
            },
            MessageOptions = CompactOptions()
        };
    }

    private static MessageWriteOptions CompactOptions()
    {
        return new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Compact,
            WriteXmlDeclaration = false,
            WriteByteOrderMark = false
        };
    }

    private static MessageEnvelope BusinessFileHeader()
    {
        string xml = "<BizFileEnvlp xmlns='" + Namespace("nvlp.002.001.02") + "'>" +
            "<PyldData><BizFileIdr>FILE-1</BizFileIdr>" +
            "<CreDt>2026-09-01T12:00:00Z</CreDt></PyldData>" +
            "</BizFileEnvlp>";
        return HeaderEnvelope(xml, string.Empty, "BizFileEnvlp", Namespace("nvlp.002.001.02"));
    }

    private static MessageEnvelope CustomHeader()
    {
        string xml = "<c:Batch xmlns:c='urn:example:batch'>" +
            "<c:BatchHeader><c:BatchId>BATCH-1</c:BatchId></c:BatchHeader>" +
            "</c:Batch>";
        return HeaderEnvelope(xml, "c", "Batch", "urn:example:batch");
    }

    private static MessageEnvelope HeaderEnvelope(
        string xml,
        string prefix,
        string localName,
        string namespaceUri)
    {
        MessageReadResult read = new XmlMessageReader().ReadGenericText(
            xml,
            new MessageReadOptions());
        return new MessageEnvelope
        {
            Name = new QualifiedNameData
            {
                Prefix = prefix,
                LocalName = localName,
                NamespaceUri = namespaceUri
            },
            GenericContent = read.Document?.Payloads[0].GenericContent
        };
    }

    private static MessageDocument Message(
        string identifier,
        string businessIdentifier,
        string messageIdentifier,
        decimal amount,
        int scale)
    {
        string amountText = amount.ToString("F" + scale, CultureInfo.InvariantCulture);
        string xml = "<AppHdr xmlns='" + Namespace("head.001.001.04") + "'>" +
            "<BizMsgIdr>" + businessIdentifier + "</BizMsgIdr>" +
            "<MsgDefIdr>" + identifier + "</MsgDefIdr></AppHdr>" +
            "<Document xmlns='" + Namespace(identifier) + "'>" +
            "<MsgId>" + messageIdentifier + "</MsgId>" +
            "<IntrBkSttlmAmt Ccy='USD'>" + amountText + "</IntrBkSttlmAmt>" +
            "</Document>";
        MessageReadOptions options = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient
        };
        MessageReadResult read = new XmlMessageReader().ReadAutoText(xml, options);
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        return read.Document ?? throw new AssertionException("Expected a message document.");
    }

    private static int Count(string value, string search)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = value.IndexOf(search, index, StringComparison.Ordinal);
            if (index >= 0)
            {
                count++;
                index = index + search.Length;
            }
        }

        return count;
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }
}
