using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class CrossLayerRoundtripTests
{
    private const string XIncludeNamespace = "http://www.w3.org/2001/XInclude";

    [Test]
    public void RoundtripsParserEventsTreeXdmSerializationAndReparse()
    {
        const string source = "<?probe data?><r:root xmlns:r='urn:root' xmlns:a='urn:attribute' a:id='7'><r:item>value</r:item><!--note--></r:root>";
        XmlParserOptions options = ParserOptions(XmlVersion.Xml10, true);
        XmlInput input = StringInput(source, "https://example.test/input.xml");

        (XmlDocument eventTree, XmlEventBatch events) = ParseEventsToTree(input, options);
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode xdmDocument = accessor.WrapXmlNode(eventTree.DocumentNode!);
        XdmNode xdmRoot = accessor.GetXdmNodeChildren(xdmDocument).Single(node => node.Kind == XdmNodeKind.Element);
        XdmSequence sequence = NodeSequence(eventTree.DocumentNode!);
        byte[] serialized = SerializeSequence(sequence, "1.0", "utf-8");
        XmlDocument reparsed = ParseBytes(serialized, XmlVersion.Xml10, "https://example.test/output.xml");

        Assert.Multiple(() =>
        {
            Assert.That(events.Events!.Select(item => item.Kind), Does.Contain(XmlEventKind.StartDocument));
            Assert.That(accessor.GetXdmNodeName(xdmRoot)!.NamespaceUri, Is.EqualTo("urn:root"));
            Assert.That(accessor.GetXdmNodeStringValue(xdmRoot), Is.EqualTo("value"));
            Assert.That(reparsed.DocumentElement!.Name!.NamespaceUri, Is.EqualTo("urn:root"));
            Assert.That(reparsed.DocumentElement.Attributes!.Single(attribute => attribute.Name!.LocalName == "id").Name!.NamespaceUri, Is.EqualTo("urn:attribute"));
            Assert.That(reparsed.DocumentElement.Children!.Select(node => node.Kind), Does.Contain(XmlNodeKind.Comment));
            Assert.That(reparsed.DocumentElement.Children!.Single(node => node.Kind == XmlNodeKind.Element).Children![0].Value, Is.EqualTo("value"));
        });
    }

    [Test]
    public void PreservationModesRetainOrNormalizeLexicalBoundariesAcrossEventsAndTrees()
    {
        const string source = "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY ent 'entity'>]><root>a<![CDATA[b]]>&ent;<!--note--><?pi data?></root>";
        XmlParserOptions preservedOptions = ParserOptions(XmlVersion.Xml10, true);
        preservedOptions.DtdProcessing = XmlDtdProcessing.Parse;
        preservedOptions.PreserveEntityReferences = true;
        XmlParserOptions normalizedOptions = ParserOptions(XmlVersion.Xml10, false);
        normalizedOptions.DtdProcessing = XmlDtdProcessing.Parse;

        (XmlDocument preserved, XmlEventBatch preservedEvents) = ParseEventsToTree(StringInput(source, null), preservedOptions);
        (XmlDocument normalized, XmlEventBatch normalizedEvents) = ParseEventsToTree(StringInput(source, null), normalizedOptions);

        XmlNodeKind[] preservedKinds = preserved.DocumentElement!.Children!.Select(node => node.Kind).ToArray();
        XmlNodeKind[] normalizedKinds = normalized.DocumentElement!.Children!.Select(node => node.Kind).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(preservedKinds, Does.Contain(XmlNodeKind.CData));
            Assert.That(preservedKinds, Does.Contain(XmlNodeKind.EntityReference));
            Assert.That(preservedKinds, Does.Contain(XmlNodeKind.Comment));
            Assert.That(preservedKinds, Does.Contain(XmlNodeKind.ProcessingInstruction));
            Assert.That(preservedEvents.Events!.Select(item => item.Kind), Does.Contain(XmlEventKind.StartEntity));
            Assert.That(normalizedKinds, Is.EqualTo(new[] { XmlNodeKind.Text }));
            Assert.That(normalized.DocumentElement.Children![0].Value, Is.EqualTo("abentity"));
            Assert.That(normalizedEvents.Events!.Select(item => item.Kind), Does.Not.Contain(XmlEventKind.CData));
        });
    }

    [Test]
    public void SerializationFixesNamespacesAndReparsePreservesExpandedNames()
    {
        XmlDocument document = ParseString("<root><child/></root>", XmlVersion.Xml11, null);
        XmlNode root = document.DocumentElement!;
        root.Name = Name("chosen", "root", "urn:elements");
        root.NamespaceBindings = [];
        root.Attributes =
        [
            new XmlNode
            {
                Kind = XmlNodeKind.Attribute,
                Name = Name("chosen", "id", "urn:attributes"),
                Value = "42",
                Parent = root,
                OwnerDocument = document
            }
        ];
        XmlNode child = root.Children![0];
        child.Name = Name(null, "child", null);
        child.NamespaceBindings = [];

        byte[] serialized = SerializeDocument(document, "1.1", "utf-8");
        XmlDocument reparsed = ParseBytes(serialized, XmlVersion.Xml11, null);
        XmlNode reparsedRoot = reparsed.DocumentElement!;
        XmlNode reparsedChild = reparsedRoot.Children!.Single(node => node.Kind == XmlNodeKind.Element);
        XmlNode reparsedAttribute = reparsedRoot.Attributes!.Single(attribute => attribute.Name!.LocalName == "id");

        Assert.Multiple(() =>
        {
            Assert.That(reparsedRoot.Name!.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(reparsedAttribute.Name!.NamespaceUri, Is.EqualTo("urn:attributes"));
            Assert.That(reparsedAttribute.Name.Prefix, Is.Not.Empty);
            Assert.That(reparsedChild.Name!.NamespaceUri, Is.Null.Or.Empty);
            Assert.That(Encoding.UTF8.GetString(serialized), Does.Contain("xmlns"));
        });
    }

    [TestCase(XmlVersion.Xml10, "1.0", "utf-8")]
    [TestCase(XmlVersion.Xml11, "1.1", "utf-16")]
    public void RoundtripsXmlVersionsAndByteEncodings(XmlVersion version, string versionText, string encoding)
    {
        string source = "<?xml version='" + versionText + "'?><root>café</root>";
        XmlDocument original = ParseString(source, version, null);

        byte[] bytes = SerializeDocument(original, versionText, encoding);
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(original.DocumentNode!, ParserOptions(version, true));
        byte[] eventBytes = SerializeEvents(events, versionText, encoding);
        XmlDocument reparsed = ParseBytes(bytes, version, "https://example.test/encoded.xml");
        XmlDocument reparsedEvents = ParseBytes(eventBytes, version, null);

        Assert.Multiple(() =>
        {
            Assert.That(reparsed.Version, Is.EqualTo(version));
            Assert.That(reparsed.EncodingName, Does.StartWith(encoding).IgnoreCase);
            Assert.That(reparsed.DocumentElement!.Children![0].Value, Is.EqualTo("café"));
            Assert.That(reparsed.DocumentUri, Is.EqualTo("https://example.test/encoded.xml"));
            Assert.That(reparsedEvents.DocumentElement!.Children![0].Value, Is.EqualTo("café"));
            if (encoding == "utf-16")
            {
                Assert.That(bytes.Take(2), Is.EqualTo(new byte[] { 0xFF, 0xFE }));
                Assert.That(eventBytes.Take(2), Is.EqualTo(new byte[] { 0xFF, 0xFE }));
            }
        });
    }

    [Test]
    public void DtdDefaultsSurviveEventTreeSerializationAndReparseAsSpecifiedAttributes()
    {
        const string source = "<!DOCTYPE root [<!ELEMENT root EMPTY><!ATTLIST root state NMTOKEN ' ready '>]><root/>";
        XmlParserOptions options = ParserOptions(XmlVersion.Xml10, true);
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.ValidationMode = XmlValidationMode.Dtd;

        (XmlDocument eventTree, XmlEventBatch events) = ParseEventsToTree(StringInput(source, null), options);
        XmlEvent rootStart = events.Events!.Single(item => item.Kind == XmlEventKind.StartElement);
        XmlAttributeData defaulted = rootStart.Attributes!.Single(attribute => attribute.Name!.LocalName == "state");
        byte[] serialized = SerializeDocument(eventTree, "1.0", "utf-8");
        XmlParserOptions reparseOptions = ParserOptions(XmlVersion.Xml10, true);
        reparseOptions.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument reparsed = ParseBytesWithOptions(serialized, reparseOptions, null);

        Assert.Multiple(() =>
        {
            Assert.That(defaulted.IsDefaulted, Is.True);
            Assert.That(defaulted.IsSpecified, Is.False);
            Assert.That(defaulted.Value, Is.EqualTo("ready"));
            Assert.That(eventTree.DocumentElement!.Attributes![0].Range, Is.Null);
            Assert.That(reparsed.DocumentElement!.Attributes![0].Value, Is.EqualTo("ready"));
            Assert.That(reparsed.DocumentElement.Attributes[0].Range, Is.Not.Null);
        });
    }

    [Test]
    public void XIncludeResultsRoundtripThroughEventsXdmAndSerialization()
    {
        string source = "<root xmlns:xi='" + XIncludeNamespace + "'><xi:include href='part.xml'/><xi:include href='note.txt' parse='text'/></root>";
        XmlDocument document = ParseString(source, XmlVersion.Xml10, "https://example.test/base/document.xml");
        RoundtripIncludeResolver resolver = new RoundtripIncludeResolver();
        XmlIncludeOptions includeOptions = new XmlIncludeOptions
        {
            ProcessNestedIncludes = true,
            FixupBaseUri = true,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 16,
                MaximumResourceBytes = 65536,
                MaximumWorkUnits = 65536
            }
        };

        new XmlIncludeProcessor().ProcessXmlIncludes(document, includeOptions, resolver);
        XmlParserOptions options = ParserOptions(XmlVersion.Xml10, true);
        XmlEventBatch includedEvents = new XmlEventEmitter().EmitXmlNodeEvents(document.DocumentNode!, options);
        XmlDocument includedTree = new TreeBuilder(document, false).BuildXmlEventTree(includedEvents.Events!);
        byte[] serialized = SerializeSequence(NodeSequence(includedTree.DocumentNode!), "1.0", "utf-8");
        XmlDocument reparsed = ParseBytes(serialized, XmlVersion.Xml10, document.BaseUri);

        XmlNode[] children = reparsed.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children[0].Name!.LocalName, Is.EqualTo("included"));
            Assert.That(children[0].Children![0].Value, Is.EqualTo("content"));
            Assert.That(children[0].Attributes!.Single(attribute => attribute.Name!.LocalName == "base").Value, Is.EqualTo("https://example.test/base/part.xml"));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(children[1].Value, Is.EqualTo("plain text"));
            Assert.That(resolver.Requests, Is.EqualTo(new[] { "part.xml", "note.txt" }));
        });
    }

    [Test]
    public void CanonicalBytesAreStableAcrossTreeEventsAndReparse()
    {
        const string source = "<p:root xmlns:p='urn:p' xmlns:z='urn:z' z:b='2' a='1'><p:empty></p:empty><!--note--></p:root>";
        XmlDocument document = ParseString(source, XmlVersion.Xml10, null);
        XmlParserOptions options = ParserOptions(XmlVersion.Xml10, true);
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(document.DocumentNode!, options);

        byte[] treeBytes = CanonicalizeDocument(document);
        byte[] eventBytes = CanonicalizeEvents(events);
        XmlDocument reparsed = ParseBytes(treeBytes, XmlVersion.Xml10, null);
        byte[] reparsedBytes = CanonicalizeDocument(reparsed);

        Assert.Multiple(() =>
        {
            Assert.That(eventBytes, Is.EqualTo(treeBytes));
            Assert.That(reparsedBytes, Is.EqualTo(treeBytes));
            Assert.That(Encoding.UTF8.GetString(treeBytes), Is.EqualTo("<p:root xmlns:p=\"urn:p\" xmlns:z=\"urn:z\" a=\"1\" z:b=\"2\"><p:empty></p:empty><!--note--></p:root>"));
        });
    }

    [Test]
    public void SourceLocationsFlowFromByteParsingThroughEventsAndTreeRebuild()
    {
        const string source = "<root>\n  <child id='1'>text</child>\n</root>";
        const string sourceUri = "https://example.test/located.xml";
        XmlParserOptions options = ParserOptions(XmlVersion.Xml10, true);
        XmlInput input = new XmlInput
        {
            Kind = XmlInputKind.ByteMemory,
            Bytes = Encoding.UTF8.GetBytes(source),
            BaseUri = sourceUri
        };

        (XmlDocument tree, XmlEventBatch events) = ParseEventsToTree(input, options);
        XmlEvent childEvent = events.Events!.Single(item => item.Kind == XmlEventKind.StartElement && item.Name!.LocalName == "child");
        XmlNode child = tree.DocumentElement!.Children!.Single(node => node.Kind == XmlNodeKind.Element);
        byte[] serialized = SerializeDocument(tree, "1.0", "utf-8");
        XmlDocument reparsed = ParseBytes(serialized, XmlVersion.Xml10, "https://example.test/reparsed.xml");
        XmlNode reparsedChild = reparsed.DocumentElement!.Children!.Single(node => node.Kind == XmlNodeKind.Element);

        Assert.Multiple(() =>
        {
            Assert.That(childEvent.Range!.Start!.SourceUri, Is.EqualTo(sourceUri));
            Assert.That(childEvent.Range.Start.Line, Is.EqualTo(2));
            Assert.That(childEvent.Range.Start.Column, Is.EqualTo(3));
            Assert.That(childEvent.Range.Start.ByteOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(child.Range!.Start!.CharacterOffset, Is.EqualTo(childEvent.Range.Start.CharacterOffset));
            Assert.That(child.Range.Start.ByteOffset, Is.EqualTo(childEvent.Range.Start.ByteOffset));
            Assert.That(child.Attributes![0].Range!.Start!.Line, Is.EqualTo(2));
            Assert.That(reparsedChild.Range!.Start!.SourceUri, Is.EqualTo("https://example.test/reparsed.xml"));
            Assert.That(reparsedChild.Range.Start.Line, Is.GreaterThanOrEqualTo(1));
        });
    }

    private static (XmlDocument Document, XmlEventBatch Events) ParseEventsToTree(XmlInput input, XmlParserOptions options)
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(input, options, null, null, null, null);
        XmlEventBatch events = parser.FinishXmlPush();
        XmlDocument metadata = new XmlDocument
        {
            Version = options.Version,
            DocumentUri = input.BaseUri,
            BaseUri = input.BaseUri
        };
        XmlDocument tree = new TreeBuilder(metadata, false).BuildXmlEventTree(events.Events!);
        return (tree, events);
    }

    private static XmlParserOptions ParserOptions(XmlVersion version, bool preserve)
    {
        return new XmlParserOptions
        {
            Version = version,
            NamespaceHandling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveCData = preserve,
            PreserveEntityReferences = preserve,
            PreserveComments = preserve,
            PreserveProcessingInstructions = preserve,
            PreservePrefixes = preserve,
            PreserveSourceLocations = true,
            RetainNamespaceDeclarations = preserve
        };
    }

    private static XmlInput StringInput(string source, string? baseUri)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri };
    }

    private static XmlDocument ParseString(string source, XmlVersion version, string? baseUri)
    {
        return new XmlParser().ParseXmlDocument(StringInput(source, baseUri), ParserOptions(version, true), null, null, null);
    }

    private static XmlDocument ParseBytes(byte[] source, XmlVersion version, string? baseUri)
    {
        return ParseBytesWithOptions(source, ParserOptions(version, true), baseUri);
    }

    private static XmlDocument ParseBytesWithOptions(byte[] source, XmlParserOptions options, string? baseUri)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = source, BaseUri = baseUri };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XdmSequence NodeSequence(XmlNode node)
    {
        return new XdmSequence
        {
            ItemKinds = [XdmItemKind.Node],
            ItemIndexes = [0],
            Nodes = [node],
            Count = 1
        };
    }

    private static XmlName Name(string? prefix, string localName, string? namespaceUri)
    {
        return new XmlName
        {
            Prefix = prefix,
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = prefix == null || prefix.Length == 0 ? localName : prefix + ":" + localName
        };
    }

    private static byte[] SerializeDocument(XmlDocument document, string version, string encoding)
    {
        MemoryStream stream = new MemoryStream();
        XmlSerializationOptions options = SerializationOptions(version, encoding);
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        new XmlSerializer().SerializeXmlDocument(document, output, options);
        return stream.ToArray();
    }

    private static byte[] SerializeSequence(XdmSequence sequence, string version, string encoding)
    {
        MemoryStream stream = new MemoryStream();
        XmlSerializationOptions options = SerializationOptions(version, encoding);
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        new XmlSerializer().SerializeXdmSequence(sequence, output, options);
        return stream.ToArray();
    }

    private static byte[] SerializeEvents(XmlEventBatch events, string version, string encoding)
    {
        MemoryStream stream = new MemoryStream();
        XmlSerializationOptions options = SerializationOptions(version, encoding);
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        new XmlSerializer().SerializeXmlEvents(events, output, options);
        return stream.ToArray();
    }

    private static XmlSerializationOptions SerializationOptions(string version, string encoding)
    {
        return new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xml,
            Version = version,
            Encoding = encoding,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
    }

    private static byte[] CanonicalizeDocument(XmlDocument document)
    {
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = collector };
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = true
        };
        new XmlCanonicalizer().CanonicalizeXml(document, output, options);
        return collector.SnapshotCollectedXmlBytes();
    }

    private static byte[] CanonicalizeEvents(XmlEventBatch events)
    {
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = collector };
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = true
        };
        new XmlCanonicalizer().CanonicalizeXmlEvents(events, output, options);
        return collector.SnapshotCollectedXmlBytes();
    }

    private sealed class RoundtripIncludeResolver : IXmlResourceResolver
    {
        public List<string> Requests = [];

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string identifier = systemIdentifier == null ? "" : systemIdentifier;
            Requests.Add(identifier);
            if (kind == XmlResourceKind.XIncludeXml)
            {
                return new XmlResolvedResource
                {
                    Kind = kind,
                    Access = XmlResourceAccess.Allowed,
                    RequestedUri = identifier,
                    AbsoluteUri = "https://example.test/base/part.xml",
                    Input = StringInput("<included>content</included>", "https://example.test/base/part.xml")
                };
            }

            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                RequestedUri = identifier,
                AbsoluteUri = "https://example.test/base/note.txt",
                Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Encoding.UTF8.GetBytes("plain text"), BaseUri = "https://example.test/base/note.txt" }
            };
        }
    }
}
