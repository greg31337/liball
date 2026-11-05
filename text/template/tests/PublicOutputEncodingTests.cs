using System;
using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicOutputEncodingTests
{
    [Test]
    public void CustomEncoder_RespectsAutomaticRawSafeAndForceEscapeOutput()
    {
        RecordingOutputEncoder encoder = new RecordingOutputEncoder();
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        options.OutputEncoder = encoder;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("<x>");
        string templateText = "{{ value }}|{{ value | safe }}|{{ value | raw }}|{{ value | force_escape }}";

        string buffered = renderer.RenderToString(templateText, model);
        string streamed = RenderToWriter(renderer, templateText, model);

        Assert.That(buffered, Is.EqualTo("[Html:<x>]|<x>|<x>|&lt;x&gt;"));
        Assert.That(streamed, Is.EqualTo(buffered));
        Assert.That(encoder.CallCount, Is.EqualTo(2));
        Assert.That(encoder.LastMode, Is.EqualTo(TemplateEscapingMode.Html));
    }

    [Test]
    public void CustomEncoder_IsBypassedWhenAutomaticEscapingIsDisabled()
    {
        RecordingOutputEncoder encoder = new RecordingOutputEncoder();
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = false;
        options.EscapingMode = TemplateEscapingMode.Xml;
        options.OutputEncoder = encoder;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("<&>");

        string buffered = renderer.RenderToString("{{ value }}", model);
        string streamed = RenderToWriter(renderer, "{{ value }}", model);

        Assert.That(buffered, Is.EqualTo("<&>"));
        Assert.That(streamed, Is.EqualTo(buffered));
        Assert.That(encoder.CallCount, Is.EqualTo(0));
    }

    [Test]
    public void CustomEncoder_IsPreservedByMustacheProfileAndRawOutputBypassesIt()
    {
        RecordingOutputEncoder encoder = new RecordingOutputEncoder();
        TemplateOptions options = new TemplateOptions();
        options.OutputEncoder = encoder;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("<x>");
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

        string buffered = renderer.RenderMustacheToString("{{ value }}|{{{ value }}}", model);
        renderer.RenderMustacheToWriter("{{ value }}|{{{ value }}}", model, writer);

        Assert.That(buffered, Is.EqualTo("[Html:<x>]|<x>"));
        Assert.That(writer.ToString(), Is.EqualTo(buffered));
        Assert.That(encoder.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void CustomEncoder_NullResultFailsBothRenderingModes()
    {
        NullOutputEncoder encoder = new NullOutputEncoder();
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        options.OutputEncoder = encoder;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("x");

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ value }}", model));
        Assert.Throws<InvalidOperationException>(() => RenderToWriter(renderer, "{{ value }}", model));
    }

    [TestCase(TemplateEscapingMode.Html, "<a title=\"'&\">", "&lt;a title=&quot;&#39;&amp;&quot;&gt;")]
    [TestCase(TemplateEscapingMode.Xml, "<a title=\"'&\">😀", "&lt;a title=&quot;&apos;&amp;&quot;&gt;😀")]
    [TestCase(TemplateEscapingMode.JsonString, "\"\\\b\f\n\r\t\u0001😀", "\\\"\\\\\\b\\f\\n\\r\\t\\u0001\\uD83D\\uDE00")]
    [TestCase(TemplateEscapingMode.Url, "é /%", "%C3%A9%20%2F%25")]
    [TestCase(TemplateEscapingMode.JavaScriptString, "'\"\\\n</script>&=-\u2028\u2029😀", "\\'\\\"\\\\\\n\\u003C/script\\u003E\\u0026\\u003D\\u002D\\u2028\\u2029\\uD83D\\uDE00")]
    [TestCase(TemplateEscapingMode.CsvField, "a,\"b\"\r\n", "\"a,\"\"b\"\"\r\n\"")]
    public void BuiltInFormats_ConformAcrossBufferedAndWriterRendering(TemplateEscapingMode mode, string value, string expected)
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = mode;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel(value);

        string buffered = renderer.RenderToString("{{ value }}", model);
        string streamed = RenderToWriter(renderer, "{{ value }}", model);

        Assert.That(buffered, Is.EqualTo(expected));
        Assert.That(streamed, Is.EqualTo(expected));
    }

    [TestCase("native", "<x>")]
    [TestCase("liquid", "<x>")]
    [TestCase("mustache", "&lt;x&gt;")]
    [TestCase("handlebars", "&lt;x&gt;")]
    [TestCase("jinja", "&lt;x&gt;")]
    [TestCase("django", "&lt;x&gt;")]
    public void OutputProfiles_ApplyTheirAutomaticEscapingDefaults(string profileName, string expected)
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateOptions options = CreateProfileOptions(catalog, profileName);
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("<x>");

        string buffered = renderer.RenderToString("{{ value }}", model);
        string streamed = RenderToWriter(renderer, "{{ value }}", model);

        Assert.That(buffered, Is.EqualTo(expected));
        Assert.That(streamed, Is.EqualTo(expected));
    }

    [Test]
    public void JinjaAutoescapeBlocks_OverrideAndRestoreTemplateEscapeState()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateRenderer renderer = CreateRenderer(catalog.CreateJinjaOptions());
        TemplateValue model = CreateModel("<x>");
        string templateText = "{{ value }}|{% autoescape false %}{{ value }}|{% autoescape true %}{{ value }}{% endautoescape %}|{{ value }}{% endautoescape %}|{{ value }}";

        string buffered = renderer.RenderToString(templateText, model);
        string streamed = RenderToWriter(renderer, templateText, model);

        Assert.That(buffered, Is.EqualTo("&lt;x&gt;|<x>|&lt;x&gt;|<x>|&lt;x&gt;"));
        Assert.That(streamed, Is.EqualTo(buffered));
    }

    [Test]
    public void FailedAutoescapeBlock_DoesNotChangeLaterRendererEscaping()
    {
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        TemplateOptions options = catalog.CreateJinjaOptions();
        options.StrictUnknownFunctions = true;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel("<x>");

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% autoescape false %}{{ unknown() }}{% endautoescape %}", model));

        Assert.That(renderer.RenderToString("{{ value }}", model), Is.EqualTo("&lt;x&gt;"));
        Assert.That(RenderToWriter(renderer, "{{ value }}", model), Is.EqualTo("&lt;x&gt;"));
        Assert.That(options.AutoEscape, Is.True);
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
    }

    [Test]
    public void RenderContextOptions_OverrideRendererDefaultsForBufferedAndStreamingDocuments()
    {
        TemplateDocumentSyntax document = ParseDocument("{{ value }}");
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Xml;
        TemplateValue model = CreateModel("<&>");
        StringWriter bufferedWriter = new StringWriter(CultureInfo.InvariantCulture);
        StringWriter streamingWriter = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext bufferedContext = CreateContext(model, bufferedWriter, options);
        TemplateRenderContext streamingContext = CreateContext(model, streamingWriter, options);
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(document, bufferedContext);
        renderer.RenderStreaming(document, streamingContext);

        Assert.That(bufferedWriter.ToString(), Is.EqualTo("&lt;&amp;&gt;"));
        Assert.That(streamingWriter.ToString(), Is.EqualTo(bufferedWriter.ToString()));
    }

    [TestCase("\u0001")]
    [TestCase("\uFFFE")]
    public void XmlOutput_RejectsCharactersForbiddenByXml10InBothRenderingModes(string value)
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Xml;
        TemplateRenderer renderer = CreateRenderer(options);
        TemplateValue model = CreateModel(value);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ value }}", model));
        Assert.Throws<InvalidOperationException>(() => RenderToWriter(renderer, "{{ value }}", model));
    }

    [Test]
    public void XmlOutput_RejectsMalformedUnicodeSurrogatesInBothRenderingModes()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Xml;
        TemplateRenderer renderer = CreateRenderer(options);
        string[] values = new string[2];
        values[0] = new string(new char[] { (char)55296 });
        values[1] = new string(new char[] { (char)56320 });
        int index = 0;
        while (index < values.Length)
        {
            TemplateValue model = CreateModel(values[index]);
            Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ value }}", model));
            Assert.Throws<InvalidOperationException>(() => RenderToWriter(renderer, "{{ value }}", model));
            index = index + 1;
        }
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

    static TemplateValue CreateModel(string value)
    {
        Hashtable model = new Hashtable();
        model["value"] = value;
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(model);
    }

    static TemplateDocumentSyntax ParseDocument(string templateText)
    {
        TemplateSource source = new TemplateSource("inline", null, null, null, templateText);
        TemplateLexer lexer = new TemplateLexer(source);
        TemplateParser parser = new TemplateParser(lexer.Lex());
        return parser.Parse().Document;
    }

    static TemplateOptions CreateProfileOptions(TemplateProfileCatalog catalog, string profileName)
    {
        if (profileName == "native")
        {
            return catalog.CreateNativeOptions();
        }

        if (profileName == "liquid")
        {
            return catalog.CreateLiquidOptions();
        }

        if (profileName == "mustache")
        {
            return catalog.CreateMustacheOptions();
        }

        if (profileName == "handlebars")
        {
            return catalog.CreateHandlebarsOptions();
        }

        if (profileName == "jinja")
        {
            return catalog.CreateJinjaOptions();
        }

        return catalog.CreateDjangoOptions();
    }
}

public class RecordingOutputEncoder : ITemplateOutputEncoder
{
    public int CallCount;
    public TemplateEscapingMode LastMode;

    public RecordingOutputEncoder()
    {
        CallCount = 0;
        LastMode = TemplateEscapingMode.None;
    }

    public string Encode(string text, TemplateEscapingMode mode)
    {
        CallCount = CallCount + 1;
        LastMode = mode;
        return "[" + mode.ToString() + ":" + text + "]";
    }
}

public class NullOutputEncoder : ITemplateOutputEncoder
{
    public string Encode(string text, TemplateEscapingMode mode)
    {
        return null!;
    }
}
