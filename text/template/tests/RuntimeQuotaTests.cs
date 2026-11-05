using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class RuntimeQuotaTests
{
    [Test]
    public void Render_EnforcesOutputLengthQuotaDuringStringRendering()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumOutputLength = 4;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("abcde", CreateRoot()));
    }

    [Test]
    public void Render_EnforcesLoopIterationQuota()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumLoopIterations = 2;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% for item in items %}{{ item }}{% endfor %}", CreateRoot()));
    }

    [Test]
    public void Render_EnforcesExpressionDepthQuota()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumExpressionDepth = 2;
        TemplateRenderer renderer = TemplateRenderer.Create(options);
        TemplateDocumentSyntax document = CreateDeepExpressionDocument();

        Assert.Throws<InvalidOperationException>(() => renderer.RenderSyntaxToString(document, CreateRoot()));
    }

    [Test]
    public void Render_EnforcesIncludeDepthQuota()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("leaf", "leaf");
        TemplateOptions options = new TemplateOptions();
        options.MaximumIncludeDepth = 0;
        TemplateRenderer renderer = TemplateRenderer.Create(options, registry);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% include \"leaf\" %}", CreateRoot()));
    }

    [Test]
    public void Render_EnforcesTemplateCountQuota()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("one", "one{% include \"two\" %}");
        registry.RegisterTemplate("two", "two");
        TemplateOptions options = new TemplateOptions();
        options.MaximumTemplateCount = 2;
        TemplateRenderer renderer = TemplateRenderer.Create(options, registry);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% include \"one\" %}", CreateRoot()));
    }

    [Test]
    public void Render_EnforcesElapsedRenderTimeQuota()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("slow", delegate(List<TemplateValue> arguments)
        {
            Thread.Sleep(25);
            return TemplateValue.FromString("done");
        });
        TemplateOptions options = new TemplateOptions();
        options.MaximumRenderMilliseconds = 1;
        TemplateRenderer renderer = TemplateRenderer.Create(options, registry);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ slow() }}", CreateRoot()));
    }

    [Test]
    public void RenderToWriter_EnforcesLoopQuotaBeforePartialOutputContinues()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumLoopIterations = 1;
        TemplateRenderer renderer = TemplateRenderer.Create(options);
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToWriter("{% for item in items %}{{ item }}{% endfor %}done", CreateRoot(), writer));
        Assert.That(writer.ToString(), Does.Not.Contain("done"));
    }

    [Test]
    public void Context_RejectsExhaustedMaximumIntegerQuotasBeforeIncrementing()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumLoopIterations = 2147483647;
        options.MaximumTemplateCount = 2147483647;
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), writer, options);
        context.RuntimeQuotas.LoopIterationCount = 2147483647;
        context.RuntimeQuotas.TemplateCount = 2147483647;

        Assert.Throws<InvalidOperationException>(() => context.CountLoopIteration());
        Assert.Throws<InvalidOperationException>(() => context.CountTemplate(TemplateSource.FromString("x")));
    }

    static TemplateDocumentSyntax CreateDeepExpressionDocument()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateExpressionSyntax literal = TemplateExpressionSyntax.Create(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Literal, TemplateLiteralSyntaxKind.Number, "1");
        TemplateExpressionSyntax inner = new TemplateExpressionSyntax(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Group, "inner");
        TemplateExpressionSyntax outer = new TemplateExpressionSyntax(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Group, "outer");
        inner.AddChild(literal);
        outer.AddChild(inner);
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(0, 1), outer, false, true);
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);
        document.AddChild(TemplateSyntaxNode.FromOutput(output));
        return document;
    }

    static TemplateValue CreateRoot()
    {
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromString("a"));
        items.Add(TemplateValue.FromString("b"));
        items.Add(TemplateValue.FromString("c"));

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["items"] = TemplateValue.FromList(items);
        return TemplateValue.FromMap(root);
    }
}
