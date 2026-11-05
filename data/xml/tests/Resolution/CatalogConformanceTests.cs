using xml;

namespace xml.Tests;

[TestFixture]
public class CatalogConformanceTests
{
    private const string CatalogNamespace = "urn:oasis:names:tc:entity:xmlns:xml:catalog";

    [Test]
    public void ResolvesEveryDirectCatalogEntryKindFromParsedXml()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "' xml:base='https://mirror.test/'>" +
            "<public publicId='-//Example//DTD Public//EN' uri='public.dtd'/>" +
            "<system systemId='urn:system:exact' uri='system.dtd'/>" +
            "<uri name='urn:uri:exact' uri='uri.xml'/>" +
            "<rewriteSystem systemIdStartString='urn:system:rewrite:' rewritePrefix='system/'/>" +
            "<rewriteURI uriStartString='urn:uri:rewrite:' rewritePrefix='uri/'/>" +
            "<systemSuffix systemIdSuffix='.system-suffix' uri='system-suffix.dtd'/>" +
            "<uriSuffix uriSuffix='.uri-suffix' uri='uri-suffix.xml'/>" +
            "</catalog>";
        CatalogMatcher matcher = new CatalogMatcher(Parse(text, "https://catalog.test/catalog.xml", false), null, false);

        Assert.That(matcher.ResolveExternalIdentifier("-//Example//DTD Public//EN", null), Is.EqualTo("https://mirror.test/public.dtd"));
        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:system:exact"), Is.EqualTo("https://mirror.test/system.dtd"));
        Assert.That(matcher.ResolveUriIdentifier("urn:uri:exact"), Is.EqualTo("https://mirror.test/uri.xml"));
        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:system:rewrite:item.dtd"), Is.EqualTo("https://mirror.test/system/item.dtd"));
        Assert.That(matcher.ResolveUriIdentifier("urn:uri:rewrite:item.xml"), Is.EqualTo("https://mirror.test/uri/item.xml"));
        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:other:item.system-suffix"), Is.EqualTo("https://mirror.test/system-suffix.dtd"));
        Assert.That(matcher.ResolveUriIdentifier("urn:other:item.uri-suffix"), Is.EqualTo("https://mirror.test/uri-suffix.xml"));
    }

    [Test]
    public void PreferScopesSelectOnlyEligiblePublicEntriesWhenSystemIdIsPresent()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "' prefer='system' xml:base='https://mirror.test/'>" +
            "<public publicId='-//Example//DTD Scoped//EN' uri='root.dtd'/>" +
            "<group prefer='public'><public publicId='-//Example//DTD Scoped//EN' uri='group.dtd'/></group>" +
            "</catalog>";
        CatalogMatcher matcher = new CatalogMatcher(Parse(text, "https://catalog.test/catalog.xml", true), null, false);

        Assert.That(matcher.ResolveExternalIdentifier("-//Example//DTD Scoped//EN", "urn:missing"), Is.EqualTo("https://mirror.test/group.dtd"));
        Assert.That(matcher.ResolveExternalIdentifier("-//Example//DTD Scoped//EN", null), Is.EqualTo("https://mirror.test/root.dtd"));
    }

    [Test]
    public void ExactRewriteAndSuffixPrecedenceDoesNotDependOnDocumentOrder()
    {
        CatalogMatcher systemMatcher = new CatalogMatcher([
            SystemSuffix("item.dtd", "https://mirror.test/suffix.dtd"),
            RewriteSystem("urn:source:", "https://mirror.test/rewrite/"),
            ExactSystem("urn:source:item.dtd", "https://mirror.test/exact.dtd")
        ], null, false);
        Assert.That(systemMatcher.ResolveExternalIdentifier(null, "urn:source:item.dtd"), Is.EqualTo("https://mirror.test/exact.dtd"));

        CatalogMatcher systemRewriteMatcher = new CatalogMatcher([
            SystemSuffix("item.dtd", "https://mirror.test/suffix.dtd"),
            RewriteSystem("urn:source:", "https://mirror.test/rewrite/")
        ], null, false);
        Assert.That(systemRewriteMatcher.ResolveExternalIdentifier(null, "urn:source:item.dtd"), Is.EqualTo("https://mirror.test/rewrite/item.dtd"));

        CatalogMatcher uriMatcher = new CatalogMatcher([
            UriSuffix("item.xml", "https://mirror.test/suffix.xml"),
            RewriteUri("urn:source:", "https://mirror.test/rewrite/"),
            ExactUri("urn:source:item.xml", "https://mirror.test/exact.xml")
        ], null, false);
        Assert.That(uriMatcher.ResolveUriIdentifier("urn:source:item.xml"), Is.EqualTo("https://mirror.test/exact.xml"));
    }

    [Test]
    public void UriRewriteAndSuffixRulesUseLongestMatchesAndFirstEqualDuplicate()
    {
        CatalogMatcher rewriteMatcher = new CatalogMatcher([
            RewriteUri("urn:source:", "https://mirror.test/broad/"),
            RewriteUri("urn:source:path:", "https://mirror.test/first/"),
            RewriteUri("urn:source:path:", "https://mirror.test/second/")
        ], null, false);
        Assert.That(rewriteMatcher.ResolveUriIdentifier("urn:source:path:item.xml"), Is.EqualTo("https://mirror.test/first/item.xml"));

        CatalogMatcher suffixMatcher = new CatalogMatcher([
            UriSuffix(".xml", "https://mirror.test/broad.xml"),
            UriSuffix("item.xml", "https://mirror.test/first.xml"),
            UriSuffix("item.xml", "https://mirror.test/second.xml")
        ], null, false);
        Assert.That(suffixMatcher.ResolveUriIdentifier("urn:source:item.xml"), Is.EqualTo("https://mirror.test/first.xml"));
    }

    [Test]
    public void PublicAndUriDelegationAreExclusiveAndOrderedByLongestPrefix()
    {
        MemoryCatalogSource publicSource = new MemoryCatalogSource();
        publicSource.Catalogs.Add("https://catalog.test/public-long.xml", []);
        publicSource.Catalogs.Add("https://catalog.test/public-short.xml", [ExactPublic("-//Example//DTD Delegated//EN", "https://mirror.test/public.dtd", false)]);
        CatalogMatcher publicMatcher = new CatalogMatcher([
            DelegatePublic("-//Example//", "https://catalog.test/public-short.xml", false),
            DelegatePublic("-//Example//DTD ", "https://catalog.test/public-long.xml", false),
            ExactPublic("-//Example//DTD Delegated//EN", "https://mirror.test/root.dtd", false)
        ], publicSource, false);
        Assert.That(publicMatcher.ResolveExternalIdentifier("-//Example//DTD Delegated//EN", null), Is.EqualTo("https://mirror.test/root.dtd"));

        CatalogMatcher publicOnlyDelegates = new CatalogMatcher([
            DelegatePublic("-//Example//", "https://catalog.test/public-short.xml", false),
            DelegatePublic("-//Example//DTD ", "https://catalog.test/public-long.xml", false)
        ], publicSource, false);
        Assert.That(publicOnlyDelegates.ResolveExternalIdentifier("-//Example//DTD Delegated//EN", null), Is.EqualTo("https://mirror.test/public.dtd"));
        Assert.That(publicSource.Requests, Is.EqualTo(new string[] { "https://catalog.test/public-long.xml", "https://catalog.test/public-short.xml" }));

        MemoryCatalogSource uriSource = new MemoryCatalogSource();
        uriSource.Catalogs.Add("https://catalog.test/uri-long.xml", []);
        uriSource.Catalogs.Add("https://catalog.test/uri-short.xml", [ExactUri("urn:source:path:item", "https://mirror.test/uri.xml")]);
        uriSource.Catalogs.Add("https://catalog.test/next.xml", [ExactUri("urn:source:path:item", "https://mirror.test/next.xml")]);
        CatalogMatcher uriMatcher = new CatalogMatcher([
            DelegateUri("urn:source:", "https://catalog.test/uri-short.xml"),
            DelegateUri("urn:source:path:", "https://catalog.test/uri-long.xml"),
            NextCatalog("https://catalog.test/next.xml")
        ], uriSource, false);
        Assert.That(uriMatcher.ResolveUriIdentifier("urn:source:path:item"), Is.EqualTo("https://mirror.test/uri.xml"));
        Assert.That(uriSource.Requests, Is.EqualTo(new string[] { "https://catalog.test/uri-long.xml", "https://catalog.test/uri-short.xml" }));
    }

    [Test]
    public void SystemDelegationIgnoresTheOriginalPublicIdentifier()
    {
        MemoryCatalogSource source = new MemoryCatalogSource();
        source.Catalogs.Add("https://catalog.test/system.xml", [
            ExactPublic("-//Example//DTD Item//EN", "https://mirror.test/public.dtd", true)
        ]);
        CatalogMatcher matcher = new CatalogMatcher([
            DelegateSystem("urn:system:", "https://catalog.test/system.xml")
        ], source, false);

        string? resolved = matcher.ResolveExternalIdentifier("-//Example//DTD Item//EN", "urn:system:item");

        Assert.That(resolved, Is.Null);
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/system.xml" }));
    }

    [Test]
    public void NextCatalogInsertionProcessesNestedChainsBeforeLaterSiblings()
    {
        MemoryCatalogSource source = new MemoryCatalogSource();
        source.Catalogs.Add("https://catalog.test/a.xml", [NextCatalog("https://catalog.test/c.xml")]);
        source.Catalogs.Add("https://catalog.test/b.xml", [ExactUri("urn:item", "https://mirror.test/b.xml")]);
        source.Catalogs.Add("https://catalog.test/c.xml", [ExactUri("urn:item", "https://mirror.test/c.xml")]);
        CatalogMatcher matcher = new CatalogMatcher([
            NextCatalog("https://catalog.test/a.xml"),
            NextCatalog("https://catalog.test/b.xml")
        ], source, false);

        Assert.That(matcher.ResolveUriIdentifier("urn:item"), Is.EqualTo("https://mirror.test/c.xml"));
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/a.xml", "https://catalog.test/c.xml" }));
    }

    [Test]
    public void UnavailableCatalogsAreMissesAndDoNotStopNormalChains()
    {
        MemoryCatalogSource source = new MemoryCatalogSource();
        source.Catalogs.Add("https://catalog.test/available.xml", [ExactSystem("urn:item", "https://mirror.test/item.dtd")]);
        CatalogMatcher matcher = new CatalogMatcher([
            NextCatalog("https://catalog.test/unavailable.xml"),
            NextCatalog("https://catalog.test/available.xml")
        ], source, true);

        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:item"), Is.EqualTo("https://mirror.test/item.dtd"));
        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:other"), Is.Null);
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/unavailable.xml", "https://catalog.test/available.xml" }));
    }

    [Test]
    public void UriPublicIdentifierUrnsRestartAsPublicIdentifierResolution()
    {
        CatalogMatcher matcher = new CatalogMatcher([
            ExactUri("urn:publicid:-:Example:DTD+Item:EN", "https://mirror.test/literal-uri"),
            ExactPublic("-//Example//DTD Item//EN", "https://mirror.test/public.dtd", false)
        ], null, false);

        Assert.That(matcher.ResolveUriIdentifier("urn:publicid:-:Example:DTD+Item:EN"), Is.EqualTo("https://mirror.test/public.dtd"));
    }

    [Test]
    public void NestedProgrammaticGroupsRetainDocumentOrder()
    {
        XmlCatalogEntry inner = new XmlCatalogEntry();
        inner.Kind = XmlCatalogEntryKind.Group;
        inner.Entries = [ExactUri("urn:item", "https://mirror.test/inner.xml")];
        XmlCatalogEntry outer = new XmlCatalogEntry();
        outer.Kind = XmlCatalogEntryKind.Group;
        outer.Entries = [inner, ExactUri("urn:item", "https://mirror.test/outer.xml")];
        CatalogMatcher matcher = new CatalogMatcher([
            outer,
            ExactUri("urn:item", "https://mirror.test/root.xml")
        ], null, false);

        Assert.That(matcher.ResolveUriIdentifier("urn:item"), Is.EqualTo("https://mirror.test/inner.xml"));
    }

    [Test]
    public void RejectsMalformedProgrammaticEntriesAndGroupCyclesWhenReached()
    {
        XmlCatalogEntry exact = new XmlCatalogEntry();
        exact.Kind = XmlCatalogEntryKind.System;
        exact.SystemIdentifier = "urn:item";
        Assert.Throws<FormatException>(() => new CatalogMatcher([exact], null, false).ResolveExternalIdentifier(null, "urn:item"));

        XmlCatalogEntry rewrite = new XmlCatalogEntry();
        rewrite.Kind = XmlCatalogEntryKind.RewriteUri;
        rewrite.Uri = "urn:";
        Assert.Throws<FormatException>(() => new CatalogMatcher([rewrite], null, false).ResolveUriIdentifier("urn:item"));

        XmlCatalogEntry delegated = new XmlCatalogEntry();
        delegated.Kind = XmlCatalogEntryKind.DelegateSystem;
        delegated.SystemIdentifier = "urn:";
        Assert.Throws<FormatException>(() => new CatalogMatcher([delegated], new MemoryCatalogSource(), false).ResolveExternalIdentifier(null, "urn:item"));

        XmlCatalogEntry group = new XmlCatalogEntry();
        group.Kind = XmlCatalogEntryKind.Group;
        group.Entries = [group];
        Assert.Throws<FormatException>(() => new CatalogMatcher([group], null, false));
    }

    [Test]
    public void DetectsCyclesReachedThroughDelegateCatalogs()
    {
        MemoryCatalogSource source = new MemoryCatalogSource();
        source.Catalogs.Add("https://catalog.test/a.xml", [DelegateUri("urn:", "https://catalog.test/b.xml")]);
        source.Catalogs.Add("https://catalog.test/b.xml", [DelegateUri("urn:", "https://catalog.test/a.xml")]);
        CatalogMatcher matcher = new CatalogMatcher([DelegateUri("urn:", "https://catalog.test/a.xml")], source, true);

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => matcher.ResolveUriIdentifier("urn:item"))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.CatalogCycle));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo("https://catalog.test/a.xml"));
        Assert.That(matcher.ActiveCatalogs, Is.Empty);
    }

    private static XmlCatalogEntry[] Parse(string text, string baseUri, bool defaultPreferPublic)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = baseUri;
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);
        return new CatalogParser(defaultPreferPublic).ParseXmlCatalog(document);
    }

    private static XmlCatalogEntry ExactPublic(string identifier, string target, bool preferPublic)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.Public;
        entry.PublicIdentifier = identifier;
        entry.TargetUri = target;
        entry.PreferPublic = preferPublic;
        return entry;
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

    private static XmlCatalogEntry RewriteSystem(string prefix, string replacement)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.RewriteSystem;
        entry.SystemIdentifier = prefix;
        entry.RewritePrefix = replacement;
        return entry;
    }

    private static XmlCatalogEntry RewriteUri(string prefix, string replacement)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.RewriteUri;
        entry.Uri = prefix;
        entry.RewritePrefix = replacement;
        return entry;
    }

    private static XmlCatalogEntry SystemSuffix(string suffix, string target)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.SystemSuffix;
        entry.Suffix = suffix;
        entry.TargetUri = target;
        return entry;
    }

    private static XmlCatalogEntry UriSuffix(string suffix, string target)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.UriSuffix;
        entry.Suffix = suffix;
        entry.TargetUri = target;
        return entry;
    }

    private static XmlCatalogEntry DelegatePublic(string prefix, string catalogUri, bool preferPublic)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.DelegatePublic;
        entry.PublicIdentifier = prefix;
        entry.CatalogUri = catalogUri;
        entry.PreferPublic = preferPublic;
        return entry;
    }

    private static XmlCatalogEntry DelegateSystem(string prefix, string catalogUri)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.DelegateSystem;
        entry.SystemIdentifier = prefix;
        entry.CatalogUri = catalogUri;
        return entry;
    }

    private static XmlCatalogEntry DelegateUri(string prefix, string catalogUri)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.DelegateUri;
        entry.Uri = prefix;
        entry.CatalogUri = catalogUri;
        return entry;
    }

    private static XmlCatalogEntry NextCatalog(string catalogUri)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.NextCatalog;
        entry.CatalogUri = catalogUri;
        return entry;
    }

    private sealed class MemoryCatalogSource : ICatalogEntrySource
    {
        internal Dictionary<string, XmlCatalogEntry[]> Catalogs = new Dictionary<string, XmlCatalogEntry[]>(StringComparer.Ordinal);
        internal List<string> Requests = new List<string>();

        public XmlCatalogEntry[]? LoadXmlCatalogEntries(string catalogUri)
        {
            Requests.Add(catalogUri);
            return Catalogs.ContainsKey(catalogUri) ? Catalogs[catalogUri] : null;
        }
    }
}
