using NUnit.Framework;
using System.Threading.Tasks;

namespace template.Tests;

[TestFixture]
public class PublicJinjaDjangoCompatibilityTests
{
    static object[] JinjaReferenceFixtures =
    {
        new object[]
        {
            "{{ missing|default('fallback') }}|{% if missing is undefined %}undefined{% endif %}|{{ 'a' ~ 'b' }}|{{ None is none }}",
            "fallback|undefined|ab|true"
        },
        new object[]
        {
            "{% set value = 'outer' %}{% for item in items %}{% set value = item %}{{ value }}{% endfor %}|{{ value }}",
            "onetwo|outer"
        },
        new object[]
        {
            "{% set reply|upper %}hello {{ name }}{% endset %}{{ reply }}",
            "HELLO ADA"
        },
        new object[]
        {
            "{% filter upper|escape %}<em>hello</em>{% endfilter %}|{% autoescape false %}{{ html }}{% endautoescape %}|{{ html|safe }}",
            "&lt;EM&gt;HELLO&lt;/EM&gt;|<b>Ada</b>|<b>Ada</b>"
        },
        new object[]
        {
            "A {{- 'B' -}} C",
            "ABC"
        }
    };

    static object[] DjangoReferenceFixtures =
    {
        new object[]
        {
            "{{ missing|default:'fallback' }}|{{ empty|default:'empty' }}|{{ html|safe }}",
            "fallback|empty|<b>Ada</b>"
        },
        new object[]
        {
            "{% with label=name empty_label=empty %}{{ label }}:{{ empty_label|default:'none' }}{% endwith %}|{{ label|default:'outside' }}",
            "Ada:none|outside"
        },
        new object[]
        {
            "{% for item in items %}{{ forloop.counter }}:{{ forloop.revcounter0 }}={{ item }};{% empty %}empty{% endfor %}",
            "1:1=one;2:0=two;"
        },
        new object[]
        {
            "A{% comment 'ignored' %}{{ html }}{% endcomment %}{% verbatim %}{{ literal }}{% endverbatim %}",
            "A{{ literal }}"
        },
        new object[]
        {
            "{% filter force_escape|lower %}<B>HELLO</B>{% endfilter %}|{% autoescape off %}{{ html }}{% endautoescape %}",
            "&lt;b&gt;hello&lt;/b&gt;|<b>Ada</b>"
        }
    };

    [TestCaseSource(nameof(JinjaReferenceFixtures))]
    public void Render_JinjaOfficialReferenceFixture_MatchesExpectedOutput(string templateText, string expected)
    {
        TemplateRenderer renderer = CreateRenderer("jinja", new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(DjangoReferenceFixtures))]
    public void Render_DjangoOfficialReferenceFixture_MatchesExpectedOutput(string templateText, string expected)
    {
        TemplateRenderer renderer = CreateRenderer("django", new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Render_JinjaCompositionFixture_SupportsOfficialMacroImportCallAndInheritanceSyntax()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "L[{% block body %}base{% endblock body %}]");
        registry.RegisterTemplate("forms", "{% macro input(name, type='text') %}<input name=\"{{ name }}\" type=\"{{ type }}\">{% endmacro %}");
        TemplateRenderer renderer = CreateRenderer("jinja", registry, TemplateCompatibilityMode.Pragmatic);
        string templateText = "{% extends 'layout' %}{% import 'forms' as forms %}{% macro surround(caller) %}<section>{{ caller() }}</section>{% endmacro %}{% block body %}{{ super() }}|{{ forms.input(name='user') }}|{% call surround() %}<b>body</b>{% endcall %}{% endblock body %}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("L[base|<input name=\"user\" type=\"text\">|<section><b>body</b></section>]") );
    }

    [Test]
    public async Task RenderAsync_JinjaInheritanceFixture_EvaluatesTopLevelImportsBeforeBlocks()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "{% block body %}base{% endblock %}");
        registry.RegisterTemplate("macros", "{% macro greet(name) %}Hello {{ name }}{% endmacro %}");
        TemplateRenderer renderer = CreateRenderer("jinja", registry, TemplateCompatibilityMode.Pragmatic);
        string templateText = "{% extends 'layout' %}{% import 'macros' as macros %}{% block body %}{{ macros.greet('Ada') }}{% endblock %}";

        string result = await renderer.RenderToStringAsync(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void Render_DjangoCompositionFixture_SupportsIncludeOnlyAndBlockSuper()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "L[{% block body %}base{% endblock body %}]");
        registry.RegisterTemplate("card", "{{ label }}:{{ name|default:'hidden' }}");
        TemplateRenderer renderer = CreateRenderer("django", registry, TemplateCompatibilityMode.Pragmatic);
        string templateText = "{% extends 'layout' %}{% block body %}{{ block.super }}|{% include 'card' with label='inside' only %}{% endblock body %}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("L[base|inside:hidden]"));
    }

    [Test]
    public void Render_JinjaUndefinedFixture_DistinguishesPragmaticAndStrictModes()
    {
        TemplateRenderer pragmatic = CreateRenderer("jinja", new TemplateRegistry(), TemplateCompatibilityMode.Pragmatic);
        TemplateRenderer strict = CreateRenderer("jinja", new TemplateRegistry(), TemplateCompatibilityMode.Strict);

        Assert.That(pragmatic.RenderToString("before{{ missing }}after", CreateRoot()), Is.EqualTo("beforeafter"));
        Assert.That(strict.RenderToString("{{ missing|default('fallback') }}", CreateRoot()), Is.EqualTo("fallback"));
        Assert.That(() => strict.RenderToString("{{ missing }}", CreateRoot()), Throws.InvalidOperationException);
    }

    static TemplateRenderer CreateRenderer(string profileName, TemplateRegistry registry, TemplateCompatibilityMode mode)
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateOptions options = profiles.CreateOptions(profileName, mode);
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.SetProfileRegistry(profiles);
        renderer.Configure(options, registry, new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue CreateRoot()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        Dictionary<string, object> root = new Dictionary<string, object>();
        root["name"] = "Ada";
        root["html"] = "<b>Ada</b>";
        root["empty"] = "";
        root["items"] = new List<string> { "one", "two" };
        return factory.Convert(root);
    }
}
