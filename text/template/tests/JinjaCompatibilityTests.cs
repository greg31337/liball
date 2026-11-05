using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class JinjaCompatibilityTests
{
    [Test]
    public void ConvertToNative_NormalizesElifBlockSetAndAutoescapeTags()
    {
        string template = "A{% set title %}Hi{% endset %}{% if missing %}x{% elif name is defined %}y{% endif %}{% autoescape off %}{{ html }}{% endautoescape %}";

        string converted = JinjaTemplateConverter.ConvertToNative(template);

        Assert.That(converted, Is.EqualTo("A{% capture title %}Hi{% endcapture %}{% if missing %}x{% elseif name is defined %}y{% endif %}{% jinjaAutoescape false %}{{ html }}{% endjinjaAutoescape %}"));
    }

    [Test]
    public void RenderToString_WithJinjaProfile_RendersFiltersTestsSetFilterBlocksAndAutoescape()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(JinjaProfile.CreateOptions());

        string template = "{{ html }}|{% autoescape false %}{{ html }}{% endautoescape %}|{{ html }}|{% set title %}{{ name|upper }}{% endset %}{{ title }}|{% if missing %}no{% elif name is defined %}yes{% endif %}|{% filter upper %}hi{% endfilter %}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;b&gt;Ada&lt;/b&gt;|<b>Ada</b>|&lt;b&gt;Ada&lt;/b&gt;|ADA|yes|HI"));
    }

    [Test]
    public void RenderToString_WithJinjaProfile_ComposesIncludesImportsMacrosBlocksAndInheritance()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "L[{% block body %}Base{% endblock %}]");
        registry.RegisterTemplate("snippet", "S {{ name }}");
        registry.RegisterTemplate("macros", "{% macro greet person %}Hi {{ person.name }}{% endmacro %}");
        TemplateRenderer renderer = TemplateRenderer.Create(JinjaProfile.CreateOptions(), registry);

        string template = "{% extends \"layout\" %}{% block body %}{% include \"snippet\" %}|{% import \"macros\" %}{{ greet(user) }}{% endblock %}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("L[S Ada|Hi Bea]"));
        AssertDependency(renderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "layout");
        AssertDependency(renderer.GetLastDependencies()[1], TemplateDependencyKind.Template, "snippet");
        AssertDependency(renderer.GetLastDependencies()[2], TemplateDependencyKind.Template, "macros");
    }

    [Test]
    public void RenderToString_WithDjangoProfile_UsesHtmlAutoescape()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(DjangoProfile.CreateOptions());

        string result = renderer.RenderToString("{{ html }}", CreateRoot());

        Assert.That(result, Is.EqualTo("&lt;b&gt;Ada&lt;/b&gt;"));
    }

    static void AssertDependency(TemplateDependency dependency, TemplateDependencyKind kind, string name)
    {
        Assert.That(dependency.Kind, Is.EqualTo(kind));
        Assert.That(dependency.Name, Is.EqualTo(name));
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString("Bea");

        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        root["html"] = TemplateValue.FromString("<b>Ada</b>");
        root["user"] = TemplateValue.FromMap(user);
        return TemplateValue.FromMap(root);
    }
}
