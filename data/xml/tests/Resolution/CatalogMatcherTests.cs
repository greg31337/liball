using xml;

namespace xml.Tests;

[TestFixture]
public class CatalogMatcherTests
{
    [Test]
    public void NormalizesPublicIdentifierWhitespaceAndPublicIdentifierUrns()
    {
        Assert.That(CatalogIdentifiers.NormalizeXmlPublicIdentifier("  -//Example\t  DTD\r\nName//EN  "), Is.EqualTo("-//Example DTD Name//EN"));
        Assert.That(CatalogIdentifiers.NormalizeXmlPublicIdentifierInput("urn:publicid:-:OASIS:DTD+DocBook+XML+V4.1.2:EN"), Is.EqualTo("-//OASIS//DTD DocBook XML V4.1.2//EN"));
        Assert.That(CatalogIdentifiers.NormalizeXmlPublicIdentifierInput("urn:publicid:owner%3Aname;part%2Fleaf"), Is.EqualTo("owner:name::part/leaf"));
        Assert.Throws<FormatException>(() => CatalogIdentifiers.NormalizeXmlPublicIdentifierInput("urn:publicid:bad%2"));
    }

    [Test]
    public void EscapesCatalogIdentifierCharactersWithoutChangingExistingEscapes()
    {
        string identifier = "documents/a file/é.xml?value=%7e#part";

        string normalized = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(identifier);

        Assert.That(normalized, Is.EqualTo("documents/a%20file/%C3%A9.xml?value=%7e#part"));
        Assert.That(CatalogIdentifiers.NormalizeXmlCatalogIdentifier(normalized), Is.EqualTo(normalized));
    }

    [Test]
    public void ExactSystemMatchHasPrecedenceAndFirstDuplicateWins()
    {
        XmlCatalogEntry publicEntry = ExactPublic("-//Example//DTD Test//EN", "https://example.test/public.dtd", true);
        XmlCatalogEntry first = ExactSystem("https://example.test/test.dtd", "https://mirror.test/first.dtd");
        XmlCatalogEntry second = ExactSystem("https://example.test/test.dtd", "https://mirror.test/second.dtd");
        CatalogMatcher matcher = new CatalogMatcher([publicEntry, first, second], null, true);

        string? resolved = matcher.ResolveExternalIdentifier("-//Example//DTD Test//EN", "https://example.test/test.dtd");

        Assert.That(resolved, Is.EqualTo("https://mirror.test/first.dtd"));
    }

    [Test]
    public void PublicMatchingHonorsPreferenceAndNormalizedIdentifiers()
    {
        XmlCatalogEntry systemPreference = ExactPublic("-//Example//DTD Test//EN", "https://example.test/system-preference.dtd", false);
        XmlCatalogEntry publicPreference = ExactPublic("-//Example//DTD Preferred//EN", "https://example.test/public-preference.dtd", true);
        CatalogMatcher matcher = new CatalogMatcher([systemPreference, publicPreference], null, false);

        Assert.That(matcher.ResolveExternalIdentifier("  -//Example//DTD   Test//EN ", null), Is.EqualTo("https://example.test/system-preference.dtd"));
        Assert.That(matcher.ResolveExternalIdentifier("-//Example//DTD Test//EN", "https://example.test/missing.dtd"), Is.Null);
        Assert.That(matcher.ResolveExternalIdentifier("-//Example//DTD Preferred//EN", "https://example.test/missing.dtd"), Is.EqualTo("https://example.test/public-preference.dtd"));
        Assert.That(matcher.ResolveExternalIdentifier(null, "urn:publicid:-:Example:DTD+Preferred:EN"), Is.EqualTo("https://example.test/public-preference.dtd"));
    }

    [Test]
    public void PublicIdentifierUrnsAreUnwrappedOnlyOnResolverInput()
    {
        XmlCatalogEntry literalUrn = ExactPublic("urn:publicid:-:Example:DTD+Test:EN", "https://example.test/literal.dtd", false);
        XmlCatalogEntry publicIdentifier = ExactPublic("-//Example//DTD Test//EN", "https://example.test/unwrapped.dtd", false);
        CatalogMatcher matcher = new CatalogMatcher([literalUrn, publicIdentifier], null, false);

        Assert.That(matcher.ResolveExternalIdentifier("urn:publicid:-:Example:DTD+Test:EN", null), Is.EqualTo("https://example.test/unwrapped.dtd"));
    }

