using xml;

namespace xml.Tests;

[TestFixture]
public class CatalogParserTests
{
    private const string CatalogNamespace = "urn:oasis:names:tc:entity:xmlns:xml:catalog";

    [Test]
    public void ParsesEveryCatalogEntryKindWithResolvedTargets()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "' xml:base='entries/' prefer='public'>" +
            "<public publicId='-//EXAMPLE//DTD Public//EN' uri='public.dtd'/>" +
            "<system systemId='https://origin.test/system.dtd' uri='system.dtd'/>" +
            "<uri name='https://origin.test/data' uri='data.xml#part'/>" +
            "<rewriteSystem systemIdStartString='https://origin.test/dtd/' rewritePrefix='rewritten/'/>" +
            "<rewriteURI uriStartString='https://origin.test/uri/' rewritePrefix='uri-rewritten/'/>" +
            "<systemSuffix systemIdSuffix='book.dtd' uri='suffix.dtd'/>" +
            "<uriSuffix uriSuffix='.schema' uri='suffix.xml#schema'/>" +
            "<delegatePublic publicIdStartString='-//EXAMPLE//' catalog='public-catalog.xml'/>" +
            "<delegateSystem systemIdStartString='https://origin.test/' catalog='system-catalog.xml'/>" +
            "<delegateURI uriStartString='https://origin.test/uri/' catalog='uri-catalog.xml'/>" +
            "<nextCatalog catalog='next.xml'/>" +
            "<group xml:base='../group/' prefer='system'><public publicId='group' uri='group.dtd'/></group>" +
            "</catalog>";

        XmlCatalogEntry[] entries = Parse(text, "https://example.test/catalog/catalog.xml", false);

