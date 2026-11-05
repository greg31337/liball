using System.Buffers;
using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class XmlMessageWriterTests
{
    [Test]
    public void StreamTextWriterBufferAndStringEntryPointsProduceEquivalentCompactXml()
    {
        MessageDocument document = ReadGeneric("<p:Root xmlns:p='urn:test' id='17'>value</p:Root>");
        MessageWriteOptions options = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Compact,
            WriteXmlDeclaration = false,
            WriteByteOrderMark = false
        };
        IXmlMessageWriter writer = new XmlMessageWriter();
        MemoryStream stream = new MemoryStream();
        StringWriter textWriter = new StringWriter();
        ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();

        MessageWriteResult streamResult = writer.WriteGenericStream(document, stream, options);
        MessageWriteResult textResult = writer.WriteGenericTextWriter(document, textWriter, options);
        MessageWriteResult bufferResult = writer.WriteGenericBuffer(document, buffer, options);
        MessageWriteResult stringResult = writer.WriteGenericString(document, options);
        string streamXml = Encoding.UTF8.GetString(stream.ToArray());
        string bufferXml = Encoding.UTF8.GetString(buffer.WrittenSpan);

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Success, Is.True);
            Assert.That(textResult.Success, Is.True);
            Assert.That(bufferResult.Success, Is.True);
            Assert.That(stringResult.Success, Is.True);
            Assert.That(streamXml, Is.EqualTo(stringResult.Text));
            Assert.That(textWriter.ToString(), Is.EqualTo(stringResult.Text));
            Assert.That(bufferXml, Is.EqualTo(stringResult.Text));
            Assert.That(bufferResult.Bytes, Is.EqualTo(buffer.WrittenSpan.ToArray()));
            Assert.That(streamResult.BytesWritten, Is.EqualTo(stream.Length));
            Assert.That(bufferResult.BytesWritten, Is.EqualTo(buffer.WrittenCount));
            Assert.That(stringResult.BytesWritten, Is.EqualTo(Encoding.UTF8.GetByteCount(stringResult.Text)));
            Assert.That(stream.CanWrite, Is.True);
            Assert.That(stringResult.Text, Is.EqualTo("<p:Root id=\"17\" xmlns:p=\"urn:test\">value</p:Root>").Or.EqualTo("<p:Root xmlns:p=\"urn:test\" id=\"17\">value</p:Root>"));
        });
    }

    [Test]
    public void PreserveModeRetainsPrefixesAttributeOrderCdataAndEmptyElementStyle()
    {
        string xml = "<p:Root xmlns:p='urn:root' xmlns:q='urn:attribute' q:b='2' a='1'><p:Short/><p:Full></p:Full><![CDATA[value<raw]]></p:Root>";
        MessageDocument document = ReadGeneric(xml);
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageWriteResult result = writer.WriteGenericString(document, new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Preserve,
            WriteXmlDeclaration = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Does.StartWith("<p:Root"));
            Assert.That(result.Text.IndexOf("q:b=", StringComparison.Ordinal), Is.LessThan(result.Text.IndexOf("a=", StringComparison.Ordinal)));
            Assert.That(result.Text, Does.Contain("<p:Short />").Or.Contain("<p:Short/>"));
            Assert.That(result.Text, Does.Contain("<p:Full></p:Full>"));
            Assert.That(result.Text, Does.Contain("<![CDATA[value<raw]]>"));
        });
    }

    [Test]
    public void IndentedModeUsesCallerWhitespaceWhileCompactModeAddsNone()
    {
        MessageDocument document = ReadGeneric("<Root><Child/></Root>");
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageWriteResult indented = writer.WriteGenericString(document, new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Indented,
            WriteXmlDeclaration = false,
            IndentText = "\t",
            NewLineText = "\r\n"
        });
        MessageWriteResult compact = writer.WriteGenericString(document, new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Compact,
            WriteXmlDeclaration = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(indented.Text, Does.Contain("\r\n\t<Child"));
            Assert.That(compact.Text, Does.Not.Contain("\r"));
            Assert.That(compact.Text, Does.Not.Contain("\n"));
        });
    }

    [Test]
    public void CanonicalModeRemovesPrefixAttributeCdataAndEmptyStyleVarianceAndIsStable()
    {
        string firstXml = "<p:Root xmlns:p='urn:z' xmlns:q='urn:a' q:b='2' a='1'><p:Empty/><![CDATA[value<]]></p:Root>";
        string secondXml = "<different:Root xmlns:different='urn:z' xmlns:attribute='urn:a' a='1' attribute:b='2'><different:Empty></different:Empty>value&lt;</different:Root>";
        MessageDocument first = ReadGeneric(firstXml);
        MessageDocument second = ReadGeneric(secondXml);
        MessageWriteOptions options = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Canonical,
            WriteXmlDeclaration = true,
            WriteByteOrderMark = true,
            PreservePrefixes = true,
            PreserveLexicalValues = true
        };
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageWriteResult firstResult = writer.WriteGenericString(first, options);
        MessageWriteResult secondResult = writer.WriteGenericString(second, options);
        MessageDocument reparsed = ReadGeneric(firstResult.Text);
        MessageWriteResult stableResult = writer.WriteGenericString(reparsed, options);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(firstResult.Text, Is.EqualTo(secondResult.Text));
            Assert.That(stableResult.Text, Is.EqualTo(firstResult.Text));
            Assert.That(firstResult.Text, Does.Not.StartWith("<?xml"));
            Assert.That(firstResult.Text[0], Is.Not.EqualTo('\uFEFF'));
            Assert.That(firstResult.Text, Does.Contain("xmlns:n0=\"urn:a\""));
            Assert.That(firstResult.Text, Does.Contain("xmlns:n1=\"urn:z\""));
            Assert.That(firstResult.Text, Does.Contain("<n1:Empty></n1:Empty>"));
            Assert.That(firstResult.Text, Does.Not.Contain("CDATA"));
            Assert.That(firstResult.Text, Does.Contain("value&lt;"));
            Assert.That(options.WriteXmlDeclaration, Is.True);
            Assert.That(options.WriteByteOrderMark, Is.True);
            Assert.That(options.PreservePrefixes, Is.True);
            Assert.That(options.PreserveLexicalValues, Is.True);
        });
    }

    [Test]
    public void StreamBomAndTextWriterDeclarationUseTheSelectedOutputEncoding()
    {
        MessageDocument document = ReadGeneric("<Root/>");
        MessageWriteOptions options = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Compact,
            WriteXmlDeclaration = true,
            WriteByteOrderMark = true
        };
        IXmlMessageWriter writer = new XmlMessageWriter();
        MemoryStream stream = new MemoryStream();
        StringWriter textWriter = new StringWriter();

        MessageWriteResult streamResult = writer.WriteGenericStream(document, stream, options);
        MessageWriteResult textResult = writer.WriteGenericTextWriter(document, textWriter, options);
        byte[] bytes = stream.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Success, Is.True);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("encoding=\"utf-8\""));
            Assert.That(textResult.Success, Is.True);
            Assert.That(textWriter.ToString(), Does.Contain("encoding=\"utf-16\""));
        });
    }

    [Test]
    public void SiblingAndEnvelopeDocumentsAreAssembledFromTheirGenericParts()
    {
        IXmlMessageReader reader = new XmlMessageReader();
        string siblingXml = Header("pacs.008.001.10") + Document("pacs.008.001.10");
        MessageDocument sibling = reader.ReadAutoText(siblingXml, new MessageReadOptions()).Document!;
        string envelopeXml = "<e:BizMsgEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'>" + siblingXml + "</e:BizMsgEnvlp>";
        MessageDocument envelope = reader.ReadAutoText(envelopeXml, new MessageReadOptions()).Document!;
        IXmlMessageWriter writer = new XmlMessageWriter();
        MessageWriteOptions options = new MessageWriteOptions { WriteXmlDeclaration = false };

        MessageWriteResult siblingResult = writer.WriteGenericString(sibling, options);
        MessageWriteResult envelopeResult = writer.WriteGenericString(envelope, options);

        Assert.Multiple(() =>
        {
            Assert.That(siblingResult.Success, Is.True);
            Assert.That(siblingResult.Text.IndexOf("AppHdr", StringComparison.Ordinal), Is.LessThan(siblingResult.Text.IndexOf("Document", StringComparison.Ordinal)));
            Assert.That(new MessageDetector().DetectText(siblingResult.Text, new MessageReadOptions()).Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(envelopeResult.Success, Is.True);
            Assert.That(envelopeResult.Text, Does.StartWith("<e:BizMsgEnvlp"));
            Assert.That(new MessageDetector().DetectText(envelopeResult.Text, new MessageReadOptions()).Layout, Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
        });
    }

    [Test]
    public void CallerNamespaceDeclarationsAreAddedWithoutMutatingTheDocumentTree()
    {
        MessageDocument document = ReadGeneric("<Root/>");
        DynamicXmlNode root = document.Payloads[0].GenericContent!;
        MessageWriteOptions options = new MessageWriteOptions
        {
            WriteXmlDeclaration = false,
            NamespaceDeclarations = new List<NamespaceDeclarationData>
            {
                new NamespaceDeclarationData { Prefix = "extra", NamespaceUri = "urn:extra" }
            }
        };
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageWriteResult result = writer.WriteGenericString(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Does.Contain("xmlns:extra=\"urn:extra\""));
            Assert.That(root.NamespaceDeclarations, Is.Empty);
        });
    }

    [Test]
    public void OutputLimitsAreEnforcedBeforeTargetsAreMutated()
    {
        MessageDocument document = ReadGeneric("<Root><Child>four</Child></Root>");
        MessageWriteOptions options = new MessageWriteOptions();
        options.Limits.MaxTextLength = 3;
        IXmlMessageWriter writer = new XmlMessageWriter();
        MemoryStream stream = new MemoryStream();

        XmlException? exception = Assert.Throws<XmlException>(() => writer.WriteGenericStream(document, stream, options));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Does.Contain("MaxTextLength"));
            Assert.That(stream.Length, Is.Zero);
        });
    }

    [Test]
    public void NullAndMissingGenericContentReturnStableFailures()
    {
        IXmlMessageWriter writer = new XmlMessageWriter();
        MessageDocument missing = new MessageDocument
        {
            Payloads = new List<MessagePayload>
            {
                new MessagePayload { TypedContent = new object() }
            }
        };

        MessageWriteResult nullDocument = writer.WriteGenericString(null, new MessageWriteOptions());
        MessageWriteResult nullOptions = writer.WriteGenericString(new MessageDocument(), null);
        MessageWriteResult nullOutput = writer.WriteGenericStream(new MessageDocument(), null, new MessageWriteOptions());
        MessageWriteResult missingContent = writer.WriteGenericString(missing, new MessageWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(nullDocument.ErrorCode, Is.EqualTo("writer.document_null"));
            Assert.That(nullOptions.ErrorCode, Is.EqualTo("writer.options_null"));
            Assert.That(nullOutput.ErrorCode, Is.EqualTo("writer.output_null"));
            Assert.That(missingContent.Success, Is.False);
            Assert.That(missingContent.ErrorCode, Is.EqualTo("writer.content_missing"));
            Assert.That(missingContent.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void EveryOutputEntryPointValidatesInputsAndMissingContent()
    {
        IXmlMessageWriter writer = new XmlMessageWriter();
        MessageDocument empty = new MessageDocument();
        MessageWriteOptions options = new MessageWriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(writer.WriteGenericStream(null, new MemoryStream(), options).ErrorCode,
                Is.EqualTo("writer.document_null"));
            Assert.That(writer.WriteGenericStream(empty, new MemoryStream(), null).ErrorCode,
                Is.EqualTo("writer.options_null"));
            Assert.That(writer.WriteGenericStream(empty, new MemoryStream(), options).ErrorCode,
                Is.EqualTo("writer.content_missing"));
            Assert.That(writer.WriteGenericTextWriter(null, new StringWriter(), options).ErrorCode,
                Is.EqualTo("writer.document_null"));
            Assert.That(writer.WriteGenericTextWriter(empty, null, options).ErrorCode,
                Is.EqualTo("writer.output_null"));
            Assert.That(writer.WriteGenericTextWriter(empty, new StringWriter(), null).ErrorCode,
                Is.EqualTo("writer.options_null"));
            Assert.That(writer.WriteGenericTextWriter(empty, new StringWriter(), options).ErrorCode,
                Is.EqualTo("writer.content_missing"));
            Assert.That(writer.WriteGenericBuffer(null, new ArrayBufferWriter<byte>(), options).ErrorCode,
                Is.EqualTo("writer.document_null"));
            Assert.That(writer.WriteGenericBuffer(empty, null, options).ErrorCode,
                Is.EqualTo("writer.output_null"));
            Assert.That(writer.WriteGenericBuffer(empty, new ArrayBufferWriter<byte>(), null).ErrorCode,
                Is.EqualTo("writer.options_null"));
            Assert.That(writer.WriteGenericBuffer(empty, new ArrayBufferWriter<byte>(), options).ErrorCode,
                Is.EqualTo("writer.content_missing"));
            Assert.That(XmlMessageWriter.CreateInputFailure(false, false, false).ErrorCode,
                Is.EqualTo("writer.input"));
        });
    }

    [Test]
    public void DocumentAssemblyCoversRawEnvelopePayloadOrderingAndUnknownPolicies()
    {
        MessageWriteOptions preserve = new MessageWriteOptions();
        RawXmlFragment rawHeader = new RawXmlFragment { Xml = "<Header/>" };
        RawXmlFragment rawPayload = new RawXmlFragment { Xml = "<Payload/>" };
        MessageDocument envelope = new MessageDocument
        {
            Envelope = new MessageEnvelope
            {
                Name = new QualifiedNameData { LocalName = "Envelope" },
                NamespaceDeclarations = new List<NamespaceDeclarationData>
                {
                    new NamespaceDeclarationData { Prefix = "e", NamespaceUri = "urn:e" }
                }
            },
            Header = new MessageHeader { RawContent = rawHeader }
        };
        envelope.Payloads.Add(new MessagePayload
        {
            GenericContent = ReadGeneric("<Generic/>").Payloads[0].GenericContent
        });
        envelope.Payloads.Add(new MessagePayload { RawContent = rawPayload });
        envelope.Payloads.Add(new MessagePayload());
        DynamicXmlNode envelopeTree = XmlMessageWriter.BuildDocumentTree(envelope, preserve) ??
            throw new AssertionException("Expected an envelope tree.");
        MessageDocument rawEnvelope = new MessageDocument
        {
            Envelope = new MessageEnvelope { RawContent = new RawXmlFragment { Xml = "<RawEnvelope/>" } }
        };
        DynamicXmlNode rawEnvelopeTree = XmlMessageWriter.BuildDocumentTree(
            rawEnvelope, preserve) ?? throw new AssertionException("Expected raw envelope content.");
        MessageDocument ignoredRawEnvelope = new MessageDocument
        {
            Envelope = new MessageEnvelope
            {
                RawContent = new RawXmlFragment { Xml = "<Ignored/>" },
                Name = new QualifiedNameData { LocalName = "EmptyEnvelope" }
            }
        };
        DynamicXmlNode ignoredRawEnvelopeTree = XmlMessageWriter.BuildDocumentTree(
            ignoredRawEnvelope,
            new MessageWriteOptions { UnknownContentMode = UnknownContentMode.Ignore }) ??
            throw new AssertionException("Expected a named empty envelope.");
        envelope.Header = new MessageHeader
        {
            GenericContent = ReadGeneric("<GenericHeader/>").Payloads[0].GenericContent
        };
        DynamicXmlNode genericHeaderEnvelopeTree = XmlMessageWriter.BuildDocumentTree(
            envelope, preserve) ?? throw new AssertionException("Expected generic header content.");

        MessageDocument reversed = new MessageDocument
        {
            Layout = EnvelopeKind.SiblingHeaderAndDocument,
            HeaderPrecedesPayload = false,
            Header = new MessageHeader
            {
                GenericContent = ReadGeneric("<Header/>").Payloads[0].GenericContent
            }
        };
        reversed.Payloads.Add(new MessagePayload
        {
            GenericContent = ReadGeneric("<Payload/>").Payloads[0].GenericContent
        });
        DynamicXmlNode reversedTree = XmlMessageWriter.BuildDocumentTree(reversed, preserve) ??
            throw new AssertionException("Expected a sibling tree.");
        DynamicXmlNode parent = new DynamicXmlNode();
        DynamicXmlNode documentNode = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        documentNode.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "a" });
        documentNode.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "b" });
        XmlMessageWriter.AddContentNode(parent, documentNode);
        DynamicXmlNode rawParts = new DynamicXmlNode();
        XmlMessageWriter.AddHeaderContentNode(
            rawParts, new MessageHeader { RawContent = rawHeader }, preserve);
        XmlMessageWriter.AddHeaderContentNode(rawParts, new MessageHeader(), preserve);
        XmlMessageWriter.AddPayloadContentNodes(
            rawParts,
            new List<MessagePayload>
            {
                new MessagePayload { RawContent = rawPayload },
                new MessagePayload()
            },
            preserve);
        MessageWriteOptions ignore = new MessageWriteOptions
        {
            UnknownContentMode = UnknownContentMode.Ignore
        };
        MessageWriteOptions reject = new MessageWriteOptions
        {
            UnknownContentMode = UnknownContentMode.Reject
        };

        Assert.Multiple(() =>
        {
            Assert.That(envelopeTree.Children[0].Children, Has.Count.EqualTo(3));
            Assert.That(rawEnvelopeTree.Children[0].Name.LocalName,
                Is.EqualTo("RawEnvelope"));
            Assert.That(ignoredRawEnvelopeTree.Children[0].Name.LocalName,
                Is.EqualTo("EmptyEnvelope"));
            Assert.That(genericHeaderEnvelopeTree.Children[0].Children[0].Name.LocalName,
                Is.EqualTo("GenericHeader"));
            Assert.That(reversedTree.Children.Select(item => item.Name.LocalName),
                Is.EqualTo(new[] { "Payload", "Header" }));
            Assert.That(parent.Children, Has.Count.EqualTo(2));
            Assert.That(rawParts.Children.Select(item => item.Name.LocalName),
                Is.EqualTo(new[] { "Header", "Payload" }));
            Assert.DoesNotThrow(() => XmlMessageWriter.AddRawContentNode(
                new DynamicXmlNode(), rawPayload, ignore));
            Assert.Throws<XmlException>(() => XmlMessageWriter.AddRawContentNode(
                new DynamicXmlNode(), rawPayload, reject));
        });
    }

    internal static MessageDocument ReadGeneric(string xml)
    {
        MessageReadResult result = new XmlMessageReader().ReadGenericText(xml, new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true
        });
        Assert.That(result.Success, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    internal static string Document(string identifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'/>";
    }

    internal static string Header(string identifier)
    {
        return "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:MsgDefIdr>" + identifier + "</h:MsgDefIdr></h:AppHdr>";
    }
}
