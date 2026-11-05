
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerAttributeTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
    const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

    [Test]
    public void GlobalAndElementRulesUseNamespacedSpecificPrecedence()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.AllowElement("img");
        policy.AllowElementInNamespace(SvgNamespaceUri, "use");
        policy.AllowAttribute("TITLE");
        policy.BlockAttribute("onclick");
        policy.AllowAttributeInNamespace(XlinkNamespaceUri, "href");
        policy.BlockAttributeOnElement("title", "IMG");
        policy.BlockAttribute("data-id");
        policy.AllowAttributeOnElement("data-id", "A");

        HtmlCompiledSanitizerPolicy compiled = policy.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.IsAttributeAllowed("title", "a"), Is.True);
            Assert.That(compiled.IsAttributeAllowed("title", "img"), Is.False);
            Assert.That(compiled.IsAttributeAllowed("onclick", "a"), Is.False);
            Assert.That(compiled.IsAttributeAllowed("data-id", "a"), Is.True);
            Assert.That(compiled.IsAttributeAllowed("data-id", "img"),
                Is.False);
            Assert.That(compiled.IsAttributeAllowedInNamespaces(XlinkNamespaceUri, "href",
                SvgNamespaceUri, "use"), Is.True);
            Assert.That(compiled.IsAttributeAllowedInNamespaces(XlinkNamespaceUri, "HREF",
                SvgNamespaceUri, "use"), Is.False);
        });
    }

    [Test]
    public void ApplyDropsRenamesNormalizesAndPreservesStableOrder()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute[] attributes =
        [
            HtmlAttribute.Create("title", " kept ", span),
            HtmlAttribute.Create("onclick", "run()", span),
            HtmlAttribute.Create("href", "first", span),
            HtmlAttribute.Create("data-link", "second", span),
            HtmlAttribute.Create("class", "  alpha\t beta  ", span),
            HtmlAttribute.Create("rel", "NOFOLLOW", span)
        ];
        HtmlNode element = CreateElement(HtmlNamespaceUri, HtmlNamespace.Html,
            "a", attributes);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.AllowAttribute("title");
        policy.BlockAttribute("onclick");
        policy.AllowAttribute("href");
        policy.RenameAttribute("data-link", "href");
        policy.NormalizeAttribute("class",
            HtmlSanitizerAttributeNormalization.CollapseHtmlWhitespace);
        policy.NormalizeAttributeOnElement("rel", "a",
            HtmlSanitizerAttributeNormalization.LowercaseAscii);

        HtmlSanitizerAttributes.Apply(element, policy.Compile());
        HtmlAttribute[] result = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(value => value.GetName()),
                Is.EqualTo(new[] { "title", "href", "class", "rel" }));
            Assert.That(result.Select(value => value.GetValue()),
                Is.EqualTo(new[]
                {
                    " kept ", "first", "alpha beta", "nofollow"
                }));
            Assert.That(result.Select(value => value.GetLocalName()).Distinct()
                .Count(), Is.EqualTo(result.Length));
        });
    }

    [Test]
    public void RequiredPreservesExistingWhileForcedOverwritesOrAppends()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.RequireAttribute("a", "rel", "noopener");
        policy.ForceAttribute("a", "target", "_blank");
        HtmlCompiledSanitizerPolicy compiled = policy.Compile();
        HtmlNode existing = CreateElement(HtmlNamespaceUri, HtmlNamespace.Html,
            "a",
            [
                HtmlAttribute.Create("rel", "author", span),
                HtmlAttribute.Create("target", "same-frame", span)
            ]);
        HtmlNode missing = CreateElement(HtmlNamespaceUri, HtmlNamespace.Html,
            "a", Array.Empty<HtmlAttribute>());

        HtmlSanitizerAttributes.Apply(existing, compiled);
        HtmlSanitizerAttributes.Apply(missing, compiled);

        Assert.Multiple(() =>
        {
            Assert.That(existing.GetAttribute("rel"),
                Is.EqualTo("author noopener noreferrer"));
            Assert.That(existing.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(missing.GetAttribute("rel"),
                Is.EqualTo("noopener noreferrer"));
            Assert.That(missing.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(compiled.HasRequiredAttribute("a", "rel"), Is.True);
            Assert.That(compiled.GetRequiredAttributeValue("a", "rel"),
                Is.EqualTo("noopener"));
            Assert.That(compiled.HasForcedAttribute("a", "target"), Is.True);
            Assert.That(compiled.GetForcedAttributeValue("a", "target"),
                Is.EqualTo("_blank"));
            Assert.That(missing.GetAttributesStorage()
                .Select(value => value.GetName()),
                Is.EqualTo(new[] { "rel", "target" }));
        });
    }

    [Test]
    public void NamespacedRenameAndRequiredAttributesKeepExpandedNames()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute source = HtmlAttribute.CreateNamespaced("xlink:href",
            "href", "xlink", XlinkNamespaceUri, "#icon", span);
        HtmlNode element = CreateElement(SvgNamespaceUri, HtmlNamespace.Svg,
            "use", [source]);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElementInNamespace(SvgNamespaceUri, "use");
        policy.RenameAttributeOnNamespacedElement(XlinkNamespaceUri, "href", SvgNamespaceUri,
            "use", "", "href", "");
        policy.RequireAttributeInNamespace(SvgNamespaceUri, "use", XlinkNamespaceUri, "title",
            "xlink", "fallback");

        HtmlSanitizerAttributes.Apply(element, policy.Compile());
        HtmlAttribute[] result = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0].GetName(), Is.EqualTo("href"));
            Assert.That(result[0].GetNamespaceUri(), Is.Empty);
            Assert.That(result[0].GetValue(), Is.EqualTo("#icon"));
            Assert.That(result[1].GetName(), Is.EqualTo("xlink:title"));
            Assert.That(result[1].GetLocalName(), Is.EqualTo("title"));
            Assert.That(result[1].GetPrefix(), Is.EqualTo("xlink"));
            Assert.That(result[1].GetNamespaceUri(),
                Is.EqualTo(XlinkNamespaceUri));
            Assert.That(result[1].GetValue(), Is.EqualTo("fallback"));
        });
    }

    [Test]
    public void CompiledRulesAndBuilderCopiesAreIndependentSnapshots()
    {
        HtmlSanitizerPolicy original = new HtmlSanitizerPolicy();
        original.AllowElement("a");
        original.RenameAttribute("data-link", "href");
        original.RequireAttribute("a", "rel", "noopener");
        HtmlSanitizerPolicy copy = original.Copy();
        HtmlCompiledSanitizerPolicy compiled = original.Compile();
        HtmlSanitizerAttributeRule[] firstRules =
            compiled.GetSanitizerAttributeRules();
        HtmlSanitizerAttributeValueRule[] firstValues =
            compiled.GetSanitizerAttributeValueRules();
        firstRules[0] = null;
        firstValues[0] = null;
        copy.BlockAttribute("data-link");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetSanitizerAttributeRules()[0].GetAction(),
                Is.EqualTo(HtmlSanitizerAttributeAction.Rename));
            Assert.That(compiled.GetSanitizerAttributeValueRules()[0]
                .IsForced(), Is.False);
            Assert.That(copy.Compile().IsAttributeAllowed("data-link", "a"),
                Is.False);
            Assert.That(compiled.IsAttributeAllowed("data-link", "a"),
                Is.True);
        });
    }

    [Test]
    public void InvalidAttributeRulesAndDomReplacementAreRejected()
    {
        HtmlSanitizerPolicy unresolved = new HtmlSanitizerPolicy();
        unresolved.RequireAttribute("a", "rel", "noopener");
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        HtmlNode text = CreateDocument().CreateTextStorage("text",
            HtmlSourceSpan.CreateUnknown());
        HtmlNode element = CreateElement(HtmlNamespaceUri, HtmlNamespace.Html,
            "a", Array.Empty<HtmlAttribute>());
        HtmlAttribute duplicate = HtmlAttribute.Create("id", "one",
            HtmlSourceSpan.CreateUnknown());

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                unresolved.Compile());
            Assert.Throws<ArgumentException>(() => policy.RenameAttributeInNamespace(
                "", "data-link", "", "href", "prefix"));
            Assert.Throws<ArgumentException>(() => policy.AllowAttributeInNamespace(
                XlinkNamespaceUri, "xlink:href"));
            Assert.Throws<ArgumentException>(() => policy.ForceAttributeInNamespace(
                SvgNamespaceUri, "use", XlinkNamespaceUri, "href",
                "bad:prefix", "#icon"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                policy.NormalizeAttribute("class",
                    (HtmlSanitizerAttributeNormalization)99));
            Assert.Throws<ArgumentException>(() =>
                HtmlSanitizerAttributes.Apply(text, policy.Compile()));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSanitizerAttributes.Apply(element, null));
            Assert.Throws<ArgumentException>(() =>
                element.ReplaceAttributesStorage([duplicate, duplicate]));
        });
    }

    static HtmlNode CreateElement(string namespaceUri,
        HtmlNamespace namespaceKind, string localName,
        HtmlAttribute[] attributes)
    {
        HtmlDocument document = CreateDocument();
        return document.CreateElementStorage(namespaceKind, namespaceUri,
            localName, localName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
