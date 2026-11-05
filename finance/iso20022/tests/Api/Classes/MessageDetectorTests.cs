using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class MessageDetectorTests
{
    [Test]
    public void DetectsStandaloneDocumentAndPreservesExactIdentifierAndReplayBytes()
    {
        string xml = Document("pacs.008.001.10");
        byte[] input = Encoding.UTF8.GetBytes(xml);
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectBytes(input, new MessageReadOptions());
        input[0] = (byte)'!';

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Layout, Is.EqualTo(EnvelopeKind.StandaloneDocument));
            Assert.That(result.RootName?.LocalName, Is.EqualTo("Document"));
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.MessageNamespace?.Kind, Is.EqualTo(NamespaceKind.Message));
            Assert.That(result.PayloadIdentifiers.Select(value => value.Value), Is.EqualTo(new[] { "pacs.008.001.10" }));
            Assert.That(Encoding.UTF8.GetString(result.PreservedBytes), Is.EqualTo(xml));
            Assert.That(ReadRemainingText(result.ReplayStream), Is.EqualTo(xml));
            Assert.That(result.ErrorCode, Is.Empty);
        });
    }

    [Test]
    public void DetectsStandaloneHeaderAndItsDeclaredMessageIdentity()
    {
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(Header("pacs.008.001.10"), new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Layout, Is.EqualTo(EnvelopeKind.StandaloneHeader));
            Assert.That(result.HeaderNamespace?.Kind, Is.EqualTo(NamespaceKind.Header));
            Assert.That(result.HeaderNamespace?.MessageIdentifier?.Value, Is.EqualTo("head.001.001.04"));
            Assert.That(result.DeclaredMessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.MessageIdentifier, Is.Null);
            Assert.That(result.PayloadIdentifiers, Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void DetectsBothSiblingHeaderDocumentOrders(bool headerFirst)
    {
        string header = Header("pacs.008.001.10");
        string document = Document("pacs.008.001.10");
        string xml = headerFirst ? header + document : document + header;
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(result.HeaderPrecedesPayload, Is.EqualTo(headerFirst));
            Assert.That(result.HeaderNamespace?.Kind, Is.EqualTo(NamespaceKind.Header));
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.DeclaredMessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
        });
    }

    [Test]
    public void DetectsBusinessMessageEnvelopeAndChecksItsSinglePayload()
    {
        string xml = "<e:BizMsgEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'>" +
            Header("pacs.008.001.10") + Document("pacs.008.001.10") + "</e:BizMsgEnvlp>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Layout, Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(result.EnvelopeNamespace?.MessageIdentifier?.Value, Is.EqualTo("nvlp.001.001.02"));
            Assert.That(result.HeaderNamespace?.Kind, Is.EqualTo(NamespaceKind.Header));
            Assert.That(result.PayloadIdentifiers.Select(value => value.Value), Is.EqualTo(new[] { "pacs.008.001.10" }));
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
        });
    }

    [Test]
    public void NestedSupplementaryDocumentIsNotASecondEnvelopePayload()
    {
        string payload =
            "<Document xmlns='" + Namespace("pacs.008.001.10") + "'>" +
            "<SplmtryData><Envlp>" +
            "<ext:Document xmlns:ext='" + Namespace("supl.001.001.01") + "'>" +
            "<ext:Value>preserved</ext:Value></ext:Document>" +
            "</Envlp></SplmtryData></Document>";
        string xml =
            "<e:BizMsgEnvlp xmlns:e='" +
            Namespace("nvlp.001.001.02") + "'>" +
            Header("pacs.008.001.10") + payload + "</e:BizMsgEnvlp>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(
            xml,
            new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Layout,
                Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(result.PayloadIdentifiers.Select(value => value.Value),
                Is.EqualTo(new[] { "pacs.008.001.10" }));
            Assert.That(result.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
        });
    }

    [Test]
    public void DetectsBusinessFileEnvelopeWithMultipleHeaderPayloadPairs()
    {
        string first = "<e:Message>" + Header("pacs.008.001.10") + Document("pacs.008.001.10") + "</e:Message>";
        string second = "<e:Message>" + Header("camt.053.001.13") + Document("camt.053.001.13") + "</e:Message>";
        string xml = "<e:BizFileEnvlp xmlns:e='" + Namespace("nvlp.002.001.02") + "'>" + first + second + "</e:BizFileEnvlp>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Layout, Is.EqualTo(EnvelopeKind.BusinessFileEnvelope));
            Assert.That(result.EnvelopeNamespace?.MessageIdentifier?.Value, Is.EqualTo("nvlp.002.001.02"));
            Assert.That(result.PayloadIdentifiers.Select(value => value.Value), Is.EqualTo(new[] { "pacs.008.001.10", "camt.053.001.13" }));
            Assert.That(result.DeclaredMessageIdentifiers.Select(value => value.Value), Is.EqualTo(new[] { "pacs.008.001.10", "camt.053.001.13" }));
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
        });
    }

    [TestCase("pacs.009.001.10", "pacs.008.001.10")]
    [TestCase("pacs.008.001.09", "pacs.008.001.10")]
    public void ReportsHeaderPayloadIdentifierConflicts(string declaredIdentifier, string payloadIdentifier)
    {
        string xml = Header(declaredIdentifier) + Document(payloadIdentifier);
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("detection.identifier_conflict"));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
            Assert.That(result.DeclaredMessageIdentifier?.Value, Is.EqualTo(declaredIdentifier));
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo(payloadIdentifier));
        });
    }

    [Test]
    public void ReportsMalformedAndAmbiguousHeaderDeclaredIdentifiers()
    {
        string malformedHeader = Header("PACS.008.001.10");
        string ambiguousHeader = "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:MsgDefIdr>pacs.008.001.10</h:MsgDefIdr><h:MsgDefIdr>camt.053.001.13</h:MsgDefIdr></h:AppHdr>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult malformed = detector.DetectText(malformedHeader, new MessageReadOptions());
        MessageDetectionResult ambiguous = detector.DetectText(ambiguousHeader, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.ErrorCode, Is.EqualTo("detection.declared_identifier"));
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.ErrorCode, Is.EqualTo("detection.declared_identifier_count"));
        });
    }

    [Test]
    public void ConfiguredWrapperMatchingIsPrefixIndependentAndUnconfiguredWrappersFail()
    {
        string xml = "<source:Batch xmlns:source='urn:custom:batch'>" + Document("pacs.008.001.10") + "</source:Batch>";
        MessageReadOptions configuredOptions = new MessageReadOptions
        {
            CustomWrapperNames = new List<QualifiedNameData>
            {
                new QualifiedNameData { Prefix = "different", LocalName = "Batch", NamespaceUri = "urn:custom:batch" }
            }
        };
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult configured = detector.DetectText(xml, configuredOptions);
        MessageDetectionResult unconfigured = detector.DetectText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(configured.Success, Is.True);
            Assert.That(configured.Layout, Is.EqualTo(EnvelopeKind.CustomWrapper));
            Assert.That(configured.WrapperName?.Prefix, Is.EqualTo("source"));
            Assert.That(configured.WrapperName?.NamespaceUri, Is.EqualTo("urn:custom:batch"));
            Assert.That(configured.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(unconfigured.Success, Is.False);
            Assert.That(unconfigured.ErrorCode, Is.EqualTo("detection.root"));
        });
    }

    [Test]
    public void LenientDetectionRecordsNamespaceAliasesAndReturnsCanonicalIdentity()
    {
        string aliasNamespace = Iso20022Namespaces.SchemaUrnPrefix + "PACS.008.001.10";
        string xml = "<Document xmlns='" + aliasNamespace + "'/>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectText(xml, new MessageReadOptions { NamespaceMode = NamespaceMode.Lenient });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.MessageNamespace?.Uri, Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(result.NamespaceRepairs, Has.Count.EqualTo(1));
            Assert.That(result.NamespaceRepairs[0].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeBusinessAreaCase));
        });
    }

    [Test]
    public void EnvelopeNameNamespaceConflictsAndInvalidPayloadCountsAreReported()
    {
        string conflictingEnvelope = "<e:BizFileEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'/>";
        string twoPayloads = "<e:BizMsgEnvlp xmlns:e='" + Namespace("nvlp.001.001.02") + "'>" +
            Document("pacs.008.001.10") + Document("camt.053.001.13") + "</e:BizMsgEnvlp>";
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult conflict = detector.DetectText(conflictingEnvelope, new MessageReadOptions());
        MessageDetectionResult count = detector.DetectText(twoPayloads, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Success, Is.False);
            Assert.That(conflict.ErrorCode, Is.EqualTo("detection.envelope_conflict"));
            Assert.That(count.Success, Is.False);
            Assert.That(count.ErrorCode, Is.EqualTo("detection.envelope_payload_count"));
        });
    }

    [Test]
    public void SeekableStreamPositionAndIdentityAreRestoredAfterDetection()
    {
        string xml = Document("pacs.008.001.10");
        byte[] prefix = Encoding.UTF8.GetBytes("xx" + xml);
        MemoryStream stream = new MemoryStream(prefix, false);
        stream.Position = 2;
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectStream(stream, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.InputWasSeekable, Is.True);
            Assert.That(result.OriginalStreamPosition, Is.EqualTo(2));
            Assert.That(result.ReplayStream, Is.SameAs(stream));
            Assert.That(stream.Position, Is.EqualTo(2));
            Assert.That(ReadRemainingText(result.ReplayStream), Is.EqualTo(xml));
            Assert.That(Encoding.UTF8.GetString(result.PreservedBytes), Is.EqualTo(xml));
        });
    }

    [Test]
    public void SeeklessStreamReturnsAReplayCopyContainingEveryConsumedByte()
    {
        string xml = Document("pacs.008.001.10");
        NonSeekableReadStream stream = new NonSeekableReadStream(Encoding.UTF8.GetBytes(xml));
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectStream(stream, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.InputWasSeekable, Is.False);
            Assert.That(result.OriginalStreamPosition, Is.Zero);
            Assert.That(result.ReplayStream, Is.Not.Null);
            Assert.That(result.ReplayStream, Is.Not.SameAs(stream));
            Assert.That(result.ReplayStream?.CanSeek, Is.True);
            Assert.That(ReadRemainingText(result.ReplayStream), Is.EqualTo(xml));
            Assert.That(Encoding.UTF8.GetString(result.PreservedBytes), Is.EqualTo(xml));
            Assert.That(stream.ReadCallCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public void SeeklessStructuralDetectionFailureStillReturnsEveryByteForReplay()
    {
        string xml = "<Unknown xmlns='urn:unknown'/>";
        NonSeekableReadStream stream = new NonSeekableReadStream(Encoding.UTF8.GetBytes(xml));
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult result = detector.DetectStream(stream, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("detection.root"));
            Assert.That(ReadRemainingText(result.ReplayStream), Is.EqualTo(xml));
            Assert.That(Encoding.UTF8.GetString(result.PreservedBytes), Is.EqualTo(xml));
        });
    }

    [Test]
    public void StreamBufferingEnforcesTheConfiguredTotalByteLimit()
    {
        string xml = Document("pacs.008.001.10");
        NonSeekableReadStream stream = new NonSeekableReadStream(Encoding.UTF8.GetBytes(xml));
        MessageReadOptions options = new MessageReadOptions();
        options.Limits.MaxTotalBytes = Encoding.UTF8.GetByteCount(xml) - 1;
        IMessageDetector detector = new MessageDetector();

        XmlException? exception = Assert.Throws<XmlException>(() => detector.DetectStream(stream, options));

        Assert.That(exception?.Message, Does.Contain("MaxTotalBytes"));
    }

    [Test]
    public void NullInputsAndOptionsReturnStableFailures()
    {
        IMessageDetector detector = new MessageDetector();

        MessageDetectionResult nullBytes = detector.DetectBytes(null, new MessageReadOptions());
        MessageDetectionResult nullStream = detector.DetectStream(null, new MessageReadOptions());
        MessageDetectionResult nullText = detector.DetectText(null, new MessageReadOptions());
        MessageDetectionResult nullByteOptions = detector.DetectBytes([], null);
        MessageDetectionResult nullStreamOptions = detector.DetectStream(
            new MemoryStream(), null);
        MessageDetectionResult nullOptions = detector.DetectText("<root/>", null);

        Assert.Multiple(() =>
        {
            Assert.That(nullBytes.ErrorCode, Is.EqualTo("detection.input_null"));
            Assert.That(nullStream.ErrorCode, Is.EqualTo("detection.input_null"));
            Assert.That(nullText.ErrorCode, Is.EqualTo("detection.input_null"));
            Assert.That(nullOptions.ErrorCode, Is.EqualTo("detection.options_null"));
            Assert.That(nullByteOptions.ErrorCode, Is.EqualTo("detection.options_null"));
            Assert.That(nullStreamOptions.ErrorCode, Is.EqualTo("detection.options_null"));
            Assert.That(nullBytes.Success, Is.False);
            Assert.That(nullOptions.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void StructuralBoundaryCasesReturnStableDetectionFailuresAndRepairs()
    {
        DynamicXmlNode none = new DynamicXmlNode();
        DynamicXmlNode three = new DynamicXmlNode();
        three.Children.Add(Element("a", "urn:a"));
        three.Children.Add(Element("b", "urn:b"));
        three.Children.Add(Element("c", "urn:c"));
        MessageReadOptions lenient = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient
        };
        string inferred = "<Outer>" + Document("pacs.008.001.10") + "</Outer>";
        MessageReadOptions repairedWrapper = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient,
            CustomWrapperNames = new List<QualifiedNameData>
            {
                new QualifiedNameData { LocalName = "Batch", NamespaceUri = "urn:expected" }
            }
        };
        string repaired = "<Batch xmlns='urn:actual'>" +
            Document("pacs.008.001.10") + "</Batch>";
        string emptyWrapper = "<Batch xmlns='urn:expected'/>";
        string badPayload = "<Batch xmlns='urn:expected'><Document xmlns='urn:bad'/></Batch>";
        string badHeader = "<Batch xmlns='urn:expected'><AppHdr xmlns='urn:bad'/></Batch>";

        MessageDetectionResult noneResult = MessageDetector.DetectDocument(
            none, new MessageReadOptions());
        MessageDetectionResult countResult = MessageDetector.DetectDocument(
            three, new MessageReadOptions());
        MessageDetectionResult inferredResult = new MessageDetector().DetectText(inferred, lenient);
        MessageDetectionResult repairedResult = new MessageDetector().DetectText(
            repaired, repairedWrapper);
        MessageDetectionResult emptyResult = new MessageDetector().DetectText(
            emptyWrapper, repairedWrapper);
        MessageDetectionResult badPayloadResult = new MessageDetector().DetectText(
            badPayload, repairedWrapper);
        MessageDetectionResult badHeaderResult = new MessageDetector().DetectText(
            badHeader, repairedWrapper);

        Assert.Multiple(() =>
        {
            Assert.That(noneResult.ErrorCode, Is.EqualTo("detection.no_root"));
            Assert.That(countResult.ErrorCode, Is.EqualTo("detection.root_count"));
            Assert.That(inferredResult.Repairs.Select(item => item.Code),
                Does.Contain("wrapper.inferred"));
            Assert.That(repairedResult.Repairs.Select(item => item.Code),
                Does.Contain("wrapper.namespace_repaired"));
            Assert.That(emptyResult.ErrorCode, Is.EqualTo("detection.wrapper_empty"));
            Assert.That(badPayloadResult.ErrorCode,
                Is.EqualTo("detection.message_namespace"));
            Assert.That(badHeaderResult.ErrorCode,
                Is.EqualTo("detection.header_namespace"));
        });
    }

    [Test]
    public void DetectorHelpersCoverMismatchCountsSearchesAndRepairLimits()
    {
        MessageDetectionResult mismatch = new MessageDetectionResult();
        mismatch.PayloadIdentifiers.Add(Identifier("pacs.008.001.10"));
        mismatch.DeclaredMessageIdentifiers.Add(Identifier("pacs.008.001.10"));
        mismatch.DeclaredMessageIdentifiers.Add(Identifier("camt.053.001.13"));
        MessageDetector.ValidateIdentifierCoherence(mismatch);
        QualifiedNameData root = new QualifiedNameData
        {
            LocalName = "Batch",
            NamespaceUri = "urn:actual"
        };
        List<QualifiedNameData> names = new List<QualifiedNameData>
        {
            new QualifiedNameData { LocalName = "Other", NamespaceUri = "urn:other" },
            new QualifiedNameData { LocalName = "Batch", NamespaceUri = "urn:expected" }
        };
        ResourceLimits noDiagnostics = new ResourceLimits { MaxDiagnosticCount = 0 };

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.ErrorCode, Is.EqualTo("detection.identifier_count"));
            Assert.That(MessageDetector.IsConfiguredWrapper(root, names), Is.False);
            Assert.That(MessageDetector.FindConfiguredWrapperByLocalName(root, names),
                Is.SameAs(names[1]));
            Assert.That(MessageDetector.FindConfiguredWrapperByLocalName(
                new QualifiedNameData { LocalName = "Missing" }, names), Is.Null);
            Assert.That(MessageDetector.IsSiblingPair(
                Element("x", ""), Element("y", "")), Is.False);
            Assert.That(MessageDetector.IsBusinessMessageEnvelopeName(
                "BusinessMessageEnvelope"), Is.True);
            Assert.That(MessageDetector.IsBusinessFileEnvelopeName(
                "BusinessFileEnvelope"), Is.True);
            Assert.Throws<XmlException>(() => MessageDetector.AddMessageRepair(
                new MessageDetectionResult(), new MessageRepair(), noDiagnostics));
        });
    }

    [Test]
    public void LenientSiblingsAndInvalidContainedNamespacesCoverDetectionBoundaries()
    {
        IMessageDetector detector = new MessageDetector();
        MessageReadOptions lenient = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient
        };
        MessageDetectionResult sibling = detector.DetectText(
            Header("pacs.008.001.10") + Document("pacs.008.001.10"), lenient);
        MessageDetectionResult badSiblingHeader = detector.DetectText(
            "<AppHdr xmlns='urn:bad'/>" + Document("pacs.008.001.10"), lenient);
        MessageDetectionResult wrongHeaderKind = detector.DetectText(
            "<AppHdr xmlns='" + Namespace("pacs.008.001.10") + "'/>" +
            Document("pacs.008.001.10"), lenient);
        MessageDetectionResult badSiblingPayload = detector.DetectText(
            Header("pacs.008.001.10") + "<Document xmlns='urn:bad'/>", lenient);
        MessageDetectionResult wrongPayloadKind = detector.DetectText(
            Header("pacs.008.001.10") +
            "<Document xmlns='" + Namespace("head.001.001.04") + "'/>", lenient);
        string envelopeNamespace = Namespace("nvlp.001.001.02");
        MessageDetectionResult badEnvelopeChild = detector.DetectText(
            "<e:BizMsgEnvlp xmlns:e='" + envelopeNamespace +
            "'><e:Container><Document xmlns='urn:bad'/></e:Container></e:BizMsgEnvlp>",
            lenient);
        MessageDetectionResult malformedNamespace = detector.DetectText(
            "<Unknown xmlns='urn:iso:std:iso:20022:tech:xsd:bad'/>", lenient);
        MessageDetectionResult cdataIdentifier = detector.DetectText(
            "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") +
            "'><h:MsgDefIdr><![CDATA[pacs.008.001.10]]></h:MsgDefIdr></h:AppHdr>",
            new MessageReadOptions());
        MessageDetectionResult inferredHeader = detector.DetectText(
            "<Outer>" + Header("pacs.008.001.10") + "</Outer>", lenient);
        string malformedIsoNamespace = Namespace("bad");
        MessageDetectionResult strictBadHeader = detector.DetectText(
            "<AppHdr xmlns='" + malformedIsoNamespace + "'/>" +
            Document("pacs.008.001.10"),
            new MessageReadOptions());
        MessageDetectionResult strictBadPayload = detector.DetectText(
            Header("pacs.008.001.10") + "<Document xmlns='" +
            malformedIsoNamespace + "'/>",
            new MessageReadOptions());
        MessageDetectionResult recursiveBadHeader = detector.DetectText(
            "<Outer><Container><AppHdr xmlns='" + malformedIsoNamespace +
            "'/></Container></Outer>", lenient);
        MessageDetectionResult inferredBadPayload = detector.DetectText(
            "<Outer><Document xmlns='" + malformedIsoNamespace + "'/></Outer>", lenient);

        Assert.Multiple(() =>
        {
            Assert.That(sibling.Repairs.Select(item => item.Code),
                Does.Contain("xml.sibling_roots_wrapped"));
            Assert.That(badSiblingHeader.ErrorCode,
                Is.EqualTo("detection.header_namespace"));
            Assert.That(wrongHeaderKind.ErrorCode,
                Is.EqualTo("detection.header_namespace"));
            Assert.That(badSiblingPayload.ErrorCode,
                Is.EqualTo("detection.message_namespace"));
            Assert.That(wrongPayloadKind.ErrorCode,
                Is.EqualTo("detection.message_namespace"));
            Assert.That(badEnvelopeChild.ErrorCode,
                Is.EqualTo("detection.message_namespace"));
            Assert.That(malformedNamespace.Success, Is.False);
            Assert.That(cdataIdentifier.DeclaredMessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(inferredHeader.HeaderNamespace?.Kind, Is.EqualTo(NamespaceKind.Header));
            Assert.Throws<InvalidOperationException>(() =>
                MessageDetector.RequireNamespaceDescriptor(new NamespaceServiceResult()));
            Assert.That(strictBadHeader.ErrorCode,
                Is.EqualTo("detection.header_namespace"));
            Assert.That(strictBadPayload.ErrorCode,
                Is.EqualTo("detection.message_namespace"));
            Assert.That(recursiveBadHeader.ErrorCode, Is.EqualTo("detection.namespace"));
            Assert.That(inferredBadPayload.ErrorCode, Is.EqualTo("detection.namespace"));
        });
    }

    private static MessageIdentifier Identifier(string value)
    {
        MessageIdentifier identifier = MessageIdentifierLexical.ParseMessageIdentifier(
            value.AsSpan()) ?? throw new AssertionException("Invalid test identifier.");
        return identifier;
    }

    private static DynamicXmlNode Element(string localName, string namespaceUri)
    {
        return new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData
            {
                LocalName = localName,
                NamespaceUri = namespaceUri
            }
        };
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    internal static string Document(string identifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'/>";
    }

    internal static string Header(string declaredIdentifier)
    {
        return "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:MsgDefIdr>" + declaredIdentifier + "</h:MsgDefIdr></h:AppHdr>";
    }

    internal static string ReadRemainingText(Stream? stream)
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

        internal int ReadCallCount;

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
            ReadCallCount++;
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
}
