using System;
using System.Collections;
using System.Collections.Generic;

namespace template.Tests;

[TestFixture]
public class PublicCompositionIntegrationTests
{
    [Test]
    public void CompositionStatements_BindParametersAndContextAliasesWithCorrectIsolation()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("include-card", "I[{{ person.name }}:{{ heading }}:{{ caller }}:{{ $.site }}]");
        registry.RegisterTemplate("render-card", "R[{{ person.name }}:{{ heading }}:{{ caller | default(\"none\") }}:{{ $.name }}]");
        registry.RegisterPartial("partial-card", "P[{{ person.name }}:{{ heading }}:{{ caller }}:{{ $.site }}]");
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);
        string templateText = "{% set caller \"outer\" %}{% set label \"label\" %}{% include \"include-card\" user heading=label as |person| %}|{% partial \"partial-card\" user heading=\"partial\" as |person| %}|{% render \"render-card\" user heading=\"render\" as |person| %}|{{ person | default(\"none\") }}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("I[Ada:label:outer:Root]|P[Ada:partial:outer:Root]|R[Ada:render:none:Ada]|none"));
    }

    [Test]
    public void DynamicInheritance_PropagatesParametersAliasesAndSuperAcrossAChain()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("base", "B({{ theme }}:{% block body %}base{% endblock %})");
        registry.RegisterTemplate("middle", "{% extends \"base\" theme=theme as |middlePage| %}{% block body %}middle[{{ super() }}]{% endblock %}");
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);
        string templateText = "{% extends layoutName theme=\"dark\" as |page| %}{% block body %}child[{{ page.user.name }}:{{ middlePage.site }}:{{ theme }}:{{ super() }}]{% endblock %}";

        string result = renderer.RenderToString(templateText, CreateRoot());
        List<TemplateDependency> dependencies = renderer.GetLastDependencies();

        Assert.That(result, Is.EqualTo("B(dark:child[Ada:Root:dark:middle[base]])"));
        AssertDependency(dependencies[0], TemplateDependencyKind.Template, "middle");
        AssertDependency(dependencies[1], TemplateDependencyKind.Template, "base");
    }

    [Test]
    public void IncludedTemplateBlocks_DoNotReuseCallerInheritanceOverrides()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "L[{% block body %}layout{% endblock %}]");
        registry.RegisterTemplate("component", "C[{% block body %}component{% endblock %}]");
        TemplateRenderer renderer = CreateRenderer(new TemplateOptions(), registry);
        string templateText = "{% extends \"layout\" %}{% block body %}{% include \"component\" %}{% endblock %}";

        string result = renderer.RenderToString(templateText, CreateRoot());

        Assert.That(result, Is.EqualTo("L[C[component]]"));
    }

    [Test]
    public void HandlebarsPartialBlocks_NestWithoutLeakingAndUseMissingFallbackBodies()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterPartial("outer", "O[{{label}}:{{name}}:{{> @partial-block}}]");
        registry.RegisterPartial("inner", "I[{{label}}:{{name}}:{{> @partial-block}}]");
        TemplateOptions options = new TemplateProfileCatalog().CreateHandlebarsOptions();
        TemplateRenderer renderer = CreateRenderer(options, registry);
        string templateText = "{{#> outer user label=\"outer\"}}A{{#> inner user label=\"inner\"}}B {{name}}{{/inner}}C{{/outer}}|{{#> missing}}F {{name}}{{/missing}}|{{> @partial-block}}";

        string result = renderer.RenderToString(templateText, CreateRoot());
        List<TemplateDependency> dependencies = renderer.GetLastDependencies();

        Assert.That(result, Is.EqualTo("O[outer:Ada:AI[inner:Ada:B Ada]C]|F Ada|"));
        AssertDependency(dependencies[0], TemplateDependencyKind.Partial, "outer");
        AssertDependency(dependencies[1], TemplateDependencyKind.Partial, "inner");
    }

    [Test]
    public void MustacheDynamicParentAndDynamicInheritanceCycles_HandleSuccessAndFailure()
    {
        TemplateRegistry mustacheRegistry = new TemplateRegistry();
        mustacheRegistry.RegisterTemplate("mustache-layout", "H{{$body}}Base{{/body}}F");
        TemplateOptions mustacheOptions = new TemplateProfileCatalog().CreateMustacheOptions();
        TemplateRenderer mustacheRenderer = CreateRenderer(mustacheOptions, mustacheRegistry);

        string result = mustacheRenderer.RenderToString("{{<*parentName}}{{$body}}Child {{name}}{{/body}}{{/parentName}}", CreateRoot());

        Assert.That(result, Is.EqualTo("HChild AdaF"));
        AssertDependency(mustacheRenderer.GetLastDependencies()[0], TemplateDependencyKind.Template, "mustache-layout");

        TemplateRegistry cyclicRegistry = new TemplateRegistry();
        cyclicRegistry.RegisterTemplate("cycle-a", "{% extends secondLayout %}");
        cyclicRegistry.RegisterTemplate("cycle-b", "{% extends firstLayout %}");
        TemplateRenderer cyclicRenderer = CreateRenderer(new TemplateOptions(), cyclicRegistry);
        Assert.Throws<InvalidOperationException>(() => cyclicRenderer.RenderToString("{% extends firstLayout %}", CreateRoot()));
    }

    static TemplateRenderer CreateRenderer(TemplateOptions options, TemplateRegistry registry)
    {
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, registry, new TemplateMemberResolver());
        return renderer;
    }

    static TemplateValue CreateRoot()
    {
        Hashtable user = new Hashtable();
        user["name"] = "Ada";
        Hashtable root = new Hashtable();
        root["name"] = "Ada";
        root["site"] = "Root";
        root["user"] = user;
        root["layoutName"] = "middle";
        root["parentName"] = "mustache-layout";
        root["firstLayout"] = "cycle-a";
        root["secondLayout"] = "cycle-b";
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.Convert(root);
    }

    static void AssertDependency(TemplateDependency dependency, TemplateDependencyKind kind, string name)
    {
        Assert.That(dependency.Kind, Is.EqualTo(kind));
        Assert.That(dependency.Name, Is.EqualTo(name));
    }
}