    [Test]
    public void RewriteAndSuffixRulesChooseTheLongestFirstDeclaredMatch()
    {
        XmlCatalogEntry broad = RewriteSystem("https://example.test/", "https://broad.test/");
        XmlCatalogEntry firstLong = RewriteSystem("https://example.test/dtd/", "https://first.test/");
        XmlCatalogEntry secondLong = RewriteSystem("https://example.test/dtd/", "https://second.test/");
        CatalogMatcher rewriteMatcher = new CatalogMatcher([broad, firstLong, secondLong], null, false);
        Assert.That(rewriteMatcher.ResolveExternalIdentifier(null, "https://example.test/dtd/test.dtd"), Is.EqualTo("https://first.test/test.dtd"));

        XmlCatalogEntry shortSuffix = SystemSuffix(".dtd", "https://example.test/short.dtd");
        XmlCatalogEntry firstLongSuffix = SystemSuffix("test.dtd", "https://example.test/first.dtd");
        XmlCatalogEntry secondLongSuffix = SystemSuffix("test.dtd", "https://example.test/second.dtd");
        CatalogMatcher suffixMatcher = new CatalogMatcher([shortSuffix, firstLongSuffix, secondLongSuffix], null, false);
        Assert.That(suffixMatcher.ResolveExternalIdentifier(null, "https://origin.test/test.dtd"), Is.EqualTo("https://example.test/first.dtd"));
    }

    [Test]
    public void UriRulesApplyExactRewriteAndSuffixPrecedence()
    {
        XmlCatalogEntry exact = ExactUri("https://example.test/exact", "https://mirror.test/exact");
        XmlCatalogEntry rewrite = RewriteUri("https://example.test/", "https://mirror.test/rewrite/");
        XmlCatalogEntry suffix = UriSuffix("schema.xsd", "https://mirror.test/suffix.xsd");
        CatalogMatcher matcher = new CatalogMatcher([suffix, rewrite, exact], null, false);

        Assert.That(matcher.ResolveUriIdentifier("https://example.test/exact"), Is.EqualTo("https://mirror.test/exact"));
        Assert.That(matcher.ResolveUriIdentifier("https://example.test/path/schema.xsd"), Is.EqualTo("https://mirror.test/rewrite/path/schema.xsd"));
        Assert.That(matcher.ResolveUriIdentifier("https://other.test/schema.xsd"), Is.EqualTo("https://mirror.test/suffix.xsd"));
    }

    [Test]
    public void ResolvesAndEscapesRelativeTargetsAgainstEntryBaseUris()
    {
        XmlCatalogEntry entry = ExactSystem("identifier with space", "targets/résumé file.dtd");
        entry.BaseUri = "https://catalog.test/catalog.xml";
        CatalogMatcher matcher = new CatalogMatcher([entry], null, false);

        string? resolved = matcher.ResolveExternalIdentifier(null, "identifier with space");

        Assert.That(resolved, Is.EqualTo("https://catalog.test/targets/r%C3%A9sum%C3%A9%20file.dtd"));
    }

    [Test]
    public void DelegatesUseLongestPrefixThenContinueInStableOrder()
    {
        RecordingCatalogSource source = new RecordingCatalogSource();
        source.Catalogs.Add("https://catalog.test/long.xml", []);
        source.Catalogs.Add("https://catalog.test/short.xml", [ExactSystem("https://id.test/long/test.dtd", "https://mirror.test/test.dtd")]);
        XmlCatalogEntry shortDelegate = DelegateSystem("https://id.test/", "https://catalog.test/short.xml");
        XmlCatalogEntry longDelegate = DelegateSystem("https://id.test/long/", "https://catalog.test/long.xml");
        CatalogMatcher matcher = new CatalogMatcher([shortDelegate, longDelegate], source, true);

        string? resolved = matcher.ResolveExternalIdentifier(null, "https://id.test/long/test.dtd");

        Assert.That(resolved, Is.EqualTo("https://mirror.test/test.dtd"));
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/long.xml", "https://catalog.test/short.xml" }));
    }

    [Test]
    public void PublicAndUriDelegatesUseTheirMatchingIdentifierKinds()
    {
        RecordingCatalogSource publicSource = new RecordingCatalogSource();
        publicSource.Catalogs.Add("https://catalog.test/public.xml", [ExactPublic("-//Example//DTD Delegated//EN", "https://mirror.test/public.dtd", false)]);
        CatalogMatcher publicMatcher = new CatalogMatcher([
            DelegatePublic("  -//Example//DTD  ", "https://catalog.test/public.xml", true)
        ], publicSource, false);
        Assert.That(publicMatcher.ResolveExternalIdentifier("-//Example//DTD Delegated//EN", "https://id.test/missing.dtd"), Is.EqualTo("https://mirror.test/public.dtd"));

        RecordingCatalogSource uriSource = new RecordingCatalogSource();
        uriSource.Catalogs.Add("https://catalog.test/uri.xml", [ExactUri("https://id.test/path/value", "https://mirror.test/value")]);
        CatalogMatcher uriMatcher = new CatalogMatcher([
            DelegateUri("https://id.test/path/", "https://catalog.test/uri.xml")
        ], uriSource, false);
        Assert.That(uriMatcher.ResolveUriIdentifier("https://id.test/path/value"), Is.EqualTo("https://mirror.test/value"));
    }

