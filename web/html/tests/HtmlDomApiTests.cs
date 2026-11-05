
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomApiTests
{
    [Test]
    public void PublicFactoriesCreateOwnedNodesWithCanonicalNames()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("DiV");
        HtmlNode svg = document.CreateElementInNamespace("http://www.w3.org/2000/svg",
            "svg:linearGradient");
        HtmlNode custom = document.CreateElementInNamespace("urn:example:widgets",
            "w:Card");
        HtmlNode text = document.CreateText("hello");
        HtmlNode comment = document.CreateComment("note");
        HtmlNode fragment = document.CreateFragment();
        HtmlNode template = document.CreateElement("TEMPLATE");

        Assert.Multiple(() =>
        {
            Assert.That(html.GetKind(), Is.EqualTo(HtmlNodeKind.Element));
            Assert.That(html.GetName(), Is.EqualTo("div"));
            Assert.That(html.GetLocalName(), Is.EqualTo("div"));
            Assert.That(html.GetNamespaceUri(),
                Is.EqualTo("http://www.w3.org/1999/xhtml"));
            Assert.That(html.GetPrefix(), Is.Empty);
            Assert.That(svg.GetName(), Is.EqualTo("svg:linearGradient"));
            Assert.That(svg.GetLocalName(), Is.EqualTo("linearGradient"));
            Assert.That(svg.GetPrefix(), Is.EqualTo("svg"));
            Assert.That(custom.GetNamespaceUri(), Is.EqualTo("urn:example:widgets"));
            Assert.That(custom.GetName(), Is.EqualTo("w:Card"));
            Assert.That(text.GetName(), Is.EqualTo("#text"));
            Assert.That(text.GetData(), Is.EqualTo("hello"));
            Assert.That(comment.GetName(), Is.EqualTo("#comment"));
            Assert.That(comment.GetData(), Is.EqualTo("note"));
            Assert.That(fragment.GetName(), Is.EqualTo("#document-fragment"));
            Assert.That(new[] { html, svg, custom, text, comment, fragment, template }
                .All(value => value.GetOwnerDocument() == document), Is.True);
            Assert.That(new[] { html, svg, custom, text, comment, fragment, template }
                .All(value => !value.GetSourceSpan().IsKnown()), Is.True);
            Assert.That(template.GetTemplateContent(), Is.Not.Null);
            Assert.That(template.GetTemplateContent().GetKind(),
                Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(html.GetTemplateContent(), Is.Null);
        });
    }

    [Test]
    public void RelationshipsAndDocumentConveniencesUseStoredTreeOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            false, "", false, "", false, HtmlSourceSpan.CreateUnknown());
        HtmlNode html = document.CreateElement("html");
        HtmlNode head = document.CreateElement("head");
        HtmlNode title = document.CreateElement("title");
        HtmlNode titleFirst = document.CreateText("Universal");
        HtmlNode titleSecond = document.CreateText("Html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode bodyText = document.CreateText("content");
        HtmlNode bodyComment = document.CreateComment("note");
        HtmlNode paragraph = document.CreateElement("p");

        title.InitializeChildrenStorage(new[] { titleFirst, titleSecond });
        head.InitializeChildrenStorage(new[] { title });
        body.InitializeChildrenStorage(new[] { bodyText, bodyComment, paragraph });
        html.InitializeChildrenStorage(new[] { head, body });
        document.GetRoot().InitializeChildrenStorage(new[] { doctype, html });

        HtmlNode[] children = body.GetChildren();
        children[0] = paragraph;

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetKind(), Is.EqualTo(HtmlNodeKind.Document));
            Assert.That(document.GetRoot().GetName(), Is.EqualTo("#document"));
            Assert.That(document.GetRoot().GetOwnerDocument(), Is.Null);
            Assert.That(document.GetDocumentElement(), Is.SameAs(html));
            Assert.That(document.GetHead(), Is.SameAs(head));
            Assert.That(document.GetBody(), Is.SameAs(body));
            Assert.That(document.GetTitle(), Is.EqualTo("UniversalHtml"));
            Assert.That(html.GetParent(), Is.SameAs(document.GetRoot()));
            Assert.That(head.GetParent(), Is.SameAs(html));
            Assert.That(head.GetNextSibling(), Is.SameAs(body));
            Assert.That(body.GetPreviousSibling(), Is.SameAs(head));
            Assert.That(body.GetFirstChild(), Is.SameAs(bodyText));
            Assert.That(body.GetLastChild(), Is.SameAs(paragraph));
            Assert.That(body.GetChildren(),
                Is.EqualTo(new[] { bodyText, bodyComment, paragraph }));
            Assert.That(body.GetChildElements(), Is.EqualTo(new[] { paragraph }));
            Assert.That(titleFirst.GetNextSibling(), Is.SameAs(titleSecond));
            Assert.That(titleSecond.GetPreviousSibling(), Is.SameAs(titleFirst));
        });
    }

    [Test]
    public void AbsentRelationshipsAndConveniencesReturnDocumentedValues()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetDocumentElement(), Is.Null);
            Assert.That(document.GetHead(), Is.Null);
            Assert.That(document.GetBody(), Is.Null);
            Assert.That(document.GetTitle(), Is.Empty);
            Assert.That(fragment.GetParent(), Is.Null);
            Assert.That(fragment.GetFirstChild(), Is.Null);
            Assert.That(fragment.GetLastChild(), Is.Null);
            Assert.That(fragment.GetPreviousSibling(), Is.Null);
            Assert.That(fragment.GetNextSibling(), Is.Null);
            Assert.That(fragment.GetChildren(), Is.Empty);
            Assert.That(fragment.GetChildElements(), Is.Empty);
            Assert.That(fragment.GetLocalName(), Is.Empty);
            Assert.That(fragment.GetNamespaceUri(), Is.Empty);
            Assert.That(fragment.GetData(), Is.Empty);
            Assert.That(fragment.GetAttributes(), Is.Empty);
        });
    }

    [Test]
    public void DocumentMetadataAndAttributeSnapshotsRemainOwned()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlDiagnostic diagnostic = HtmlDiagnostic.Create(
            HtmlDiagnosticCode.TreeBuilderUnexpectedToken,
            HtmlDiagnosticSeverity.Warning, "unexpected", span);
        HtmlDocument document = new HtmlDocument("https://example.test/page",
            "https://example.test/", HtmlDocumentMode.Quirks,
            new[] { diagnostic }, span);
        HtmlAttribute attribute = HtmlAttribute.Create("id", "main", span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            "http://www.w3.org/1999/xhtml", "p", "p", "",
            new[] { attribute }, span);
        HtmlDiagnostic[] diagnostics = document.GetDiagnostics();
        HtmlAttribute[] attributes = element.GetAttributes();
        diagnostics[0] = null;
        attributes[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(document.GetUrl(), Is.EqualTo("https://example.test/page"));
            Assert.That(document.GetBaseUrl(), Is.EqualTo("https://example.test/"));
            Assert.That(document.GetMode(), Is.EqualTo(HtmlDocumentMode.Quirks));
            Assert.That(document.GetDiagnostics(), Is.EqualTo(new[] { diagnostic }));
            Assert.That(element.GetAttributes(), Is.EqualTo(new[] { attribute }));
        });
    }

    [Test]
    public void NamespacedFactoriesEnforceQualifiedNameContracts()
    {
        HtmlDocument document = CreateDocument();
        const string Xml = "http://www.w3.org/XML/1998/namespace";
        const string Xmlns = "http://www.w3.org/2000/xmlns/";

        Assert.That(document.CreateElementInNamespace(Xml, "xml:lang").GetPrefix(),
            Is.EqualTo("xml"));
        Assert.That(document.CreateElementInNamespace(Xmlns, "xmlns:item").GetPrefix(),
            Is.EqualTo("xmlns"));
        Assert.Throws<ArgumentNullException>(() => document.CreateElement(null));
        Assert.Throws<ArgumentException>(() => document.CreateElement("1invalid"));
        Assert.Throws<ArgumentNullException>(() => document.CreateElementInNamespace(null, "p"));
        Assert.Throws<ArgumentNullException>(() => document.CreateElementInNamespace("", null));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace("", "x:item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(Xml, ":item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(Xml, "x::item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(
            "urn:not-xml", "xml:item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(
            "urn:not-xmlns", "xmlns:item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(Xmlns, "item"));
        Assert.Throws<ArgumentException>(() => document.CreateElementInNamespace(
            "urn:example", "bad name"));
    }

    [Test]
    public void OneTimeRelationshipInitializationRejectsInconsistentOwnership()
    {
        HtmlDocument first = CreateDocument();
        HtmlDocument second = CreateDocument();
        HtmlNode parent = first.CreateElement("div");
        HtmlNode child = first.CreateElement("p");
        HtmlNode foreign = second.CreateElement("p");

        Assert.Throws<ArgumentException>(() => parent.InitializeChildrenStorage(
            new[] { foreign }));
        Assert.Throws<ArgumentException>(() => parent.InitializeChildrenStorage(
            new[] { child, child }));
        Assert.Throws<ArgumentException>(() => parent.InitializeChildrenStorage(
            new[] { first.GetRoot() }));
        parent.InitializeChildrenStorage(new[] { child });
        Assert.Throws<InvalidOperationException>(() => parent.InitializeChildrenStorage(
            Array.Empty<HtmlNode>()));
        HtmlNode leaf = first.CreateText("leaf");
        Assert.Throws<InvalidOperationException>(() => leaf.InitializeChildrenStorage(
            new[] { first.CreateText("nested") }));
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}
