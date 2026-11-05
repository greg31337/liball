using System.Text;
using iso20022;

namespace iso20022.Tests;

public class XmlMessageReaderTests
{
    [Test]
    public void EveryReadEntryPointReportsMissingInputOptionsAndExactIdentifier()
    {
        IXmlMessageReader reader = new XmlMessageReader();
        byte[] bytes = "<x/>"u8.ToArray();
        MemoryStream stream = new MemoryStream(bytes);
        MessageIdentifier identifier = new MessageIdentifier
        {
            Value = "pacs.008.001.10"
        };

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadGenericBytes(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadGenericBytes(bytes, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadGenericStream(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadGenericStream(stream, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadGenericText(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadGenericText("<x/>", null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadAutoBytes(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadAutoBytes(bytes, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadAutoStream(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadAutoStream(stream, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadHeaderBytes(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadHeaderBytes(bytes, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadHeaderStream(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadHeaderStream(stream, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadHeaderText(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadHeaderText("<x/>", null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadMetadataBytes(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadMetadataBytes(bytes, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadMetadataStream(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadMetadataStream(stream, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadMetadataText(null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadMetadataText("<x/>", null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadExactBytes(bytes, null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.exact_identifier_null"));
            Assert.That(reader.ReadExactBytes(null, identifier, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadExactBytes(bytes, identifier, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadExactStream(stream, null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.exact_identifier_null"));
            Assert.That(reader.ReadExactStream(null, identifier, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadExactStream(stream, identifier, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
            Assert.That(reader.ReadExactText("<x/>", null, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.exact_identifier_null"));
            Assert.That(reader.ReadExactText(null, identifier, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.input_null"));
            Assert.That(reader.ReadExactText("<x/>", identifier, null).ErrorCode,
                Is.EqualTo("reader.options_null"));
        });
    }

    [Test]
    public void ReaderHelpersReportEmptyRootsSiblingRepairLimitsAndIdentifierConflicts()
    {
        DynamicXmlNode emptyTree = new DynamicXmlNode();
        DynamicXmlNode siblingTree = new DynamicXmlNode();
        siblingTree.Children.Add(new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "a" }
        });
        siblingTree.Children.Add(new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "b" }
        });
        MessageReadOptions lenient = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient
        };
        MessageReadResult limited = new MessageReadResult();
        ResourceLimits limits = new ResourceLimits { MaxDiagnosticCount = 0 };
        DynamicXmlNode attributed = new DynamicXmlNode();
        attributed.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData { LocalName = "other" },
            Value = "first"
        });
        attributed.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData { LocalName = "wanted" },
            Value = "second"
        });
        MessageIdentifier nullableFields = new MessageIdentifier();
        nullableFields.Value = null!;
        nullableFields.BusinessArea = null!;
        nullableFields.MessageFunctionality = null!;
        nullableFields.Variant = null!;
        nullableFields.Version = null!;
        MessageDetectionResult emptyDetection = new MessageDetectionResult();
        MessageDetectionResult messageDetection = new MessageDetectionResult
        {
            MessageNamespace = new NamespaceDescriptor { Uri = "urn:message" }
        };
        MessageMetadata emptyMetadata = new MessageMetadata();
        XmlMessageReader.PopulateMetadata(emptyMetadata, siblingTree, emptyDetection);
        MessageMetadata messageMetadata = new MessageMetadata();
        XmlMessageReader.PopulateMetadata(messageMetadata, siblingTree, messageDetection);
        DynamicXmlNode invalidAmountTree = ElementWithAmount("bad", "USD");
        DynamicXmlNode missingCurrencyTree = ElementWithAmount("1.00", null);
        XmlMessageReader.PopulateMetadata(
            new MessageMetadata(), invalidAmountTree, emptyDetection);
        XmlMessageReader.PopulateMetadata(
            new MessageMetadata(), missingCurrencyTree, emptyDetection);

        Assert.Multiple(() =>
        {
            Assert.That(XmlMessageReader.BuildGenericResult(
                emptyTree, 0, new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.no_root"));
            Assert.That(XmlMessageReader.BuildGenericResult(
                siblingTree, 0, lenient).Repairs.Select(item => item.Code),
                Does.Contain("xml.multiple_roots_wrapped"));
            Assert.Throws<System.Xml.XmlException>(() => XmlMessageReader.AddSingleRepair(
                limited, new MessageRepair(), limits));
            Assert.That(XmlMessageReader.FindAttributeValue(
                siblingTree.Children[0], "missing"), Is.Null);
            Assert.That(XmlMessageReader.FindAttributeValue(attributed, "wanted"),
                Is.EqualTo("second"));
            Assert.That(XmlMessageReader.NormalizeExpectedIdentifier(new MessageIdentifier
            {
                Value = "invalid"
            }), Is.Null);
            Assert.That(XmlMessageReader.NormalizeExpectedIdentifier(new MessageIdentifier
            {
                Value = "pacs.008.001.10",
                BusinessArea = "camt"
            }), Is.Null);
            Assert.That(XmlMessageReader.CreateInputFailure(false, false).ErrorCode,
                Is.EqualTo("reader.input"));
            Assert.That(XmlMessageReader.NormalizeExpectedIdentifier(nullableFields), Is.Null);
            Assert.That(XmlMessageReader.BuildExactResult(
                emptyTree, 0, new MessageIdentifier { Value = "pacs.008.001.10" },
                new MessageReadOptions()).Success, Is.False);
            Assert.That(XmlMessageReader.CreateHeader(
                siblingTree.Children[0], emptyDetection).Identifier, Is.Null);
            Assert.That(XmlMessageReader.CreateHeader(
                siblingTree.Children[0], emptyDetection).BusinessMessageIdentifier, Is.Empty);
            Assert.That(XmlMessageReader.BuildExactResult(
                ReadTree("<AppHdr xmlns='" + Namespace("head.001.001.04") + "'/>") ,
                0, new MessageIdentifier { Value = "pacs.008.001.10" },
                new MessageReadOptions()).ErrorCode,
                Is.EqualTo("reader.exact_identifier_mismatch"));
            Assert.That(emptyMetadata.NamespaceUri, Is.Empty);
            Assert.That(messageMetadata.NamespaceUri, Is.EqualTo("urn:message"));
            Assert.Throws<InvalidOperationException>(() =>
                XmlMessageReader.RequireNamespaceDescriptor(new NamespaceServiceResult()));
        });
    }

    private static DynamicXmlNode ElementWithAmount(string value, string? currency)
    {
        DynamicXmlNode root = new DynamicXmlNode();
        DynamicXmlNode amount = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "IntrBkSttlmAmt" }
        };
        amount.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = value });
        if (currency != null)
        {
            amount.Attributes.Add(new XmlAttributeData
            {
                Name = new QualifiedNameData { LocalName = "Ccy" },
                Value = currency
            });
        }

        root.Children.Add(amount);
        return root;
    }

    private static DynamicXmlNode ReadTree(string xml)
    {
        return new GenericXmlNodeReader(new MessageReadOptions()).ReadDocumentFromText(xml, null);
    }

    [Test]
    public void GenericEntryPointsPreserveUnknownXmlWithoutRequiringDetection()
    {
        string xml = "<x:Root xmlns:x='urn:unknown:model' id='17'>before<x:Child/>after</x:Root>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult textResult = reader.ReadGenericText(xml, new MessageReadOptions());
        MessageReadResult byteResult = reader.ReadGenericBytes(bytes, new MessageReadOptions());
        MemoryStream stream = new MemoryStream(bytes, false);
        MessageReadResult streamResult = reader.ReadGenericStream(stream, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(textResult.Success, Is.True);
            Assert.That(textResult.Document?.Layout, Is.EqualTo(EnvelopeKind.Unknown));
            Assert.That(textResult.Document?.Payloads, Has.Count.EqualTo(1));
            Assert.That(textResult.Document?.Payloads[0].Namespace?.Kind, Is.EqualTo(NamespaceKind.Unknown));
            Assert.That(textResult.Document?.Payloads[0].GenericContent?.Name.LocalName, Is.EqualTo("Root"));
            Assert.That(textResult.Document?.Payloads[0].GenericContent?.Attributes[0].Value, Is.EqualTo("17"));
            Assert.That(textResult.Document?.Payloads[0].GenericContent?.Children.Select(node => node.Kind), Is.EqualTo(new[] { XmlNodeKind.Text, XmlNodeKind.Element, XmlNodeKind.Text }));
            Assert.That(textResult.BytesConsumed, Is.EqualTo(bytes.Length));
            Assert.That(textResult.EndOfInput, Is.True);
            Assert.That(byteResult.Success, Is.True);
            Assert.That(byteResult.BytesConsumed, Is.EqualTo(bytes.Length));
            Assert.That(streamResult.Success, Is.True);
            Assert.That(streamResult.BytesConsumed, Is.EqualTo(bytes.Length));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void AutoEntryPointsMaterializeDetectedHeaderPayloadAndMetadataConsistently()
    {
        string xml = RichSiblingMessage();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult textResult = reader.ReadAutoText(xml, new MessageReadOptions());
        MessageReadResult byteResult = reader.ReadAutoBytes(bytes, new MessageReadOptions());
        MessageReadResult streamResult = reader.ReadAutoStream(new MemoryStream(bytes, false), new MessageReadOptions());

        AssertAutoDocument(textResult, bytes.Length);
        AssertAutoDocument(byteResult, bytes.Length);
        AssertAutoDocument(streamResult, bytes.Length);
    }

    [Test]
    public void ExactVersionEntryPointsAcceptValueOnlyOrComponentIdentifiersAndRejectMismatches()
    {
        string xml = RichSiblingMessage();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        MessageIdentifier valueOnly = new MessageIdentifier { Value = "pacs.008.001.10" };
        MessageIdentifier components = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10"
        };
        MessageIdentifier mismatch = new MessageIdentifier { Value = "pacs.008.001.09" };
        MessageIdentifier conflict = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "09",
            Value = "pacs.008.001.10"
        };
        MessageIdentifier runtimeNull = new MessageIdentifier
        {
            BusinessArea = null!,
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10"
        };
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult textResult = reader.ReadExactText(xml, valueOnly, new MessageReadOptions());
        MessageReadResult byteResult = reader.ReadExactBytes(bytes, components, new MessageReadOptions());
        MessageReadResult streamResult = reader.ReadExactStream(new MemoryStream(bytes, false), valueOnly, new MessageReadOptions());
        MessageReadResult mismatchResult = reader.ReadExactText(xml, mismatch, new MessageReadOptions());
        MessageReadResult conflictResult = reader.ReadExactText(xml, conflict, new MessageReadOptions());
        MessageReadResult runtimeNullResult = reader.ReadExactText(xml, runtimeNull, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(textResult.Success, Is.True);
            Assert.That(byteResult.Success, Is.True);
            Assert.That(streamResult.Success, Is.True);
            Assert.That(textResult.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(mismatchResult.Success, Is.False);
            Assert.That(mismatchResult.ErrorCode, Is.EqualTo("reader.exact_identifier_mismatch"));
            Assert.That(conflictResult.Success, Is.False);
            Assert.That(conflictResult.ErrorCode, Is.EqualTo("reader.exact_identifier_invalid"));
            Assert.That(runtimeNullResult.Success, Is.False);
            Assert.That(runtimeNullResult.ErrorCode, Is.EqualTo("reader.exact_identifier_invalid"));
        });
    }

    [Test]
    public void HeaderOnlyEntryPointsRetainHeaderAndMetadataButDoNotExposePayloadTrees()
    {
        string xml = RichSiblingMessage();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult textResult = reader.ReadHeaderText(xml, new MessageReadOptions());
        MessageReadResult byteResult = reader.ReadHeaderBytes(bytes, new MessageReadOptions());
        MessageReadResult streamResult = reader.ReadHeaderStream(new MemoryStream(bytes, false), new MessageReadOptions());

        AssertHeaderOnly(textResult);
        AssertHeaderOnly(byteResult);
        AssertHeaderOnly(streamResult);
    }

    [Test]
    public void MetadataOnlyEntryPointsReturnBusinessValuesWithoutHeaderPayloadOrEnvelopeTrees()
    {
        string xml = RichSiblingMessage();
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult textResult = reader.ReadMetadataText(xml, new MessageReadOptions());
        MessageReadResult byteResult = reader.ReadMetadataBytes(bytes, new MessageReadOptions());
        MessageReadResult streamResult = reader.ReadMetadataStream(new MemoryStream(bytes, false), new MessageReadOptions());

        AssertMetadataOnly(textResult);
        AssertMetadataOnly(byteResult);
        AssertMetadataOnly(streamResult);
    }

    [Test]
    public void AutoReadingMaterializesBusinessFileEnvelopeAndEveryPayloadInOrder()
    {
        string first = "<e:Message>" + Header("pacs.008.001.10", "BUS-1") + Document("pacs.008.001.10", "MSG-1") + "</e:Message>";
        string second = "<e:Message>" + Header("camt.053.001.13", "BUS-2") + Document("camt.053.001.13", "MSG-2") + "</e:Message>";
        string xml = "<e:BizFileEnvlp xmlns:e='" + Namespace("nvlp.002.001.02") + "'>" + first + second + "</e:BizFileEnvlp>";
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult result = reader.ReadAutoText(xml, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document?.Layout, Is.EqualTo(EnvelopeKind.BusinessFileEnvelope));
            Assert.That(result.Document?.Envelope?.Kind, Is.EqualTo(EnvelopeKind.BusinessFileEnvelope));
            Assert.That(result.Document?.Envelope?.GenericContent?.Name.LocalName, Is.EqualTo("BizFileEnvlp"));
            Assert.That(result.Document?.Payloads.Select(payload => payload.Identifier?.Value), Is.EqualTo(new[] { "pacs.008.001.10", "camt.053.001.13" }));
            Assert.That(result.Document?.Envelope?.Payloads, Is.SameAs(result.Document?.Payloads));
        });
    }

    [Test]
    public void LenientAutoAndGenericReadsExposeNamespaceRepairs()
    {
        string xml = "<Document xmlns='" + Iso20022Namespaces.SchemaUrnPrefix + "PACS.008.001.10'/>";
        MessageReadOptions options = new MessageReadOptions { NamespaceMode = NamespaceMode.Lenient };
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult autoResult = reader.ReadAutoText(xml, options);
        MessageReadResult genericResult = reader.ReadGenericText(xml, options);

        Assert.Multiple(() =>
        {
            Assert.That(autoResult.Success, Is.True);
            Assert.That(autoResult.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(autoResult.Repairs, Has.Count.EqualTo(1));
            Assert.That(autoResult.Repairs[0].OriginalValue, Does.Contain("PACS"));
            Assert.That(autoResult.Repairs[0].RepairedValue, Does.Contain("pacs"));
            Assert.That(genericResult.Success, Is.True);
            Assert.That(genericResult.Repairs, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DetectionAndInputFailuresReturnStableReadResults()
    {
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult unknown = reader.ReadAutoText("<Unknown xmlns='urn:unknown'/>", new MessageReadOptions());
        MessageReadResult nullText = reader.ReadAutoText(null, new MessageReadOptions());
        MessageReadResult nullOptions = reader.ReadGenericText("<Root/>", null);
        MessageReadResult nullExact = reader.ReadExactText("<Root/>", null, new MessageReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.ErrorCode, Is.EqualTo("detection.root"));
            Assert.That(unknown.EndOfInput, Is.True);
            Assert.That(nullText.ErrorCode, Is.EqualTo("reader.input_null"));
            Assert.That(nullOptions.ErrorCode, Is.EqualTo("reader.options_null"));
            Assert.That(nullExact.ErrorCode, Is.EqualTo("reader.exact_identifier_null"));
        });
    }

    internal static void AssertAutoDocument(MessageReadResult result, int expectedBytes)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document?.Layout, Is.EqualTo(EnvelopeKind.SiblingHeaderAndDocument));
            Assert.That(result.Document?.Header?.Identifier?.Value, Is.EqualTo("head.001.001.04"));
            Assert.That(result.Document?.Header?.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-17"));
            Assert.That(result.Document?.Header?.BusinessService, Is.EqualTo("swift.cbprplus.03"));
            Assert.That(result.Document?.Header?.GenericContent?.Name.LocalName, Is.EqualTo("AppHdr"));
            Assert.That(result.Document?.Payloads, Has.Count.EqualTo(1));
            Assert.That(result.Document?.Payloads[0].Identifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.Document?.Payloads[0].GenericContent?.Name.LocalName, Is.EqualTo("Document"));
            Assert.That(result.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.Document?.Metadata.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-17"));
            Assert.That(result.Document?.Metadata.BusinessService, Is.EqualTo("swift.cbprplus.03"));
            Assert.That(result.Document?.Metadata.Sender, Is.EqualTo("BANKUS33"));
            Assert.That(result.Document?.Metadata.Receiver, Is.EqualTo("BANKGB22"));
            Assert.That(result.Document?.Metadata.CreationDateTime?.LexicalValue, Is.EqualTo("2026-08-31T12:00:00Z"));
            Assert.That(result.Document?.Metadata.HasPossibleDuplicate, Is.True);
            Assert.That(result.Document?.Metadata.PossibleDuplicate, Is.False);
            Assert.That(result.Document?.Metadata.Priority, Is.EqualTo("HIGH"));
            Assert.That(result.Document?.Metadata.Uetr, Is.EqualTo("12345678-1234-1234-1234-123456789abc"));
            Assert.That(result.Document?.Metadata.PrimaryAmount?.Amount.Value, Is.EqualTo(123.450m));
            Assert.That(result.Document?.Metadata.PrimaryAmount?.Amount.Scale, Is.EqualTo(3));
            Assert.That(result.Document?.Metadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(result.Document?.Metadata.ValueDate?.LexicalValue, Is.EqualTo("2026-09-01"));
            Assert.That(result.Document?.Metadata.References, Is.EqualTo(new[] { "MSG-17", "INSTR-17", "END-17", "TX-17" }));
            Assert.That(result.BytesConsumed, Is.EqualTo(expectedBytes));
            Assert.That(result.EndOfInput, Is.True);
        });
    }

    internal static void AssertHeaderOnly(MessageReadResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document?.Header?.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-17"));
            Assert.That(result.Document?.Header?.GenericContent, Is.Not.Null);
            Assert.That(result.Document?.Payloads, Is.Empty);
            Assert.That(result.Document?.Envelope, Is.Null);
            Assert.That(result.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
        });
    }

    internal static void AssertMetadataOnly(MessageReadResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document?.Header, Is.Null);
            Assert.That(result.Document?.Payloads, Is.Empty);
            Assert.That(result.Document?.Envelope, Is.Null);
            Assert.That(result.Document?.Metadata.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-17"));
            Assert.That(result.Document?.Metadata.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(result.Document?.Metadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(result.Document?.Metadata.References, Does.Contain("END-17"));
        });
    }

    internal static string RichSiblingMessage()
    {
        return Header("pacs.008.001.10", "BUSINESS-17") +
            "<Document xmlns='" + Namespace("pacs.008.001.10") + "'><FIToFICstmrCdtTrf><GrpHdr><MsgId>MSG-17</MsgId><IntrBkSttlmDt>2026-09-01</IntrBkSttlmDt></GrpHdr><CdtTrfTxInf><PmtId><InstrId>INSTR-17</InstrId><EndToEndId>END-17</EndToEndId><TxId>TX-17</TxId><UETR>12345678-1234-1234-1234-123456789abc</UETR></PmtId><IntrBkSttlmAmt Ccy='USD'>123.450</IntrBkSttlmAmt></CdtTrfTxInf></FIToFICstmrCdtTrf></Document>";
    }

    internal static string Header(string identifier, string businessMessageIdentifier)
    {
        return "<h:AppHdr xmlns:h='" + Namespace("head.001.001.04") + "'><h:Fr><h:Id>BANKUS33</h:Id></h:Fr><h:To><h:Id>BANKGB22</h:Id></h:To><h:BizMsgIdr>" + businessMessageIdentifier + "</h:BizMsgIdr><h:MsgDefIdr>" + identifier + "</h:MsgDefIdr><h:BizSvc>swift.cbprplus.03</h:BizSvc><h:CreDt>2026-08-31T12:00:00Z</h:CreDt><h:PssblDplct>false</h:PssblDplct><h:Prty>HIGH</h:Prty></h:AppHdr>";
    }

    internal static string Document(string identifier, string messageIdentifier)
    {
        return "<Document xmlns='" + Namespace(identifier) + "'><MsgId>" + messageIdentifier + "</MsgId></Document>";
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }
}