    [Test]
    public void MatchingDelegateIsExclusiveFromPublicAndNextCatalogFallbacks()
    {
        RecordingCatalogSource source = new RecordingCatalogSource();
        source.Catalogs.Add("https://catalog.test/delegate.xml", []);
        source.Catalogs.Add("https://catalog.test/next.xml", [ExactSystem("https://id.test/item.dtd", "https://mirror.test/next.dtd")]);
        XmlCatalogEntry delegated = DelegateSystem("https://id.test/", "https://catalog.test/delegate.xml");
        XmlCatalogEntry publicEntry = ExactPublic("-//Example//DTD Item//EN", "https://mirror.test/public.dtd", true);
        XmlCatalogEntry next = NextCatalog("https://catalog.test/next.xml");
        CatalogMatcher matcher = new CatalogMatcher([delegated, publicEntry, next], source, false);

        string? resolved = matcher.ResolveExternalIdentifier("-//Example//DTD Item//EN", "https://id.test/item.dtd");

        Assert.That(resolved, Is.Null);
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/delegate.xml" }));
    }

    [Test]
    public void NextCatalogsAreChainedInDocumentOrderAndLoadedOnceWhenCached()
    {
        RecordingCatalogSource source = new RecordingCatalogSource();
        source.Catalogs.Add("https://catalog.test/first.xml", []);
        source.Catalogs.Add("https://catalog.test/second.xml", [
            ExactUri("https://id.test/one", "https://mirror.test/one"),
            ExactUri("https://id.test/two", "https://mirror.test/two")
        ]);
        CatalogMatcher matcher = new CatalogMatcher([
            NextCatalog("https://catalog.test/first.xml"),
            NextCatalog("https://catalog.test/second.xml")
        ], source, true);

        Assert.That(matcher.ResolveUriIdentifier("https://id.test/one"), Is.EqualTo("https://mirror.test/one"));
        Assert.That(matcher.ResolveUriIdentifier("https://id.test/one"), Is.EqualTo("https://mirror.test/one"));
        Assert.That(matcher.ResolveUriIdentifier("https://id.test/two"), Is.EqualTo("https://mirror.test/two"));
        Assert.That(source.Requests, Is.EqualTo(new string[] { "https://catalog.test/first.xml", "https://catalog.test/second.xml" }));
    }

    [Test]
    public void NestedGroupsPreserveDocumentOrderForDuplicateEntries()
    {
        XmlCatalogEntry group = new XmlCatalogEntry();
        group.Kind = XmlCatalogEntryKind.Group;
        group.Entries = [
            ExactUri("https://id.test/value", "https://mirror.test/inside"),
            ExactUri("https://id.test/value", "https://mirror.test/later-inside")
        ];
        CatalogMatcher matcher = new CatalogMatcher([
            group,
            ExactUri("https://id.test/value", "https://mirror.test/outside")
        ], null, false);

        Assert.That(matcher.ResolveUriIdentifier("https://id.test/value"), Is.EqualTo("https://mirror.test/inside"));
    }

    [Test]
    public void DetectsCatalogCyclesWithCatalogDiagnostic()
    {
        RecordingCatalogSource source = new RecordingCatalogSource();
        source.Catalogs.Add("https://catalog.test/a.xml", [NextCatalog("https://catalog.test/b.xml")]);
        source.Catalogs.Add("https://catalog.test/b.xml", [NextCatalog("https://catalog.test/a.xml")]);
        CatalogMatcher matcher = new CatalogMatcher([NextCatalog("https://catalog.test/a.xml")], source, true);

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => matcher.ResolveUriIdentifier("https://id.test/missing"))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.CatalogCycle));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo("https://catalog.test/a.xml"));
        Assert.That(matcher.ActiveCatalogs, Is.Empty);
    }

    [Test]
    public void RejectsConflictingPublicIdentifierAndSystemUrn()
    {
        CatalogMatcher matcher = new CatalogMatcher([], null, false);

        Assert.Throws<FormatException>(() => matcher.ResolveExternalIdentifier("-//One//EN", "urn:publicid:-:Two:EN"));
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

    private static XmlCatalogEntry DelegateSystem(string prefix, string catalogUri)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = XmlCatalogEntryKind.DelegateSystem;
        entry.SystemIdentifier = prefix;
        entry.CatalogUri = catalogUri;
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

    private sealed class RecordingCatalogSource : ICatalogEntrySource
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
