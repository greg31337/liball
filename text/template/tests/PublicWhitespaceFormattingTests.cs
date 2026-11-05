using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicWhitespaceFormattingTests
{
    [TestCase(TemplateWhitespaceMode.Preserve, "A  \r\n \t\r\n\n\nB\t \rC  ")]
    [TestCase(TemplateWhitespaceMode.NormalizeLineFeeds, "A  \n \t\n\n\nB\t \nC  ")]
    [TestCase(TemplateWhitespaceMode.NormalizeCarriageReturnLineFeeds, "A  \r\n \t\r\n\r\n\r\nB\t \r\nC  ")]
    [TestCase(TemplateWhitespaceMode.Minimize, "A\n\nB\nC")]
    public void WhitespaceModes_AreEquivalentAcrossEveryRenderEntryPoint(TemplateWhitespaceMode mode, string expected)
    {
        TemplateOptions options = new TemplateOptions();
        options.WhitespaceMode = mode;
        string templateText = "A  \r{{ empty }}\n \t\r\n\n\nB\t \rC  ";

        AssertAllNativeRenderModes(templateText, CreateModel(), options, expected);
    }

    [Test]
    public void GreedyAndNonGreedyMarkers_HaveDistinctSymmetricBehavior()
    {
        TemplateOptions options = new TemplateOptions();
        TemplateValue model = CreateModel();

        AssertAllNativeRenderModes("A \n  {{- value -}} \r\n  B", model, options, "AXB");
        AssertAllNativeRenderModes("A \n  {{~ value ~}} \r\n  B", model, options, "A \nX  B");
        AssertAllNativeRenderModes("A\n  {%~ if true ~%}\n  X\n  {%~ endif ~%}\nB", model, options, "A\n  X\nB");
    }

    [Test]
    public void StandaloneBlockAndBranchLines_AreRemovedWithoutBlankLoopLines()
    {
        TemplateOptions options = new TemplateOptions();
        TemplateValue model = CreateModel();
        string loopTemplate = "before\r\n  {% for item in items %}\r\n    {{ item }}\r\n  {% endfor %}\r\nafter";
        string branchTemplate = "before\n{% if false %}\nno\n{% else %}\nyes\n{% endif %}\nafter";

        AssertAllNativeRenderModes(loopTemplate, model, options, "before\r\n    A\r\n    B\r\nafter");
        AssertAllNativeRenderModes(branchTemplate, model, options, "before\nyes\nafter");
    }

    [Test]
    public void ProfileCanDisableStandaloneLineTrimmingDuringRendering()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateProfile profile = catalog.CreateNativeProfile();
        profile.Name = "preserve-lines";
        profile.StandaloneStatementTrimming = false;
        TemplateProfileRegistry profileRegistry = new TemplateProfileRegistry();
        profileRegistry.Register(profile);
        TemplateOptions options = profileRegistry.CreateOptions("preserve-lines", TemplateCompatibilityMode.Pragmatic);
        TemplateRenderer renderer = CreateRenderer(options);
        renderer.SetProfileRegistry(profileRegistry);
        TemplateValue model = CreateModel();
        string templateText = "A\n  {% set x 1 %}\nB";

        Assert.That(renderer.RenderToString(templateText, model), Is.EqualTo("A\n  \nB"));
        Assert.That(RenderToWriter(renderer, templateText, model), Is.EqualTo("A\n  \nB"));
    }

    [Test]
    public void HandlebarsTildes_RetainGreedyTrimBehaviorAfterProfileConversion()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateOptions options = catalog.CreateHandlebarsOptions();
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel();
        string templateText = "A \n  {{~ value ~}} \n  B";

        Assert.That(renderer.RenderToString(templateText, model), Is.EqualTo("AXB"));
        Assert.That(RenderToWriter(renderer, templateText, model), Is.EqualTo("AXB"));
        Assert.That(renderer.RenderToString("A\n  {{! note }}\nB", model), Is.EqualTo("A\nB"));
        Assert.That(RenderToWriter(renderer, "A\n  {{! note }}\nB", model), Is.EqualTo("A\nB"));
    }

    [TestCase(TemplateWhitespaceMode.Preserve, "items:\r\n\t  a\n\t  b\r\n\t  c\r\t  d")]
    [TestCase(TemplateWhitespaceMode.NormalizeLineFeeds, "items:\n\t  a\n\t  b\n\t  c\n\t  d")]
    public void AutoIndent_PreservesIndentationAcrossEveryNewlineKind(TemplateWhitespaceMode mode, string expected)
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoIndentMultilineValues = true;
        options.WhitespaceMode = mode;

        AssertAllNativeRenderModes("items:\r\n\t  {{ multiline }}", CreateModel(), options, expected);
    }

    [Test]
    public void NonGreedyLeftTrim_DisablesAutomaticIndentForThatOutput()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoIndentMultilineValues = true;

        AssertAllNativeRenderModes("items:\n  {{~ multiline }}", CreateModel(), options, "items:\na\nb\r\nc\rd");
    }

    [TestCase(TemplateWhitespaceMode.Preserve, "line\n\n", "line\n")]
    [TestCase(TemplateWhitespaceMode.NormalizeLineFeeds, "line\r\n", "line")]
    [TestCase(TemplateWhitespaceMode.NormalizeCarriageReturnLineFeeds, "line\n\n", "line\r\n")]
    [TestCase(TemplateWhitespaceMode.Minimize, "line  \r\n\n\n", "line\n")]
    public void FinalNewlineTrimming_RunsAfterWhitespaceNormalization(TemplateWhitespaceMode mode, string templateText, string expected)
    {
        TemplateOptions options = new TemplateOptions();
        options.WhitespaceMode = mode;
        options.TrimFinalNewline = true;

        AssertAllNativeRenderModes(templateText, CreateModel(), options, expected);
    }

    [Test]
    public void IndentFilter_SupportsWidthFirstLineBlankLinesAndMixedNewlines()
    {
        TemplateOptions options = new TemplateOptions();
        TemplateValue model = CreateModel();

        AssertAllNativeRenderModes("{{ indented | indent(2) }}", model, options, "a\n\n  b\r\n  c");
        AssertAllNativeRenderModes("{{ indented | indent(\"--\", true, true) }}", model, options, "--a\n--\n--b\r\n--c");
    }

    [Test]
    public void WhitespaceNormalizer_PreservesUnterminatedTrailingSpacesExceptWhenMinimizing()
    {
        TemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();

        Assert.That(normalizer.TrimTrailingLineWhitespace("a  \nb  "), Is.EqualTo("a\nb  "));
        Assert.That(normalizer.MinimizeWhitespace("a  \nb  "), Is.EqualTo("a\nb"));
    }

    [Test]
    public void Formatter_RetainsTrimKindsAndIsStableAcrossRoundTrips()
    {
        TemplateFormatter formatter = new TemplateFormatter();
        string templateText = "{%- if ok ~%} {{~ value -}} {#~ note ~#}{%~ endif -%}{%~ raw -%} raw {%~ endraw -%}";

        string formatted = formatter.Format(templateText);
        string roundTripped = formatter.Format(formatted);

        Assert.That(formatted, Is.EqualTo(
            "{%- if ok ~%}\n" +
            "  {{~ value -}}\n" +
            "  {#~ note ~#}\n" +
            "{%~ endif -%}\n" +
            "{%~ raw -%}\n" +
            "  raw\n" +
            "{%~ endraw -%}\n"));
        Assert.That(roundTripped, Is.EqualTo(formatted));
    }

    static void AssertAllNativeRenderModes(string templateText, TemplateValue model, TemplateOptions options, string expected)
    {
        TemplateRenderer renderer = CreateRenderer(options);
        Assert.That(renderer.RenderToString(templateText, model), Is.EqualTo(expected));
        Assert.That(RenderToWriter(renderer, templateText, model), Is.EqualTo(expected));

        TemplateDocumentSyntax document = ParseDocument(templateText);
        StringWriter bufferedWriter = new StringWriter(CultureInfo.InvariantCulture);
        StringWriter streamingWriter = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext bufferedContext = CreateContext(model, bufferedWriter, options);
        TemplateRenderContext streamingContext = CreateContext(model, streamingWriter, options);
        renderer.Render(document, bufferedContext);
        renderer.RenderStreaming(document, streamingContext);

        Assert.That(bufferedWriter.ToString(), Is.EqualTo(expected));
        Assert.That(streamingWriter.ToString(), Is.EqualTo(expected));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        return renderer;
    }

    static TemplateRenderContext CreateContext(TemplateValue model, TextWriter writer, TemplateOptions options)
    {
        return new TemplateRenderContext(model, writer, options, new TemplateRegistry(), new TemplateMemberResolver());
    }

    static string RenderToWriter(TemplateRenderer renderer, string templateText, TemplateValue model)
    {
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        renderer.RenderToWriter(templateText, model, writer);
        return writer.ToString();
    }

    static TemplateDocumentSyntax ParseDocument(string templateText)
    {
        TemplateSource source = new TemplateSource("whitespace", null, null, null, templateText);
        TemplateLexer lexer = new TemplateLexer(source);
        TemplateParser parser = new TemplateParser(lexer.Lex());
        return parser.Parse().Document;
    }

    static TemplateValue CreateModel()
    {
        Hashtable model = new Hashtable();
        ArrayList items = new ArrayList();
        items.Add("A");
        items.Add("B");
        model["empty"] = "";
        model["value"] = "X";
        model["multiline"] = "a\nb\r\nc\rd";
        model["indented"] = "a\n\nb\r\nc";
        model["items"] = items;
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(model);
    }
}
