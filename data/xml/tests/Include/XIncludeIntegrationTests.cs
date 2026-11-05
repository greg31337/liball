using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XIncludeIntegrationTests
{
    private const string Xi = "http://www.w3.org/2001/XInclude";

    [Test]
    public void AppliesIncludeCatalogsToXmlAndTextResources()
    {
        CatalogIncludeResolver resolver = new CatalogIncludeResolver();
        resolver.AddXml("https://mirror.test/part.xml", "<part/>");
        resolver.AddText("https://mirror.test/value.txt", "catalog text");
        XmlDocument document = Parse("<root xmlns:xi='" + Xi + "'><xi:include href='urn:part'/><xi:include href='urn:value' parse='text'/></root>", "https://source.test/document.xml");
        XmlResolverPolicy callerPolicy = AllowedPolicy();
        callerPolicy.AllowedSchemes = ["https"];
        XmlIncludeOptions options = new XmlIncludeOptions
        {
            ResolverPolicy = callerPolicy,
            CatalogEntries =
            [
                ExactUri("urn:part", "https://mirror.test/part.xml"),
                ExactUri("urn:value", "https://mirror.test/value.txt")
            ]
        };

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        XmlNode[] children = document.DocumentElement!.Children!;
        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(2));
            Assert.That(children[0].Name!.LocalName, Is.EqualTo("part"));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(children[1].Value, Is.EqualTo("catalog text"));
            Assert.That(resolver.Identifiers, Is.EqualTo(new[] { "https://mirror.test/part.xml", "https://mirror.test/value.txt" }));
            Assert.That(resolver.Kinds, Is.EqualTo(new[] { XmlResourceKind.XIncludeXml, XmlResourceKind.XIncludeText }));
            Assert.That(resolver.Policies, Has.All.Matches<XmlResolverPolicy>(policy => policy.AllowedSchemes!.SequenceEqual(new[] { "https" })));
            Assert.That(callerPolicy.CatalogEntries, Is.Null);
        });
    }

    [Test]
    public void ResolvesSameDocumentPointersAgainstTheOriginalSourceInfoset()
    {
        CatalogIncludeResolver resolver = new CatalogIncludeResolver();
        resolver.AddXml("part.xml", "<part/>");
        string source = "<root xmlns:xi='" + Xi + "'>" +
            "<xi:include href='part.xml'/>" +
            "<xi:include href='' xpointer='element(/1/1)'/>" +
            "</root>";
        XmlDocument document = Parse(source, "https://source.test/document.xml");
        XmlIncludeOptions options = new XmlIncludeOptions
        {
            ProcessNestedIncludes = true,
            ResolverPolicy = AllowedPolicy()
        };

        new XmlIncludeProcessor().ProcessXmlIncludes(document, options, resolver);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentElement!.Children, Has.Length.EqualTo(2));
            Assert.That(document.DocumentElement.Children!.Select(node => node.Name!.LocalName), Is.EqualTo(new[] { "part", "part" }));
            Assert.That(resolver.Identifiers, Is.EqualTo(new[] { "part.xml", "part.xml" }));
        });
    }

    [Test]
    public void DefaultResolverHonorsDeniedAndRootRestrictedFilePolicies()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xml-xinclude-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "included.xml");
        File.WriteAllText(path, "<included/>", Encoding.UTF8);
        string href = new Uri(path).AbsoluteUri;

        try
        {
            string source = "<root xmlns:xi='" + Xi + "'><xi:include href='" + href + "'><xi:fallback><denied/></xi:fallback></xi:include></root>";
            XmlDocument denied = Parse(source, "https://source.test/document.xml");
            new XmlIncludeProcessor().ProcessXmlIncludes(denied, new XmlIncludeOptions(), new XmlDefaultResolver());

            XmlDocument allowed = Parse(source, "https://source.test/document.xml");
            XmlResolverPolicy policy = AllowedPolicy();
            policy.AllowedSchemes = ["file"];
            policy.AllowedRoots = [directory];
            XmlIncludeOptions options = new XmlIncludeOptions { ResolverPolicy = policy };
            new XmlIncludeProcessor().ProcessXmlIncludes(allowed, options, new XmlDefaultResolver());

            Assert.Multiple(() =>
            {
                Assert.That(denied.DocumentElement!.Children![0].Name!.LocalName, Is.EqualTo("denied"));
                Assert.That(allowed.DocumentElement!.Children![0].Name!.LocalName, Is.EqualTo("included"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static XmlCatalogEntry ExactUri(string identifier, string target)
    {
        return new XmlCatalogEntry
        {
            Kind = XmlCatalogEntryKind.Uri,
            Uri = identifier,
            TargetUri = target
        };
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            MaximumDepth = 32,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
    }

    private static XmlDocument Parse(string source, string baseUri)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri };
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

    private sealed class CatalogIncludeResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, CatalogIncludeResource> resources = [];
        public readonly List<string> Identifiers = [];
        public readonly List<XmlResourceKind> Kinds = [];
        public readonly List<XmlResolverPolicy> Policies = [];

        public void AddXml(string identifier, string source)
        {
            resources[identifier] = new CatalogIncludeResource
            {
                Kind = XmlResourceKind.XIncludeXml,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = source }
            };
        }

        public void AddText(string identifier, string source)
        {
            resources[identifier] = new CatalogIncludeResource
            {
                Kind = XmlResourceKind.XIncludeText,
                Input = new XmlInput { Kind = XmlInputKind.ByteMemory, Bytes = Encoding.UTF8.GetBytes(source) }
            };
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            if (systemIdentifier == null || !resources.TryGetValue(systemIdentifier, out CatalogIncludeResource? resource))
            {
                throw new InvalidOperationException("No include resource was registered for " + systemIdentifier + ".");
            }

            Identifiers.Add(systemIdentifier);
            Kinds.Add(kind);
            Policies.Add(policy);
            return new XmlResolvedResource
            {
                Kind = resource.Kind,
                Access = XmlResourceAccess.Allowed,
                Input = resource.Input,
                RequestedUri = systemIdentifier,
                AbsoluteUri = systemIdentifier
            };
        }
    }

    private sealed class CatalogIncludeResource
    {
        public XmlResourceKind Kind;
        public XmlInput? Input;
    }
}
