using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class RendererTests
{
    [Test]
    public void RenderToString_RendersTextOutputCommentsAndRawBlocks()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("Hello {{ user.name }}{# hidden #}!{% raw %}{{ user.name }}{% endraw %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada!{{ user.name }}"));
    }

    [Test]
    public void RenderToString_UsesConfiguredEscapingAndSafeStringBypass()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = true;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("{{ plain }} {{ safe }}", CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;b&gt;plain&lt;/b&gt; <b>safe</b>"));
    }

    [Test]
    public void RenderToString_RendersScalarOutputsAndMissingValues()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ number }}|{{ ok }}|{{ missing }}|{{ nullValue }}", CreateRoot());

        Assert.That(result, Is.EqualTo("7.5|true||"));
    }

    [Test]
    public void RenderToString_UsesConfiguredNonHtmlEscapingModes()
    {
        TemplateOptions jsonOptions = new TemplateOptions();
        jsonOptions.EscapingMode = TemplateEscapingMode.JsonString;
        jsonOptions.AutoEscape = true;
        TemplateRenderer jsonRenderer = TemplateRenderer.Create(jsonOptions);
        TemplateOptions csvOptions = new TemplateOptions();
        csvOptions.EscapingMode = TemplateEscapingMode.CsvField;
        csvOptions.AutoEscape = true;
        TemplateRenderer csvRenderer = TemplateRenderer.Create(csvOptions);

        Assert.That(jsonRenderer.RenderToString("{{ quote }}", CreateRoot()), Is.EqualTo("a\\\"b\\nc"));
        Assert.That(csvRenderer.RenderToString("{{ csv }}", CreateRoot()), Is.EqualTo("\"a,b\""));
    }

    [Test]
    public void RenderToString_UsesBuiltInStringFilters()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ user.name | upper }} {{ \"a,b\" | split(\",\") | join(\"|\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("ADA a|b"));
    }

    [Test]
    public void RenderToString_UsesBuiltInEncodingFiltersWithoutDoubleEscaping()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string template = "{{ plain | html_escape }} {{ plain | safe }} {{ quote | json_escape }} {{ \"a b&c=/\" | url_encode }} {{ csv | csv_escape }}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;b&gt;plain&lt;/b&gt; <b>plain</b> a\\\"b\\nc a%20b%26c%3D%2F \"a,b\""));
    }

    [Test]
    public void RenderToString_UsesBuiltInNumberAndDateTimeFilters()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ number | floor }} {{ number | times(2) }} {{ started | date(\"yyyy-MM-dd\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("7 15.0 2026-08-04"));
    }

    [Test]
    public void RenderToString_UsesBuiltInCollectionAndObjectFilters()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ [\"b\", \"a\", \"b\"] | uniq | sort | join(\",\") }} {{ {\"theme\": \"dark\"} | has_key(\"theme\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("a,b true"));
    }

    [Test]
    public void RenderToString_UsesBuiltInPredicateTests()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ number is number }} {{ safe is safe }} {{ 9 is divisible by 3 }} {{ number is not equal to 8 }}", CreateRoot());

        Assert.That(result, Is.EqualTo("true true true true"));
    }

    [Test]
    public void Render_RespectsRawOutputFlagOnOutputSyntax()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateExpressionSyntax expression = TemplateExpressionSyntax.Create(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Literal, TemplateLiteralSyntaxKind.String, "\"<b>raw</b>\"");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(0, 1), expression, true, false);
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);
        document.AddChild(TemplateSyntaxNode.FromOutput(output));
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderSyntaxToString(document, CreateRoot());

        Assert.That(result, Is.EqualTo("<b>raw</b>"));
    }

    [Test]
    public void Render_RespectsEscapeFalseAsRawOutput()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateExpressionSyntax expression = TemplateExpressionSyntax.Create(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Literal, TemplateLiteralSyntaxKind.String, "\"<b>raw</b>\"");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(0, 1), expression, false, false);
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);
        document.AddChild(TemplateSyntaxNode.FromOutput(output));
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderSyntaxToString(document, CreateRoot());

        Assert.That(result, Is.EqualTo("<b>raw</b>"));
    }

    [Test]
    public void Render_StatementArgumentsAreEvaluatedWithoutOutput()
    {
        TemplateParseResult parseResult = TemplateParser.ParseNative("{% return touch() %}Done");
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), inner);
        int calls = 0;
        context.Evaluator.RegisterFunction("touch", delegate(List<TemplateValue> arguments)
        {
            calls = calls + 1;
            return TemplateValue.Null();
        });
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(parseResult.Document, context);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(inner.ToString(), Is.EqualTo("Done"));
    }

    [Test]
    public void Render_IncludeStatement_RendersNamedTemplateWithInheritedContext()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("greeting", "Hello {{ user.name }}{{ marker }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set marker \"!\" %}{% include \"greeting\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada!"));
    }

    [Test]
    public void Render_IncludeStatement_CanUseExplicitCurrentModel()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("person", "{{ name }}:{{ $.site }}:{{ marker }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set marker \"!\" %}{% include \"person\" user %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada:Root:!"));
    }

    [Test]
    public void Render_RenderStatement_UsesIsolatedContext()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("card", "{{ user.name }}:{{ marker | default(\"none\") }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set marker \"hidden\" %}{% render \"card\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada:none"));
    }

    [Test]
    public void Render_RenderStatement_CanUseExplicitIsolatedModel()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("person", "{{ name }}:{{ user.name | default(\"none\") }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% render \"person\" user %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada:none"));
    }

    [Test]
    public void Render_IncludeStatement_CanLoadTemplatesFromFileLoader()
    {
        string root = Path.Combine(Path.GetTempPath(), "template-render-include-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "greeting.tpl"), "File {{ user.name }}");
        TemplateRegistry registry = new TemplateRegistry();
        registry.SetFileLoader(new FileTemplateLoader(root));
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% include \"greeting.tpl\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo("File Ada"));
        Directory.Delete(root, true);
    }

    [Test]
    public void Render_IncludeStatement_MissingTemplateCanBeIgnoredOrStrict()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateRenderer strictRenderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("{% include \"missing\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo(""));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{% render \"missing\" %}", CreateRoot()));
    }

    [Test]
    public void Render_PartialStatement_RendersRegisteredPartialWithInheritedContext()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("badge", "Partial {{ user.name }}{{ marker }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set marker \"!\" %}{% partial \"badge\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Partial Ada!"));
    }

    [Test]
    public void Render_PartialStatement_CanUseExplicitCurrentModel()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("person", "{{ name }}:{{ $.site }}:{{ marker }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set marker \"!\" %}{% partial \"person\" user %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada:Root:!"));
    }

    [Test]
    public void Render_PartialStatement_MissingPartialCanBeIgnoredOrStrict()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateRenderer strictRenderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("{% partial \"missing\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo(""));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{% partial \"missing\" %}", CreateRoot()));
    }

    [Test]
    public void Render_IncludeRenderAndPartialStatements_ResolveDynamicNames()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("partials/greeting", "Hello {{ user.name }}");
        registry.RegisterTemplate("partials/card", "{{ user.name }}:{{ marker | default(\"none\") }}");
        registry.RegisterPartial("partials/badge", "Badge {{ user.name }}{{ marker }}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string template = "{% set includeName \"partials/greeting\" %}{% set renderName \"partials/card\" %}{% set partialName \"partials/badge\" %}{% set marker \"!\" %}{% include includeName %}|{% render renderName %}|{% partial partialName %}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada|Ada:none|Badge Ada!"));
    }

    [Test]
    public void Render_IncludeRenderAndPartialStatements_RejectUnsafeDynamicNames()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("safe", "safe");
        registry.RegisterPartial("badge", "badge");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% set target \"../safe\" %}{% include target %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% set target \"partials//safe\" %}{% render target %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% set target \"partials/./badge\" %}{% partial target %}", CreateRoot()));
    }

    [Test]
    public void Render_IncludeRenderAndPartialStatements_RejectRecursiveCycles()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("include-loop", "A{% include \"include-loop\" %}");
        registry.RegisterTemplate("render-a", "A{% render \"render-b\" %}");
        registry.RegisterTemplate("render-b", "B{% render \"render-a\" %}");
        registry.RegisterPartial("partial-loop", "P{% partial \"partial-loop\" %}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% include \"include-loop\" %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% render \"render-a\" %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% partial \"partial-loop\" %}", CreateRoot()));
    }

    [Test]
    public void Render_IncludeRenderAndPartialStatements_EnforceMaximumIncludeDepth()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("leaf", "leaf");
        registry.RegisterTemplate("branch", "branch{% include \"leaf\" %}");
        registry.RegisterPartial("partial-leaf", "leaf");
        registry.RegisterPartial("partial-branch", "branch{% partial \"partial-leaf\" %}");
        TemplateOptions options = new TemplateOptions();
        options.MaximumIncludeDepth = 1;
        TemplateRenderer renderer = TemplateRenderer.Create(options, registry);

        Assert.That(renderer.RenderToString("{% include \"leaf\" %}", CreateRoot()), Is.EqualTo("leaf"));
        Assert.That(renderer.RenderToString("{% partial \"partial-leaf\" %}", CreateRoot()), Is.EqualTo("leaf"));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% include \"branch\" %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% partial \"partial-branch\" %}", CreateRoot()));
    }

    [Test]
    public void Render_IncludeRenderAndPartialStatements_RecordLoadedDependencies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("partials\\card", "{% include \"partials/leaf\" %}Card");
        registry.RegisterTemplate("partials/leaf", "Leaf");
        registry.RegisterPartial("partials\\badge", "Badge");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% set card \"partials/card\" %}{% include card %}{% render card %}{% partial \"partials/badge\" %}", CreateRoot());
        List<TemplateDependency> dependencies = renderer.GetLastDependencies();

        Assert.That(result, Is.EqualTo("LeafCardLeafCardBadge"));
        Assert.That(dependencies.Count, Is.EqualTo(3));
        AssertDependency(dependencies[0], TemplateDependencyKind.Template, "partials/card");
        AssertDependency(dependencies[1], TemplateDependencyKind.Template, "partials/leaf");
        AssertDependency(dependencies[2], TemplateDependencyKind.Partial, "partials/badge");
    }

    [Test]
    public void Render_ContextExposesDependenciesAndReturnsCopies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("child", "Child");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), writer, new TemplateOptions(), registry);
        TemplateParseResult parseResult = TemplateParser.ParseNative("{% include \"child\" %}");

        renderer.Render(parseResult.Document, context);
        List<TemplateDependency> dependencies = context.GetDependencies();
        dependencies[0].Name = "changed";

        Assert.That(writer.ToString(), Is.EqualTo("Child"));
        AssertDependency(context.GetDependencies()[0], TemplateDependencyKind.Template, "child");
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "child");
    }

    [Test]
    public void Render_UsesCompilationCacheForInlineAndLoadedTemplates()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("child", "Child");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string first = renderer.RenderToString("{% include \"child\" %}", CreateRoot());
        string second = renderer.RenderToString("{% include \"child\" %}", CreateRoot());

        Assert.That(first, Is.EqualTo("Child"));
        Assert.That(second, Is.EqualTo("Child"));
        Assert.That(renderer.CompilationCache.Count(), Is.EqualTo(2));
        Assert.That(renderer.CompilationCache.GetMissCount(), Is.EqualTo(2));
        Assert.That(renderer.CompilationCache.GetHitCount(), Is.EqualTo(2));
    }

    [Test]
    public void Render_CompilationCacheUsesLoadedTemplateVersionIdentity()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("child", "One");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string first = renderer.RenderToString("{% include \"child\" %}", CreateRoot());
        registry.RegisterTemplate("child", "Two");
        string second = renderer.RenderToString("{% include \"child\" %}", CreateRoot());

        Assert.That(first, Is.EqualTo("One"));
        Assert.That(second, Is.EqualTo("Two"));
        Assert.That(renderer.CompilationCache.GetMissCount(), Is.EqualTo(3));
        Assert.That(renderer.CompilationCache.GetHitCount(), Is.EqualTo(1));
    }

    [Test]
    public void Render_BlockStatement_RendersDefaultContentWithoutExtends()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("A{% block title %}Default {{ user.name }}{% endblock %}B", CreateRoot());

        Assert.That(result, Is.EqualTo("ADefault AdaB"));
    }

    [Test]
    public void Render_ExtendsStatement_RendersParentWithBlockOverrides()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "H{% block title %}Default{% endblock %}|{% block body %}Base{% endblock %}F");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("ignored{% extends \"layout\" %}{% block title %}Child {{ user.name }}{% endblock %}", CreateRoot());

        Assert.That(result, Is.EqualTo("HChild Ada|BaseF"));
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "layout");
    }

    [Test]
    public void Render_BlockOverride_SupportsSuper()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "P{% block body %}Base {{ user.name }}{% endblock %}Q");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% extends \"layout\" %}{% block body %}Before {{ super() }} After{% endblock %}", CreateRoot());

        Assert.That(result, Is.EqualTo("PBefore Base Ada AfterQ"));
    }

    [Test]
    public void Render_ExtendsStatement_ChainsBlockOverridesAcrossMultipleParents()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("base", "{% block body %}Base{% endblock %}");
        registry.RegisterTemplate("middle", "{% extends \"base\" %}{% block body %}Middle {{ super() }}{% endblock %}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% extends \"middle\" %}{% block body %}Child {{ super() }}{% endblock %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Child Middle Base"));
        List<TemplateDependency> dependencies = renderer.GetLastDependencies();
        AssertDependency(dependencies[0], TemplateDependencyKind.Template, "middle");
        AssertDependency(dependencies[1], TemplateDependencyKind.Template, "base");
    }

    [Test]
    public void RenderMustacheToString_RendersParentTemplateWithBlockOverrides()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "H {{$title}}Default{{/title}}|{{$body}}Base {{ user.name }}{{/body}} F");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderMustacheToString("ignored {{<layout}}{{$title}}Child {{ user.name }}{{/title}}{{/layout}}", CreateRoot());

        Assert.That(result, Is.EqualTo("ignored H Child Ada|Base Ada F"));
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "layout");
    }

    [Test]
    public void Render_MacroBlock_RegistersReusableTemplateFunction()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% macro greet person punctuation %}Hello {{ person.name }}{{ punctuation }}{% endmacro %}{{ greet(user, \"!\") }} {{ greet(user.child, \"?\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada! Hello Child?"));
    }

    [Test]
    public void Render_MacroBlock_CanBeCalledBeforeDefinition()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{{ badge(user.name) }}{% macro badge name %}[{{ name }}]{% endmacro %}", CreateRoot());

        Assert.That(result, Is.EqualTo("[Ada]"));
    }

    [Test]
    public void Render_MacroBlock_UsesLocalParameterScope()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% set name \"Outer\" %}{% macro wrap name %}{{ name }}{% set name \"Inner\" %}{% endmacro %}{{ wrap(\"Arg\") }}|{{ name }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Arg|Outer"));
    }

    [Test]
    public void Render_ImportStatement_RegistersMacrosFromNamedTemplate()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("macros", "ignored{% macro greet person %}Hi {{ person.name }}{% endmacro %}{% macro wrap text %}[{{ text }}]{% endmacro %}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% import \"macros\" %}{{ greet(user) }} {{ wrap(\"x\") }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hi Ada [x]"));
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "macros");
    }

    [Test]
    public void Render_ImportStatement_ImportedMacrosCanCallEachOther()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("macros", "{% macro wrap text %}[{{ text }}]{% endmacro %}{% macro label person %}{{ wrap(person.name) }}{% endmacro %}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% import \"macros\" %}{{ label(user.child) }}", CreateRoot());

        Assert.That(result, Is.EqualTo("[Child]"));
    }

    [Test]
    public void Render_ImportStatement_MissingTemplateCanBeIgnoredOrStrict()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateRenderer strictRenderer = TemplateRenderer.Create(options);

        Assert.That(renderer.RenderToString("{% import \"missing\" %}after", CreateRoot()), Is.EqualTo("after"));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{% import \"missing\" %}", CreateRoot()));
    }

    [Test]
    public void Render_FilterBlock_AppliesFilterToRenderedBody()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% filter upper %}Hello {{ user.name }}{% endfilter %}|{% filter replace(\"Ada\", \"Grace\") %}Hello Ada{% endfilter %}", CreateRoot());

        Assert.That(result, Is.EqualTo("HELLO ADA|Hello Grace"));
    }

    [Test]
    public void Render_CallBlock_PassesRenderedBodyToMacro()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% macro panel title body %}<h1>{{ title }}</h1>{{ body }}{% endmacro %}{% call panel(\"Title\") %}Hello {{ user.name }}{% endcall %}", CreateRoot());

        Assert.That(result, Is.EqualTo("<h1>Title</h1>Hello Ada"));
    }

    [Test]
    public void Render_CallBlock_CanInvokeRegisteredFunction()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterFunction("surround", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.FromString(arguments[0].StringValue + arguments[1].StringValue + arguments[0].StringValue);
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% call surround(\"*\") %}Ada{% endcall %}", CreateRoot());

        Assert.That(result, Is.EqualTo("*Ada*"));
    }

    [Test]
    public void Render_FilterAndCallBlocks_UnknownNamesCanBeIgnoredOrStrict()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateOptions options = new TemplateOptions();
        options.StrictUnknownFunctions = true;
        TemplateRenderer strictRenderer = TemplateRenderer.Create(options);

        Assert.That(renderer.RenderToString("{% filter missing %}Body{% endfilter %}|{% call missing %}Body{% endcall %}", CreateRoot()), Is.EqualTo("Body|"));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{% filter missing %}Body{% endfilter %}", CreateRoot()));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{% call missing %}Body{% endcall %}", CreateRoot()));
    }

    [Test]
    public void RenderToWriter_UsesTemplateWriterOutputLimitAfterEncoding()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = true;
        options.MaximumOutputLength = 4;
        TemplateRenderer renderer = TemplateRenderer.Create(options);
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToWriter("{{ plain }}!", CreateRoot(), inner));
        Assert.That(inner.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void Render_TrimMarkers_RemoveAdjacentWhitespace()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("A {{- user.name -}} B", CreateRoot());

        Assert.That(result, Is.EqualTo("AAdaB"));
    }

    [Test]
    public void Render_StandaloneCommentsAndStatements_RemoveTheirOwnLines()
    {
        TemplateParseResult parseResult = TemplateParser.ParseNative("before\n  {# hidden #}  \n  {% return touch() %}  \nafter");
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), inner);
        int calls = 0;
        context.Evaluator.RegisterFunction("touch", delegate(List<TemplateValue> arguments)
        {
            calls = calls + 1;
            return TemplateValue.Null();
        });
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(parseResult.Document, context);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(inner.ToString(), Is.EqualTo("before\nafter"));
    }

    [Test]
    public void Render_InlineCommentsDoNotTrimSurroundingText()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("a {# hidden #} b", CreateRoot());

        Assert.That(result, Is.EqualTo("a  b"));
    }

    [Test]
    public void Render_StandaloneRawBlockDelimiters_DoNotAddExtraBlankLines()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% raw %}\n{{ user.name }}\n{% endraw %}\nafter", CreateRoot());

        Assert.That(result, Is.EqualTo("{{ user.name }}\nafter"));
    }

    [Test]
    public void Render_TrimFinalNewline_RemovesOneTrailingLineBreak()
    {
        TemplateOptions options = new TemplateOptions();
        options.TrimFinalNewline = true;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("line\n", CreateRoot());
        string doubleNewline = renderer.RenderToString("line\n\n", CreateRoot());

        Assert.That(result, Is.EqualTo("line"));
        Assert.That(doubleNewline, Is.EqualTo("line\n"));
    }

    [Test]
    public void Render_AutoIndentMultilineValues_UsesCurrentLineIndentation()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoIndentMultilineValues = true;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("items:\n  {{ multiline }}", CreateRoot());

        Assert.That(result, Is.EqualTo("items:\n  a\n  b"));
    }

    [Test]
    public void Render_AutoIndentMultilineValues_DoesNotIndentAfterNonWhitespaceContent()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoIndentMultilineValues = true;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string result = renderer.RenderToString("value: {{ multiline }}", CreateRoot());

        Assert.That(result, Is.EqualTo("value: a\nb"));
    }

    [Test]
    public void Render_RightTrimMarker_RemovesCrLfAndIndentationAfterOutput()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("A {{ user.name -}}\r\n  B", CreateRoot());

        Assert.That(result, Is.EqualTo("A AdaB"));
    }

    [Test]
    public void Render_IfBlock_RendersThenBranchWhenConditionIsTruthy()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% if ok %}yes{% else %}no{% endif %}", CreateConditionRoot(true, false, 0m));

        Assert.That(result, Is.EqualTo("yes"));
    }

    [Test]
    public void Render_IfBlock_RendersElseBranchWhenConditionIsFalsy()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% if ok %}yes{% else %}no{% endif %}", CreateConditionRoot(false, false, 0m));

        Assert.That(result, Is.EqualTo("no"));
    }

    [Test]
    public void Render_IfBlock_EvaluatesElseIfBranchesInOrder()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% if ok %}A{% elseif maybe %}B{% elseif other %}C{% else %}D{% endif %}", CreateConditionRoot(false, true, 0m));

        Assert.That(result, Is.EqualTo("B"));
    }

    [Test]
    public void Render_IfBlock_EvaluatesOnlySelectedBranchContent()
    {
        TemplateParseResult parseResult = TemplateParser.ParseNative("{% if ok %}{{ touch() }}{% else %}{{ touch() }}{% endif %}");
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateConditionRoot(true, false, 0m), inner);
        int calls = 0;
        context.Evaluator.RegisterFunction("touch", delegate(List<TemplateValue> arguments)
        {
            calls = calls + 1;
            return TemplateValue.FromString("hit");
        });
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(parseResult.Document, context);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(inner.ToString(), Is.EqualTo("hit"));
    }

    [Test]
    public void Render_UnlessBlock_InvertsStartConditionAndSupportsElse()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string falsyResult = renderer.RenderToString("{% unless ok %}then{% else %}else{% endunless %}", CreateConditionRoot(false, false, 0m));
        string truthyResult = renderer.RenderToString("{% unless ok %}then{% else %}else{% endunless %}", CreateConditionRoot(true, false, 0m));

        Assert.That(falsyResult, Is.EqualTo("then"));
        Assert.That(truthyResult, Is.EqualTo("else"));
    }

    [Test]
    public void Render_IfBlock_UsesConfiguredTruthinessMode()
    {
        TemplateOptions liquidOptions = new TemplateOptions();
        liquidOptions.TruthinessMode = TemplateTruthinessMode.Liquid;
        TemplateRenderer nativeRenderer = new TemplateRenderer();
        TemplateRenderer liquidRenderer = TemplateRenderer.Create(liquidOptions);

        string nativeResult = nativeRenderer.RenderToString("{% if number %}then{% else %}else{% endif %}", CreateConditionRoot(false, false, 0m));
        string liquidResult = liquidRenderer.RenderToString("{% if number %}then{% else %}else{% endif %}", CreateConditionRoot(false, false, 0m));

        Assert.That(nativeResult, Is.EqualTo("else"));
        Assert.That(liquidResult, Is.EqualTo("then"));
    }

    [Test]
    public void Render_CaseBlock_RendersFirstMatchingWhenBranch()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% case kind %}{% when \"admin\" %}A{% when \"user\" %}U{% else %}G{% endcase %}", CreateCaseRoot("user", 0m));

        Assert.That(result, Is.EqualTo("U"));
    }

    [Test]
    public void Render_CaseBlock_RendersElseBranchWhenNoWhenMatches()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% case kind %}{% when \"admin\" %}A{% when \"user\" %}U{% else %}G{% endcase %}", CreateCaseRoot("guest", 0m));

        Assert.That(result, Is.EqualTo("G"));
    }

    [Test]
    public void Render_CaseBlock_MatchesListWhenValues()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% case kind %}{% when [\"admin\", \"owner\"] %}staff{% else %}guest{% endcase %}", CreateCaseRoot("owner", 0m));

        Assert.That(result, Is.EqualTo("staff"));
    }

    [Test]
    public void Render_CaseBlock_UsesTemplateValueEquality()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% case number %}{% when \"7.5\" %}string{% when 7.5 %}number{% else %}none{% endcase %}", CreateCaseRoot("x", 7.5m));

        Assert.That(result, Is.EqualTo("string"));
    }

    [Test]
    public void Render_CaseBlock_EvaluatesCaseOnceAndOnlySelectedBranchContent()
    {
        TemplateParseResult parseResult = TemplateParser.ParseNative("{% case choose() %}{% when \"a\" %}{{ touch(\"a\") }}{% when \"b\" %}{{ touch(\"b\") }}{% else %}{{ touch(\"else\") }}{% endcase %}");
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateCaseRoot("x", 0m), inner);
        int choices = 0;
        int touches = 0;
        context.Evaluator.RegisterFunction("choose", delegate(List<TemplateValue> arguments)
        {
            choices = choices + 1;
            return TemplateValue.FromString("b");
        });
        context.Evaluator.RegisterFunction("touch", delegate(List<TemplateValue> arguments)
        {
            touches = touches + 1;
            return TemplateValue.FromString(arguments[0].StringValue);
        });
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(parseResult.Document, context);

        Assert.That(choices, Is.EqualTo(1));
        Assert.That(touches, Is.EqualTo(1));
        Assert.That(inner.ToString(), Is.EqualTo("b"));
    }

    [Test]
    public void Render_ForBlock_IteratesTemplateValueListsAndRestoresOuterLocal()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in items %}{{ item }} {% endfor %}|{{ item }}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada Grace |root"));
    }

    [Test]
    public void Render_ForBlock_IteratesRangeLiterals()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for n in 1..3 %}{{ n }}{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("123"));
    }

    [Test]
    public void Render_ForBlock_IteratesMapsAsKeyValueEntries()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for entry in scores %}{{ entry.key }}={{ entry.value }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("ada=10;grace=20;"));
    }

    [Test]
    public void Render_ForBlock_BindsDestructuredKeyAndValueVariables()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for name, score in scores %}{{ name }}:{{ score }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("ada:10;grace:20;"));
    }

    [Test]
    public void Render_ForBlock_IteratesStrings()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for ch in letters %}{{ ch }}-{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("a-b-"));
    }

    [Test]
    public void Render_ForBlock_IteratesObjectArraysAndEnumerables()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string arrayResult = renderer.RenderToString("{% for item in arrayObject %}{{ item }}{% endfor %}", CreateLoopRoot());
        string enumerableResult = renderer.RenderToString("{% for item in queueObject %}{{ item }}{% endfor %}", CreateLoopRoot());

        Assert.That(arrayResult, Is.EqualTo("xy"));
        Assert.That(enumerableResult, Is.EqualTo("uv"));
    }

    [Test]
    public void Render_ForBlock_ProvidesLoopPositionMetadata()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in items %}{{ loop.index0 }}:{{ loop.index }}:{{ loop.first }}:{{ loop.last }}:{{ loop.length }}:{{ loop.reverseIndex }}:{{ loop.reverseIndex0 }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("0:1:true:false:2:2:1;1:2:false:true:2:1:0;"));
    }

    [Test]
    public void Render_ForBlock_ProvidesLoopKeyAndValueMetadata()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string listResult = renderer.RenderToString("{% for item in items %}{{ loop.key }}={{ loop.value }};{% endfor %}", CreateLoopRoot());
        string mapResult = renderer.RenderToString("{% for entry in scores %}{{ loop.key }}={{ loop.value }};{% endfor %}", CreateLoopRoot());

        Assert.That(listResult, Is.EqualTo("0=Ada;1=Grace;"));
        Assert.That(mapResult, Is.EqualTo("ada=10;grace=20;"));
    }

    [Test]
    public void Render_ForBlock_ProvidesNestedLoopDepthAndParentLoop()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for group in groups %}{% for item in group.items %}{{ loop.depth }}:{{ loop.parent.index }}.{{ loop.index }}={{ item }};{% endfor %}{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("2:1.1=a;2:1.2=b;2:2.1=c;"));
    }

    [Test]
    public void Render_ForBlock_AppliesOffsetLimitAndReversedOptions()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems offset 1 limit 2 reversed %}{{ item }}:{{ loop.index }}/{{ loop.length }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Linus:1/2;Grace:2/2;"));
    }

    [Test]
    public void Render_ForBlock_EvaluatesLoopOptionExpressions()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems offset start limit count %}{{ item }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Grace;Linus;"));
    }

    [Test]
    public void Render_ForBlock_RendersEmptyBranchWhenNoItemsAreSelected()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string emptyResult = renderer.RenderToString("{% for item in emptyItems %}item{% empty %}empty{% endfor %}", CreateLoopRoot());
        string elseResult = renderer.RenderToString("{% for item in moreItems offset 10 %}item{% else %}else{% endfor %}", CreateLoopRoot());

        Assert.That(emptyResult, Is.EqualTo("empty"));
        Assert.That(elseResult, Is.EqualTo("else"));
    }

    [Test]
    public void Render_ForBlock_BreakStopsCurrentLoop()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems %}{{ item }}{% if item == \"Grace\" %}{% break %}{% endif %},{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada,Grace"));
    }

    [Test]
    public void Render_ForBlock_ContinueSkipsRemainingIterationContent()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems %}{% if item == \"Grace\" %}{% continue %}{% endif %}{{ item }};{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada;Linus;"));
    }

    [Test]
    public void Render_ForBlock_NestedBreakOnlyStopsInnerLoop()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for group in groups %}[{% for item in group.items %}{{ item }}{% break %}{% endfor %}]{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("[a][c]"));
    }

    [Test]
    public void Render_BreakAndContinueStatementsDoNotEvaluateArguments()
    {
        TemplateParseResult parseResult = TemplateParser.ParseNative("{% for item in moreItems %}{% break touch() %}{% endfor %}");
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateLoopRoot(), inner);
        int calls = 0;
        context.Evaluator.RegisterFunction("touch", delegate(List<TemplateValue> arguments)
        {
            calls = calls + 1;
            return TemplateValue.Null();
        });
        TemplateRenderer renderer = new TemplateRenderer();

        renderer.Render(parseResult.Document, context);

        Assert.That(calls, Is.EqualTo(0));
        Assert.That(inner.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void Render_WithBlock_ChangesCurrentContextAndRestoresAfterExit()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% with user %}{{ name }}:{{ this.name }}:{{ ../site }}{% endwith %}|{{ user.name }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Ada:Ada:Root|Ada"));
    }

    [Test]
    public void Render_WithBlock_NestedParentAndRootContextAccess()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% with user %}{% with child %}{{ name }}:{{ ../name }}:{{ $.site }}{% endwith %}{% endwith %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Child:Ada:Root"));
    }

    [Test]
    public void Render_WithBlock_PropagatesLoopControlToNearestForBlock()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems %}{% with item %}{% if this == \"Grace\" %}{% continue %}{% endif %}{{ this }};{% endwith %}{% endfor %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada;Linus;"));
    }

    [Test]
    public void Render_SetStatement_BindsLocalInCurrentScope()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% set greeting \"Hello\" %}{{ greeting }} {{ user.name }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void Render_AssignStatement_SupportsEqualsCompatibilitySyntax()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% assign total = 1 + 2 %}{{ total }}", CreateRoot());

        Assert.That(result, Is.EqualTo("3"));
    }

    [Test]
    public void Render_SetStatement_UsesCurrentFrameScope()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% set name \"Root\" %}{% with user %}{% set name \"Inner\" %}{{ name }}{% endwith %}|{{ name }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Inner|Root"));
    }

    [Test]
    public void Render_SetStatement_LoopAssignmentsAreScopedPerIteration()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems limit 2 %}{% set seen item %}{{ seen }};{% endfor %}{{ seen }}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada;Grace;"));
    }

    [Test]
    public void Render_CaptureBlock_StoresRenderedContentInLocal()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% capture intro %}Hello {{ user.name }}{% endcapture %}{{ intro }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void Render_CaptureBlock_DoesNotWriteCapturedContentDirectly()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("A{% capture middle %}B{% endcapture %}C{{ middle }}", CreateRoot());

        Assert.That(result, Is.EqualTo("ACB"));
    }

    [Test]
    public void Render_CaptureBlock_UsesCurrentFrameScope()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% capture name %}Root{% endcapture %}{% with user %}{% capture name %}Inner {{ this.name }}{% endcapture %}{{ name }}{% endwith %}|{{ name }}", CreateRoot());

        Assert.That(result, Is.EqualTo("Inner Ada|Root"));
    }

    [Test]
    public void Render_CaptureBlock_LoopCapturesAreScopedPerIteration()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% for item in moreItems limit 2 %}{% capture seen %}{{ item }}{% endcapture %}{{ seen }};{% endfor %}{{ seen }}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("Ada;Grace;"));
    }

    [Test]
    public void Render_IncrementStatement_OutputsCurrentValueThenIncrements()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% increment count %},{% increment count %},{% increment count %}", CreateRoot());

        Assert.That(result, Is.EqualTo("0,1,2"));
    }

    [Test]
    public void Render_DecrementStatement_DecrementsThenOutputsCurrentValue()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% decrement count %},{% decrement count %},{% decrement count %}", CreateRoot());

        Assert.That(result, Is.EqualTo("-1,-2,-3"));
    }

    [Test]
    public void Render_IncrementAndDecrementStatements_ShareNamedCounters()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% increment count %},{% decrement count %},{% increment count %}", CreateRoot());

        Assert.That(result, Is.EqualTo("0,0,0"));
    }

    [Test]
    public void Render_CounterStatements_KeepSeparateNamesAndPersistAcrossLoopScopes()
    {
        TemplateRenderer renderer = new TemplateRenderer();

        string result = renderer.RenderToString("{% increment a %}:{% increment b %}|{% for item in items %}{% increment a %}{% endfor %}|{% increment a %}:{% increment b %}", CreateLoopRoot());

        Assert.That(result, Is.EqualTo("0:0|12|3:1"));
    }

    [Test]
    public void ConstructorsAndRenderMethods_RejectNullRequiredValues()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(TemplateSource.FromString(""));
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = TemplateRenderContext.Create(CreateRoot(), inner);
        TemplateSyntaxNode node = TemplateSyntaxNode.FromText(new TemplateTextSyntax(TemplateSource.FromString("x").CreateSpan(0, 1)));

        Assert.Throws<ArgumentNullException>(() => TemplateRenderer.Create((TemplateOptions)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(null!, inner));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(CreateRoot(), null!));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(null!, inner, new TemplateOptions()));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(CreateRoot(), null!, new TemplateOptions()));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderContext.Create(CreateRoot(), inner, null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderToString((string)null!, CreateRoot()));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderToString("", null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderSyntaxToString((TemplateDocumentSyntax)null!, CreateRoot()));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderSyntaxToString(document, null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderToWriter((string)null!, CreateRoot(), inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderToWriter("", null!, inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderToWriter("", CreateRoot(), null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderDocumentToWriter((TemplateDocumentSyntax)null!, CreateRoot(), inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderDocumentToWriter(document, null!, inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderDocumentToWriter(document, CreateRoot(), null!));
        Assert.Throws<ArgumentNullException>(() => renderer.Render(null!, context));
        Assert.Throws<ArgumentNullException>(() => renderer.Render(document, null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderNode(null!, context));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderNode(node, null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderMustacheToString(null!, CreateRoot()));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderMustacheToString("", null!));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderMustacheToWriter(null!, CreateRoot(), inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderMustacheToWriter("", null!, inner));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderMustacheToWriter("", CreateRoot(), null!));
    }

    static void AssertDependency(TemplateDependency dependency, TemplateDependencyKind kind, string name)
    {
        Assert.That(dependency.Kind, Is.EqualTo(kind));
        Assert.That(dependency.Name, Is.EqualTo(name));
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        Dictionary<string, TemplateValue> child = new Dictionary<string, TemplateValue>();
        child["name"] = TemplateValue.FromString("Child");
        user["name"] = TemplateValue.FromString("Ada");
        user["child"] = TemplateValue.FromMap(child);
        root["user"] = TemplateValue.FromMap(user);
        root["site"] = TemplateValue.FromString("Root");
        root["plain"] = TemplateValue.FromString("<b>plain</b>");
        root["safe"] = TemplateValue.FromSafeString("<b>safe</b>");
        root["multiline"] = TemplateValue.FromString("a\nb");
        root["number"] = TemplateValue.FromNumber(7.5m);
        root["ok"] = TemplateValue.FromBoolean(true);
        root["nullValue"] = TemplateValue.Null();
        root["quote"] = TemplateValue.FromString("a\"b\nc");
        root["csv"] = TemplateValue.FromString("a,b");
        root["started"] = TemplateValue.FromObject(new DateTime(2026, 8, 4, 9, 15, 30));
        return TemplateValue.FromMap(root);
    }

    static TemplateValue CreateConditionRoot(bool ok, bool maybe, decimal number)
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["ok"] = TemplateValue.FromBoolean(ok);
        root["maybe"] = TemplateValue.FromBoolean(maybe);
        root["other"] = TemplateValue.FromBoolean(true);
        root["number"] = TemplateValue.FromNumber(number);
        return TemplateValue.FromMap(root);
    }

    static TemplateValue CreateLoopRoot()
    {
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromString("Ada"));
        items.Add(TemplateValue.FromString("Grace"));

        List<TemplateValue> moreItems = new List<TemplateValue>();
        moreItems.Add(TemplateValue.FromString("Ada"));
        moreItems.Add(TemplateValue.FromString("Grace"));
        moreItems.Add(TemplateValue.FromString("Linus"));

        List<TemplateValue> emptyItems = new List<TemplateValue>();

        Dictionary<string, TemplateValue> scores = new Dictionary<string, TemplateValue>();
        scores["ada"] = TemplateValue.FromNumber(10m);
        scores["grace"] = TemplateValue.FromNumber(20m);

        Queue<string> queue = new Queue<string>();
        queue.Enqueue("u");
        queue.Enqueue("v");

        List<TemplateValue> firstGroupItems = new List<TemplateValue>();
        firstGroupItems.Add(TemplateValue.FromString("a"));
        firstGroupItems.Add(TemplateValue.FromString("b"));
        Dictionary<string, TemplateValue> firstGroup = new Dictionary<string, TemplateValue>();
        firstGroup["items"] = TemplateValue.FromList(firstGroupItems);

        List<TemplateValue> secondGroupItems = new List<TemplateValue>();
        secondGroupItems.Add(TemplateValue.FromString("c"));
        Dictionary<string, TemplateValue> secondGroup = new Dictionary<string, TemplateValue>();
        secondGroup["items"] = TemplateValue.FromList(secondGroupItems);

        List<TemplateValue> groups = new List<TemplateValue>();
        groups.Add(TemplateValue.FromMap(firstGroup));
        groups.Add(TemplateValue.FromMap(secondGroup));

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["item"] = TemplateValue.FromString("root");
        root["items"] = TemplateValue.FromList(items);
        root["moreItems"] = TemplateValue.FromList(moreItems);
        root["emptyItems"] = TemplateValue.FromList(emptyItems);
        root["scores"] = TemplateValue.FromMap(scores);
        root["letters"] = TemplateValue.FromString("ab");
        root["arrayObject"] = TemplateValue.FromObject(new string[] { "x", "y" });
        root["queueObject"] = TemplateValue.FromObject(queue);
        root["groups"] = TemplateValue.FromList(groups);
        root["start"] = TemplateValue.FromNumber(1m);
        root["count"] = TemplateValue.FromNumber(2m);
        return TemplateValue.FromMap(root);
    }

    static TemplateValue CreateCaseRoot(string kind, decimal number)
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["kind"] = TemplateValue.FromString(kind);
        root["number"] = TemplateValue.FromNumber(number);
        return TemplateValue.FromMap(root);
    }
}
