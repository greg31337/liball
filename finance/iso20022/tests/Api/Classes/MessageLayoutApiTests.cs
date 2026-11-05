using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageLayoutApiTests
{
    [TestCase(EnvelopeKind.StandaloneDocument)]
    [TestCase(EnvelopeKind.StandaloneHeader)]
    [TestCase(EnvelopeKind.SiblingHeaderAndDocument)]
    [TestCase(EnvelopeKind.BusinessMessageEnvelope)]
    [TestCase(EnvelopeKind.BusinessFileEnvelope)]
    [TestCase(EnvelopeKind.CustomWrapper)]
    public void DetectorReaderAndWriterRoundTripEverySupportedLayout(EnvelopeKind layout)
    {
        string xml = LayoutXml(layout, true);
        MessageReadOptions options = LayoutOptions(layout);
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageDetectionResult firstDetection = detector.DetectText(xml, options);
        MessageReadResult firstRead = reader.ReadAutoText(xml, options);
        MessageWriteResult write = writer.WriteGenericString(firstRead.Document, WriteOptions());
        MessageDetectionResult secondDetection = detector.DetectText(write.Text, LayoutOptions(layout));
        MessageReadResult secondRead = reader.ReadAutoText(write.Text, LayoutOptions(layout));

        Assert.Multiple(() =>
        {
            Assert.That(firstDetection.Success, Is.True);
            Assert.That(firstDetection.Layout, Is.EqualTo(layout));
            Assert.That(firstRead.Success, Is.True);
            Assert.That(firstRead.Document?.Layout, Is.EqualTo(layout));
            Assert.That(write.Success, Is.True);
            Assert.That(write.Text, Is.Not.Empty);
            Assert.That(secondDetection.Success, Is.True);
            Assert.That(secondDetection.Layout, Is.EqualTo(layout));
            Assert.That(secondRead.Success, Is.True);
            Assert.That(secondRead.Document?.Layout, Is.EqualTo(layout));
        });

        if (layout == EnvelopeKind.StandaloneDocument)
        {
            Assert.That(secondRead.Document?.Payloads.Select(value => value.Identifier?.Value), Is.EqualTo(new[] { "pacs.008.001.10" }));
        }
        else if (layout == EnvelopeKind.StandaloneHeader)
        {
            Assert.That(secondRead.Document?.Header?.Identifier?.Value, Is.EqualTo("head.001.001.04"));
        }
        else if (layout == EnvelopeKind.BusinessFileEnvelope)
        {
            Assert.That(secondRead.Document?.Payloads.Select(value => value.Identifier?.Value), Is.EqualTo(new[] { "pacs.008.001.10", "camt.053.001.13" }));
        }
        else if (layout == EnvelopeKind.CustomWrapper)
        {
            Assert.That(secondRead.Document?.Envelope?.Name.NamespaceUri, Is.EqualTo("urn:custom:batch"));
        }
        else
        {
            Assert.That(secondRead.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SiblingHeaderDocumentOrderSurvivesReaderWriterRoundTrip(bool headerFirst)
    {
        string xml = LayoutXml(EnvelopeKind.SiblingHeaderAndDocument, headerFirst);
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();
        IMessageDetector detector = new MessageDetector();

        MessageReadResult read = reader.ReadAutoText(xml, new MessageReadOptions());
        MessageWriteResult write = writer.WriteGenericString(read.Document, WriteOptions());
        MessageDetectionResult detection = detector.DetectText(write.Text, new MessageReadOptions());
        int headerIndex = write.Text.IndexOf("AppHdr", StringComparison.Ordinal);
        int payloadIndex = write.Text.IndexOf("Document", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True);
            Assert.That(read.Document?.HeaderPrecedesPayload, Is.EqualTo(headerFirst));
            Assert.That(write.Success, Is.True);
            Assert.That(headerIndex >= 0, Is.True);
            Assert.That(payloadIndex >= 0, Is.True);
            Assert.That(headerIndex < payloadIndex, Is.EqualTo(headerFirst));
            Assert.That(detection.Success, Is.True);
            Assert.That(detection.Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(detection.HeaderPrecedesPayload, Is.EqualTo(headerFirst));
        });
    }

    [Test]
    public void UnknownMessagesFailAutoDetectionButRemainAvailableToGenericReaderWriter()
    {
        string xml = "<u:FutureMessage xmlns:u='urn:future:message'><u:Value>17</u:Value></u:FutureMessage>";
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageDetectionResult detection = detector.DetectText(xml, new MessageReadOptions());
        MessageReadResult automatic = reader.ReadAutoText(xml, new MessageReadOptions());
        MessageReadResult generic = reader.ReadGenericText(xml, new MessageReadOptions());
        MessageWriteResult write = writer.WriteGenericString(generic.Document, WriteOptions());
        MessageReadResult repeated = reader.ReadGenericText(write.Text, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detection.Success, Is.False);
            Assert.That(detection.ErrorCode, Is.EqualTo("detection.root"));
            Assert.That(automatic.Success, Is.False);
            Assert.That(automatic.ErrorCode, Is.EqualTo("detection.root"));
            Assert.That(generic.Success, Is.True);
            Assert.That(generic.Document?.Layout, Is.EqualTo(EnvelopeKind.Unknown));
            Assert.That(generic.Document?.Payloads[0].Namespace?.Kind, Is.EqualTo(NamespaceKind.Unknown));
            Assert.That(write.Success, Is.True);
            Assert.That(repeated.Success, Is.True);
            Assert.That(repeated.Document?.Payloads[0].GenericContent?.Name.LocalName, Is.EqualTo("FutureMessage"));
            Assert.That(detector.DetectText(write.Text, new MessageReadOptions()).Success, Is.False);
        });
    }

    [TestCase(EnvelopeKind.SiblingHeaderAndDocument)]
    [TestCase(EnvelopeKind.BusinessMessageEnvelope)]
    [TestCase(EnvelopeKind.CustomWrapper)]
    public void ConflictingHeaderAndPayloadIdentifiersProduceStableDetectorAndReaderFailures(EnvelopeKind layout)
    {
        string content = Header("pacs.009.001.10", "CONFLICT") + Document("pacs.008.001.10", "PAYLOAD");
        string xml = content;
        if (layout == EnvelopeKind.BusinessMessageEnvelope)
        {
            xml = "<e:BizMsgEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'>" + content + "</e:BizMsgEnvlp>";
        }
        else if (layout == EnvelopeKind.CustomWrapper)
        {
            xml = "<w:Batch xmlns:w='urn:custom:batch'>" + content + "</w:Batch>";
        }

        MessageReadOptions options = LayoutOptions(layout);
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();

        MessageDetectionResult detection = detector.DetectText(xml, options);
        MessageReadResult read = reader.ReadAutoText(xml, LayoutOptions(layout));

        Assert.Multiple(() =>
        {
            Assert.That(detection.Success, Is.False);
            Assert.That(detection.ErrorCode, Is.EqualTo("detection.identifier_conflict"));
            Assert.That(detection.DeclaredMessageIdentifier?.Value, Is.EqualTo("pacs.009.001.10"));
            Assert.That(detection.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(read.Success, Is.False);
            Assert.That(read.ErrorCode, Is.EqualTo("detection.identifier_conflict"));
            Assert.That(read.Document, Is.Null);
        });
    }

    [Test]
    public void NonSeekableDetectorReaderAndWriterStreamsPreserveCompleteEnvelopeBytes()
    {
        string xml = LayoutXml(EnvelopeKind.BusinessMessageEnvelope, true);
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        NonSeekableReadStream detectionInput = new NonSeekableReadStream(bytes);
        NonSeekableReadStream readerInput = new NonSeekableReadStream(bytes);
        IMessageDetector detector = new MessageDetector();
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageDetectionResult detection = detector.DetectStream(detectionInput, new MessageReadOptions());
        string replay = ReadRemaining(detection.ReplayStream);
        MessageReadResult read = reader.ReadAutoStream(readerInput, new MessageReadOptions());
        NonSeekableWriteStream output = new NonSeekableWriteStream();
        MessageWriteResult write = writer.WriteGenericStream(read.Document, output, WriteOptions());
        byte[] outputBytes = output.ToArray();
        MessageDetectionResult outputDetection = detector.DetectBytes(outputBytes, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(detection.Success, Is.True);
            Assert.That(detection.InputWasSeekable, Is.False);
            Assert.That(detection.ReplayStream, Is.Not.SameAs(detectionInput));
            Assert.That(replay, Is.EqualTo(xml));
            Assert.That(detection.PreservedBytes, Is.EqualTo(bytes));
            Assert.That(detectionInput.ReadCount, Is.GreaterThan(0));
            Assert.That(read.Success, Is.True);
            Assert.That(readerInput.ReadCount, Is.GreaterThan(0));
            Assert.That(write.Success, Is.True);
            Assert.That(write.BytesWritten, Is.EqualTo(outputBytes.Length));
            Assert.That(output.WriteCount, Is.GreaterThan(0));
            Assert.That(outputDetection.Success, Is.True);
            Assert.That(outputDetection.Layout, Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
        });
    }

    internal static MessageReadOptions LayoutOptions(EnvelopeKind layout)
    {
        MessageReadOptions options = new MessageReadOptions();
        if (layout == EnvelopeKind.CustomWrapper)
        {
            options.CustomWrapperNames.Add(new QualifiedNameData
            {
                Prefix = "configured",
                LocalName = "Batch",
                NamespaceUri = "urn:custom:batch"
            });
        }

        return options;
    }

    internal static MessageWriteOptions WriteOptions()
    {
        return new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Preserve,
            WriteXmlDeclaration = false,
            PreservePrefixes = true,
            PreserveLexicalValues = true
        };
    }

    internal static string LayoutXml(EnvelopeKind layout, bool headerFirst)
    {
        string header = Header("pacs.008.001.10", "BUSINESS-1");
        string document = Document("pacs.008.001.10", "MESSAGE-1");
        if (layout == EnvelopeKind.StandaloneDocument)
        {
            return document;
        }

        if (layout == EnvelopeKind.StandaloneHeader)
        {
            return header;
        }

        string content = headerFirst ? header + document : document + header;
        if (layout == EnvelopeKind.SiblingHeaderAndDocument)
        {
            return content;
        }

        if (layout == EnvelopeKind.BusinessMessageEnvelope)
        {
            return "<e:BizMsgEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'>" + content + "</e:BizMsgEnvlp>";
        }

        if (layout == EnvelopeKind.BusinessFileEnvelope)
        {
            string first = "<e:Message>" + content + "</e:Message>";
            string second = "<e:Message>" + Header("camt.053.001.13", "BUSINESS-2") + Document("camt.053.001.13", "MESSAGE-2") + "</e:Message>";
            return "<e:BizFileEnvlp xmlns:e='" + Namespace("nvlp.002.001.02") + "'>" + first + second + "</e:BizFileEnvlp>";
        }

        return "<w:Batch xmlns:w='urn:custom:batch'>" + content + "</w:Batch>";
    }

    internal static string Header(string identifier, string businessIdentifier)
    {
        return "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:BizMsgIdr>" + businessIdentifier + "</h:BizMsgIdr><h:MsgDefIdr>" + identifier + "</h:MsgDefIdr></h:AppHdr>";
    }

    internal static string Document(string identifier, string messageIdentifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'><MsgId>" + messageIdentifier + "</MsgId></Document>";
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    internal static string ReadRemaining(Stream? stream)
    {
        Assert.That(stream, Is.Not.Null);
        using MemoryStream output = new MemoryStream();
        stream!.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    internal class NonSeekableReadStream : Stream
    {
        MemoryStream inner;

        internal NonSeekableReadStream(byte[] bytes)
        {
            inner = new MemoryStream(bytes, false);
        }

        internal int ReadCount;

        public override bool CanRead => true;
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
            return inner.Read(buffer, offset, count);
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
    }

    internal class NonSeekableWriteStream : Stream
    {
        MemoryStream inner = new MemoryStream();

        internal int WriteCount;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
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
            WriteCount++;
            inner.Write(buffer, offset, count);
        }

        internal byte[] ToArray()
        {
            return inner.ToArray();
        }
    }
}
