
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerPresetTests
{
    [Test]
    public void TextOnlyAllowsNoMarkupCapabilities()
    {
        HtmlCompiledSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateTextOnly().Compile();

        Assert.Multiple(() =>
        {
            Assert.That(policy.GetAllowedElements(), Is.Empty);
            Assert.That(policy.GetBlockedElements(), Is.Empty);
            Assert.That(policy.GetAllowedAttributes(), Is.Empty);
            Assert.That(policy.GetAllowedUrlSchemes(), Is.Empty);
            Assert.That(policy.GetAllowedUrlHosts(), Is.Empty);
            Assert.That(policy.GetAllowedCssProperties(), Is.Empty);
            Assert.That(policy.GetAllowDataAttributes(), Is.False);
            Assert.That(policy.GetAllowAriaAttributes(), Is.False);
        });
    }

    [Test]
    public void BasicFormattingHasAnExactNonLinkFormattingSurface()
    {
        HtmlCompiledSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();

        Assert.Multiple(() =>
        {
            Assert.That(policy.GetAllowedElements(), Is.EqualTo(new[]
            {
                "b", "br", "code", "em", "i", "mark", "p", "pre", "s",
                "small", "span", "strong", "sub", "sup", "u"
            }));
            Assert.That(policy.GetAllowedAttributes(),
                Is.EqualTo(new[] { "dir", "lang", "title" }));
            Assert.That(policy.IsElementAllowed("a"), Is.False);
            Assert.That(policy.IsElementAllowed("img"), Is.False);
            Assert.That(policy.GetAllowedUrlSchemes(), Is.Empty);
            Assert.That(policy.GetAllowedCssProperties(), Is.Empty);
        });
    }

    [Test]
    public void RichTextAddsStructuredLinksImagesAndTables()
    {
        HtmlCompiledSanitizerPolicy basic =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();
        HtmlCompiledSanitizerPolicy rich =
            HtmlSanitizerPolicy.CreateRichText().Compile();

        Assert.Multiple(() =>
        {
            foreach (string element in basic.GetAllowedElements())
            {
                Assert.That(rich.IsElementAllowed(element), Is.True, element);
            }

            Assert.That(new[]
            {
                "a", "article", "blockquote", "details", "h1", "img", "ol",
                "table", "td", "th", "time", "ul"
            }.All(rich.IsElementAllowed), Is.True);
            Assert.That(rich.GetAllowedAttributes(),
                Is.EqualTo(new[] { "class", "dir", "lang", "title" }));
            Assert.That(rich.GetAllowedAttributesForElement("a"),
                Is.EqualTo(new[] { "href", "hreflang", "rel", "target" }));
            Assert.That(rich.GetAllowedAttributesForElement("img"),
                Is.EqualTo(new[] { "alt", "height", "loading", "src", "width" }));
            Assert.That(rich.GetAllowedAttributesForElement("th"),
                Is.EqualTo(new[] { "colspan", "headers", "rowspan", "scope" }));
            Assert.That(rich.GetAllowedUrlSchemes(),
                Is.EqualTo(new[] { "http", "https", "mailto", "tel" }));
            Assert.That(rich.GetAllowedUrlHosts(), Is.Empty);
        });
    }

    [Test]
    public void HtmlOnlyAddsSelectedNonScriptableHtmlMediaAndDataElements()
    {
        HtmlCompiledSanitizerPolicy rich =
            HtmlSanitizerPolicy.CreateRichText().Compile();
        HtmlCompiledSanitizerPolicy html =
            HtmlSanitizerPolicy.CreateHtmlOnly().Compile();

        Assert.Multiple(() =>
        {
            foreach (string element in rich.GetAllowedElements())
            {
                Assert.That(html.IsElementAllowed(element), Is.True, element);
            }

            Assert.That(new[]
            {
                "audio", "canvas", "col", "datalist", "fieldset", "meter",
                "option", "picture", "progress", "source", "track", "video"
            }.All(html.IsElementAllowed), Is.True);
            Assert.That(html.GetAllowedAttributesForElement("video"),
                Is.EqualTo(new[]
                {
                    "controls", "height", "loop", "muted", "playsinline",
                    "poster", "preload", "src", "width"
                }));
            Assert.That(html.GetAllowedAttributesForElement("track"),
                Is.EqualTo(new[]
                {
                    "default", "kind", "label", "src", "srclang"
                }));
            Assert.That(html.GetAllowedUrlSchemes(),
                Is.EqualTo(rich.GetAllowedUrlSchemes()));
        });
    }

    [Test]
    public void EveryPresetDisablesForeignStyleActiveAndCustomMarkup()
    {
        HtmlCompiledSanitizerPolicy[] policies =
        [
            HtmlSanitizerPolicy.CreateTextOnly().Compile(),
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile(),
            HtmlSanitizerPolicy.CreateRichText().Compile(),
            HtmlSanitizerPolicy.CreateHtmlOnly().Compile()
        ];
        string[] forbiddenElements =
        [
            "svg", "math", "style", "script", "iframe", "object", "embed",
            "form", "input", "template", "x-widget"
        ];

        Assert.Multiple(() =>
        {
            foreach (HtmlCompiledSanitizerPolicy policy in policies)
            {
                foreach (string element in forbiddenElements)
                {
                    Assert.That(policy.IsElementAllowed(element), Is.False,
                        element);
                }

                Assert.That(policy.IsAttributeAllowed("style", "div"),
                    Is.False);
                Assert.That(policy.GetAllowedCssProperties(), Is.Empty);
                Assert.That(policy.GetAllowDataAttributes(), Is.False);
                Assert.That(policy.GetAllowAriaAttributes(), Is.False);
            }
        });
    }

    [Test]
    public void PresetFactoriesReturnIndependentBuildersFromSharedBaselines()
    {
        HtmlSanitizerPolicy changed =
            HtmlSanitizerPolicy.CreateBasicFormatting();
        changed.AllowElement("script");
        changed.AllowAttribute("style");
        changed.AllowCssProperty("color");
        HtmlCompiledSanitizerPolicy changedCompiled = changed.Compile();
        HtmlCompiledSanitizerPolicy fresh =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();

        Assert.Multiple(() =>
        {
            Assert.That(changedCompiled.IsElementAllowed("script"), Is.True);
            Assert.That(changedCompiled.IsAttributeAllowed("style", "p"),
                Is.True);
            Assert.That(changedCompiled.IsCssPropertyAllowed("color"), Is.True);
            Assert.That(fresh.IsElementAllowed("script"), Is.False);
            Assert.That(fresh.IsAttributeAllowed("style", "p"), Is.False);
            Assert.That(fresh.GetAllowedCssProperties(), Is.Empty);
        });
    }

    [Test]
    public void PresetFactoriesAreSafeForConcurrentReuse()
    {
        int failures = 0;

        Parallel.For(0, 1000, index =>
        {
            HtmlSanitizerPolicy builder = index % 2 == 0
                ? HtmlSanitizerPolicy.CreateRichText()
                : HtmlSanitizerPolicy.CreateHtmlOnly();
            builder.AllowElement("local-widget");
            HtmlCompiledSanitizerPolicy local = builder.Compile();
            HtmlCompiledSanitizerPolicy fresh = index % 2 == 0
                ? HtmlSanitizerPolicy.CreateRichText().Compile()
                : HtmlSanitizerPolicy.CreateHtmlOnly().Compile();
            if (!local.IsElementAllowed("local-widget")
                || fresh.IsElementAllowed("local-widget")
                || fresh.IsElementAllowed("script")
                || fresh.IsAttributeAllowed("style", "div"))
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.That(failures, Is.Zero);
    }
}
