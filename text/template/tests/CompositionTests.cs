using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class CompositionTests
{
    [Test]
    public void Render_ComposesIncludesPartialsInheritanceMacrosAndImports()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "Layout[{% block body %}Base{% endblock %}]");
        registry.RegisterTemplate("macros", "{% macro panel tag body %}<{{ tag }}>{{ body }}</{{ tag }}>{% endmacro %}");
        registry.RegisterTemplate("snippet", "Snippet {{ user.name }}");
        registry.RegisterPartial("row", "Partial {{ item }};");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string template = "{% extends \"layout\" %}{% block body %}{% import \"macros\" %}{% set item \"Ada\" %}{% partial \"row\" %}{% call panel(\"section\") %}{% include \"snippet\" %}{% endcall %}{% endblock %}";
        string result = renderer.RenderToString(template, CreateRoot());

        Assert.That(result, Is.EqualTo("Layout[Partial Ada;<section>Snippet Ada</section>]"));
        List<TemplateDependency> dependencies = renderer.GetLastDependencies();
        AssertDependency(dependencies[0], TemplateDependencyKind.Template, "layout");
        AssertDependency(dependencies[1], TemplateDependencyKind.Template, "macros");
        AssertDependency(dependencies[2], TemplateDependencyKind.Partial, "row");
        AssertDependency(dependencies[3], TemplateDependencyKind.Template, "snippet");
    }

    [Test]
    public void Render_ComposedTemplatesStillRejectRecursiveIncludes()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "Layout[{% block body %}Base{% endblock %}]");
        registry.RegisterTemplate("loop", "Loop{% include \"loop\" %}");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string template = "{% extends \"layout\" %}{% block body %}{% include \"loop\" %}{% endblock %}";

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString(template, CreateRoot()));
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
        user["name"] = TemplateValue.FromString("Ada");
        root["user"] = TemplateValue.FromMap(user);
        return TemplateValue.FromMap(root);
    }
}
