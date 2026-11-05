using iso20022;

namespace iso20022.Tests;

public class GenericMessageDataTests
{
    [Test]
    public void GenericXmlTreePreservesQualifiedNamesMixedContentAndRawNamespaceClosure()
    {
        string namespaceUri = Iso20022Namespaces.SchemaUrnPrefix + "pacs.008.001.10";
        NamespaceDeclarationData declaration = new NamespaceDeclarationData
        {
            Prefix = "pacs",
            NamespaceUri = namespaceUri
        };
        XmlAttributeData attribute = new XmlAttributeData
        {
            Name = new QualifiedNameData { LocalName = "Id" },
            Value = "A-17",
            IsSpecified = true,
            Location = new SourceLocation { HasLineInformation = true, LineNumber = 2, ColumnNumber = 11 }
        };
        DynamicXmlNode textBefore = new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "before" };
        DynamicXmlNode component = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { NamespaceUri = namespaceUri, LocalName = "Component", Prefix = "pacs" },
            IsEmptyElement = true
        };
        DynamicXmlNode comment = new DynamicXmlNode { Kind = XmlNodeKind.Comment, Value = "preserved" };
        DynamicXmlNode cdata = new DynamicXmlNode { Kind = XmlNodeKind.CData, Value = "after & exact" };
        DynamicXmlNode document = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { NamespaceUri = namespaceUri, LocalName = "Document", Prefix = "pacs" },
            Attributes = new List<XmlAttributeData> { attribute },
            NamespaceDeclarations = new List<NamespaceDeclarationData> { declaration },
            Children = new List<DynamicXmlNode> { textBefore, component, comment, cdata },
            IsNil = false,
            IsEmptyElement = false,
            Location = new SourceLocation { HasLineInformation = true, LineNumber = 1, ColumnNumber = 1 }
        };
        RawXmlFragment raw = new RawXmlFragment
        {
            Xml = "<pacs:Component/>",
            Utf8Bytes = "<pacs:Component/>"u8.ToArray(),
            RootName = component.Name,
            NamespaceDeclarations = new List<NamespaceDeclarationData> { declaration },
            Location = component.Location
        };

        Assert.Multiple(() =>
        {
            Assert.That(document.Name.LocalName, Is.EqualTo("Document"));
            Assert.That(document.Attributes[0].Name.LocalName, Is.EqualTo("Id"));
            Assert.That(document.Attributes[0].IsSpecified, Is.True);
            Assert.That(document.Attributes[0].Location.ColumnNumber, Is.EqualTo(11));
            Assert.That(document.NamespaceDeclarations[0], Is.SameAs(declaration));
            Assert.That(document.Children.Select(node => node.Kind), Is.EqualTo(new[] { XmlNodeKind.Text, XmlNodeKind.Element, XmlNodeKind.Comment, XmlNodeKind.CData }));
            Assert.That(document.Children[0].Value, Is.EqualTo("before"));
            Assert.That(document.Children[3].Value, Is.EqualTo("after & exact"));
            Assert.That(raw.RootName, Is.SameAs(component.Name));
            Assert.That(raw.NamespaceDeclarations[0], Is.SameAs(declaration));
            Assert.That(raw.Utf8Bytes, Is.EqualTo("<pacs:Component/>"u8.ToArray()));
        });
    }

    [Test]
    public void MessageDocumentComposesHeaderPayloadEnvelopeAndMetadataWithoutLosingRepresentations()
    {
        MessageIdentifier payloadIdentifier = CreateIdentifier("pacs", "008", "001", "10");
        NamespaceDescriptor payloadNamespace = CreateNamespace(payloadIdentifier, NamespaceKind.Message);
        DynamicXmlNode genericContent = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { NamespaceUri = payloadNamespace.Uri, LocalName = "Document" }
        };
        object typedContent = new object();
        RawXmlFragment rawContent = new RawXmlFragment { Xml = "<Document/>" };
        MessagePayload payload = new MessagePayload
        {
            Identifier = payloadIdentifier,
            Namespace = payloadNamespace,
            GenericContent = genericContent,
            TypedContent = typedContent,
            RawContent = rawContent
        };
        MessageHeader header = new MessageHeader
        {
            Identifier = CreateIdentifier("head", "001", "001", "04"),
            Namespace = CreateNamespace(CreateIdentifier("head", "001", "001", "04"), NamespaceKind.Header),
            BusinessMessageIdentifier = "BUSINESS-17",
            BusinessService = "swift.cbprplus.03",
            GenericContent = new DynamicXmlNode { Kind = XmlNodeKind.Element, Name = new QualifiedNameData { LocalName = "AppHdr" } },
            RawContent = new RawXmlFragment { Xml = "<AppHdr/>" }
        };
        MessageMetadata metadata = new MessageMetadata
        {
            MessageIdentifier = payloadIdentifier,
            NamespaceUri = payloadNamespace.Uri,
            BusinessMessageIdentifier = "BUSINESS-17",
            BusinessService = "swift.cbprplus.03",
            Sender = "BANKUS33",
            Receiver = "BANKGB22",
            CreationDateTime = new IsoDateTimeValue { LexicalValue = "2026-08-31T12:00:00Z" },
            HasPossibleDuplicate = true,
            PossibleDuplicate = false,
            Priority = "HIGH",
            Uetr = "12345678-1234-1234-1234-123456789abc",
            PrimaryAmount = new IsoAmountValue { Amount = new IsoDecimalValue { Value = 12.34m, Scale = 2 }, Currency = "USD" },
            ValueDate = new IsoDateValue { Year = 2026, Month = 9, Day = 1 },
            References = new List<string> { "END-TO-END", "TX-17" }
        };
        MessageDocument nested = new MessageDocument
        {
            Layout = EnvelopeKind.SiblingHeaderAndDocument,
            Header = header,
            Payloads = new List<MessagePayload> { payload },
            Metadata = metadata
        };
        MessageEnvelope envelope = new MessageEnvelope
        {
            Kind = EnvelopeKind.BusinessFileEnvelope,
            Name = new QualifiedNameData { LocalName = "BizFileEnvlp" },
            Namespace = CreateNamespace(CreateIdentifier("nvlp", "002", "001", "02"), NamespaceKind.Envelope),
            Header = header,
            Payloads = new List<MessagePayload> { payload },
            Documents = new List<MessageDocument> { nested },
            NamespaceDeclarations = new List<NamespaceDeclarationData> { new NamespaceDeclarationData { Prefix = "nvlp", NamespaceUri = "urn:envelope" } },
            GenericContent = new DynamicXmlNode { Kind = XmlNodeKind.Element, Name = new QualifiedNameData { LocalName = "BizFileEnvlp" } },
            RawContent = new RawXmlFragment { Xml = "<BizFileEnvlp/>" }
        };
        MessageDocument document = new MessageDocument
        {
            Layout = EnvelopeKind.BusinessFileEnvelope,
            Envelope = envelope,
            Metadata = metadata,
            NamespaceDeclarations = envelope.NamespaceDeclarations,
            Location = new SourceLocation { SourceName = "batch.xml", HasCharacterOffset = true, CharacterOffset = 42 }
        };

        Assert.Multiple(() =>
        {
            Assert.That(document.Envelope?.Kind, Is.EqualTo(EnvelopeKind.BusinessFileEnvelope));
            Assert.That(document.Envelope?.Documents[0], Is.SameAs(nested));
            Assert.That(document.Envelope?.Header, Is.SameAs(header));
            Assert.That(document.Envelope?.Payloads[0], Is.SameAs(payload));
            Assert.That(payload.GenericContent, Is.SameAs(genericContent));
            Assert.That(payload.TypedContent, Is.SameAs(typedContent));
            Assert.That(payload.RawContent, Is.SameAs(rawContent));
            Assert.That(header.BusinessService, Is.EqualTo("swift.cbprplus.03"));
            Assert.That(header.GenericContent?.Name.LocalName, Is.EqualTo("AppHdr"));
            Assert.That(header.RawContent?.Xml, Is.EqualTo("<AppHdr/>"));
            Assert.That(document.Metadata.Sender, Is.EqualTo("BANKUS33"));
            Assert.That(document.Metadata.HasPossibleDuplicate, Is.True);
            Assert.That(document.Metadata.PossibleDuplicate, Is.False);
            Assert.That(document.Metadata.PrimaryAmount?.Currency, Is.EqualTo("USD"));
            Assert.That(document.Metadata.ValueDate?.Day, Is.EqualTo(1));
            Assert.That(document.Metadata.References, Is.EqualTo(new[] { "END-TO-END", "TX-17" }));
            Assert.That(document.Location.CharacterOffset, Is.EqualTo(42));
        });
    }

    [Test]
    public void ReadWriteAndMetadataOptionsRetainExplicitPolicies()
    {
        XmlPathData selectedPath = new XmlPathData { Value = "/Document/Payment" };
        QualifiedNameData wrapperName = new QualifiedNameData { NamespaceUri = "urn:example:wrapper", LocalName = "Messages", Prefix = "w" };
        NamespaceDeclarationData declaration = new NamespaceDeclarationData { Prefix = "p", NamespaceUri = "urn:payload" };
        MessageReadOptions read = new MessageReadOptions
        {
            NamespaceMode = NamespaceMode.Lenient,
            UnknownContentMode = UnknownContentMode.Reject,
            VersionSelector = new VersionSelector { Kind = VersionSelectionKind.Exact, BaselineIdentifier = CreateIdentifier("pacs", "008", "001", "10") },
            Limits = new ResourceLimits { MaxDepth = 12 },
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = false,
            PreserveLexicalValues = false,
            SelectedPaths = new List<XmlPathData> { selectedPath },
            CustomWrapperNames = new List<QualifiedNameData> { wrapperName }
        };
        MessageWriteOptions write = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Canonical,
            UnknownContentMode = UnknownContentMode.Ignore,
            Limits = new ResourceLimits { MaxElementCount = 500 },
            WriteXmlDeclaration = false,
            WriteByteOrderMark = true,
            PreservePrefixes = false,
            PreserveLexicalValues = false,
            IndentText = "\t",
            NewLineText = "\r\n",
            NamespaceDeclarations = new List<NamespaceDeclarationData> { declaration }
        };
        MetadataExtractionOptions metadata = new MetadataExtractionOptions
        {
            IncludeHeader = false,
            IncludePayload = true,
            RedactSensitiveValues = true,
            Limits = new ResourceLimits { MaxTextLength = 128 },
            AdditionalReferencePaths = new List<XmlPathData> { selectedPath }
        };

        Assert.Multiple(() =>
        {
            Assert.That(read.NamespaceMode, Is.EqualTo(NamespaceMode.Lenient));
            Assert.That(read.UnknownContentMode, Is.EqualTo(UnknownContentMode.Reject));
            Assert.That(read.VersionSelector?.BaselineIdentifier?.Version, Is.EqualTo("10"));
            Assert.That(read.Limits.MaxDepth, Is.EqualTo(12));
            Assert.That(read.PreserveComments, Is.True);
            Assert.That(read.PreserveProcessingInstructions, Is.True);
            Assert.That(read.PreserveWhitespace, Is.False);
            Assert.That(read.PreserveLexicalValues, Is.False);
            Assert.That(read.SelectedPaths[0], Is.SameAs(selectedPath));
            Assert.That(read.CustomWrapperNames[0], Is.SameAs(wrapperName));
            Assert.That(write.FormattingMode, Is.EqualTo(XmlFormattingMode.Canonical));
            Assert.That(write.UnknownContentMode, Is.EqualTo(UnknownContentMode.Ignore));
            Assert.That(write.Limits.MaxElementCount, Is.EqualTo(500));
            Assert.That(write.WriteXmlDeclaration, Is.False);
            Assert.That(write.WriteByteOrderMark, Is.True);
            Assert.That(write.PreservePrefixes, Is.False);
            Assert.That(write.PreserveLexicalValues, Is.False);
            Assert.That(write.IndentText, Is.EqualTo("\t"));
            Assert.That(write.NewLineText, Is.EqualTo("\r\n"));
            Assert.That(write.NamespaceDeclarations[0], Is.SameAs(declaration));
            Assert.That(metadata.IncludeHeader, Is.False);
            Assert.That(metadata.IncludePayload, Is.True);
            Assert.That(metadata.RedactSensitiveValues, Is.True);
            Assert.That(metadata.Limits.MaxTextLength, Is.EqualTo(128));
            Assert.That(metadata.AdditionalReferencePaths[0], Is.SameAs(selectedPath));
        });
    }

    [Test]
    public void ResultsRepairsAndStreamOptionsRetainOutcomeAndControlState()
    {
        XmlPathData path = new XmlPathData { Value = "/AppHdr/MsgDefIdr" };
        SourceLocation location = new SourceLocation { SourceName = "batch.xml", HasLineInformation = true, LineNumber = 7, ColumnNumber = 4 };
        MessageRepair repair = new MessageRepair
        {
            Code = "message.namespace.alias",
            Description = "Normalized a namespace alias.",
            Path = path,
            Location = location,
            OriginalValue = "PACS.008.001.10",
            RepairedValue = "pacs.008.001.10"
        };
        MessageDocument document = new MessageDocument { Layout = EnvelopeKind.StandaloneDocument };
        MessageReadResult readResult = new MessageReadResult
        {
            Success = true,
            Document = document,
            Repairs = new List<MessageRepair> { repair },
            BytesConsumed = 2048,
            EndOfInput = true
        };
        MessageWriteResult writeResult = new MessageWriteResult
        {
            Success = false,
            Bytes = new byte[] { 1, 2, 3 },
            Text = "partial",
            BytesWritten = 3,
            Repairs = new List<MessageRepair> { repair },
            ErrorCode = "write.required",
            ErrorMessage = "A required value is absent."
        };
        MessageStreamItem item = new MessageStreamItem { Index = 17, Result = readResult, Location = location };
        StreamReadOptions streamRead = new StreamReadOptions
        {
            MessageOptions = new MessageReadOptions { UnknownContentMode = UnknownContentMode.Preserve },
            ContinueAfterInvalidItem = true,
            MaxItems = 25,
            WrapperNames = new List<QualifiedNameData> { new QualifiedNameData { LocalName = "Messages" } }
        };
        StreamWriteOptions streamWrite = new StreamWriteOptions
        {
            MessageOptions = new MessageWriteOptions { FormattingMode = XmlFormattingMode.Indented },
            EnvelopeKind = EnvelopeKind.CustomWrapper,
            CustomWrapperName = new QualifiedNameData { NamespaceUri = "urn:custom", LocalName = "Batch" },
            HasPrecomputedMessageCount = true,
            PrecomputedMessageCount = 25,
            HasPrecomputedControlTotal = true,
            PrecomputedControlTotal = new IsoDecimalValue { Value = 123.45m, Scale = 2 },
            UseDeferredTotals = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Document, Is.SameAs(document));
            Assert.That(readResult.Repairs[0], Is.SameAs(repair));
            Assert.That(readResult.BytesConsumed, Is.EqualTo(2048));
            Assert.That(readResult.EndOfInput, Is.True);
            Assert.That(repair.Path, Is.SameAs(path));
            Assert.That(repair.Location, Is.SameAs(location));
            Assert.That(repair.OriginalValue, Is.EqualTo("PACS.008.001.10"));
            Assert.That(writeResult.Success, Is.False);
            Assert.That(writeResult.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(writeResult.Text, Is.EqualTo("partial"));
            Assert.That(writeResult.BytesWritten, Is.EqualTo(3));
            Assert.That(writeResult.Repairs[0], Is.SameAs(repair));
            Assert.That(writeResult.ErrorCode, Is.EqualTo("write.required"));
            Assert.That(writeResult.ErrorMessage, Is.Not.Empty);
            Assert.That(item.Index, Is.EqualTo(17));
            Assert.That(item.Result, Is.SameAs(readResult));
            Assert.That(item.Location, Is.SameAs(location));
            Assert.That(streamRead.ContinueAfterInvalidItem, Is.True);
            Assert.That(streamRead.MaxItems, Is.EqualTo(25));
            Assert.That(streamRead.WrapperNames[0].LocalName, Is.EqualTo("Messages"));
            Assert.That(streamWrite.EnvelopeKind, Is.EqualTo(EnvelopeKind.CustomWrapper));
            Assert.That(streamWrite.CustomWrapperName?.LocalName, Is.EqualTo("Batch"));
            Assert.That(streamWrite.HasPrecomputedMessageCount, Is.True);
            Assert.That(streamWrite.PrecomputedMessageCount, Is.EqualTo(25));
            Assert.That(streamWrite.HasPrecomputedControlTotal, Is.True);
            Assert.That(streamWrite.PrecomputedControlTotal.Value, Is.EqualTo(123.45m));
            Assert.That(streamWrite.UseDeferredTotals, Is.False);
        });
    }

    [Test]
    public void MutableDefaultsAreIndependentAndUseLosslessSafePolicies()
    {
        DynamicXmlNode firstNode = new DynamicXmlNode();
        DynamicXmlNode secondNode = new DynamicXmlNode();
        MessageDocument firstDocument = new MessageDocument();
        MessageDocument secondDocument = new MessageDocument();
        MessageReadOptions firstRead = new MessageReadOptions();
        MessageReadOptions secondRead = new MessageReadOptions();
        MessageWriteOptions firstWrite = new MessageWriteOptions();
        MessageWriteOptions secondWrite = new MessageWriteOptions();
        StreamReadOptions firstStreamRead = new StreamReadOptions();
        StreamReadOptions secondStreamRead = new StreamReadOptions();
        StreamWriteOptions firstStreamWrite = new StreamWriteOptions();
        StreamWriteOptions secondStreamWrite = new StreamWriteOptions();
        MetadataExtractionOptions firstMetadata = new MetadataExtractionOptions();
        MetadataExtractionOptions secondMetadata = new MetadataExtractionOptions();

        firstNode.Children.Add(new DynamicXmlNode());
        firstDocument.Payloads.Add(new MessagePayload());
        firstDocument.Metadata.References.Add("reference");
        firstRead.SelectedPaths.Add(new XmlPathData());
        firstWrite.NamespaceDeclarations.Add(new NamespaceDeclarationData());
        firstStreamRead.MessageOptions.SelectedPaths.Add(new XmlPathData());
        firstStreamWrite.MessageOptions.NamespaceDeclarations.Add(new NamespaceDeclarationData());
        firstStreamWrite.PrecomputedControlTotal.Value = 1m;
        firstMetadata.AdditionalReferencePaths.Add(new XmlPathData());

        Assert.Multiple(() =>
        {
            Assert.That(secondNode.Children, Is.Empty);
            Assert.That(secondDocument.Payloads, Is.Empty);
            Assert.That(secondDocument.Metadata.References, Is.Empty);
            Assert.That(secondRead.SelectedPaths, Is.Empty);
            Assert.That(secondWrite.NamespaceDeclarations, Is.Empty);
            Assert.That(secondStreamRead.MessageOptions.SelectedPaths, Is.Empty);
            Assert.That(secondStreamWrite.MessageOptions.NamespaceDeclarations, Is.Empty);
            Assert.That(secondStreamWrite.PrecomputedControlTotal.Value, Is.Zero);
            Assert.That(secondMetadata.AdditionalReferencePaths, Is.Empty);
            Assert.That(firstNode.Children, Is.Not.SameAs(secondNode.Children));
            Assert.That(firstDocument.Metadata, Is.Not.SameAs(secondDocument.Metadata));
            Assert.That(firstRead.Limits, Is.Not.SameAs(secondRead.Limits));
            Assert.That(firstWrite.Limits, Is.Not.SameAs(secondWrite.Limits));
            Assert.That(firstRead.NamespaceMode, Is.EqualTo(NamespaceMode.Strict));
            Assert.That(firstRead.UnknownContentMode, Is.EqualTo(UnknownContentMode.Preserve));
            Assert.That(firstRead.PreserveWhitespace, Is.True);
            Assert.That(firstRead.PreserveLexicalValues, Is.True);
            Assert.That(firstWrite.FormattingMode, Is.EqualTo(XmlFormattingMode.Compact));
            Assert.That(firstWrite.UnknownContentMode, Is.EqualTo(UnknownContentMode.Preserve));
            Assert.That(firstWrite.WriteXmlDeclaration, Is.True);
            Assert.That(firstWrite.PreservePrefixes, Is.True);
            Assert.That(firstWrite.PreserveLexicalValues, Is.True);
            Assert.That(firstMetadata.IncludeHeader, Is.True);
            Assert.That(firstMetadata.IncludePayload, Is.True);
        });
    }

    internal static MessageIdentifier CreateIdentifier(string area, string functionality, string variant, string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = functionality,
            Variant = variant,
            Version = version,
            Value = area + "." + functionality + "." + variant + "." + version
        };
    }

    internal static NamespaceDescriptor CreateNamespace(MessageIdentifier identifier, NamespaceKind kind)
    {
        return new NamespaceDescriptor
        {
            Uri = Iso20022Namespaces.SchemaUrnPrefix + identifier.Value,
            Kind = kind,
            Mode = NamespaceMode.Strict,
            MessageIdentifier = identifier
        };
    }
}
