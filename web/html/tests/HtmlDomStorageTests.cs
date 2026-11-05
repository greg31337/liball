
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomStorageTests
{
    [Test]
    public void TaggedStorageRepresentsEveryDomNodeKind()
    {
        HtmlSourceSpan span = CreateSpan(0, 5);
        HtmlDocument document = CreateDocument(span);
        HtmlNode fragment = document.CreateFragmentStorage(span);
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            false, "", false, "", false, span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "p", "p", "",
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode text = document.CreateTextStorage("text", span);
        HtmlNode comment = document.CreateCommentStorage("note", span);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRootStorage().GetKindStorage(),
                Is.EqualTo(HtmlNodeKind.Document));
            Assert.That(fragment.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(doctype.GetKindStorage(), Is.EqualTo(HtmlNodeKind.DocumentType));
            Assert.That(element.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Element));
            Assert.That(text.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(comment.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Comment));
            Assert.That(text.GetDataStorage(), Is.EqualTo("text"));
            Assert.That(comment.GetDataStorage(), Is.EqualTo("note"));
            Assert.That(element.GetDataStorage(), Is.Empty);
            Assert.That(document.GetRootStorage().GetOwnerDocumentStorage(),
                Is.SameAs(document));
            Assert.That(document.GetRootStorage().GetOwnerDocument(), Is.Null);
            Assert.That(new[] { fragment, doctype, element, text, comment }
                .All(value => ReferenceEquals(value.GetOwnerDocumentStorage(), document)),
                Is.True);
            Assert.That(element.GetChildrenStorage(), Is.Empty);
            Assert.That(element.GetParentStorage(), Is.Null);
            Assert.That(element.GetFirstChildStorage(), Is.Null);
            Assert.That(element.GetLastChildStorage(), Is.Null);
            Assert.That(element.GetPreviousSiblingStorage(), Is.Null);
            Assert.That(element.GetNextSiblingStorage(), Is.Null);
        });
    }

    [Test]
    public void ElementStorageOwnsOrderedNamespacedAttributeSnapshots()
    {
        HtmlSourceSpan span = CreateSpan(0, 20);
        HtmlAttribute id = HtmlAttribute.Create("id", "main", span);
        HtmlAttribute link = HtmlAttribute.CreateNamespaced("xlink:href", "href",
            "xlink", "http://www.w3.org/1999/xlink", "target", span);
        HtmlAttribute language = HtmlAttribute.CreateNamespaced("xml:lang", "lang",
            "xml", "http://www.w3.org/XML/1998/namespace", "en", span);
        HtmlAttribute[] supplied = [id, link, language];
        HtmlDocument document = CreateDocument(span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Svg,
            "http://www.w3.org/2000/svg", "svg:a", "a", "svg", supplied, span);
        supplied[0] = language;
        HtmlAttribute[] first = element.GetAttributesStorage();
        first[1] = id;
        HtmlAttribute[] second = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(element.GetNamespaceStorage(), Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(element.GetNamespaceUriStorage(),
                Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(element.GetNameStorage(), Is.EqualTo("svg:a"));
            Assert.That(element.GetLocalNameStorage(), Is.EqualTo("a"));
            Assert.That(element.GetPrefixStorage(), Is.EqualTo("svg"));
            Assert.That(second, Is.EqualTo(new[] { id, link, language }));
            Assert.That(second.Select(value => value.GetNamespaceUri()), Is.EqualTo(new[]
            {
                "", "http://www.w3.org/1999/xlink",
                "http://www.w3.org/XML/1998/namespace"
            }));
        });
    }

    [Test]
    public void DoctypeStorageDistinguishesMissingAndEmptyIdentifiers()
    {
        HtmlSourceSpan span = CreateSpan(0, 15);
        HtmlDocument document = CreateDocument(span);
        HtmlNode missing = document.CreateDocumentTypeStorage(false, "",
            false, "", false, "", true, span);
        HtmlNode empty = document.CreateDocumentTypeStorage(true, "",
            true, "", true, "", false, span);

        Assert.Multiple(() =>
        {
            Assert.That(missing.HasDoctypeNameStorage(), Is.False);
            Assert.That(missing.HasPublicIdentifierStorage(), Is.False);
            Assert.That(missing.HasSystemIdentifierStorage(), Is.False);
            Assert.That(missing.IsForceQuirksStorage(), Is.True);
            Assert.That(empty.HasDoctypeNameStorage(), Is.True);
            Assert.That(empty.GetNameStorage(), Is.Empty);
            Assert.That(empty.HasPublicIdentifierStorage(), Is.True);
            Assert.That(empty.GetPublicIdentifierStorage(), Is.Empty);
            Assert.That(empty.HasSystemIdentifierStorage(), Is.True);
            Assert.That(empty.GetSystemIdentifierStorage(), Is.Empty);
            Assert.That(empty.IsForceQuirksStorage(), Is.False);
        });
    }

    [Test]
    public void TemplateOwnsASeparateDocumentFragmentStorage()
    {
        HtmlSourceSpan span = CreateSpan(0, 10);
        HtmlDocument document = CreateDocument(span);
        HtmlNode template = document.CreateTemplateStorage(
            Array.Empty<HtmlAttribute>(), span);
        HtmlNode content = template.GetTemplateContentStorage();
        HtmlNode ordinary = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "div", "div", "",
            Array.Empty<HtmlAttribute>(), span);

        Assert.Multiple(() =>
        {
            Assert.That(template.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Element));
            Assert.That(template.GetNameStorage(), Is.EqualTo("template"));
            Assert.That(content, Is.Not.Null);
            Assert.That(content.GetKindStorage(), Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(content.GetTemplateHostStorage(), Is.SameAs(template));
            Assert.That(content.GetOwnerDocumentStorage(), Is.SameAs(document));
            Assert.That(content.GetSourceSpanStorage().IsKnown(), Is.False);
            Assert.That(template.GetChildrenStorage(), Is.Empty);
            Assert.That(content.GetChildrenStorage(), Is.Empty);
            Assert.That(ordinary.GetTemplateContentStorage(), Is.Null);
        });
    }

    [Test]
    public void DocumentStorageOwnsMetadataAndDiagnosticSnapshots()
    {
        HtmlSourceSpan span = CreateSpan(2, 3);
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.TreeBuilderUnexpectedToken,
            HtmlDiagnosticSeverity.Warning, "unexpected", span);
        HtmlDiagnostic[] supplied = [diagnostic];
        HtmlDocument document = new HtmlDocument("https://example.test/page",
            "https://example.test/", HtmlDocumentMode.LimitedQuirks,
            supplied, span);
        supplied[0] = null;
        HtmlDiagnostic[] snapshot = document.GetDiagnosticsStorage();
        snapshot[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(document.GetUrlStorage(),
                Is.EqualTo("https://example.test/page"));
            Assert.That(document.GetBaseUrlStorage(),
                Is.EqualTo("https://example.test/"));
            Assert.That(document.GetModeStorage(),
                Is.EqualTo(HtmlDocumentMode.LimitedQuirks));
            Assert.That(document.GetDiagnosticsStorage(),
                Is.EqualTo(new[] { diagnostic }));
            Assert.That(document.IsFrozenStorage(), Is.False);
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
            Assert.That(document.GetRootStorage().GetSourceSpanStorage(),
                Is.SameAs(span));
        });
    }

    [Test]
    public void StorageFactoriesRejectInconsistentTaggedData()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlDocument document = CreateDocument(span);

        Assert.Throws<ArgumentException>(() => document.CreateElementStorage(
            HtmlNamespace.Html, "", "p", "p", "",
            Array.Empty<HtmlAttribute>(), span));
        Assert.Throws<ArgumentException>(() => document.CreateElementStorage(
            HtmlNamespace.Other, "", "custom:item", "item", "custom",
            Array.Empty<HtmlAttribute>(), span));
        Assert.Throws<ArgumentException>(() => document.CreateElementStorage(
            HtmlNamespace.Html, "http://www.w3.org/1999/xhtml", "p", "p", "",
            new HtmlAttribute[] { null }, span));
        Assert.Throws<ArgumentException>(() => document.CreateDocumentTypeStorage(
            false, "html", false, "", false, "", false, span));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlDocument("", "",
            (HtmlDocumentMode)99, Array.Empty<HtmlDiagnostic>(), span));
    }

    static HtmlDocument CreateDocument(HtmlSourceSpan span)
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), span);
    }

    static HtmlSourceSpan CreateSpan(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
