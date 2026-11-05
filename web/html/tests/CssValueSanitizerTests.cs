
namespace UniversalHtml.Tests;

[TestFixture]
public class CssValueSanitizerTests
{
    [TestCase("color", "red", "red")]
    [TestCase("color", "#A1B2C3", "#a1b2c3")]
    [TestCase("border-color", "red #0F08 transparent", "red #0f08 transparent")]
    [TestCase("border-style", "SOLID", "solid")]
    [TestCase("border-width", "2PX", "2px")]
    [TestCase("margin", "-1px auto 5% 0", "-1px auto 5% 0")]
    [TestCase("padding", "1px 2em 3% 0", "1px 2em 3% 0")]
    [TestCase("font-size", "clamp(1rem, 2vw, 3rem)",
        "clamp(1rem, 2vw, 3rem)")]
    [TestCase("line-height", "1.5", "1.5")]
    [TestCase("opacity", "50%", "50%")]
    [TestCase("text-decoration-line", "UNDERLINE overline",
        "underline overline")]
    [TestCase("width", "calc(100% - min(2px, 1em))",
        "calc(100% - min(2px, 1em))")]
    public void AcceptsValuesMatchingConservativePropertyProfiles(
        string property, string value, string expected)
    {
        CssValueSanitizeResult result = CssValueSanitizer.Sanitize(property,
            value, CreateUrlPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAllowed(), Is.True);
            Assert.That(result.GetValue(), Is.EqualTo(expected));
            Assert.That(result.ContainsUrl(), Is.False);
            Assert.That(result.WasRewritten(), Is.EqualTo(value != expected));
        });
    }

    [Test]
    public void ValidatesNestedColorMathAndVariableFunctions()
    {
        CssValueSanitizeResult color = CssValueSanitizer.Sanitize("color",
            "RGB(calc(100% - 10%) 0 0 / 50%)", CreateUrlPolicy());
        CssValueSanitizeResult variable = CssValueSanitizer.Sanitize("color",
            "var(--Theme, hsl(120deg 50% 50%))", CreateUrlPolicy());
        CssValueSanitizeResult length = CssValueSanitizer.Sanitize("width",
            "calc((100% - var(--Gap, 2px)) / 2)", CreateUrlPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(color.IsAllowed(), Is.True);
            Assert.That(color.GetValue(),
                Is.EqualTo("rgb(calc(100% - 10%) 0 0 / 50%)"));
            Assert.That(variable.IsAllowed(), Is.True);
            Assert.That(variable.GetValue(),
                Is.EqualTo("var(--Theme, hsl(120deg 50% 50%))"));
            Assert.That(length.IsAllowed(), Is.True);
            Assert.That(length.GetValue(),
                Is.EqualTo("calc((100% - var(--Gap, 2px)) / 2)"));
        });
    }

    [TestCase("color", "10px")]
    [TestCase("width", "red")]
    [TestCase("padding", "-1px")]
    [TestCase("opacity", "101%")]
    [TestCase("font-weight", "1001")]
    [TestCase("border-color", "red blue green yellow black")]
    [TestCase("text-decoration-line", "none underline")]
    [TestCase("width", "calc(1px +)")]
    [TestCase("width", "clamp(1px, 2px)")]
    [TestCase("width", "calc(1px + 1s)")]
    [TestCase("width", "calc(1px + 1deg)")]
    [TestCase("opacity", "calc(1px / 2)")]
    [TestCase("font-family", "serif,")]
    public void RejectsValuesOutsideTheirPropertyGrammar(string property,
        string value)
    {
        CssValueSanitizeResult result = CssValueSanitizer.Sanitize(property,
            value, CreateUrlPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAllowed(), Is.False);
            Assert.That(result.GetValue(), Is.Empty);
            Assert.That(result.ContainsUrl(), Is.False);
        });
    }

    [Test]
    public void NormalizesEscapedNamesFunctionsUnitsAndStrings()
    {
        CssDeclaration declaration = CssDeclarationParser.Parse(
            @"c\6flor:r\65 d").GetDeclarations()[0];
        CssValueSanitizeResult property = CssValueSanitizer.Sanitize(
            declaration, CreateUrlPolicy());
        CssValueSanitizeResult function = CssValueSanitizer.Sanitize("color",
            @"r\67 b(255 0 0)", CreateUrlPolicy());
        CssValueSanitizeResult unit = CssValueSanitizer.Sanitize("width",
            @"2p\78", CreateUrlPolicy());
        CssValueSanitizeResult family = CssValueSanitizer.Sanitize(
            "font-family", "\"Open\\20 Sans\", SERIF", CreateUrlPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(property.IsAllowed(), Is.True);
            Assert.That(property.GetValue(), Is.EqualTo("red"));
            Assert.That(function.IsAllowed(), Is.True);
            Assert.That(function.GetValue(), Is.EqualTo("rgb(255 0 0)"));
            Assert.That(unit.IsAllowed(), Is.True);
            Assert.That(unit.GetValue(), Is.EqualTo("2px"));
            Assert.That(family.IsAllowed(), Is.True);
            Assert.That(family.GetValue(), Is.EqualTo("\"Open Sans\", serif"));
        });
    }

    [Test]
    public void ReusesUrlPolicyForQuotedUnquotedAndRelativeUrls()
    {
        HtmlSanitizerUrlPolicy preserving = CreateUrlPolicy();
        HtmlSanitizerUrlPolicy rewriting = new HtmlSanitizerUrlPolicy(
            new[] { "https" }, Array.Empty<string>(), Array.Empty<int>(),
            false, true, false, true, "https://example.test/base/", true);

        CssValueSanitizeResult quoted = CssValueSanitizer.Sanitize(
            "background-image", "url(\"https://example.test/a.png\")",
            preserving);
        CssValueSanitizeResult unquoted = CssValueSanitizer.Sanitize(
            "list-style-image", "URL(https://example.test/list.svg)",
            preserving);
        CssValueSanitizeResult relative = CssValueSanitizer.Sanitize(
            "background-image", "url(images/a.png)", rewriting);

        Assert.Multiple(() =>
        {
            Assert.That(quoted.IsAllowed(), Is.True);
            Assert.That(quoted.GetValue(),
                Is.EqualTo("url(\"https://example.test/a.png\")"));
            Assert.That(quoted.ContainsUrl(), Is.True);
            Assert.That(unquoted.IsAllowed(), Is.True);
            Assert.That(unquoted.GetValue(),
                Is.EqualTo("url(\"https://example.test/list.svg\")"));
            Assert.That(relative.IsAllowed(), Is.True);
            Assert.That(relative.GetValue(),
                Is.EqualTo("url(\"https://example.test/base/images/a.png\")"));
            Assert.That(relative.WasRewritten(), Is.True);
        });
    }

    [TestCase("background-image", "url(javascript:alert(1))")]
    [TestCase("background-image", @"url(j\61vascript:alert(1))")]
    [TestCase("background-image", "url(//evil.test/a.png)")]
    [TestCase("background-image", "url(https://user@example.test/a.png)")]
    [TestCase("background-image", "var(--external-image)")]
    [TestCase("color", "url(https://example.test/a.png)")]
    public void RejectsUrlsThatAreUnsafeOrInvalidForTheProperty(
        string property, string value)
    {
        CssValueSanitizeResult result = CssValueSanitizer.Sanitize(property,
            value, CreateUrlPolicy());

        Assert.That(result.IsAllowed(), Is.False);
    }

    [TestCase("behavior", "url(https://example.test/a.htc)")]
    [TestCase("-moz-binding", "url(https://example.test/a.xml#xbl)")]
    [TestCase("position", "fixed")]
    [TestCase("color", "expression(alert(1))")]
    [TestCase("color", @"e\78 pression(alert(1))")]
    [TestCase("color", "attr(data-color)")]
    [TestCase("background-image", "image-set(url(a.png) 1x)")]
    [TestCase("color", "@import")]
    [TestCase("width", "1fr")]
    [TestCase("width", "1cqw")]
    public void RejectsLegacyExecutionBindingImportAndExcludedConstructs(
        string property, string value)
    {
        Assert.That(CssValueSanitizer.Sanitize(property, value,
            CreateUrlPolicy()).IsAllowed(), Is.False);
    }

    [TestCase("color", "rgb(1 2 3")]
    [TestCase("color", "red;")]
    [TestCase("color", "red}")]
    [TestCase("font-family", "\"unterminated")]
    [TestCase("background-image", "url(bad\"part)")]
    public void RejectsMalformedOrBoundaryBreakingTokens(string property,
        string value)
    {
        Assert.That(CssValueSanitizer.Sanitize(property, value,
            CreateUrlPolicy()).IsAllowed(), Is.False);
    }

    [Test]
    public void FailsClosedAtTokenInputAndNestingLimits()
    {
        HtmlLimits inputLimits = HtmlLimits.CreateDefault();
        inputLimits.SetMaximumInputCharacters(4);
        HtmlLimits tokenLimits = HtmlLimits.CreateDefault();
        tokenLimits.SetMaximumCssTokens(2);
        string deeplyNested = new string('(', 65) + "1px"
            + new string(')', 65);

        Assert.Multiple(() =>
        {
            Assert.That(CssValueSanitizer.Sanitize("color", "rebeccapurple",
                CreateUrlPolicy(), inputLimits).IsAllowed(), Is.False);
            Assert.That(CssValueSanitizer.Sanitize("margin", "1px 2px",
                CreateUrlPolicy(), tokenLimits).IsAllowed(), Is.False);
            Assert.That(CssValueSanitizer.Sanitize("width", deeplyNested,
                CreateUrlPolicy()).IsAllowed(), Is.False);
        });
    }

    [Test]
    public void ValidatesArgumentsAndRejectsUnknownOrEmptyProperties()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssValueSanitizer.Sanitize("--custom", "red",
                CreateUrlPolicy()).IsAllowed(), Is.False);
            Assert.That(CssValueSanitizer.Sanitize("made-up", "red",
                CreateUrlPolicy()).IsAllowed(), Is.False);
            Assert.That(CssValueSanitizer.Sanitize("color", "",
                CreateUrlPolicy()).IsAllowed(), Is.False);
            Assert.That(() => CssValueSanitizer.Sanitize(null, "red",
                CreateUrlPolicy()), Throws.ArgumentNullException);
            Assert.That(() => CssValueSanitizer.Sanitize("color", null,
                CreateUrlPolicy()), Throws.ArgumentNullException);
            Assert.That(() => CssValueSanitizer.Sanitize("color", "red",
                null), Throws.ArgumentNullException);
            Assert.That(() => CssValueSanitizer.Sanitize(
                (CssDeclaration)null, CreateUrlPolicy()),
                Throws.ArgumentNullException);
        });
    }

    static HtmlSanitizerUrlPolicy CreateUrlPolicy()
    {
        return new HtmlSanitizerUrlPolicy(new[] { "https" },
            Array.Empty<string>(), Array.Empty<int>(), false, true, false,
            true, "", false);
    }
}
