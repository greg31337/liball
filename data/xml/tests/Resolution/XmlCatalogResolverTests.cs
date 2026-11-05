using xml;

namespace xml.Tests;

[TestFixture]
public class XmlCatalogResolverTests
{
    private const string CatalogNamespace = "urn:oasis:names:tc:entity:xmlns:xml:catalog";

    [Test]
    public void RequiresCatalogLoadingBeforeResolution()
    {
        XmlCatalogResolver resolver = new XmlCatalogResolver();

        Assert.Throws<InvalidOperationException>(() => resolver.TryResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document"));
    }

    [Test]
    public void ResolvesMappedResourcesAndPropagatesNormalizedMetadata()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(XmlResourceKind.ExternalSubset, "https://mirror.test/root.dtd", "<!ELEMENT root EMPTY>", "https://mirror.test/root.dtd");
        XmlCatalogResolver resolver = new XmlCatalogResolver();
        resolver.LoadXmlCatalog([
            ExactSystem("urn:original", "https://mirror.test/root.dtd")
        ], resources, AllowedPolicy(false));

        XmlResolvedResource result = resolver.ResolveXmlCatalog(XmlResourceKind.ExternalSubset, "  -//Example//DTD   Root//EN ", "urn:original", null);

        Assert.That(result.Access, Is.EqualTo(XmlResourceAccess.Allowed));
        Assert.That(result.PublicIdentifier, Is.EqualTo("-//Example//DTD Root//EN"));
        Assert.That(result.SystemIdentifier, Is.EqualTo("https://mirror.test/root.dtd"));
        Assert.That(result.Input!.BaseUri, Is.EqualTo("https://mirror.test/root.dtd"));
        Assert.That(resources.Requests, Is.EqualTo(new string[] { "ExternalSubset|https://mirror.test/root.dtd" }));
    }

    [Test]
    public void ReportsCatalogMissesThroughThrowingAndExceptionlessApis()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        XmlCatalogResolver resolver = new XmlCatalogResolver();
        resolver.LoadXmlCatalog([], resources, AllowedPolicy(false));

        Assert.That(resolver.TryResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:missing"), Is.Null);
        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:missing"))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceNotFound));
        Assert.That(resources.Requests, Is.Empty);
    }

    [Test]
    public void LoadsAndParsesChainedCatalogsThroughTheCallerResolver()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        string catalogUri = "https://catalog.test/nested/catalog.xml";
        string catalogText = "<catalog xmlns='" + CatalogNamespace + "'>" +
            "<uri name='urn:document' uri='document.xml'/>" +
            "</catalog>";
        resources.AddStringResource(XmlResourceKind.Catalog, catalogUri, catalogText, catalogUri);
        resources.AddStringResource(XmlResourceKind.Document, "https://catalog.test/nested/document.xml", "<root/>", "https://catalog.test/nested/document.xml");
        XmlCatalogResolver resolver = new XmlCatalogResolver();
        resolver.LoadXmlCatalog([NextCatalog(catalogUri)], resources, AllowedPolicy(true));

        XmlResolvedResource first = resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document");
        XmlResolvedResource second = resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document");

