
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerElementTests
{
    const string HtmlNamespace = "http://www.w3.org/1999/xhtml";
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string MathNamespace = "http://www.w3.org/1998/Math/MathML";

    [Test]
    public void ElementRulesUseNamespaceAndLocalNameIdentity()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("A");
        policy.AllowElementInNamespace(SvgNamespace, "circle");
        policy.AllowElementInNamespace(SvgNamespace, "linearGradient");
        policy.BlockElementInNamespace(MathNamespace, "mi",
            HtmlBlockedElementAction.ReplaceWithText);

        HtmlCompiledSanitizerPolicy compiled = policy.Compile();
        HtmlSanitizerElementName[] rules = compiled.GetAllowedElementRules();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetAllowedElements(),
                Is.EqualTo(new[] { "a" }));
            Assert.That(rules.Length, Is.EqualTo(3));
            Assert.That(rules.Any(rule =>
                rule.GetNamespaceUri() == SvgNamespace
                && rule.GetLocalName() == "linearGradient"), Is.True);
            Assert.That(compiled.IsElementAllowedInNamespace(HtmlNamespace, "A"), Is.True);
            Assert.That(compiled.IsElementAllowedInNamespace(SvgNamespace, "circle"),
                Is.True);
            Assert.That(compiled.IsElementAllowedInNamespace(SvgNamespace, "Circle"),
                Is.False);
            Assert.That(compiled.IsElementAllowedInNamespace(SvgNamespace, "a"), Is.False);
            Assert.That(compiled.IsElementBlockedInNamespace(MathNamespace, "mi"),
                Is.True);
            Assert.That(compiled.GetBlockedElementActionInNamespace(MathNamespace, "mi"),
                Is.EqualTo(HtmlBlockedElementAction.ReplaceWithText));
            Assert.That(compiled.GetBlockedElements(), Is.Empty);
        });
    }

    [Test]
    public void BuiltInPresetsAllowOnlyHtmlNamespaceElements()
    {
        HtmlCompiledSanitizerPolicy basic =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();
        HtmlCompiledSanitizerPolicy rich =
            HtmlSanitizerPolicy.CreateRichText().Compile();

        Assert.Multiple(() =>
        {
            Assert.That(basic.IsElementAllowedInNamespace(HtmlNamespace, "mark"), Is.True);
            Assert.That(basic.IsElementAllowedInNamespace(MathNamespace, "mark"), Is.False);
            Assert.That(rich.IsElementAllowedInNamespace(HtmlNamespace, "a"), Is.True);
            Assert.That(rich.IsElementAllowedInNamespace(SvgNamespace, "a"), Is.False);
            Assert.That(rich.IsElementAllowedInNamespace(SvgNamespace, "svg"), Is.False);
            Assert.That(rich.IsElementAllowedInNamespace(MathNamespace, "math"), Is.False);
        });
    }

    [Test]
    public void DefaultDecisionsKeepAllowedUnwrapOrdinaryAndDropSensitiveTrees()
    {
        HtmlDocument document = CreateDocument();
        HtmlCompiledSanitizerPolicy textOnly =
            HtmlSanitizerPolicy.CreateTextOnly().Compile();
        HtmlCompiledSanitizerPolicy basic =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();
        HtmlNode paragraph = document.CreateElement("p");
        HtmlNode ordinary = document.CreateElement("unknown-box");
        HtmlNode script = document.CreateElement("script");
        HtmlNode style = document.CreateElement("style");
        HtmlNode title = document.CreateElement("title");
        HtmlNode textarea = document.CreateElement("textarea");
        HtmlNode template = document.CreateElement("template");
        HtmlNode foreign = document.CreateElementInNamespace(SvgNamespace, "circle");

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerElements.GetAction(paragraph, basic),
                Is.EqualTo(HtmlSanitizerElementAction.Keep));
            Assert.That(HtmlSanitizerElements.GetAction(ordinary, textOnly),
                Is.EqualTo(HtmlSanitizerElementAction.Unwrap));
            foreach (HtmlNode sensitive in new[]
            {
                script, style, title, textarea, template, foreign
            })
            {
                Assert.That(HtmlSanitizerElements.GetAction(sensitive, textOnly),
                    Is.EqualTo(HtmlSanitizerElementAction.DropSubtree),
                    sensitive.GetName());
            }
        });
    }

    [Test]
    public void ExplicitNamespacedActionsOverrideSafeDefaults()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode svg = document.CreateElementInNamespace(SvgNamespace, "svg");
        HtmlNode script = document.CreateElement("script");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.BlockElementInNamespace(SvgNamespace, "svg",
            HtmlBlockedElementAction.Unwrap);
        builder.BlockElement("script", HtmlBlockedElementAction.EscapeAsText);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerElements.GetAction(svg, policy),
                Is.EqualTo(HtmlSanitizerElementAction.Unwrap));
            Assert.That(HtmlSanitizerElements.GetAction(script, policy),
                Is.EqualTo(HtmlSanitizerElementAction.EscapeAsText));
        });
    }

    [Test]
    public void ApplyingEveryElementActionPreservesTreeInvariants()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode kept = document.CreateElement("p");
        kept.Append(document.CreateText("kept"));
        HtmlNode dropped = document.CreateElement("script");
        dropped.Append(document.CreateText("alert(1)"));
        HtmlNode unwrapped = document.CreateElement("wrapper-box");
        HtmlNode emphasis = document.CreateElement("em");
        emphasis.Append(document.CreateText("wrapped"));
        unwrapped.Append(emphasis);
        HtmlNode replaced = document.CreateElement("replace-box");
        HtmlNode strong = document.CreateElement("strong");
        strong.Append(document.CreateText("plain <value>"));
        replaced.Append(strong);
        HtmlNode escaped = document.CreateElement("escape-box");
        escaped.SetAttribute("data-x", "1");
        HtmlNode escapedEmphasis = document.CreateElement("em");
        escapedEmphasis.Append(document.CreateText("<"));
        escaped.Append(escapedEmphasis);
        fragment.Append(kept);
        fragment.Append(dropped);
        fragment.Append(unwrapped);
        fragment.Append(replaced);
        fragment.Append(escaped);

        HtmlSanitizerPolicy builder = HtmlSanitizerPolicy.CreateBasicFormatting();
        builder.BlockElement("replace-box",
            HtmlBlockedElementAction.ReplaceWithText);
        builder.BlockElement("escape-box",
            HtmlBlockedElementAction.EscapeAsText);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        Assert.That(HtmlSanitizerElements.Apply(kept, policy),
            Is.EqualTo(HtmlSanitizerElementAction.Keep));
        Assert.That(HtmlSanitizerElements.Apply(dropped, policy),
            Is.EqualTo(HtmlSanitizerElementAction.DropSubtree));
        Assert.That(HtmlSanitizerElements.Apply(unwrapped, policy),
            Is.EqualTo(HtmlSanitizerElementAction.Unwrap));
        Assert.That(HtmlSanitizerElements.Apply(replaced, policy),
            Is.EqualTo(HtmlSanitizerElementAction.ReplaceWithText));
        Assert.That(HtmlSanitizerElements.Apply(escaped, policy),
            Is.EqualTo(HtmlSanitizerElementAction.EscapeAsText));

        HtmlNode[] children = fragment.GetChildren();
        Assert.Multiple(() =>
        {
            Assert.That(children.Select(node => node.GetKind()), Is.EqualTo(
                new[]
                {
                    HtmlNodeKind.Element, HtmlNodeKind.Element,
                    HtmlNodeKind.Text, HtmlNodeKind.Text
                }));
            Assert.That(children[0], Is.SameAs(kept));
            Assert.That(children[1], Is.SameAs(emphasis));
            Assert.That(children[2].GetData(), Is.EqualTo("plain <value>"));
            Assert.That(children[3].GetData(), Is.EqualTo(
                "<escape-box data-x=\"1\"><em>&lt;</em></escape-box>"));
            Assert.That(HtmlDomInvariant.FindViolation(document, fragment),
                Is.Empty);
        });
    }

    [Test]
    public void ApplyingReplacementToDetachedOrNonElementNodesIsRejected()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode detached = document.CreateElement("div");
        HtmlNode text = document.CreateText("value");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                HtmlSanitizerElements.Apply(detached,
                    HtmlSanitizerElementAction.DropSubtree));
            Assert.Throws<ArgumentException>(() =>
                HtmlSanitizerElements.Apply(text,
                    HtmlSanitizerElementAction.DropSubtree));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HtmlSanitizerElements.Apply(detached,
                    (HtmlSanitizerElementAction)99));
        });
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}
