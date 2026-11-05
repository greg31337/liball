using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace template.Tests;

public class CountingTextWriter : TextWriter
{
    public StringBuilder Builder;
    public int StringWriteCount;
    public int CharWriteCount;

    public CountingTextWriter()
    {
        Builder = new StringBuilder();
        StringWriteCount = 0;
        CharWriteCount = 0;
    }

    public override Encoding Encoding
    {
        get
        {
            return Encoding.UTF8;
        }
    }

    public override void Write(string? value)
    {
        if (value != null)
        {
            Builder.Append(value);
        }

        StringWriteCount = StringWriteCount + 1;
    }

    public override void Write(char value)
    {
        Builder.Append(value);
        CharWriteCount = CharWriteCount + 1;
    }

    public override string ToString()
    {
        return Builder.ToString();
    }
}

[TestFixture]
public class StreamingRenderTests
{
    [Test]
    public void RenderToWriter_StreamsStableChunksToWriter()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        CountingTextWriter writer = new CountingTextWriter();

        renderer.RenderToWriter("A {{ name }} B {{ name }} C", CreateRoot(), writer);

        Assert.That(writer.ToString(), Is.EqualTo("A Ada B Ada C"));
        Assert.That(writer.StringWriteCount, Is.GreaterThan(1));
    }

    [Test]
    public void RenderToWriter_StreamingPreservesStringRenderTrimSemantics()
    {
        TemplateOptions options = new TemplateOptions();
        options.TrimFinalNewline = true;
        TemplateRenderer renderer = TemplateRenderer.Create(options);
        TemplateParseResult parseResult = TemplateParser.ParseNative("A {{- name -}} \n");
        StringWriter streamingWriter = new StringWriter(CultureInfo.InvariantCulture);
        StringWriter composedWriter = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), composedWriter, options);

        renderer.RenderDocumentToWriter(parseResult.Document, CreateRoot(), streamingWriter);
        renderer.Render(parseResult.Document, context);

        Assert.That(streamingWriter.ToString(), Is.EqualTo(composedWriter.ToString()));
        Assert.That(streamingWriter.ToString(), Is.EqualTo("AAda"));
    }

    [Test]
    public void RenderToWriter_StreamingUsesTemplateWriterOutputLimit()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumOutputLength = 4;
        TemplateRenderer renderer = TemplateRenderer.Create(options);
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToWriter("ab {{ name }}", CreateRoot(), writer));
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(root);
    }
}