        Assert.That(entries.Select(item => item.Kind), Is.EqualTo(new XmlCatalogEntryKind[]
        {
            XmlCatalogEntryKind.Public,
            XmlCatalogEntryKind.System,
            XmlCatalogEntryKind.Uri,
            XmlCatalogEntryKind.RewriteSystem,
            XmlCatalogEntryKind.RewriteUri,
            XmlCatalogEntryKind.SystemSuffix,
            XmlCatalogEntryKind.UriSuffix,
            XmlCatalogEntryKind.DelegatePublic,
            XmlCatalogEntryKind.DelegateSystem,
            XmlCatalogEntryKind.DelegateUri,
            XmlCatalogEntryKind.NextCatalog,
            XmlCatalogEntryKind.Group
        }));
        Assert.That(entries.All(item => item.Range != null), Is.True);
        Assert.That(entries[0].PublicIdentifier, Is.EqualTo("-//EXAMPLE//DTD Public//EN"));
        Assert.That(entries[0].TargetUri, Is.EqualTo("https://example.test/catalog/entries/public.dtd"));
        Assert.That(entries[0].PreferPublic, Is.True);
        Assert.That(entries[1].SystemIdentifier, Is.EqualTo("https://origin.test/system.dtd"));
        Assert.That(entries[1].TargetUri, Is.EqualTo("https://example.test/catalog/entries/system.dtd"));
        Assert.That(entries[2].Uri, Is.EqualTo("https://origin.test/data"));
        Assert.That(entries[2].TargetUri, Is.EqualTo("https://example.test/catalog/entries/data.xml#part"));
        Assert.That(entries[3].SystemIdentifier, Is.EqualTo("https://origin.test/dtd/"));
        Assert.That(entries[3].RewritePrefix, Is.EqualTo("https://example.test/catalog/entries/rewritten/"));
        Assert.That(entries[4].Uri, Is.EqualTo("https://origin.test/uri/"));
        Assert.That(entries[4].RewritePrefix, Is.EqualTo("https://example.test/catalog/entries/uri-rewritten/"));
        Assert.That(entries[5].Suffix, Is.EqualTo("book.dtd"));
        Assert.That(entries[5].TargetUri, Is.EqualTo("https://example.test/catalog/entries/suffix.dtd"));
        Assert.That(entries[6].Suffix, Is.EqualTo(".schema"));
        Assert.That(entries[6].TargetUri, Is.EqualTo("https://example.test/catalog/entries/suffix.xml#schema"));
        Assert.That(entries[7].CatalogUri, Is.EqualTo("https://example.test/catalog/entries/public-catalog.xml"));
        Assert.That(entries[8].CatalogUri, Is.EqualTo("https://example.test/catalog/entries/system-catalog.xml"));
        Assert.That(entries[9].CatalogUri, Is.EqualTo("https://example.test/catalog/entries/uri-catalog.xml"));
        Assert.That(entries[10].CatalogUri, Is.EqualTo("https://example.test/catalog/entries/next.xml"));
        Assert.That(entries[11].BaseUri, Is.EqualTo("https://example.test/catalog/group/"));
        Assert.That(entries[11].PreferPublic, Is.False);
        Assert.That(entries[11].Entries, Has.Length.EqualTo(1));
        XmlCatalogEntry groupChild = entries[11].Entries![0];
        Assert.That(groupChild.TargetUri, Is.EqualTo("https://example.test/catalog/group/group.dtd"));
        Assert.That(groupChild.PreferPublic, Is.False);
    }

    [Test]
    public void ScopesXmlBaseAndPreferenceWithoutLeakingToSiblings()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "'>" +
            "<group xml:base='nested/' prefer='public'><system xml:base='leaf/' systemId='one' uri='one.dtd'/></group>" +
            "<system systemId='two' uri='two.dtd'/>" +
            "</catalog>";

        XmlCatalogEntry[] entries = Parse(text, "https://example.test/catalog.xml", false);

        XmlCatalogEntry nested = entries[0].Entries![0];
        Assert.That(nested.BaseUri, Is.EqualTo("https://example.test/nested/leaf/"));
        Assert.That(nested.TargetUri, Is.EqualTo("https://example.test/nested/leaf/one.dtd"));
        Assert.That(nested.PreferPublic, Is.True);
        Assert.That(entries[1].BaseUri, Is.EqualTo("https://example.test/catalog.xml"));
        Assert.That(entries[1].TargetUri, Is.EqualTo("https://example.test/two.dtd"));
        Assert.That(entries[1].PreferPublic, Is.False);
    }

    [Test]
    public void AcceptsPrefixedCatalogElementsAndIgnoresForeignSubtrees()
    {
        string text = "<c:catalog xmlns:c='" + CatalogNamespace + "' xmlns:e='urn:extension' e:setting='value'>" +
            "<!--comment--><e:block><c:system systemId='ignored' uri='ignored.dtd'/></e:block>" +
            "<c:system systemId='kept' uri='kept.dtd'/>" +
            "</c:catalog>";

        XmlCatalogEntry[] entries = Parse(text, "https://example.test/catalog.xml", true);

        Assert.That(entries, Has.Length.EqualTo(1));
        Assert.That(entries[0].SystemIdentifier, Is.EqualTo("kept"));
        Assert.That(entries[0].TargetUri, Is.EqualTo("https://example.test/kept.dtd"));
        Assert.That(entries[0].PreferPublic, Is.True);
    }

    [TestCase("<catalog xmlns='urn:wrong'><system systemId='a' uri='a'/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><unknown/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><system uri='a'/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog' prefer='other'><system systemId='a' uri='a'/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><system systemId='a' uri='a'>text</system></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><group><group><system systemId='a' uri='a'/></group></group></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><nextCatalog catalog='next.xml#part'/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog'><system systemId='a' uri='a' unknown='value'/></catalog>")]
    [TestCase("<catalog xmlns='urn:oasis:names:tc:entity:xmlns:xml:catalog' xmlns:c='urn:oasis:names:tc:entity:xmlns:xml:catalog'><system c:systemId='a' uri='a'/></catalog>")]
    public void RejectsMalformedCatalogStructures(string text)
    {
        Assert.Throws<FormatException>(() => Parse(text, "https://example.test/catalog.xml", false));
    }

    [Test]
    public void RequiresABaseForRelativeTargetReferences()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "'><system systemId='a' uri='relative.dtd'/></catalog>";

        Assert.Throws<FormatException>(() => Parse(text, null, false));
    }

    [Test]
    public void EscapesExcludedAndNonAsciiCharactersInCatalogReferences()
    {
        string text = "<catalog xmlns='" + CatalogNamespace + "' xml:base='catalog folder/'>" +
            "<system systemId='identifier with space' uri='résumé file.dtd'/>" +
            "</catalog>";

        XmlCatalogEntry[] entries = Parse(text, "https://example.test/root.xml", false);

        Assert.That(entries[0].BaseUri, Is.EqualTo("https://example.test/catalog%20folder/"));
        Assert.That(entries[0].TargetUri, Is.EqualTo("https://example.test/catalog%20folder/r%C3%A9sum%C3%A9%20file.dtd"));
    }

    private static XmlCatalogEntry[] Parse(string text, string? baseUri, bool defaultPreferPublic)
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
        options.PreserveSourceLocations = true;
        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);
        return new CatalogParser(defaultPreferPublic).ParseXmlCatalog(document);
    }
}
