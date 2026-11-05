
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorEscapingAndErrorsTests
{
    [Test]
    public void IdentifierEscapingFollowsCssomCharacterRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSelectors.EscapeIdentifier(""), Is.Empty);
            Assert.That(HtmlSelectors.EscapeIdentifier("\0"),
                Is.EqualTo("�"));
            Assert.That(HtmlSelectors.EscapeIdentifier(
                "\u0001\u001f\u007f"), Is.EqualTo("\\1 \\1f \\7f "));
            Assert.That(HtmlSelectors.EscapeIdentifier("0name"),
                Is.EqualTo("\\30 name"));
            Assert.That(HtmlSelectors.EscapeIdentifier("-0name"),
                Is.EqualTo("-\\30 name"));
            Assert.That(HtmlSelectors.EscapeIdentifier("-"),
                Is.EqualTo("\\-"));
            Assert.That(HtmlSelectors.EscapeIdentifier("--name"),
                Is.EqualTo("--name"));
            Assert.That(HtmlSelectors.EscapeIdentifier("a b#c"),
                Is.EqualTo("a\\ b\\#c"));
            Assert.That(HtmlSelectors.EscapeIdentifier("_é😀"),
                Is.EqualTo("_é😀"));
            Assert.That(HtmlSelectors.EscapeIdentifier("a\uD800b"),
                Is.EqualTo("a�b"));
        });
    }

    [Test]
    public void StringEscapingReturnsACompleteDoubleQuotedCssString()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSelectors.EscapeString(""),
                Is.EqualTo("\"\""));
            Assert.That(HtmlSelectors.EscapeString("a\"b\\c'd"),
                Is.EqualTo("\"a\\\"b\\\\c'd\""));
            Assert.That(HtmlSelectors.EscapeString("\0\n\u007f"),
                Is.EqualTo("\"�\\a \\7f \""));
            Assert.That(HtmlSelectors.EscapeString("é😀"),
                Is.EqualTo("\"é😀\""));
            Assert.That(HtmlSelectors.EscapeString("\uDC00"),
                Is.EqualTo("\"�\""));
        });
    }

    [Test]
    public void EscapedIdentifiersAndStringsRoundTripThroughQueries()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode node = document.CreateElement("div");
        string id = "0 leading#😀";
        string value = "line\n\"slash\\end";
        node.SetAttribute("id", id);
        node.SetAttribute("data-value", value);
        document.QuerySelector("main").Append(node);

        string idSelector = "#" + HtmlSelectors.EscapeIdentifier(id);
        string attributeSelector = "[data-value="
            + HtmlSelectors.EscapeString(value) + "]";

        Assert.Multiple(() =>
        {
            Assert.That(document.QuerySelector(idSelector), Is.SameAs(node));
            Assert.That(document.QuerySelector(attributeSelector),
                Is.SameAs(node));
        });
    }

    [Test]
    public void EscapingRejectsNullInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSelectors.EscapeIdentifier(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSelectors.EscapeString(null));
        });
    }

    [Test]
    public void CompileErrorsExposeCodeMessagePositionAndKind()
    {
        HtmlSelectorCompileResult invalid = HtmlSelectors.Compile("div>");
        HtmlSelectorCompileResult unsupported = HtmlSelectors.Compile(
            "div,\n::before");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumSelectorComplexity(1);
        HtmlSelectorCompileResult complexity = HtmlSelectors.Compile(
            "div.item", limits);
        HtmlSelectorCompileError syntaxError = invalid.GetErrors()[0];
        HtmlSelectorCompileError unsupportedError =
            unsupported.GetErrors()[0];
        HtmlSelectorCompileError complexityError =
            complexity.GetErrors()[0];

        Assert.Multiple(() =>
        {
            Assert.That(syntaxError.GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.SelectorInvalidSyntax));
            Assert.That(syntaxError.GetIdentifier(), Is.EqualTo("UH05001"));
            Assert.That(syntaxError.GetMessage(), Is.Not.Empty);
            Assert.That(syntaxError.GetPosition().GetCharacterOffset(),
                Is.EqualTo(4));
            Assert.That(syntaxError.GetKind(), Is.EqualTo(
                HtmlSelectorCompileErrorKind.InvalidSyntax));
            Assert.That(syntaxError.IsUnsupportedFeature(), Is.False);

            Assert.That(unsupportedError.GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.SelectorUnsupportedFeature));
            Assert.That(unsupportedError.GetPosition().GetLine(),
                Is.EqualTo(2));
            Assert.That(unsupportedError.GetPosition().GetColumn(),
                Is.EqualTo(1));
            Assert.That(unsupportedError.GetKind(), Is.EqualTo(
                HtmlSelectorCompileErrorKind.UnsupportedFeature));
            Assert.That(unsupportedError.IsUnsupportedFeature(), Is.True);

            Assert.That(complexityError.GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.SelectorComplexityExceeded));
            Assert.That(complexityError.GetKind(), Is.EqualTo(
                HtmlSelectorCompileErrorKind.ComplexityExceeded));
            Assert.That(complexityError.IsUnsupportedFeature(), Is.False);
        });
    }

    [Test]
    public void CompileErrorCollectionsAreSnapshotsAndSuccessHasNoErrors()
    {
        HtmlSelectorCompileResult failed = HtmlSelectors.Compile("::after");
        HtmlSelectorCompileError original = failed.GetErrors()[0];
        HtmlSelectorCompileError[] snapshot = failed.GetErrors();
        snapshot[0] = null;
        HtmlSelectorCompileResult success = HtmlSelectors.Compile("main");

        Assert.Multiple(() =>
        {
            Assert.That(failed.GetErrors()[0], Is.SameAs(original));
            Assert.That(failed.GetErrors(), Has.Length.EqualTo(
                failed.GetDiagnostics().Length));
            Assert.That(success.IsSuccess(), Is.True);
            Assert.That(success.GetErrors(), Is.Empty);
        });
    }
}