        Assert.That(first.AbsoluteUri, Is.EqualTo("https://catalog.test/nested/document.xml"));
        Assert.That(second.AbsoluteUri, Is.EqualTo(first.AbsoluteUri));
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(resources.Requests, Is.EqualTo(new string[]
        {
            "Catalog|https://catalog.test/nested/catalog.xml",
            "Document|https://catalog.test/nested/document.xml"
        }));
    }

    [Test]
    public void ClearRemovesMatchAndResourceCachesWithoutUnloadingEntries()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(XmlResourceKind.Document, "https://mirror.test/document.xml", "<root/>", "https://mirror.test/document.xml");
        XmlCatalogResolver resolver = new XmlCatalogResolver();
        resolver.LoadXmlCatalog([
            ExactUri("urn:document", "https://mirror.test/document.xml")
        ], resources, AllowedPolicy(true));

        resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document");
        resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document");
        resolver.ClearXmlCatalogCache();
        resolver.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:document");

        Assert.That(resources.Requests, Has.Count.EqualTo(2));
        Assert.That(resolver.Entries, Has.Length.EqualTo(1));
    }

    [TestCase(XmlResourceKind.Document)]
    [TestCase(XmlResourceKind.XIncludeXml)]
    [TestCase(XmlResourceKind.XIncludeText)]
    [TestCase(XmlResourceKind.Schema)]
    [TestCase(XmlResourceKind.Stylesheet)]
    [TestCase(XmlResourceKind.Package)]
    public void ResourceManagerUsesUriCatalogsForDocumentIncludeSchemaAndXsltResources(XmlResourceKind kind)
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(kind, "https://mirror.test/resource.xml", "<resource/>", "https://mirror.test/resource.xml");
        XmlResolverPolicy policy = AllowedPolicy(false);
        policy.CatalogEntries = [ExactUri("urn:original", "https://mirror.test/resource.xml")];
        ResourceManager manager = new ResourceManager(resources, policy);

        XmlResolvedResource result = manager.ResolveXmlResource(kind, null, "urn:original", "https://base.test/source.xml", policy);

        Assert.That(result.AbsoluteUri, Is.EqualTo("https://mirror.test/resource.xml"));
        Assert.That(resources.Requests, Is.EqualTo(new string[] { kind + "|https://mirror.test/resource.xml" }));
    }

    [TestCase(XmlResourceKind.ExternalSubset)]
    [TestCase(XmlResourceKind.GeneralEntity)]
    [TestCase(XmlResourceKind.ParameterEntity)]
    public void ResourceManagerUsesExternalIdentifierCatalogsForDtdResources(XmlResourceKind kind)
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(kind, "https://mirror.test/entity.dtd", "replacement", "https://mirror.test/entity.dtd");
        XmlResolverPolicy policy = AllowedPolicy(false);
        policy.CatalogEntries = [ExactSystem("original.dtd", "https://mirror.test/entity.dtd")];
        ResourceManager manager = new ResourceManager(resources, policy);

        XmlResolvedResource result = manager.ResolveXmlResource(kind, "-//Example//DTD Entity//EN", "original.dtd", "https://base.test/document.xml", policy);

        Assert.That(result.AbsoluteUri, Is.EqualTo("https://mirror.test/entity.dtd"));
        Assert.That(resources.Requests, Is.EqualTo(new string[] { kind + "|https://mirror.test/entity.dtd" }));
    }

    [Test]
    public void ResourceManagerFallsBackToCallerResolverAfterCatalogMiss()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(XmlResourceKind.Schema, "schema.xsd", "<schema/>", "https://base.test/schema.xsd");
        XmlResolverPolicy policy = AllowedPolicy(false);
        policy.CatalogEntries = [ExactUri("urn:mapped", "https://mirror.test/schema.xsd")];
        ResourceManager manager = new ResourceManager(resources, policy);

        XmlResolvedResource result = manager.ResolveXmlResource(XmlResourceKind.Schema, null, "schema.xsd", "https://base.test/document.xml", policy);

        Assert.That(result.AbsoluteUri, Is.EqualTo("https://base.test/schema.xsd"));
        Assert.That(resources.BaseUris, Is.EqualTo(new string?[] { "https://base.test/document.xml" }));
        Assert.That(resources.Requests, Is.EqualTo(new string[] { "Schema|schema.xsd" }));
    }

    [Test]
    public void DtdLoadingConsultsCatalogBeforeCallerResolverFallback()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(XmlResourceKind.ExternalSubset, "https://mirror.test/root.dtd", "<!ELEMENT root EMPTY>", "https://mirror.test/root.dtd");
        XmlResolverPolicy policy = AllowedPolicy(false);
        policy.CatalogEntries = [ExactSystem("original.dtd", "https://mirror.test/root.dtd")];
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root SYSTEM 'original.dtd'><root/>";
        input.BaseUri = "https://source.test/document.xml";

        XmlDocument document = new XmlParser().ParseXmlDocument(input, ParserOptions(policy), resources, null, null);

        Assert.That(document.DocumentElement!.Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(resources.Requests, Is.EqualTo(new string[] { "ExternalSubset|https://mirror.test/root.dtd" }));
    }

    [Test]
    public void DtdLoadingFallsBackToOriginalIdentifierWhenCatalogMisses()
    {
        RecordingResourceResolver resources = new RecordingResourceResolver();
        resources.AddStringResource(XmlResourceKind.ExternalSubset, "original.dtd", "<!ELEMENT root EMPTY>", "https://source.test/original.dtd");
        XmlResolverPolicy policy = AllowedPolicy(false);
        policy.CatalogEntries = [ExactSystem("other.dtd", "https://mirror.test/other.dtd")];
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root SYSTEM 'original.dtd'><root/>";
        input.BaseUri = "https://source.test/document.xml";

        new XmlParser().ParseXmlDocument(input, ParserOptions(policy), resources, null, null);

        Assert.That(resources.Requests, Is.EqualTo(new string[] { "ExternalSubset|original.dtd" }));
        Assert.That(resources.BaseUris, Is.EqualTo(new string?[] { "https://source.test/document.xml" }));
    }

    private static XmlCatalogEntry ExactSystem(string identifier, string target)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.System;
        entry.SystemIdentifier = identifier;
        entry.TargetUri = target;
        return entry;
    }

    private static XmlCatalogEntry ExactUri(string identifier, string target)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.Uri;
        entry.Uri = identifier;
        entry.TargetUri = target;
        return entry;
    }

    private static XmlCatalogEntry NextCatalog(string uri)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.NextCatalog;
        entry.CatalogUri = uri;
        return entry;
    }

    private static XmlResolverPolicy AllowedPolicy(bool cacheResources)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        policy.CacheResources = cacheResources;
        return policy;
    }

    private static XmlParserOptions ParserOptions(XmlResolverPolicy policy)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.ResolverPolicy = policy;
        return options;
    }

    private sealed class RecordingResourceResolver : IXmlResourceResolver
    {
        internal Dictionary<string, XmlResolvedResource> Resources = new Dictionary<string, XmlResolvedResource>(StringComparer.Ordinal);
        internal List<string> Requests = new List<string>();
        internal List<string?> BaseUris = new List<string?>();

        internal void AddStringResource(XmlResourceKind kind, string identifier, string text, string absoluteUri)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = absoluteUri;
            Resources.Add(kind + "|" + identifier, resource);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string identifier = systemIdentifier == null ? "" : systemIdentifier;
            string key = kind + "|" + identifier;
            Requests.Add(key);
            BaseUris.Add(baseUri);
            if (Resources.ContainsKey(key))
            {
                return Resources[key];
            }

            XmlResolvedResource denied = new XmlResolvedResource();
            denied.Kind = kind;
            denied.Access = XmlResourceAccess.Denied;
            return denied;
        }
    }
}
