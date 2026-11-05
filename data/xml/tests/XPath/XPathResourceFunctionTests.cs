using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XPathResourceFunctionTests
{
    [Test]
    public void ResolvesRelativeDocumentsOnceWithStableNodeIdentity()
    {
        RecordingCollectionResolver resolver = new RecordingCollectionResolver();
        XPathDynamicContext context = DynamicContext(resolver);

        XdmSequence result = Evaluate("(doc('data.xml') is doc('data.xml'), doc-available('data.xml'), name(doc('data.xml')/*))", context, "https://example.test/base/query.xq");

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "true", "true", "root" }));
            Assert.That(resolver.DocumentCalls, Is.EqualTo(1));
            Assert.That(resolver.LastDocumentUri, Is.EqualTo("https://example.test/base/data.xml"));
        });
    }

    [Test]
    public void DocumentAvailabilitySuppressesRetrievalFailures()
    {
        RecordingCollectionResolver resolver = new RecordingCollectionResolver();
        resolver.DocumentAvailable = false;
        XPathDynamicContext context = DynamicContext(resolver);

        XdmSequence available = Evaluate("(doc-available('urn:missing'), doc-available('urn:missing'))", context);
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("doc('urn:missing')", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(available.AtomicValues!.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { false, false }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
            Assert.That(resolver.DocumentCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void ResolvesAndCachesDefaultCollectionsAndUriCollections()
    {
        RecordingCollectionResolver resolver = new RecordingCollectionResolver();
        XPathDynamicContext context = DynamicContext(resolver);
        context.DefaultCollectionUri = "urn:default-collection";

        XdmSequence result = Evaluate("(collection(), collection(()), uri-collection(), uri-collection(()))", context);

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "member", "member", "urn:first", "urn:second", "urn:first", "urn:second" }));
            Assert.That(resolver.CollectionCalls, Is.EqualTo(1));
            Assert.That(resolver.UriCollectionCalls, Is.EqualTo(1));
            Assert.That(resolver.LastCollectionUri, Is.EqualTo("urn:default-collection"));
            Assert.That(resolver.LastUriCollectionUri, Is.EqualTo("urn:default-collection"));
            Assert.That(result.IsImmutable, Is.True);
        });
    }

    [Test]
    public void CollectionFailuresUseTheCollectionErrorCode()
    {
        RecordingCollectionResolver resolver = new RecordingCollectionResolver();
        resolver.FailCollections = true;
        XPathDynamicContext context = DynamicContext(resolver);

        XPathDynamicException collection = Assert.Throws<XPathDynamicException>(() => Evaluate("collection('urn:missing')", context))!;
        XPathDynamicException uris = Assert.Throws<XPathDynamicException>(() => Evaluate("uri-collection('urn:missing')", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(collection.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
            Assert.That(uris.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
        });
    }

    [Test]
    public void DecodesCachesAndSplitsUnparsedText()
    {
        RecordingTextResolver resolver = new RecordingTextResolver();
        resolver.Bytes = [0x41, 0xE9, 0x0D, 0x0A, 0x42, 0x0D, 0x43];
        XPathDynamicContext context = DynamicContext(new RecordingCollectionResolver());
        context.ResourceResolver = resolver;
        string source = "(unparsed-text('text.txt','iso-8859-1'), " +
            "unparsed-text-available('text.txt','iso-8859-1'), " +
            "unparsed-text-lines('text.txt','iso-8859-1'))";

        XdmSequence result = Evaluate(source, context, "https://example.test/resources/query.xq");
        context.ResolverPolicy!.AllowedSchemes = ["file"];

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "Aé\r\nB\rC", "true", "Aé", "B", "C" }));
            Assert.That(resolver.Calls, Is.EqualTo(1));
            Assert.That(resolver.LastUri, Is.EqualTo("https://example.test/resources/text.txt"));
            Assert.That(resolver.Policy, Is.Not.SameAs(context.ResolverPolicy));
            Assert.That(resolver.Policy!.AllowedSchemes, Is.EqualTo(new[] { "https", "urn" }));
        });
    }

    [Test]
    public void ReportsEncodingAndDecodingErrorsAndAvailability()
    {
        RecordingTextResolver resolver = new RecordingTextResolver();
        resolver.Bytes = [0xFF];
        XPathDynamicContext context = DynamicContext(new RecordingCollectionResolver());
        context.ResourceResolver = resolver;

        XPathDynamicException encoding = Assert.Throws<XPathDynamicException>(() => Evaluate("unparsed-text('urn:text','unknown-encoding')", context))!;
        XPathDynamicException decoding = Assert.Throws<XPathDynamicException>(() => Evaluate("unparsed-text('urn:text','utf-8')", context))!;
        XPathDynamicException inference = Assert.Throws<XPathDynamicException>(() => Evaluate("unparsed-text('urn:text')", context))!;
        XdmSequence available = Evaluate("unparsed-text-available('urn:text','utf-8')", context);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOUT1190));
            Assert.That(decoding.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOUT1190));
            Assert.That(inference.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOUT1200));
            Assert.That(available.AtomicValues![0].BooleanValue, Is.False);
        });
    }

    [Test]
    public void PassesDocumentAndCollectionFragmentsButRejectsTextFragments()
    {
        XPathDynamicContext context = DynamicContext(new RecordingCollectionResolver());
        context.ResourceResolver = new RecordingTextResolver();

        XdmSequence document = Evaluate("doc-available('urn:document#part')", context);
        XdmSequence collection = Evaluate("collection('urn:collection#part')", context);
        XPathDynamicException text = Assert.Throws<XPathDynamicException>(() => Evaluate("unparsed-text('urn:text#part')", context))!;
        XdmSequence textAvailable = Evaluate("unparsed-text-available('urn:text#part')", context);

        Assert.Multiple(() =>
        {
            Assert.That(document.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(collection.AtomicValues![0].StringValue, Is.EqualTo("member"));
            Assert.That(((RecordingCollectionResolver)context.CollectionResolver!).LastDocumentUri, Is.EqualTo("urn:document#part"));
            Assert.That(((RecordingCollectionResolver)context.CollectionResolver!).LastCollectionUri, Is.EqualTo("urn:collection#part"));
            Assert.That(text.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOUT1170));
            Assert.That(textAvailable.AtomicValues![0].BooleanValue, Is.False);
        });
    }

    [Test]
    public void DistinguishesInvalidUrisFromMissingStaticBases()
    {
        XPathDynamicContext context = DynamicContext(new RecordingCollectionResolver());

        XPathDynamicException relativeDocument = Assert.Throws<XPathDynamicException>(() => Evaluate("doc('relative.xml')", context))!;
        XPathDynamicException relativeCollection = Assert.Throws<XPathDynamicException>(() => Evaluate("collection('relative')", context))!;
        XPathDynamicException invalidDocument = Assert.Throws<XPathDynamicException>(() => Evaluate("doc('http://[')", context))!;
        XPathDynamicException invalidSchemeDocument = Assert.Throws<XPathDynamicException>(() => Evaluate("doc(':/')", context, "https://example.test/base/"))!;
        XPathDynamicException invalidCollection = Assert.Throws<XPathDynamicException>(() => Evaluate("collection('http://[')", context))!;
        XdmSequence available = Evaluate("doc-available('http://[')", context);

        Assert.Multiple(() =>
        {
            Assert.That(relativeDocument.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
            Assert.That(relativeCollection.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0002));
            Assert.That(invalidDocument.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0005));
            Assert.That(invalidSchemeDocument.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0005));
            Assert.That(invalidCollection.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FODC0004));
            Assert.That(available.AtomicValues![0].BooleanValue, Is.False);
        });
    }

    [Test]
    public void EmptyOptionalUrisProduceEmptyOrUnavailableResults()
    {
        XPathDynamicContext context = DynamicContext(new RecordingCollectionResolver());
        context.ResourceResolver = new RecordingTextResolver();

        XdmSequence result = Evaluate("(count(doc(())), doc-available(()), count(unparsed-text(())), unparsed-text-available(()))", context);

        Assert.That(result.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "0", "false", "0", "false" }));
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context)
    {
        return Evaluate(source, context, null);
    }

    private static XdmSequence Evaluate(string source, XPathDynamicContext context, string? staticBaseUri)
    {
        XPathStaticContext staticContext = new XPathStaticContext { BaseUri = staticBaseUri };
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, context).Value!;
    }

    private static XPathDynamicContext DynamicContext(IXPathCollectionResolver resolver)
    {
        return new XPathDynamicContext
        {
            CollectionResolver = resolver,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed, AllowedSchemes = ["https", "urn"] }
        };
    }

    private static XdmSequence StringSequence(string value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
        return new XdmSequenceOperations().CreateXdmAtomicSequence(atomic);
    }

    private static XmlDocument Parse(string source)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source };
        return new XmlParser().ParseXmlDocument(input, new XmlParserOptions(), null, null, null);
    }

    private sealed class RecordingCollectionResolver : IXPathCollectionResolver
    {
        internal int DocumentCalls;
        internal int CollectionCalls;
        internal int UriCollectionCalls;
        internal bool DocumentAvailable = true;
        internal bool FailCollections;
        internal string? LastDocumentUri;
        internal string? LastCollectionUri;
        internal string? LastUriCollectionUri;

        public XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy)
        {
            DocumentCalls = DocumentCalls + 1;
            LastDocumentUri = uri;
            return DocumentAvailable ? Parse("<root/>") : null;
        }

        public XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy)
        {
            CollectionCalls = CollectionCalls + 1;
            LastCollectionUri = uri;
            if (FailCollections)
            {
                throw new InvalidOperationException("missing collection");
            }

            return StringSequence("member");
        }

        public string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy)
        {
            UriCollectionCalls = UriCollectionCalls + 1;
            LastUriCollectionUri = uri;
            if (FailCollections)
            {
                throw new InvalidOperationException("missing URI collection");
            }

            return ["urn:first", "urn:second"];
        }
    }

    private sealed class RecordingTextResolver : IXmlResourceResolver
    {
        internal byte[] Bytes = Encoding.UTF8.GetBytes("text");
        internal int Calls;
        internal string? LastUri;
        internal XmlResolverPolicy? Policy;

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Calls = Calls + 1;
            LastUri = systemIdentifier;
            Policy = policy;
            XmlInput input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Bytes };
            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                Input = input,
                RequestedUri = systemIdentifier,
                AbsoluteUri = systemIdentifier
            };
        }
    }
}
