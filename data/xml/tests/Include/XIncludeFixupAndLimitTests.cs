using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XIncludeFixupAndLimitTests
{
    private const string Xi = "http://www.w3.org/2001/XInclude";

    [Test]
    public void FixesTopLevelBaseUriAndLanguageWithoutChangingDescendantSemantics()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("parts/selected.xml", "https://cdn.test/parts/selected.xml", "<selected xml:base='assets/' xml:lang='fr'><child/></selected>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "' xml:base='container/' xml:lang='en'><xi:include href='parts/selected.xml'/></root>");
        XmlIncludeOptions options = Options();
        options.FixupBaseUri = true;
        options.FixupLanguage = true;

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        XmlNode selected = document.DocumentElement!.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        Assert.Multiple(() =>
        {
            Assert.That(Attribute(selected, XmlNamespaces.Xml, "base")!.Value, Is.EqualTo("https://cdn.test/parts/assets/"));
            Assert.That(Attribute(selected, XmlNamespaces.Xml, "lang")!.Value, Is.EqualTo("fr"));
            Assert.That(navigator.GetXmlNodeBaseUri(selected), Is.EqualTo("https://cdn.test/parts/assets/"));
            Assert.That(navigator.GetXmlNodeBaseUri(selected.Children![0]), Is.EqualTo("https://cdn.test/parts/assets/"));
            Assert.That(navigator.GetXmlNodeLanguage(selected), Is.EqualTo("fr"));
            Assert.That(navigator.GetXmlNodeLanguage(selected.Children[0]), Is.EqualTo("fr"));
        });
    }

    [Test]
    public void AddsAnEmptyLanguageFixupWhenTheIncludedLanguageIsAbsent()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("neutral.xml", "https://cdn.test/neutral.xml", "<neutral/>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "' xml:lang='en'><xi:include href='neutral.xml'/></root>");
        XmlIncludeOptions options = Options();
        options.FixupLanguage = true;

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        XmlNode neutral = document.DocumentElement!.Children![0];
        Assert.That(Attribute(neutral, XmlNamespaces.Xml, "lang")!.Value, Is.Empty);
        Assert.That(new XmlTreeNavigator().GetXmlNodeLanguage(neutral), Is.Null);
    }

    [Test]
    public void PreservesInheritedNamespaceBindingsOnAnXPointerSelectedElement()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("namespaces.xml", "https://cdn.test/namespaces.xml", "<holder xmlns:p='urn:selected'><p:selected p:value='yes'/></holder>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='namespaces.xml' xpointer='element(/1/1)'/></root>");

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        XmlNode selected = document.DocumentElement!.Children![0];
        XmlNamespaceBinding[] bindings = new XmlTreeNavigator().SelectXmlNamespaces(selected);
        Assert.Multiple(() =>
        {
            Assert.That(selected.Name!.Prefix, Is.EqualTo("p"));
            Assert.That(selected.Name.NamespaceUri, Is.EqualTo("urn:selected"));
            Assert.That(bindings.Any(binding => binding.Prefix == "p" && binding.NamespaceUri == "urn:selected"), Is.True);
            Assert.That(selected.Attributes![0].Name!.NamespaceUri, Is.EqualTo("urn:selected"));
        });
    }

    [Test]
    public void RecordsRecursiveIncludeHistoryInOutermostToInnermostOrder()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("outer.xml", "https://cdn.test/outer.xml", "<container xmlns:xi='" + Xi + "'><xi:include href='inner.xml'/></container>");
        resolver.AddXml("inner.xml", "https://cdn.test/inner.xml", "<inner/>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='outer.xml' xpointer='element(/1/1)'/></root>");
        XmlIncludeOptions options = Options();
        options.ProcessNestedIncludes = true;

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        XmlNode included = document.DocumentElement!.Children![0];
        Assert.Multiple(() =>
        {
            Assert.That(included.Name!.LocalName, Is.EqualTo("inner"));
            Assert.That(included.IncludeHistory, Has.Length.EqualTo(2));
            Assert.That(Attribute(included.IncludeHistory![0], null, "href")!.Value, Is.EqualTo("outer.xml"));
            Assert.That(Attribute(included.IncludeHistory[1], null, "href")!.Value, Is.EqualTo("inner.xml"));
            Assert.That(included.IncludeHistory[0].Parent, Is.Null);
            Assert.That(included.IncludeHistory[1].Parent, Is.Null);
        });
    }

    [Test]
    public void RecordsIncludeHistoryOnTextAndFallbackCharacterItems()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddTextBytes("text.txt", Encoding.UTF8.GetBytes("included"));
        resolver.AddDenied("missing.xml");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='text.txt' parse='text'/>" +
            "<xi:include href='missing.xml'><xi:fallback>fallback</xi:fallback></xi:include>" +
            "</root>";
        XmlDocument document = Parse(source);

        new XmlIncludeProcessor().ProcessXmlIncludes(document, Options(), resolver);

        XmlNode[] children = document.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(2));
            Assert.That(children[0].Value, Is.EqualTo("included"));
            Assert.That(children[1].Value, Is.EqualTo("fallback"));
            Assert.That(Attribute(children[0].IncludeHistory![0], null, "href")!.Value, Is.EqualTo("text.txt"));
            Assert.That(Attribute(children[1].IncludeHistory![0], null, "href")!.Value, Is.EqualTo("missing.xml"));
        });
    }

    [Test]
    public void ExcludesIncludeContentAndOptionallyPreservesTheFallbackWrapper()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("actual.xml", "https://cdn.test/actual.xml", "<actual/>");
        resolver.AddDenied("missing.xml");
        string successfulSource = "<root xmlns:xi='" + Xi + "'><xi:include href='actual.xml'><ignored/><xi:fallback><unused/></xi:fallback></xi:include></root>";
        XmlDocument successful = Parse(successfulSource);

        new XmlIncludeProcessor().ProcessXmlIncludes(successful, Options(), resolver);

        Assert.Multiple(() =>
        {
            Assert.That(successful.DocumentElement!.Children, Has.Length.EqualTo(1));
            Assert.That(successful.DocumentElement.Children![0].Name!.LocalName, Is.EqualTo("actual"));
            Assert.That(successful.DocumentElement.Children[0].IncludeHistory, Has.Length.EqualTo(1));
        });

        string fallbackSource = "<root xmlns:xi='" + Xi + "'><xi:include href='missing.xml'><xi:fallback><replacement/></xi:fallback></xi:include></root>";
        XmlDocument ordinaryFallback = Parse(fallbackSource);
        new XmlIncludeProcessor().ProcessXmlIncludes(ordinaryFallback, Options(), resolver);
        Assert.That(ordinaryFallback.DocumentElement!.Children![0].Name!.LocalName, Is.EqualTo("replacement"));

        XmlDocument preservedFallback = Parse(fallbackSource);
        XmlIncludeOptions preserve = Options();
        preserve.PreserveFallbackNodes = true;
        new XmlIncludeProcessor().ProcessXmlIncludes(preservedFallback, preserve, resolver);
        XmlNode wrapper = preservedFallback.DocumentElement!.Children![0];
        Assert.Multiple(() =>
        {
            Assert.That(wrapper.Name!.NamespaceUri, Is.EqualTo(Xi));
            Assert.That(wrapper.Name.LocalName, Is.EqualTo("fallback"));
            Assert.That(wrapper.Children![0].Name!.LocalName, Is.EqualTo("replacement"));
            Assert.That(wrapper.IncludeHistory, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ProcessesNestedIncludesInsideAPreservedFallbackWrapper()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddDenied("missing.xml");
        resolver.AddXml("available.xml", "https://cdn.test/available.xml", "<available/>");
        string source = "<root xmlns:xi='" + Xi + "'><xi:include href='missing.xml'><xi:fallback><xi:include href='available.xml'/></xi:fallback></xi:include></root>";
        XmlDocument document = Parse(source);
        XmlIncludeOptions options = Options();
        options.PreserveFallbackNodes = true;
        options.ProcessNestedIncludes = true;

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        XmlNode wrapper = document.DocumentElement!.Children![0];
        Assert.That(wrapper.Name!.LocalName, Is.EqualTo("fallback"));
        Assert.That(wrapper.Children![0].Name!.LocalName, Is.EqualTo("available"));
        Assert.That(wrapper.Children[0].IncludeHistory, Has.Length.EqualTo(1));
    }

    [Test]
    public void EnforcesConfiguredIncludeDepthBeforeResolvingTheNextResource()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("a.xml", "https://cdn.test/a.xml", "<a xmlns:xi='" + Xi + "'><xi:include href='b.xml'/></a>");
        resolver.AddXml("b.xml", "https://cdn.test/b.xml", "<b xmlns:xi='" + Xi + "'><xi:include href='c.xml'/></b>");
        resolver.AddXml("c.xml", "https://cdn.test/c.xml", "<c/>");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='a.xml'/></root>");
        XmlIncludeOptions options = Options();
        options.ProcessNestedIncludes = true;
        options.MaximumDepth = 2;

        XmlLimitException failure = Assert.Throws<XmlLimitException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
            Assert.That(resolver.Requests, Is.EqualTo(new[] { "a.xml", "b.xml" }));
        });
    }

    [Test]
    public void UsesFallbackWhenXmlOrTextResourcesExceedTheConfiguredSize()
    {
        FixupResolver resolver = new FixupResolver();
        resolver.AddXml("large.xml", "https://cdn.test/large.xml", "<large/>");
        resolver.AddTextBytes("large.txt", Encoding.UTF8.GetBytes("12345"));
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='large.xml'><xi:fallback><xml-limit/></xi:fallback></xi:include>" +
            "<xi:include href='large.txt' parse='text'><xi:fallback><text-limit/></xi:fallback></xi:include>" +
            "</root>";
        XmlDocument document = Parse(source);
        XmlIncludeOptions options = Options();
        options.MaximumResourceBytes = 4;

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children![0].Name!.LocalName, Is.EqualTo("xml-limit"));
            Assert.That(document.DocumentElement.Children[1].Name!.LocalName, Is.EqualTo("text-limit"));
        });
    }

    [Test]
    public void RejectsNegativeIncludeLimits()
    {
        FixupResolver resolver = new FixupResolver();
        XmlDocument document = Parse("<root/>");
        XmlIncludeOptions depth = Options();
        depth.MaximumDepth = -1;
        XmlIncludeOptions bytes = Options();
        bytes.MaximumResourceBytes = -1;

        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, depth, resolver));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlIncludeProcessor().ProcessXmlIncludes(document, bytes, resolver));
    }

    private static XmlNode? Attribute(XmlNode element, string? namespaceUri, string localName)
    {
        return (element.Attributes ?? []).FirstOrDefault(attribute => attribute.Name != null && attribute.Name.LocalName == localName && attribute.Name.NamespaceUri == namespaceUri);
    }

    private static XmlDocument Parse(string source)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/source.xml" };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveSourceLocations = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlIncludeOptions Options()
    {
        return new XmlIncludeOptions
        {
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                MaximumDepth = 32,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 1048576
            }
        };
    }

    private sealed class FixupResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, FixupResource> resources = [];
        public readonly List<string> Requests = [];

        public void AddXml(string reference, string absoluteUri, string source)
        {
            resources[reference] = new FixupResource
            {
                Kind = XmlResourceKind.XIncludeXml,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = absoluteUri,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = source }
            };
        }

        public void AddTextBytes(string reference, byte[] bytes)
        {
            resources[reference] = new FixupResource
            {
                Kind = XmlResourceKind.XIncludeText,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = "https://cdn.test/" + reference,
                Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = bytes }
            };
        }

        public void AddDenied(string reference)
        {
            resources[reference] = new FixupResource
            {
                Kind = XmlResourceKind.XIncludeXml,
                Access = XmlResourceAccess.Denied,
                AbsoluteUri = "https://cdn.test/" + reference
            };
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            Requests.Add(systemIdentifier ?? "");
            if (systemIdentifier == null || !resources.TryGetValue(systemIdentifier, out FixupResource? resource))
            {
                throw new InvalidOperationException("No include resource was registered for " + systemIdentifier + ".");
            }

            return new XmlResolvedResource
            {
                Kind = resource.Kind,
                Access = resource.Access,
                Input = resource.Input,
                RequestedUri = systemIdentifier,
                AbsoluteUri = resource.AbsoluteUri
            };
        }
    }

    private sealed class FixupResource
    {
        public XmlResourceKind Kind;
        public XmlResourceAccess Access;
        public string? AbsoluteUri;
        public XmlInput? Input;
    }
}
