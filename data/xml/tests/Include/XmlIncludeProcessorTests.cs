using xml;

namespace xml.Tests;

[TestFixture]
public class XmlIncludeProcessorTests
{
    private const string Xi = "http://www.w3.org/2001/XInclude";

    [Test]
    public void IncludesXmlRecursivelyAndUsesResolvedDocumentUriAsNestedBase()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.Add("dir/outer.xml", "https://example.test/base/dir/outer.xml", "<outer xmlns:xi='" + Xi + "'><xi:include href='inner.xml'/></outer>");
        resolver.Add("inner.xml", "https://example.test/base/dir/inner.xml", "<inner/>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='dir/outer.xml'/></root>", "https://example.test/base/source.xml");
        long originalVersion = document.MutationVersion;

        XmlDocument result = new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(true), resolver);

        XmlNode root = result.DocumentElement!;
        XmlNode outer = ElementChildren(root)[0];
        XmlNode inner = ElementChildren(outer)[0];
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(document));
            Assert.That(outer.Name!.LocalName, Is.EqualTo("outer"));
            Assert.That(inner.Name!.LocalName, Is.EqualTo("inner"));
            Assert.That(outer.OwnerDocument, Is.SameAs(document));
            Assert.That(inner.Parent, Is.SameAs(outer));
            Assert.That(document.DocumentUri, Is.EqualTo("https://example.test/base/source.xml"));
            Assert.That(document.BaseUri, Is.EqualTo("https://example.test/base/source.xml"));
            Assert.That(document.MutationVersion, Is.GreaterThan(originalVersion));
            Assert.That(resolver.Requests, Has.Count.EqualTo(2));
            Assert.That(resolver.Requests[0].Kind, Is.EqualTo(XmlResourceKind.XIncludeXml));
            Assert.That(resolver.Requests[0].BaseUri, Is.EqualTo("https://example.test/base/source.xml"));
            Assert.That(resolver.Requests[1].BaseUri, Is.EqualTo("https://example.test/base/dir/outer.xml"));
        });
    }

    [Test]
    public void SelectsAnExternalElementWithXPointer()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.Add("parts.xml", "https://example.test/parts.xml", "<parts><first/><second><inside/></second></parts>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><before/><xi:include href='parts.xml' xpointer='element(/1/2)'/><after/></root>", "https://example.test/source.xml");

        new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver);

        XmlNode[] children = ElementChildren(document.DocumentElement!);
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(children[0].Name!.LocalName, Is.EqualTo("before"));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("second"));
            Assert.That(ElementChildren(children[1])[0].Name!.LocalName, Is.EqualTo("inside"));
            Assert.That(children[2].Name!.LocalName, Is.EqualTo("after"));
        });
    }

    [Test]
    public void IncludesDocumentChildrenButExcludesTheDocumentType()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.Add("document.xml", "https://example.test/document.xml", "<?before data?><!DOCTYPE included><included/><?after data?>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='document.xml'/></root>", "https://example.test/source.xml");

        new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver);

        XmlNode[] children = document.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(children[0].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Element));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("included"));
            Assert.That(children[2].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction));
            Assert.That(children, Has.None.Matches<XmlNode>(node => node.Kind == XmlNodeKind.DocumentType));
        });
    }

    [Test]
    public void SelectsAndCopiesAnElementFromTheIncludingDocument()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><item xml:id='chosen'><value/></item><xi:include href='' xpointer='chosen'/></root>", "https://example.test/source.xml");
        XmlNode original = ElementChildren(document.DocumentElement!)[0];

        new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver);

        XmlNode[] children = ElementChildren(document.DocumentElement!);
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(2));
            Assert.That(children[0], Is.SameAs(original));
            Assert.That(children[1], Is.Not.SameAs(original));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("item"));
            Assert.That(children[1].Identity, Is.Not.EqualTo(original.Identity));
            Assert.That(resolver.Requests, Is.Empty);
        });
    }

    [Test]
    public void UsesFallbackForAResolverExceptionAndProcessesNestedFallbackIncludes()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.ThrowFor = "missing.xml";
        resolver.Add("available.xml", "https://example.test/available.xml", "<available/>");
        string source = "<root xmlns:xi='" + Xi + "'><xi:include href='missing.xml'><xi:fallback><replacement><xi:include href='available.xml'/></replacement></xi:fallback></xi:include></root>";
        XmlDocument document = Parse(source, "https://example.test/source.xml");

        new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(true), resolver);

        XmlNode replacement = ElementChildren(document.DocumentElement!)[0];
        Assert.Multiple(() =>
        {
            Assert.That(replacement.Name!.LocalName, Is.EqualTo("replacement"));
            Assert.That(ElementChildren(replacement)[0].Name!.LocalName, Is.EqualTo("available"));
            Assert.That(resolver.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void UsesFallbackForDeniedResourcesAndXPointerSelectionFailures()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.AddDenied("denied.xml", "https://example.test/denied.xml");
        resolver.Add("parts.xml", "https://example.test/parts.xml", "<parts/>");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='denied.xml'><xi:fallback><denied-fallback/></xi:fallback></xi:include>" +
            "<xi:include href='parts.xml' xpointer='missing'><xi:fallback><pointer-fallback/></xi:fallback></xi:include>" +
            "</root>";
        XmlDocument document = Parse(source, "https://example.test/source.xml");

        new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver);

        XmlNode[] children = ElementChildren(document.DocumentElement!);
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(2));
            Assert.That(children[0].Name!.LocalName, Is.EqualTo("denied-fallback"));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("pointer-fallback"));
        });
    }

    [Test]
    public void ReportsMissingFallbackAsAnIncludeResourceError()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.AddDenied("denied.xml", "https://example.test/denied.xml");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='denied.xml'/></root>", "https://example.test/source.xml");

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic, Is.Not.Null);
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFallbackMissing));
            Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Include));
            Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        });
    }

    [Test]
    public void DetectsRecursiveIncludeLoopsButAllowsSequentialReuse()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.Add("a.xml", "https://example.test/a.xml", "<a xmlns:xi='" + Xi + "'><xi:include href='b.xml'/></a>");
        resolver.Add("b.xml", "https://example.test/b.xml", "<b xmlns:xi='" + Xi + "'><xi:include href='a.xml'/></b>");
        XmlDocument cyclic = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='a.xml'/></root>", "https://example.test/source.xml");

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(cyclic, IncludeOptions(true), resolver))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeCycle));

        RecordingIncludeResolver reusableResolver = new RecordingIncludeResolver();
        reusableResolver.Add("part.xml", "https://example.test/part.xml", "<part/>");
        XmlDocument reusable = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='part.xml'/><xi:include href='part.xml'/></root>", "https://example.test/source.xml");

        new XmlIncludeProcessor().ProcessXmlIncludes(reusable, IncludeOptions(true), reusableResolver);

        Assert.That(ElementChildren(reusable.DocumentElement!), Has.Length.EqualTo(2));
        Assert.That(reusableResolver.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void TreatsMalformedAcquiredXmlAsFatalInsteadOfUsingFallback()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        resolver.Add("broken.xml", "https://example.test/broken.xml", "<broken>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='broken.xml'><xi:fallback><unused/></xi:fallback></xi:include></root>", "https://example.test/source.xml");

        Assert.Throws<XmlWellFormednessException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver));
    }

    [Test]
    public void RejectsFragmentsInvalidParseModesAndMisplacedFallbackElements()
    {
        RecordingIncludeResolver resolver = new RecordingIncludeResolver();
        string[] documents =
        [
            "<root xmlns:xi='" + Xi + "'><xi:include href='part.xml#section'/></root>",
            "<root xmlns:xi='" + Xi + "'><xi:include href='part.xml' parse='binary'/></root>",
            "<root xmlns:xi='" + Xi + "'><xi:fallback/></root>"
        ];

        foreach (string source in documents)
        {
            XmlDocument document = Parse(source, "https://example.test/source.xml");
            XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, IncludeOptions(false), resolver))!;
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.IncludeFailure));
        }
    }

    private static XmlDocument Parse(string text, string baseUri)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = baseUri };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveSourceLocations = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlIncludeOptions IncludeOptions(bool nested)
    {
        return new XmlIncludeOptions
        {
            ProcessNestedIncludes = nested,
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                MaximumDepth = 32,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private static XmlNode[] ElementChildren(XmlNode parent)
    {
        return (parent.Children ?? []).Where(node => node.Kind == XmlNodeKind.Element).ToArray();
    }

    private sealed class RecordingIncludeResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, IncludeResource> resources = [];
        public readonly List<IncludeRequest> Requests = [];
        public string? ThrowFor;

        public void Add(string reference, string absoluteUri, string text)
        {
            resources[reference] = new IncludeResource
            {
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = absoluteUri,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = text }
            };
        }

        public void AddDenied(string reference, string absoluteUri)
        {
            resources[reference] = new IncludeResource
            {
                Access = XmlResourceAccess.Denied,
                AbsoluteUri = absoluteUri
            };
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Requests.Add(new IncludeRequest { Kind = kind, SystemIdentifier = systemIdentifier, BaseUri = baseUri, Policy = policy });
            if (systemIdentifier == ThrowFor)
            {
                throw new InvalidOperationException("The requested include is unavailable.");
            }

            if (systemIdentifier == null || !resources.TryGetValue(systemIdentifier, out IncludeResource? entry))
            {
                throw new InvalidOperationException("No test include was registered for " + systemIdentifier + ".");
            }

            return new XmlResolvedResource
            {
                Kind = XmlResourceKind.XIncludeXml,
                Access = entry.Access,
                Input = entry.Input,
                RequestedUri = systemIdentifier,
                AbsoluteUri = entry.AbsoluteUri
            };
        }
    }

    private sealed class IncludeResource
    {
        public XmlResourceAccess Access;
        public string? AbsoluteUri;
        public XmlInput? Input;
    }

    private sealed class IncludeRequest
    {
        public XmlResourceKind Kind;
        public string? SystemIdentifier;
        public string? BaseUri;
        public XmlResolverPolicy? Policy;
    }
}
