
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerLinkHardeningTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
    const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

    [Test]
    public void BlankLinkRemovesOpenerAndMergesRelAtOriginalPosition()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "a",
            [
                Attribute("id", "link"),
                Attribute("rel", "author opener NOREFERRER author"),
                Attribute("target", " \t_BLANK\n"),
                Attribute("title", "Example")
            ]);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.AllowAttribute("id");
        policy.AllowAttribute("target");
        policy.AllowAttribute("title");

        HtmlSanitizerAttributes.Apply(element, policy.Compile());
        HtmlAttribute[] result = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(value => value.GetName()),
                Is.EqualTo(new[] { "id", "rel", "target", "title" }));
            Assert.That(element.GetAttribute("rel"),
                Is.EqualTo("author NOREFERRER noopener"));
            Assert.That(element.GetAttribute("target"),
                Is.EqualTo(" \t_BLANK\n"));
        });
    }

    [Test]
    public void ForcedBlankTargetAppendsMissingHardenedRelDeterministically()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "area",
            [Attribute("title", "Map"), Attribute("href", "/place")]);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("area");
        policy.AllowAttribute("title");
        policy.AllowAttribute("href");
        policy.ForceAttribute("area", "target", "_blank");

        HtmlSanitizerAttributes.Apply(element, policy.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttributesStorage()
                .Select(value => value.GetName()), Is.EqualTo(
                    new[] { "title", "href", "target", "rel" }));
            Assert.That(element.GetAttribute("rel"),
                Is.EqualTo("noopener noreferrer"));
        });
    }

    [Test]
    public void NonBlankTargetDoesNotApplyLinkHardening()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "a",
            [Attribute("rel", "opener author"),
                Attribute("target", "preview")]);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.AllowAttribute("rel");
        policy.AllowAttribute("target");

        HtmlSanitizerAttributes.Apply(element, policy.Compile());

        Assert.That(element.GetAttribute("rel"),
            Is.EqualTo("opener author"));
    }

    [Test]
    public void ForcedTokensMergeWithoutMovingAttributeAndAreIdempotent()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Html,
            HtmlNamespaceUri, "div",
            [Attribute("id", "box"), Attribute("class", "Alpha beta ALPHA"),
                Attribute("title", "Box")]);
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("div");
        policy.AllowAttribute("id");
        policy.AllowAttribute("title");
        policy.ForceAttributeTokens("div", "class",
            new[] { "BETA", "gamma" });
        HtmlCompiledSanitizerPolicy compiled = policy.Compile();

        HtmlSanitizerAttributes.Apply(element, compiled);
        HtmlAttribute[] first = element.GetAttributesStorage();
        HtmlSanitizerAttributes.Apply(element, compiled);
        HtmlAttribute[] second = element.GetAttributesStorage();

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(value => value.GetName()),
                Is.EqualTo(new[] { "id", "class", "title" }));
            Assert.That(element.GetAttribute("class"),
                Is.EqualTo("Alpha beta gamma"));
            Assert.That(second.Select(value => value.GetName()),
                Is.EqualTo(first.Select(value => value.GetName())));
            Assert.That(second.Select(value => value.GetValue()),
                Is.EqualTo(first.Select(value => value.GetValue())));
        });
    }

    [Test]
    public void MissingAndNamespacedForcedTokenAttributesAreAppended()
    {
        HtmlNode element = CreateElement(HtmlNamespace.Svg,
            SvgNamespaceUri, "use", Array.Empty<HtmlAttribute>());
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElementInNamespace(SvgNamespaceUri, "use");
        policy.ForceAttributeTokensInNamespace(SvgNamespaceUri, "use",
            XlinkNamespaceUri, "role", "xlink", new[] { "one", "two" });

        HtmlSanitizerAttributes.Apply(element, policy.Compile());
        HtmlAttribute result = element.GetAttributesStorage()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.GetName(), Is.EqualTo("xlink:role"));
            Assert.That(result.GetLocalName(), Is.EqualTo("role"));
            Assert.That(result.GetNamespaceUri(), Is.EqualTo(XlinkNamespaceUri));
            Assert.That(result.GetValue(), Is.EqualTo("one two"));
        });
    }

    [Test]
    public void ForcedTokenRulesValidateAndCompileAsIndependentSnapshots()
    {
        string[] tokens = ["noopener", "noreferrer"];
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("a");
        policy.ForceAttributeTokens("a", "rel", tokens);
        tokens[0] = "changed";
        HtmlCompiledSanitizerPolicy compiled = policy.Compile();
        HtmlSanitizerAttributeTokenRule[] first =
            compiled.GetSanitizerAttributeTokenRules();
        string[] returnedTokens = first[0].GetTokens();
        returnedTokens[0] = "changed-again";
        first[0] = null;
        HtmlSanitizerPolicy unresolved = new HtmlSanitizerPolicy();
        unresolved.ForceAttributeTokens("a", "rel", ["noopener"]);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.IsAttributeAllowed("rel", "a"), Is.True);
            Assert.That(compiled.GetSanitizerAttributeTokenRules()[0]
                .GetTokens(), Is.EqualTo(new[] { "noopener", "noreferrer" }));
            Assert.Throws<ArgumentNullException>(() =>
                policy.ForceAttributeTokens("a", "rel", null));
            Assert.Throws<ArgumentException>(() =>
                policy.ForceAttributeTokens("a", "rel", Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() =>
                policy.ForceAttributeTokens("a", "rel", ["one two"]));
            Assert.Throws<ArgumentException>(() =>
                policy.ForceAttributeTokens("a", "rel", ["ONE", "one"]));
            Assert.Throws<InvalidOperationException>(() => unresolved.Compile());
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
        HtmlDocument document = new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
        return document.CreateElementStorage(namespaceKind, namespaceUri,
            localName, localName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }
}
