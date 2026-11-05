using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class LiquidCompatibilityTests
{
    [Test]
    public void ConvertToNative_NormalizesLiquidTagsFiltersCommentsAndTemplateStatements()
    {
        string template = "{% assign title = name | append: mark %}{% elsif other %}Other{% for item in items limit:2 offset:1 %}{{ item | upcase }}{% endfor %}{% comment %}hidden{% endcomment %}{% render \"card\" with user %}";

        string converted = LiquidTemplateConverter.ConvertToNative(template);

        Assert.That(converted, Is.EqualTo("{% assign title = name | append(mark) %}{% elseif other %}Other{% for item in items limit 2 offset 1 %}{{ item | upcase }}{% endfor %}{% render \"card\" user as |card| %}"));
    }

    [Test]
    public void RenderToString_WithLiquidProfile_RendersObjectsFiltersControlFlowIterationAndForloop()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(LiquidProfile.CreateOptions());

        string template = "{% assign title = name | append: mark | upcase %}{% capture cap %}{{ title }}{% endcapture %}{{ cap }}|{% if false %}no{% elsif ok %}yes{% endif %}|{% for item in items limit:2 offset:1 %}{{ forloop.index }}:{{ item }};{% else %}empty{% endfor %}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("ADA!|yes|1:B;2:C;"));
    }

    [Test]
    public void RenderToString_WithMaximumLoopOptions_ReturnsEmptyOffsetRange()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(LiquidProfile.CreateOptions());

        string result = renderer.RenderToString("{% for item in items limit:2147483647 offset:2147483647 %}{{ item }}{% else %}empty{% endfor %}", CreateRoot());

        Assert.That(result, Is.EqualTo("empty"));
    }

    [Test]
    public void RenderToString_WithLiquidProfile_RendersRawCommentLiquidIncludeRenderAndWhitespaceTrim()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("card", "Card {{ name }}");
        registry.RegisterTemplate("snippet", "Snippet {{ name }}");
        TemplateRenderer renderer = TemplateRenderer.Create(LiquidProfile.CreateOptions(), registry);

        string template = "{% comment %}hidden{% endcomment %}{% raw %}{{ raw }}{% endraw %}|{% liquid\n assign word = \"Hi\"\n echo word\n%}|{% include \"snippet\" %}|{% render \"card\" with user %}|A {%- echo name -%} B";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("{{ raw }}|Hi|Snippet Ada|Card Bea|AAdaB"));
    }

    [TestCase("{% if x == empty %}empty{% else %}not{% endif %}", "empty")]
    [TestCase("{% if missing == empty %}empty{% else %}not{% endif %}", "not")]
    [TestCase("{% if spaces == blank %}blank{% else %}not{% endif %}", "blank")]
    [TestCase("{% if zero %}truthy{% else %}falsy{% endif %}", "truthy")]
    [TestCase("{{ missing | default: 'fallback' }}", "fallback")]
    public void RenderToString_LiquidTruthinessFixtures_MatchExpected(string template, string expected)
    {
        TemplateRenderer renderer = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);

        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("{{ 'aaba' | replace_first: 'a', 'X' }}", "Xaba")]
    [TestCase("{{ 'aaba' | remove_first: 'a' }}", "aba")]
    [TestCase("{{ 'a\\nb\\r\\nc' | strip_newlines }}", "abc")]
    [TestCase("{{ 2 | at_least: 5 | at_most: 4 }}", "4")]
    [TestCase("{{ items | concat: items | join: '-' }}", "a-b-c-a-b-c")]
    [TestCase("{{ nested | flatten | join: '-' }}", "a-b-c")]
    public void RenderToString_LiquidFilterFixtures_MatchExpected(string template, string expected)
    {
        TemplateRenderer renderer = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);

        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RenderToString_LiquidPartialFixtures_ApplyParametersAliasesIterationAndScope()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("card", "{{ item }}:{{ price }}:{{ forloop.index }}");
        registry.RegisterTemplate("setter", "{% assign leaked = 'yes' %}{{ visible }}");
        TemplateRenderer renderer = CreatePublicRenderer(registry, TemplateCompatibilityMode.Pragmatic);

        string template = "{% assign visible = 'outer' %}{% render 'card' for items as item, price: 9 %}|{% render 'card' with featured as item, price: 7 %}|{% include 'setter' %}:{{ leaked }}";
        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo("a:9:1b:9:2c:9:3|Widget:7:|outer:yes"));
    }

    [Test]
    public void RenderToString_LiquidIncludeAndRenderFixtures_UseSequentialAndIsolatedState()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("params", "{{ a }}-{{ b }}");
        registry.RegisterTemplate("product", "{{ product }}");
        registry.RegisterTemplate("counter", "{% increment x %}");
        TemplateRenderer renderer = CreatePublicRenderer(registry, TemplateCompatibilityMode.Pragmatic);
        string template = "{% assign target = 'product' %}{% include 'params' a: 1, b: a %}|{% render 'params' a: 1, b: a %}|{% include target with featured %}|{% increment x %}{% render 'counter' %}{% increment x %}";

        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo("1-1|1-|Widget|001"));
    }

    [Test]
    public void RenderToString_LiquidControlAndVariableFixtures_RespectCaseInterruptsAndLoopScope()
    {
        TemplateRenderer renderer = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);
        string template = "{% assign x = 'outer' %}{% for x in (1..3) %}{% if x == 2 %}{% continue %}{% endif %}{% assign saved = x %}{{ x }}{% if x == 3 %}{% break %}{% endif %}{% endfor %}{{ x }}:{{ saved }}|{% case kind %}{% when 'a' or 'b' %}match{% else %}miss{% endcase %}|{% if nil %}bad{% else %}nil{% endif %}";

        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo("13outer:3|match|nil"));
    }

    [Test]
    public void RenderToString_LiquidIterationFixtures_RenderCycleOffsetContinueParentloopAndTablerow()
    {
        TemplateRenderer renderer = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);
        string nested = "{% for i in (1..1) %}{% for j in (1..1) %}{{ forloop.parentloop.index }}.{{ forloop.index }}{% endfor %}{% endfor %}";
        Assert.That(renderer.RenderToString("{% for i in (1..2) %}{{ i }}{% endfor %}", CreateSpecificationRoot()), Is.EqualTo("12"));
        Assert.That(renderer.RenderToString(nested, CreateSpecificationRoot()), Is.EqualTo("1.1"));
        string template = "{% cycle 'a', 'b' %}{% cycle 'a', 'b' %}|{% for i in items limit:2 %}{{ i }}{% endfor %}{% for i in items offset:continue limit:1 %}{{ i }}{% endfor %}|{% for i in (1..1) %}{% for j in (1..1) %}{{ forloop.parentloop.index }}.{{ forloop.index }}{% endfor %}{% endfor %}|{% tablerow item in items cols:2 %}{{ tablerowloop.row }}.{{ tablerowloop.col }}={{ item }}{% endtablerow %}";

        string result = renderer.RenderToString(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo("ab|abc|1.1|<tr class=\"row1\">\n<td class=\"col1\">1.1=a</td><td class=\"col2\">1.2=b</td></tr>\n<tr class=\"row2\"><td class=\"col1\">2.1=c</td></tr>\n"));
    }

    [Test]
    public async Task RenderToStringAsync_LiquidIterationAndPartialFixtures_MatchBufferedRendering()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("item", "{{ item }}{{ forloop.index }}");
        registry.RegisterTemplate("params", "{{ a }}-{{ b }}");
        TemplateRenderer renderer = CreatePublicRenderer(registry, TemplateCompatibilityMode.Pragmatic);
        string template = "{% for item in items limit:2 %}{{ item }}{% endfor %}{% for item in items offset:continue limit:1 %}{{ item }}{% endfor %}|{% render 'item' for items %}|{% cycle 'x', 'y' %}{% cycle 'x', 'y' %}|{% include 'params' a: 1, b: a %}";

        string result = await renderer.RenderToStringAsync(template, CreateSpecificationRoot());

        Assert.That(result, Is.EqualTo("abc|a1b2c3|xy|1-1"));
    }

    [Test]
    public void Preparse_LiquidParserErrorFixtures_ReportDiagnosticsAndLaxRenderingRecovers()
    {
        TemplateRenderer lax = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);
        TemplateRenderer strict = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Strict);

        TemplateParseResult laxParse = lax.Preparse("before{% unknown value %}after");
        TemplateParseResult strictParse = strict.Preparse("before{% unknown value %}after");
        string recovered = lax.RenderToString("before{% unknown value %}after", CreateSpecificationRoot());

        Assert.That(laxParse.Diagnostics, Is.Not.Empty);
        Assert.That(strictParse.Diagnostics, Is.Not.Empty);
        Assert.That(recovered, Is.EqualTo("beforeafter"));
        Assert.Throws<InvalidOperationException>(() => strict.RenderToString("before{% unknown value %}after", CreateSpecificationRoot()));
    }

    [TestCase("before{{ value")]
    [TestCase("before{% comment %}hidden")]
    [TestCase("before{% raw %}hidden")]
    [TestCase("{% liquid\nunknown value\n%}")]
    [TestCase("{% assign x = x extra %}")]
    [TestCase("{% raw value %}{% endraw %}")]
    [TestCase("{{ value extra }}")]
    public void Preparse_LiquidMalformedFixtureSamples_ReportRecoverableErrors(string template)
    {
        TemplateRenderer lax = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);
        TemplateRenderer strict = CreatePublicRenderer(new TemplateRegistry(), TemplateCompatibilityMode.Strict);

        Assert.That(lax.Preparse(template).Diagnostics, Is.Not.Empty);
        Assert.That(strict.Preparse(template).Diagnostics, Is.Not.Empty);
        Assert.DoesNotThrow(() => lax.RenderToString(template, CreateSpecificationRoot()));
        Assert.Throws<InvalidOperationException>(() => strict.RenderToString(template, CreateSpecificationRoot()));
    }

    static TemplateRenderer CreatePublicRenderer(TemplateRegistry registry, TemplateCompatibilityMode mode)
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(profiles.CreateOptions("liquid", mode), registry, new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue CreateSpecificationRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromString("a"));
        items.Add(TemplateValue.FromString("b"));
        items.Add(TemplateValue.FromString("c"));
        root["items"] = TemplateValue.FromList(items);
        root["x"] = TemplateValue.FromString("");
        root["spaces"] = TemplateValue.FromString("   ");
        root["zero"] = TemplateValue.FromNumber(0m);
        root["featured"] = TemplateValue.FromString("Widget");
        root["kind"] = TemplateValue.FromString("b");
        List<TemplateValue> nested = new List<TemplateValue>();
        nested.Add(TemplateValue.FromString("a"));
        List<TemplateValue> nestedTail = new List<TemplateValue>();
        nestedTail.Add(TemplateValue.FromString("b"));
        nestedTail.Add(TemplateValue.FromString("c"));
        nested.Add(TemplateValue.FromList(nestedTail));
        root["nested"] = TemplateValue.FromList(nested);
        return TemplateValue.FromMap(root);
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString("Bea");

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        root["user"] = TemplateValue.FromMap(user);
        root["mark"] = TemplateValue.FromString("!");
        root["ok"] = TemplateValue.FromBoolean(true);
        root["other"] = TemplateValue.FromBoolean(false);
        root["items"] = TemplateValue.FromList(CreateItems());
        return TemplateValue.FromMap(root);
    }

    static List<TemplateValue> CreateItems()
    {
        List<TemplateValue> items = new List<TemplateValue>();
        items.Add(TemplateValue.FromString("A"));
        items.Add(TemplateValue.FromString("B"));
        items.Add(TemplateValue.FromString("C"));
        items.Add(TemplateValue.FromString("D"));
        return items;
    }
}
