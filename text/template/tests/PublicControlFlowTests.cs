using System;
using System.Collections;

namespace template.Tests;

[TestFixture]
public class PublicControlFlowTests
{
    [Test]
    public void Render_WhileSupportsAssignmentContinueBreakAndNestedCapture()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        string templateText = "{% set n 0 %}{% capture trace %}{% while n < 5 %}{% set n n + 1 %}{% if n == 2 %}{% continue %}{% endif %}{{ n }}{% if n == 4 %}{% break %}{% endif %}{% endwhile %}{% endcapture %}{{ trace }}|{{ n }}";

        string result = renderer.RenderToString(templateText, EmptyModel());

        Assert.That(result, Is.EqualTo("134|4"));
    }

    [Test]
    public void Render_WhileCountsIterationsAgainstTheConfiguredQuota()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumLoopIterations = 2;
        TemplateRenderer renderer = CreateRenderer(options);
        string templateText = "{% set n 0 %}{% while n < 3 %}{% set n n + 1 %}{% endwhile %}";

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString(templateText, EmptyModel()));
        Assert.That(renderer.RenderToString("{% while false %}never{% endwhile %}done", EmptyModel()), Is.EqualTo("done"));
    }

    [Test]
    public void Render_ReturnPropagatesThroughNestedBlocksAndReturnsAValueFromMacro()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        string templateText = "{% macro find limit %}{% set n 0 %}{% while n < limit %}{% set n n + 1 %}{% for item in 1..2 %}{% with item %}{% if n == 3 and this == 2 %}{% return n * 10 %}{% endif %}{% endwith %}{% endfor %}{% endwhile %}none{% endmacro %}{{ find(5) }}|{{ find(2) }}";

        string result = renderer.RenderToString(templateText, EmptyModel());

        Assert.That(result, Is.EqualTo("30|none"));
    }

    [Test]
    public void Render_NestedMacroReturnIsConsumedAtEachMacroBoundary()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        string templateText = "{% macro inner %}ignored{% return \"inner\" %}after{% endmacro %}{% macro outer %}before{{ inner() }}{% return \"outer\" %}after{% endmacro %}{{ outer() }}|{{ inner() }}|tail";

        string result = renderer.RenderToString(templateText, EmptyModel());

        Assert.That(result, Is.EqualTo("outer|inner|tail"));
    }

    [Test]
    public void Render_ReturnWithoutValueProducesNullAndReturnedStringsUseNormalEscaping()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = CreateRenderer(options);
        string templateText = "{% macro stop %}ignored{% return %}after{% endmacro %}{% macro value %}{% return \"<b>value</b>\" %}{% endmacro %}{% macro body %}<b>body</b>{% endmacro %}{{ stop() }}|{{ value() }}|{{ body() }}";

        string result = renderer.RenderToString(templateText, EmptyModel());

        Assert.That(result, Is.EqualTo("|&lt;b&gt;value&lt;/b&gt;|<b>body</b>"));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue EmptyModel()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(new Hashtable());
    }
}
