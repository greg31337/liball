
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerClobberingTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";

    [Test]
    public void AttributeSanitizationPrefixesDangerousGlobalAndFormNames()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "input",
            [
                Attribute("title", "Control"),
                Attribute("id", "location"),
                Attribute("name", "submit")
            ]);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("input");
        builder.AllowAttribute("title");
        builder.AllowAttribute("id");
        builder.AllowAttribute("name");
        builder.SetDomClobberPrefix("safe-");

        HtmlSanitizerAttributes.Apply(element, builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttributesStorage()
                .Select(value => value.GetName()),
                Is.EqualTo(new[] { "title", "id", "name" }));
            Assert.That(element.GetAttribute("id"),
                Is.EqualTo("safe-location"));
            Assert.That(element.GetAttribute("name"),
                Is.EqualTo("safe-submit"));
        });
    }

    [Test]
    public void PrefixingIsComprehensiveAndIdempotent()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "div",
            [
                Attribute("id", "Location"),
                Attribute("name", "user-content-submit")
            ]);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("div");
        builder.AllowAttribute("id");
        builder.AllowAttribute("name");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(element, policy);
        HtmlAttribute[] first = element.GetAttributesStorage();
        HtmlSanitizerAttributes.Apply(element, policy);
        HtmlAttribute[] second = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("id"),
                Is.EqualTo("user-content-Location"));
            Assert.That(element.GetAttribute("name"),
                Is.EqualTo("user-content-submit"));
            Assert.That(second.Select(value => value.GetValue()),
                Is.EqualTo(first.Select(value => value.GetValue())));
        });
    }

    [Test]
    public void SubtreePassRewritesAndReportsUniqueDuplicatesInTreeOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode first = CreateElement(document, "div",
            [Attribute("id", "same")]);
        HtmlNode second = CreateElement(document, "span",
            [Attribute("id", "same")]);
        HtmlNode third = CreateElement(document, "form",
            [Attribute("id", "location")]);
        HtmlNode fourth = CreateElement(document, "input",
            [Attribute("id", "location"), Attribute("name", "elements")]);
        fragment.Append(first);
        fragment.Append(second);
        fragment.Append(third);
        fragment.Append(fourth);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.SetDomClobberPrefix("safe-");

        HtmlSanitizerClobberingResult result =
            HtmlSanitizerClobbering.Apply(fragment, builder.Compile());
        string[] duplicates = result.GetDuplicateIdentifiers();
        duplicates[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRewrittenAttributeCount(), Is.EqualTo(5));
            Assert.That(result.HasDuplicateIdentifiers(), Is.True);
            Assert.That(result.GetDuplicateIdentifiers(),
                Is.EqualTo(new[] { "safe-same", "safe-location" }));
            Assert.That(third.GetAttribute("id"),
                Is.EqualTo("safe-location"));
            Assert.That(fourth.GetAttribute("id"),
                Is.EqualTo("safe-location"));
            Assert.That(fourth.GetAttribute("name"),
                Is.EqualTo("safe-elements"));
        });
    }

    [Test]
    public void RootElementParticipatesInDuplicateDetection()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode root = CreateElement(document, "div",
            [Attribute("id", "duplicate")]);
        HtmlNode child = CreateElement(document, "span",
            [Attribute("id", "duplicate")]);
        root.Append(child);

        HtmlSanitizerClobberingResult result =
            HtmlSanitizerClobbering.Apply(root,
                new HtmlSanitizerPolicy().Compile());

        Assert.That(result.GetDuplicateIdentifiers(),
            Is.EqualTo(new[] { "user-content-duplicate" }));
    }

    [Test]
    public void ForeignAndNamespacedAttributesAreNotRewritten()
    {
        HtmlAttribute foreignId = HtmlAttribute.CreateNamespaced("x:id", "id",
            "x", "urn:example", "location", HtmlSourceSpan.CreateUnknown());
        HtmlNode svg = CreateElement(HtmlNamespace.Svg, SvgNamespaceUri,
            "svg", [Attribute("id", "location")]);
        HtmlNode html = CreateElement(HtmlNamespace.Html, HtmlNamespaceUri,
            "div", [foreignId]);
        HtmlCompiledSanitizerPolicy policy =
            new HtmlSanitizerPolicy().Compile();

        HtmlSanitizerClobbering.Apply(svg, policy);
        HtmlSanitizerClobbering.Apply(html, policy);

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetAttribute("id"), Is.EqualTo("location"));
            Assert.That(html.GetAttributesStorage()[0].GetValue(),
                Is.EqualTo("location"));
        });
    }

    [Test]
    public void DuplicateReportingIncludesForeignElementIdentifiers()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode first = document.CreateElementStorage(HtmlNamespace.Svg,
            SvgNamespaceUri, "svg", "svg", "",
            [Attribute("id", "shape")], HtmlSourceSpan.CreateUnknown());
        HtmlNode second = document.CreateElementStorage(HtmlNamespace.Svg,
            SvgNamespaceUri, "path", "path", "",
            [Attribute("id", "shape")], HtmlSourceSpan.CreateUnknown());
        fragment.Append(first);
        fragment.Append(second);

        HtmlSanitizerClobberingResult result =
            HtmlSanitizerClobbering.Apply(fragment,
                new HtmlSanitizerPolicy().Compile());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRewrittenAttributeCount(), Is.Zero);
            Assert.That(result.GetDuplicateIdentifiers(),
                Is.EqualTo(new[] { "shape" }));
        });
    }

    [Test]
    public void DangerTablesExposeIndependentStableSnapshots()
    {
        string[] globals = HtmlSanitizerClobbering.GetDangerousGlobalNames();
        string[] forms = HtmlSanitizerClobbering.GetDangerousFormNames();
        globals[0] = "changed";
        forms[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerClobbering.IsDangerousGlobalName(
                "document"), Is.True);
            Assert.That(HtmlSanitizerClobbering.IsDangerousGlobalName(
                "location"), Is.True);
            Assert.That(HtmlSanitizerClobbering.IsDangerousFormName(
                "submit"), Is.True);
            Assert.That(HtmlSanitizerClobbering.IsDangerousFormName(
                "elements"), Is.True);
            Assert.That(HtmlSanitizerClobbering.IsDangerousName("ordinary"),
                Is.False);
            Assert.That(HtmlSanitizerClobbering.GetDangerousGlobalNames()[0],
                Is.EqualTo("__proto__"));
            Assert.That(HtmlSanitizerClobbering.GetDangerousFormNames()[0],
                Is.EqualTo("__proto__"));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSanitizerClobbering.IsDangerousName(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSanitizerClobbering.Apply(null,
                    new HtmlSanitizerPolicy().Compile()));
        });
    }

    static HtmlAttribute Attribute(string name, string value)
    {
        return HtmlAttribute.Create(name, value,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateElement(HtmlNamespace namespaceKind,
        string namespaceUri, string localName, HtmlAttribute[] attributes)
    {
        return CreateDocument().CreateElementStorage(namespaceKind,
            namespaceUri, localName, localName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateElement(HtmlDocument document, string localName,
        HtmlAttribute[] attributes)
    {
        return document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceUri, localName, localName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
