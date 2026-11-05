using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class MetadataExtractorTests
{
    [Test]
    public void ExtractsHeaderIdentityPaymentValuesAndOrderedReferences()
    {
        MessageDocument document = ReadDocument(CompleteBusinessMessage());
        MetadataExtractionOptions options = new MetadataExtractionOptions();
        XmlPathData additional = new XmlPathData();
        string messageNamespace = Namespace("pacs.008.001.10");
        additional.Segments.Add(Name(messageNamespace, "Document"));
        additional.Segments.Add(Name(messageNamespace, "FIToFICstmrCdtTrf"));
        additional.Segments.Add(Name(messageNamespace, "CdtTrfTxInf"));
        additional.Segments.Add(Name(messageNamespace, "CustomRef"));
        options.AdditionalReferencePaths.Add(additional);
        IMetadataExtractor extractor = new MetadataExtractor();

        MessageMetadata metadata = extractor.ExtractMetadata(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(metadata, Is.Not.SameAs(document.Metadata));
            Assert.That(metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(metadata.NamespaceUri, Is.EqualTo(messageNamespace));
            Assert.That(metadata.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-1"));
            Assert.That(metadata.BusinessService, Is.EqualTo("swift.cbprplus.03"));
            Assert.That(metadata.Sender, Is.EqualTo("BANKUS33XXX"));
            Assert.That(metadata.Receiver, Is.EqualTo("5493001KJTIIGC8Y1R12"));
            Assert.That(metadata.CreationDateTime?.LexicalValue,
                Is.EqualTo("2026-09-01T12:34:56.789Z"));
            Assert.That(metadata.HasPossibleDuplicate, Is.True);
            Assert.That(metadata.PossibleDuplicate, Is.True);
            Assert.That(metadata.Priority, Is.EqualTo("HIGH"));
            Assert.That(metadata.Uetr,
                Is.EqualTo("123e4567-e89b-12d3-a456-426614174000"));
            Assert.That(metadata.PrimaryAmount?.Amount.Value, Is.EqualTo(123.450m));
            Assert.That(metadata.PrimaryAmount?.Amount.Scale, Is.EqualTo(3));
            Assert.That(metadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(metadata.ValueDate?.LexicalValue, Is.EqualTo("2026-09-02"));
            Assert.That(metadata.References, Is.EqualTo(new[]
            {
                "MESSAGE-1",
                "INSTRUCTION-1",
                "END-TO-END-1",
                "TRANSACTION-1",
                "CUSTOM-1"
            }));
        });
    }

    [Test]
    public void HeaderAndPayloadPoliciesSelectOnlyTheirOwnMetadataFamilies()
    {
        MessageDocument document = ReadDocument(CompleteBusinessMessage());
        MetadataExtractionOptions payloadOnlyOptions = new MetadataExtractionOptions();
        payloadOnlyOptions.IncludeHeader = false;
        MetadataExtractionOptions headerOnlyOptions = new MetadataExtractionOptions();
        headerOnlyOptions.IncludePayload = false;
        IMetadataExtractor extractor = new MetadataExtractor();

        MessageMetadata payloadOnly = extractor.ExtractMetadata(
            document,
            payloadOnlyOptions);
        MessageMetadata headerOnly = extractor.ExtractMetadata(
            document,
            headerOnlyOptions);

        Assert.Multiple(() =>
        {
            Assert.That(payloadOnly.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(payloadOnly.BusinessMessageIdentifier, Is.Empty);
            Assert.That(payloadOnly.Sender, Is.Empty);
            Assert.That(payloadOnly.Uetr, Is.Not.Empty);
            Assert.That(payloadOnly.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(payloadOnly.References, Is.Not.Empty);
            Assert.That(headerOnly.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(headerOnly.BusinessMessageIdentifier, Is.EqualTo("BUSINESS-1"));
            Assert.That(headerOnly.Sender, Is.EqualTo("BANKUS33XXX"));
            Assert.That(headerOnly.Uetr, Is.Empty);
            Assert.That(headerOnly.PrimaryAmount, Is.Null);
            Assert.That(headerOnly.ValueDate, Is.Null);
            Assert.That(headerOnly.References, Is.Empty);
        });
    }

    [Test]
    public void AppliesAliasPrecedenceAndRedactsSensitiveValuesWithoutHidingTypeIdentity()
    {
        string messageNamespace = Namespace("pain.001.001.11");
        string xml = "<Document xmlns='" + messageNamespace + "'>" +
            "<CstmrCdtTrfInitn><PmtInf>" +
            "<PmtInfId>PAYMENT-INFO-1</PmtInfId>" +
            "<EqvtAmt Ccy='GBP'>99.00</EqvtAmt>" +
            "<InstdAmt Ccy='EUR'>10.2500</InstdAmt>" +
            "<ReqdExctnDt>2026-10-03</ReqdExctnDt>" +
            "</PmtInf></CstmrCdtTrfInitn></Document>";
        MessageDocument document = ReadDocument(xml);
        IMetadataExtractor extractor = new MetadataExtractor();
        MessageMetadata visible = extractor.ExtractMetadata(
            document,
            new MetadataExtractionOptions());
        MetadataExtractionOptions redaction = new MetadataExtractionOptions();
        redaction.RedactSensitiveValues = true;
        MessageMetadata redacted = extractor.ExtractMetadata(document, redaction);

        Assert.Multiple(() =>
        {
            Assert.That(visible.MessageIdentifier?.Value,
                Is.EqualTo("pain.001.001.11"));
            Assert.That(visible.PrimaryAmount?.Amount.Value, Is.EqualTo(10.2500m));
            Assert.That(visible.PrimaryAmount?.Amount.Scale, Is.EqualTo(4));
            Assert.That(visible.PrimaryAmount?.Currency, Is.EqualTo("EUR"));
            Assert.That(visible.ValueDate?.LexicalValue, Is.EqualTo("2026-10-03"));
            Assert.That(visible.References, Is.EqualTo(new[] { "PAYMENT-INFO-1" }));
            Assert.That(redacted.MessageIdentifier?.Value,
                Is.EqualTo("pain.001.001.11"));
            Assert.That(redacted.NamespaceUri, Is.EqualTo(messageNamespace));
            Assert.That(redacted.PrimaryAmount?.Amount.LexicalValue,
                Is.EqualTo("[REDACTED]"));
            Assert.That(redacted.PrimaryAmount?.Currency,
                Is.EqualTo("[REDACTED]"));
            Assert.That(redacted.ValueDate?.LexicalValue,
                Is.EqualTo("[REDACTED]"));
            Assert.That(redacted.References, Is.EqualTo(new[] { "[REDACTED]" }));
        });
    }

    [Test]
    public void EnforcesTraversalAndAdditionalReferencePathBounds()
    {
        MessageDocument document = ReadDocument(CompleteBusinessMessage());
        MetadataExtractionOptions elementLimit = new MetadataExtractionOptions();
        elementLimit.Limits.MaxElementCount = 4;
        MetadataExtractionOptions pathLimit = new MetadataExtractionOptions();
        pathLimit.Limits.MaxCollectionCount = 1;
        pathLimit.AdditionalReferencePaths.Add(new XmlPathData
        {
            Value = "/Document/First"
        });
        pathLimit.AdditionalReferencePaths.Add(new XmlPathData
        {
            Value = "/Document/Second"
        });
        IMetadataExtractor extractor = new MetadataExtractor();

        XmlException? elements = Assert.Throws<XmlException>(() =>
            extractor.ExtractMetadata(document, elementLimit));
        XmlException? paths = Assert.Throws<XmlException>(() =>
            extractor.ExtractMetadata(document, pathLimit));

        Assert.Multiple(() =>
        {
            Assert.That(elements?.Message, Does.Contain("MaxElementCount"));
            Assert.That(paths?.Message, Does.Contain("MaxCollectionCount"));
        });
    }

    [Test]
    public void ReturnsAbsentValuesWithoutInventingMetadata()
    {
        MessageDocument document = ReadDocument(
            "<Document xmlns='" + Namespace("camt.053.001.13") +
            "'><BkToCstmrStmt/></Document>");

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            document,
            new MetadataExtractionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MessageIdentifier?.Value,
                Is.EqualTo("camt.053.001.13"));
            Assert.That(metadata.NamespaceUri,
                Is.EqualTo(Namespace("camt.053.001.13")));
            Assert.That(metadata.BusinessMessageIdentifier, Is.Empty);
            Assert.That(metadata.BusinessService, Is.Empty);
            Assert.That(metadata.Sender, Is.Empty);
            Assert.That(metadata.Receiver, Is.Empty);
            Assert.That(metadata.CreationDateTime, Is.Null);
            Assert.That(metadata.HasPossibleDuplicate, Is.False);
            Assert.That(metadata.PossibleDuplicate, Is.False);
            Assert.That(metadata.Priority, Is.Empty);
            Assert.That(metadata.Uetr, Is.Empty);
            Assert.That(metadata.PrimaryAmount, Is.Null);
            Assert.That(metadata.ValueDate, Is.Null);
            Assert.That(metadata.References, Is.Empty);
        });
    }

    [Test]
    public void SelectsAmbiguousCandidatesDeterministically()
    {
        string messageNamespace = Namespace("pacs.008.001.10");
        string xml = "<Document xmlns='" + messageNamespace + "'>" +
            "<FIToFICstmrCdtTrf><GrpHdr>" +
            "<MsgId>MESSAGE-1</MsgId><MsgId>MESSAGE-2</MsgId>" +
            "<IntrBkSttlmDt>2026-09-02</IntrBkSttlmDt>" +
            "<IntrBkSttlmDt>2026-09-03</IntrBkSttlmDt>" +
            "</GrpHdr><CdtTrfTxInf><PmtId>" +
            "<TxId>TRANSACTION-1</TxId><TxId>TRANSACTION-1</TxId>" +
            "<UETR>11111111-1111-4111-8111-111111111111</UETR>" +
            "<UETR>22222222-2222-4222-8222-222222222222</UETR>" +
            "</PmtId>" +
            "<IntrBkSttlmAmt Ccy='USD'>10.00</IntrBkSttlmAmt>" +
            "<IntrBkSttlmAmt Ccy='EUR'>20.00</IntrBkSttlmAmt>" +
            "</CdtTrfTxInf></FIToFICstmrCdtTrf></Document>";

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            ReadDocument(xml),
            new MetadataExtractionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Uetr,
                Is.EqualTo("11111111-1111-4111-8111-111111111111"));
            Assert.That(metadata.PrimaryAmount?.Amount.Value, Is.EqualTo(10.00m));
            Assert.That(metadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(metadata.ValueDate?.LexicalValue,
                Is.EqualTo("2026-09-02"));
            Assert.That(metadata.References,
                Is.EqualTo(new[] { "MESSAGE-1", "MESSAGE-2", "TRANSACTION-1" }));
        });
    }

    [TestCase("pacs.008.001.02", "IntrBkSttlmAmt", "IntrBkSttlmDt")]
    [TestCase("pain.001.001.11", "InstdAmt", "ReqdExctnDt")]
    public void ExtractsVersionSpecificAmountAndDateAliases(
        string identifier,
        string amountName,
        string dateName)
    {
        string xml = "<Document xmlns='" + Namespace(identifier) + "'>" +
            "<Message><MsgId>VERSIONED-1</MsgId><" + amountName +
            " Ccy='CHF'>42.500</" + amountName + "><" + dateName +
            ">2026-11-05</" + dateName + "></Message></Document>";

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            ReadDocument(xml),
            new MetadataExtractionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MessageIdentifier?.Value, Is.EqualTo(identifier));
            Assert.That(metadata.PrimaryAmount?.Amount.Value, Is.EqualTo(42.500m));
            Assert.That(metadata.PrimaryAmount?.Amount.Scale, Is.EqualTo(3));
            Assert.That(metadata.PrimaryAmount?.Currency, Is.EqualTo("CHF"));
            Assert.That(metadata.ValueDate?.LexicalValue,
                Is.EqualTo("2026-11-05"));
            Assert.That(metadata.References, Is.EqualTo(new[] { "VERSIONED-1" }));
        });
    }

    [Test]
    public void RedactsEverySensitiveFieldAndPreservesOperationalMetadata()
    {
        MessageDocument document = ReadDocument(CompleteBusinessMessage());
        MetadataExtractionOptions options = new MetadataExtractionOptions();
        options.RedactSensitiveValues = true;

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(metadata.NamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(metadata.BusinessMessageIdentifier,
                Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.BusinessService, Is.EqualTo("swift.cbprplus.03"));
            Assert.That(metadata.Sender, Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.Receiver, Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.CreationDateTime?.LexicalValue,
                Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.HasPossibleDuplicate, Is.True);
            Assert.That(metadata.PossibleDuplicate, Is.True);
            Assert.That(metadata.Priority, Is.EqualTo("HIGH"));
            Assert.That(metadata.Uetr, Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.PrimaryAmount?.Amount.LexicalValue,
                Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.PrimaryAmount?.Currency,
                Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.ValueDate?.LexicalValue,
                Is.EqualTo("[REDACTED]"));
            Assert.That(metadata.References,
                Is.EqualTo(new[]
                {
                    "[REDACTED]",
                    "[REDACTED]",
                    "[REDACTED]",
                    "[REDACTED]"
                }));
        });
    }

    [Test]
    public void ExtractsKnownSemanticFieldsFromAnUnknownGenericModel()
    {
        const string unknownNamespace = "urn:example:payments:custom";
        string xml = "<VendorPayment xmlns='" + unknownNamespace + "'>" +
            "<MsgId>VENDOR-MESSAGE-1</MsgId>" +
            "<UETR>33333333-3333-4333-8333-333333333333</UETR>" +
            "<SettlementAmt Ccy='CAD'>7.25</SettlementAmt>" +
            "<ValDt>2026-12-01</ValDt></VendorPayment>";
        MessageReadResult read = new XmlMessageReader().ReadGenericText(
            xml,
            new MessageReadOptions());
        Assert.That(read.Success, Is.True, read.ErrorMessage);

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            read.Document ?? throw new AssertionException("Expected a generic document."),
            new MetadataExtractionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MessageIdentifier, Is.Null);
            Assert.That(metadata.NamespaceUri, Is.EqualTo(unknownNamespace));
            Assert.That(metadata.Uetr,
                Is.EqualTo("33333333-3333-4333-8333-333333333333"));
            Assert.That(metadata.PrimaryAmount?.Amount.Value, Is.EqualTo(7.25m));
            Assert.That(metadata.PrimaryAmount?.Currency, Is.EqualTo("CAD"));
            Assert.That(metadata.ValueDate?.LexicalValue,
                Is.EqualTo("2026-12-01"));
            Assert.That(metadata.References, Is.EqualTo(new[] { "VENDOR-MESSAGE-1" }));
        });
    }

    [Test]
    public void BodyIdentityWinsWhenTheHeaderDeclaresAConflictingMessage()
    {
        MessageDocument document = ReadDocument(CompleteBusinessMessage());
        document.Header = ReadHeader(
            "<AppHdr xmlns='" + Namespace("head.001.001.04") + "'>" +
            "<BizMsgIdr>HEADER-BUSINESS-1</BizMsgIdr>" +
            "<MsgDefIdr>camt.053.001.13</MsgDefIdr>" +
            "<BizSvc>conflicting.service</BizSvc></AppHdr>");

        MessageMetadata metadata = new MetadataExtractor().ExtractMetadata(
            document,
            new MetadataExtractionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(metadata.NamespaceUri,
                Is.EqualTo(Namespace("pacs.008.001.10")));
            Assert.That(metadata.BusinessMessageIdentifier,
                Is.EqualTo("HEADER-BUSINESS-1"));
            Assert.That(metadata.BusinessService,
                Is.EqualTo("conflicting.service"));
            Assert.That(metadata.Uetr,
                Is.EqualTo("123e4567-e89b-12d3-a456-426614174000"));
        });
    }

    [Test]
    public void MalformedOptionsRawContentAndFallbackHelpersHaveStableBehavior()
    {
        MetadataExtractor extractor = new MetadataExtractor();
        MetadataExtractionOptions noLimits = new MetadataExtractionOptions { Limits = null! };
        MetadataExtractionOptions noPaths = new MetadataExtractionOptions
            { AdditionalReferencePaths = null! };
        Assert.That(() => extractor.ExtractMetadata(new MessageDocument(), noLimits),
            Throws.InstanceOf<ArgumentException>());
        Assert.That(() => extractor.ExtractMetadata(new MessageDocument(), noPaths),
            Throws.InstanceOf<ArgumentException>());

        foreach (XmlPathData? path in new XmlPathData?[]
        {
            null,
            new XmlPathData { Segments = null! },
            new XmlPathData { Value = null! },
            new XmlPathData()
        })
        {
            MetadataExtractionOptions invalid = new MetadataExtractionOptions();
            invalid.AdditionalReferencePaths.Add(path!);
            Assert.That(() => extractor.ExtractMetadata(new MessageDocument(), invalid),
                Throws.InstanceOf<ArgumentException>());
        }

        MetadataExtractionOptions tooDeep = new MetadataExtractionOptions();
        tooDeep.Limits.MaxDepth = 0;
        XmlPathData deepPath = new XmlPathData();
        deepPath.Segments.Add(Name("urn:test", "x"));
        tooDeep.AdditionalReferencePaths.Add(deepPath);
        Assert.That(() => extractor.ExtractMetadata(new MessageDocument(), tooDeep),
            Throws.InstanceOf<ArgumentException>());
        MetadataExtractionOptions directDepth = new MetadataExtractionOptions();
        directDepth.Limits.MaxDepth = 1;
        XmlPathData twoSegments = new XmlPathData();
        twoSegments.Segments.Add(Name("urn:test", "one"));
        twoSegments.Segments.Add(Name("urn:test", "two"));
        directDepth.AdditionalReferencePaths.Add(twoSegments);
        Assert.That(() => MetadataExtractor.ValidateAdditionalPaths(
            directDepth, new ResourceLimitGuard(directDepth.Limits)),
            Throws.InstanceOf<ArgumentException>());

        string messageNamespace = Namespace("pacs.008.001.10");
        MessageDocument raw = new MessageDocument
        {
            Header = new MessageHeader
            {
                RawContent = new RawXmlFragment
                {
                    Xml = "<AppHdr xmlns='" + Namespace("head.001.001.04") +
                        "'><BizMsgIdr>RAW-H</BizMsgIdr></AppHdr>"
                }
            }
        };
        raw.Payloads.Add(new MessagePayload
        {
            RawContent = new RawXmlFragment
            {
                Xml = "<Document xmlns='" + messageNamespace +
                    "'><Amt Ccy='USD'>1.25</Amt></Document>",
                RootName = Name(messageNamespace, "Document")
            }
        });
        MessageMetadata rawMetadata = extractor.ExtractMetadata(
            raw, new MetadataExtractionOptions());

        MessageDocument fallback = new MessageDocument();
        fallback.Metadata.MessageIdentifier = new MessageIdentifier
            { Value = "pacs.008.001.10", BusinessArea = "pacs",
                MessageFunctionality = "008", Variant = "001", Version = "10" };
        fallback.Metadata.PrimaryAmount = new IsoAmountValue
        {
            Amount = new IsoDecimalValue { Value = 2.50m, Scale = 2 },
            Currency = "EUR"
        };
        fallback.Metadata.ValueDate = new IsoDateValue
            { Year = 2026, Month = 9, Day = 15 };
        MessageMetadata fallbackMetadata = extractor.ExtractMetadata(
            fallback, new MetadataExtractionOptions());

        DynamicXmlNode container = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        DynamicXmlNode text = new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "x" };
        DynamicXmlNode element = new DynamicXmlNode
            { Kind = XmlNodeKind.Element, Name = Name("urn:test", "e") };
        text.Children.Add(element);
        container.Children.Add(text);
        DynamicXmlNode invalidAmount = new DynamicXmlNode
            { Kind = XmlNodeKind.Element, Name = Name("urn:test", "Amt") };
        invalidAmount.Attributes.Add(new XmlAttributeData
            { Name = Name(string.Empty, "Ccy"), Value = "USD" });
        invalidAmount.Children.Add(new DynamicXmlNode
            { Kind = XmlNodeKind.Text, Value = "not-a-decimal" });

        Assert.Multiple(() =>
        {
            Assert.That(rawMetadata.BusinessMessageIdentifier, Is.EqualTo("RAW-H"));
            Assert.That(rawMetadata.NamespaceUri, Is.EqualTo(messageNamespace));
            Assert.That(rawMetadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(fallbackMetadata.NamespaceUri, Is.EqualTo(messageNamespace));
            Assert.That(fallbackMetadata.PrimaryAmount?.Amount.Value, Is.EqualTo(2.50m));
            Assert.That(fallbackMetadata.ValueDate?.Day, Is.EqualTo(15));
            Assert.That(MetadataExtractor.ResolveHeaderTree(null, new ResourceLimits()), Is.Null);
            Assert.That(MetadataExtractor.ResolvePayloadTree(null, new ResourceLimits()), Is.Null);
            Assert.That(MetadataExtractor.ResolveHeaderTree(new MessageHeader(),
                new ResourceLimits()), Is.Null);
            Assert.That(MetadataExtractor.ResolvePayloadTree(new MessagePayload(),
                new ResourceLimits()), Is.Null);
            Assert.That(MetadataExtractor.FindFirstElementNode(container), Is.SameAs(element));
            Assert.That(MetadataExtractor.FindFirstElementNode(
                new DynamicXmlNode { Kind = XmlNodeKind.Comment }), Is.Null);
            Assert.That(MetadataExtractor.ParseAmountNode(new DynamicXmlNode()), Is.Null);
            Assert.That(MetadataExtractor.ParseAmountNode(invalidAmount), Is.Null);
        });

        DynamicXmlNode identity = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = Name("urn:test", "Party")
        };
        foreach (string localName in new[] { "BIC", "LEI", "Id", "Nm" })
        {
            DynamicXmlNode valueNode = new DynamicXmlNode
            {
                Kind = XmlNodeKind.Element,
                Name = Name("urn:test", localName)
            };
            valueNode.Children.Add(new DynamicXmlNode
                { Kind = XmlNodeKind.Text, Value = localName + "-VALUE" });
            identity.Children.Clear();
            identity.Children.Add(valueNode);
            Assert.That(MetadataExtractor.PartyIdentity(identity, "Party"),
                Is.EqualTo(localName + "-VALUE"));
        }
        identity.Children.Clear();
        identity.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = " raw " });
        Assert.That(MetadataExtractor.PartyIdentity(identity, "Party"), Is.EqualTo("raw"));

        DynamicXmlNode amountContainer = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        amountContainer.Children.Add(invalidAmount);
        Assert.That(MetadataExtractor.FindAmountNamed(amountContainer, "Amt"), Is.Null);
        Assert.That(MetadataExtractor.FindAnyCurrencyAmount(amountContainer), Is.Null);
        DynamicXmlNode invalidDate = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = Name("urn:test", "IntrBkSttlmDt")
        };
        invalidDate.Children.Add(new DynamicXmlNode
            { Kind = XmlNodeKind.Text, Value = "not-a-date" });
        Assert.That(MetadataExtractor.FindPrimaryValueDate(invalidDate), Is.Null);

        MessageDocument structural = new MessageDocument();
        structural.Payloads.Add(new MessagePayload
        {
            RawContent = new RawXmlFragment
            {
                RootName = Name("urn:raw", "Document")
            }
        });
        MessageMetadata structuralMetadata = new MessageMetadata();
        MetadataExtractor.ExtractStructuralIdentity(structural, structuralMetadata);
        Assert.That(structuralMetadata.NamespaceUri, Is.EqualTo("urn:raw"));

        DynamicXmlNode genericRoot = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = Name("urn:generic", "Document")
        };
        MessageDocument genericStructural = new MessageDocument();
        genericStructural.Payloads.Add(new MessagePayload
        {
            Namespace = null!,
            GenericContent = genericRoot
        });
        MessageMetadata genericMetadata = new MessageMetadata();
        MetadataExtractor.ExtractStructuralIdentity(genericStructural, genericMetadata);
        Assert.That(genericMetadata.NamespaceUri, Is.EqualTo("urn:generic"));

        MessageDocument headerFallback = new MessageDocument
        {
            Header = new MessageHeader
            {
                BusinessMessageIdentifier = "FALLBACK-ID",
                BusinessService = "fallback.service"
            }
        };
        MessageMetadata fallbackTarget = new MessageMetadata();
        MetadataExtractor.ApplyHeaderFallback(headerFallback, fallbackTarget);
        Assert.Multiple(() =>
        {
            Assert.That(fallbackTarget.BusinessMessageIdentifier, Is.EqualTo("FALLBACK-ID"));
            Assert.That(fallbackTarget.BusinessService, Is.EqualTo("fallback.service"));
        });
        DynamicXmlNode noElement = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        noElement.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Comment });
        Assert.That(MetadataExtractor.FindFirstElementNode(noElement), Is.Null);
    }

    private static MessageDocument ReadDocument(string xml)
    {
        MessageReadResult result = new XmlMessageReader().ReadAutoText(
            xml,
            new MessageReadOptions());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.Document ?? throw new AssertionException("Expected a message document.");
    }

    private static MessageHeader ReadHeader(string xml)
    {
        MessageReadResult result = new XmlMessageReader().ReadGenericText(
            xml,
            new MessageReadOptions());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        MessagePayload payload = result.Document?.Payloads[0] ??
            throw new AssertionException("Expected generic header content.");
        return new MessageHeader
        {
            GenericContent = payload.GenericContent
        };
    }

    private static QualifiedNameData Name(string namespaceUri, string localName)
    {
        return new QualifiedNameData
        {
            NamespaceUri = namespaceUri,
            LocalName = localName
        };
    }

    private static string CompleteBusinessMessage()
    {
        return "<BizMsgEnvlp xmlns='" + Namespace("nvlp.001.001.02") + "'>" +
            "<AppHdr xmlns='" + Namespace("head.001.001.04") + "'>" +
            "<Fr><FIId><FinInstnId><BICFI>BANKUS33XXX</BICFI></FinInstnId></FIId></Fr>" +
            "<To><OrgId><LEI>5493001KJTIIGC8Y1R12</LEI></OrgId></To>" +
            "<BizMsgIdr>BUSINESS-1</BizMsgIdr>" +
            "<MsgDefIdr>pacs.008.001.10</MsgDefIdr>" +
            "<BizSvc>swift.cbprplus.03</BizSvc>" +
            "<CreDt>2026-09-01T12:34:56.789Z</CreDt>" +
            "<PssblDplct>true</PssblDplct><Prty>HIGH</Prty>" +
            "</AppHdr>" +
            "<Document xmlns='" + Namespace("pacs.008.001.10") + "'>" +
            "<FIToFICstmrCdtTrf>" +
            "<GrpHdr><MsgId>MESSAGE-1</MsgId>" +
            "<IntrBkSttlmDt>2026-09-02</IntrBkSttlmDt></GrpHdr>" +
            "<CdtTrfTxInf><PmtId>" +
            "<InstrId>INSTRUCTION-1</InstrId>" +
            "<EndToEndId>END-TO-END-1</EndToEndId>" +
            "<TxId>TRANSACTION-1</TxId>" +
            "<UETR>123e4567-e89b-12d3-a456-426614174000</UETR>" +
            "</PmtId><IntrBkSttlmAmt Ccy='USD'>123.450</IntrBkSttlmAmt>" +
            "<CustomRef>CUSTOM-1</CustomRef>" +
            "</CdtTrfTxInf></FIToFICstmrCdtTrf></Document>" +
            "</BizMsgEnvlp>";
    }

    private static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }
}
