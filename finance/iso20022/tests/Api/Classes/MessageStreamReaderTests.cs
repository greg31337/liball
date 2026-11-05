using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageStreamReaderTests
{
    [Test]
    public void PullsOfficialBusinessFilePayloadsInOrder()
    {
        string xml = "<BizFileEnvlp xmlns='" + Namespace("nvlp.002.001.02") + "'>" +
            "<PyldData><BizFileIdr>FILE-1</BizFileIdr><CreDt>2026-09-01T12:00:00Z</CreDt></PyldData>" +
            Payload("pacs.008.001.10", "PAYMENT-1") +
            Payload("semt.018.001.14", "SECURITIES-1") +
            "</BizFileEnvlp>";
        MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml), false);
        IMessageStreamReader reader = new MessageStreamReader(input, new StreamReadOptions());

        MessageStreamItem? first = reader.ReadNextMessage();
        MessageStreamItem? second = reader.ReadNextMessage();
        MessageStreamItem? end = reader.ReadNextMessage();
        ((IDisposable)reader).Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(first?.Index, Is.Zero);
            Assert.That(first?.Result.Success, Is.True, first?.Result.ErrorMessage);
            Assert.That(first?.Result.Document?.Layout, Is.EqualTo(EnvelopeKind.CustomWrapper));
            Assert.That(first?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(first?.Result.Document?.Metadata.References,
                Does.Contain("PAYMENT-1"));
            Assert.That(first?.Location.HasLineInformation, Is.True);
            Assert.That(second?.Index, Is.EqualTo(1));
            Assert.That(second?.Result.Success, Is.True, second?.Result.ErrorMessage);
            Assert.That(second?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("semt.018.001.14"));
            Assert.That(second?.Result.Document?.Metadata.References,
                Does.Contain("SECURITIES-1"));
            Assert.That(end, Is.Null);
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void PullsBusinessMessageEnvelopeSequenceWithoutFlatteningItems()
    {
        QualifiedNameData wrapper = new QualifiedNameData
        {
            NamespaceUri = "urn:example:sequence",
            LocalName = "Messages"
        };
        StreamReadOptions options = new StreamReadOptions();
        options.WrapperNames.Add(wrapper);
        string xml = "<Messages xmlns='urn:example:sequence'>" +
            BusinessMessageEnvelope("pacs.008.001.10", "PAYMENT-2") +
            BusinessMessageEnvelope("camt.053.001.13", "STATEMENT-2") +
            "</Messages>";
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        MessageStreamItem? first = reader.ReadNextMessage();
        MessageStreamItem? second = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(first?.Result.Success, Is.True, first?.Result.ErrorMessage);
            Assert.That(first?.Result.Document?.Layout,
                Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(first?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(second?.Result.Success, Is.True, second?.Result.ErrorMessage);
            Assert.That(second?.Result.Document?.Layout,
                Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(second?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("camt.053.001.13"));
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void PairsSiblingHeadersAndDocumentsInsideCallerWrapper()
    {
        StreamReadOptions options = WrapperOptions();
        string xml = "<Batch xmlns='urn:example:batch'>" +
            Header("pacs.008.001.10", "BUSINESS-1") +
            Document("pacs.008.001.10", "PAYMENT-3") +
            Header("fxtr.008.001.07", "BUSINESS-2") +
            Document("fxtr.008.001.07", "FX-3") +
            "</Batch>";
        using MessageStreamReader reader = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            options);

        MessageStreamItem? first = reader.ReadNextMessage();
        MessageStreamItem? second = reader.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(first?.Result.Success, Is.True, first?.Result.ErrorMessage);
            Assert.That(first?.Result.Document?.Layout,
                Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(first?.Result.Document?.Header?.BusinessMessageIdentifier,
                Is.EqualTo("BUSINESS-1"));
            Assert.That(first?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(second?.Result.Success, Is.True, second?.Result.ErrorMessage);
            Assert.That(second?.Result.Document?.Header?.BusinessMessageIdentifier,
                Is.EqualTo("BUSINESS-2"));
            Assert.That(second?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("fxtr.008.001.07"));
            Assert.That(reader.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void ReturnsPerItemFailureAndHonorsContinuationPolicy()
    {
        string xml = "<Batch xmlns='urn:example:batch'>" +
            "<Item><Unknown/></Item>" +
            "<Item>" + Document("tsmt.009.001.04", "TRADE-4") + "</Item>" +
            "</Batch>";
        StreamReadOptions continuingOptions = WrapperOptions();
        continuingOptions.ContinueAfterInvalidItem = true;
        using MessageStreamReader continuing = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            continuingOptions);
        MessageStreamItem? invalid = continuing.ReadNextMessage();
        MessageStreamItem? valid = continuing.ReadNextMessage();

        using MessageStreamReader stopping = new MessageStreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(xml), false),
            WrapperOptions());
        MessageStreamItem? stoppedInvalid = stopping.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(invalid?.Index, Is.Zero);
            Assert.That(invalid?.Result.Success, Is.False);
            Assert.That(invalid?.Result.ErrorCode, Is.EqualTo("detection.wrapper_empty"));
            Assert.That(invalid?.Result.ErrorMessage, Is.Not.Empty);
            Assert.That(valid?.Index, Is.EqualTo(1));
            Assert.That(valid?.Result.Success, Is.True, valid?.Result.ErrorMessage);
            Assert.That(valid?.Result.Document?.Metadata.MessageIdentifier?.Value,
                Is.EqualTo("tsmt.009.001.04"));
            Assert.That(continuing.ReadNextMessage(), Is.Null);
            Assert.That(stoppedInvalid?.Result.ErrorCode,
                Is.EqualTo("detection.wrapper_empty"));
            Assert.That(stopping.ReadNextMessage(), Is.Null);
        });
    }

    [Test]
    public void ConstructorAndWrapperBoundariesReturnLocalizedItems()
    {
        MessageStreamReader noOptions = new MessageStreamReader(new MemoryStream(), null);
        MessageStreamItem? noOptionsItem = noOptions.ReadNextMessage();
        StreamReadOptions invalid = new StreamReadOptions { MessageOptions = null! };
        MessageStreamReader invalidOptions = new MessageStreamReader(
            new MemoryStream(), invalid);
        MessageStreamItem? invalidItem = invalidOptions.ReadNextMessage();
        MessageStreamReader empty = new MessageStreamReader(
            new MemoryStream(), new StreamReadOptions());
        MessageStreamItem? emptyItem = empty.InitializeSequence();
        MessageStreamReader unsupported = new MessageStreamReader(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<Unknown/>")),
            new StreamReadOptions());
        MessageStreamItem? unsupportedItem = unsupported.ReadNextMessage();

        Assert.Multiple(() =>
        {
            Assert.That(noOptionsItem?.Result.ErrorCode, Is.EqualTo("stream.options_null"));
            Assert.That(invalidItem?.Result.ErrorCode,
                Is.EqualTo("stream.message_options_invalid"));
            Assert.That(emptyItem?.Result.ErrorCode, Is.EqualTo("stream.empty"));
            Assert.That(unsupportedItem?.Result.ErrorCode,
                Is.EqualTo("stream.wrapper_unsupported"));
            Assert.That(noOptions.ReadNextMessage(), Is.Null);
        });

        MessageStreamReader helpers = new MessageStreamReader(
            new MemoryStream(), new StreamReadOptions());
        Assert.Multiple(() =>
        {
            Assert.That(helpers.IsBusinessFileRoot(new QualifiedNameData()), Is.False);
            Assert.That(helpers.IsBusinessMessageRoot(new QualifiedNameData()), Is.False);
            Assert.That(helpers.IsConfiguredWrapper(new QualifiedNameData()), Is.False);
            Assert.That(helpers.CaptureName().LocalName, Is.Empty);
            Assert.That(helpers.CaptureLocation().HasLineInformation, Is.False);
            Assert.That(helpers.ResolveMaximumItems(), Is.GreaterThan(0));
        });
        helpers.Dispose();
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

    private static string Payload(string identifier, string messageIdentifier)
    {
        return "<Pyld><BizCntt>" + Document(identifier, messageIdentifier) +
            "</BizCntt></Pyld>";
    }

    private static string BusinessMessageEnvelope(string identifier, string messageIdentifier)
    {
        return "<BizMsgEnvlp xmlns='" + Namespace("nvlp.001.001.02") + "'>" +
            Header(identifier, "BUSINESS-" + messageIdentifier) +
            Document(identifier, messageIdentifier) +
            "</BizMsgEnvlp>";
    }

    private static string Header(string identifier, string businessIdentifier)
    {
        return "<AppHdr xmlns='" + Namespace("head.001.001.04") + "'>" +
            "<BizMsgIdr>" + businessIdentifier + "</BizMsgIdr>" +
            "<MsgDefIdr>" + identifier + "</MsgDefIdr>" +
            "</AppHdr>";
    }

    private static string Document(string identifier, string messageIdentifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'>" +
            "<MsgId>" + messageIdentifier + "</MsgId></Document>";
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }
}
