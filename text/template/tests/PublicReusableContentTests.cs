using System;
using System.Collections.Generic;

namespace template.Tests;

[TestFixture]
public class PublicReusableContentTests
{
    [Test]
    public void Render_MacrosUseAnIsolatedRootContextAndDoNotLeakCallerLocals()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        string templateText = "{% set caller \"outer\" %}{% with nested %}{{ reveal(\"argument\") }}{% endwith %}|{{ caller }}{% macro reveal value %}{{ value }}:{{ caller }}:{{ this.rootName }}{% set caller \"inner\" %}{% endmacro %}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("argument::root|outer"));
    }

    [Test]
    public void Render_ImportedMacrosAreScopeIsolatedAndCanReturnValues()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("macros", "{% macro describe value %}{{ value }}:{{ caller }}:{{ rootName }}{% endmacro %}{% macro answer value %}ignored{% return value %}after{% endmacro %}");
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);
        string templateText = "{% set caller \"outer\" %}{% import \"macros\" %}{{ describe(\"argument\") }}|{{ answer(\"returned\") }}|{{ caller }}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("argument::root|returned|outer"));
        Assert.That(renderer.GetLastDependencies().Count, Is.EqualTo(1));
        Assert.That(renderer.GetLastDependencies()[0].Name, Is.EqualTo("macros"));
    }

    [Test]
    public void Render_MacroSignaturesRejectMissingExtraDuplicateAndInvalidArguments()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% macro pair left right %}{{ left }}{{ right }}{% endmacro %}{{ pair(\"a\") }}", CreateRoot()))!;
        InvalidOperationException extra = Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% macro one value %}{{ value }}{% endmacro %}{{ one(\"a\", \"b\") }}", CreateRoot()))!;
        InvalidOperationException duplicate = Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% macro duplicate value value %}{{ value }}{% endmacro %}", CreateRoot()))!;

        Assert.That(missing.Message, Does.Contain("expects 2 arguments but received 1"));
        Assert.That(extra.Message, Does.Contain("expects 1 arguments but received 2"));
        Assert.That(duplicate.Message, Does.Contain("duplicate parameter 'value'"));
    }

    [Test]
    public void Render_RegistryFunctionsFiltersTestsAndHelpersUseTheirCallablePaths()
    {
        int functionCalls = 0;
        int filterCalls = 0;
        int testCalls = 0;
        int helperCalls = 0;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("functionValue", delegate(List<TemplateValue> arguments)
        {
            functionCalls = functionCalls + 1;
            return TemplateValue.FromString("<function:" + arguments[0].StringValue + ">");
        });
        registry.RegisterFilter("filterValue", delegate(List<TemplateValue> arguments)
        {
            filterCalls = filterCalls + 1;
            return TemplateValue.FromString("<filter:" + arguments[0].StringValue + ">");
        });
        registry.RegisterTest("accepted", delegate(List<TemplateValue> arguments)
        {
            testCalls = testCalls + 1;
            return TemplateValue.FromBoolean(arguments[0].StringValue == "x");
        });
        registry.RegisterHelper("helperValue", delegate(List<TemplateValue> arguments)
        {
            helperCalls = helperCalls + 1;
            return TemplateValue.FromString("<helper:" + arguments[0].StringValue + ">");
        });
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = CreateRenderer(options, registry);

        string result = renderer.RenderToString("{{ functionValue(\"x\") }}|{{ \"x\" | filterValue }}|{{ \"x\" is accepted }}|{{ helperValue(\"x\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;function:x&gt;|&lt;filter:x&gt;|true|&lt;helper:x&gt;"));
        Assert.That(functionCalls, Is.EqualTo(1));
        Assert.That(filterCalls, Is.EqualTo(1));
        Assert.That(testCalls, Is.EqualTo(1));
        Assert.That(helperCalls, Is.EqualTo(1));
    }

    [Test]
    public void Render_FilterCallAndBlockHelperResultsHonorSafeStrings()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFilter("plainFilter", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("<plain-filter>");
        });
        registry.RegisterFilter("safeFilter", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromSafeString("<safe-filter>");
        });
        registry.RegisterFunction("plainCall", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString("<plain-call>");
        });
        registry.RegisterFunction("safeCall", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromSafeString("<safe-call>");
        });
        registry.RegisterBlockHelper("plainBlock", delegate(TemplateBlockHelperContext context)
        {
            return TemplateValue.FromString("<plain-block>");
        });
        registry.RegisterBlockHelper("safeBlock", delegate(TemplateBlockHelperContext context)
        {
            return TemplateValue.FromSafeString("<safe-block>");
        });
        TemplateOptions nativeOptions = new TemplateOptions();
        nativeOptions.AutoEscape = true;
        nativeOptions.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer nativeRenderer = CreateRenderer(nativeOptions, registry);

        string nativeResult = nativeRenderer.RenderToString("{% filter plainFilter %}body{% endfilter %}|{% filter safeFilter %}body{% endfilter %}|{% call plainCall %}body{% endcall %}|{% call safeCall %}body{% endcall %}", CreateRoot());

        TemplateOptions handlebarsOptions = new TemplateOptions();
        handlebarsOptions.SyntaxProfile = "handlebars";
        handlebarsOptions.AutoEscape = true;
        handlebarsOptions.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer handlebarsRenderer = CreateRenderer(handlebarsOptions, registry);
        string blockResult = handlebarsRenderer.RenderToString("{{#plainBlock}}body{{/plainBlock}}|{{#safeBlock}}body{{/safeBlock}}", CreateRoot());

        Assert.That(nativeResult, Is.EqualTo("&lt;plain-filter&gt;|<safe-filter>|&lt;plain-call&gt;|<safe-call>"));
        Assert.That(blockResult, Is.EqualTo("&lt;plain-block&gt;|<safe-block>"));
    }

    [Test]
    public void Render_ReturnInsideFilterAndCallBodiesSkipsTheOuterCallable()
    {
        int filterCalls = 0;
        int functionCalls = 0;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFilter("observeFilter", delegate(List<TemplateValue> arguments)
        {
            filterCalls = filterCalls + 1;
            return TemplateValue.FromString("wrong");
        });
        registry.RegisterFunction("observeCall", delegate(List<TemplateValue> arguments)
        {
            functionCalls = functionCalls + 1;
            return TemplateValue.FromString("wrong");
        });
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = CreateRenderer(options, registry);
        string templateText = "{% macro filtered %}{% filter observeFilter %}before{% return \"<filtered>\" %}after{% endfilter %}unreachable{% endmacro %}{% macro called %}{% call observeCall %}before{% return \"<called>\" %}after{% endcall %}unreachable{% endmacro %}{{ filtered() }}|{{ called() }}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;filtered&gt;|&lt;called&gt;"));
        Assert.That(filterCalls, Is.EqualTo(0));
        Assert.That(functionCalls, Is.EqualTo(0));
    }

    [Test]
    public void Render_FilterAndCallBlocksForwardNamedArguments()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFilter("describeFilter", delegate(List<TemplateValue> arguments)
        {
            TemplateValue named = arguments[2];
            return TemplateValue.FromString(arguments[0].StringValue + ":" + arguments[1].StringValue + ":" + named.MapValue["tone"].StringValue);
        });
        registry.RegisterFunction("describeCall", delegate(List<TemplateValue> arguments)
        {
            TemplateValue named = arguments[1];
            return TemplateValue.FromString(arguments[2].StringValue + ":" + arguments[0].StringValue + ":" + named.MapValue["tone"].StringValue);
        });
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);

        string result = renderer.RenderToString("{% filter describeFilter(\"position\", tone=\"quiet\") %}filter-body{% endfilter %}|{% call describeCall(\"position\", tone=\"quiet\") %}call-body{% endcall %}", CreateRoot());

        Assert.That(result, Is.EqualTo("filter-body:position:quiet|call-body:position:quiet"));
    }

    [Test]
    public void Registry_RejectsEmptyCallableNames()
    {
        TemplateRegistry registry = new TemplateRegistry();
        Func<List<TemplateValue>, TemplateValue> callable = delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.Null();
        };
        Func<TemplateBlockHelperContext, TemplateValue> blockCallable = delegate(TemplateBlockHelperContext context)
        {
            return TemplateValue.Null();
        };

        Assert.Throws<ArgumentException>(() => registry.RegisterFunction("", callable));
        Assert.Throws<ArgumentException>(() => registry.RegisterFilter("", callable));
        Assert.Throws<ArgumentException>(() => registry.RegisterTest("", callable));
        Assert.Throws<ArgumentException>(() => registry.RegisterHelper("", callable));
        Assert.Throws<ArgumentException>(() => registry.RegisterBlockHelper("", blockCallable));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options, TemplateRegistry registry)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, registry, new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> nested = new Dictionary<string, TemplateValue>();
        nested["rootName"] = TemplateValue.FromString("nested");
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["rootName"] = TemplateValue.FromString("root");
        root["nested"] = TemplateValue.FromMap(nested);
        return TemplateValue.FromMap(root);
    }
}
