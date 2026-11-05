using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class InterfaceTests
{
    [Test]
    public void TemplateRegistry_CustomLoadersFormAnOrderedFallbackChain()
    {
        TestTemplateLoader first = new TestTemplateLoader();
        TestTemplateLoader second = new TestTemplateLoader();
        second.SetTemplate("card", "Custom {{ name }}");
        TemplateRegistry registry = new TemplateRegistry();
        registry.AddTemplateLoader(first);
        registry.AddTemplateLoader(second);
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string result = renderer.RenderToString("{% include \"card\" %}", CreateRoot());

        Assert.That(result, Is.EqualTo("Custom Ada"));
        Assert.That(first.LoadCount, Is.EqualTo(1));
        Assert.That(second.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void TemplateRegistry_RegisteredTemplatesTakePriorityOverCustomLoaders()
    {
        TestTemplateLoader loader = new TestTemplateLoader();
        loader.SetTemplate("card", "custom");
        TemplateRegistry registry = new TemplateRegistry();
        registry.AddTemplateLoader(loader);
        registry.RegisterTemplate("card", "registered");

        TemplateSource source = registry.LoadTemplate("card")!;

        Assert.That(source.Text, Is.EqualTo("registered"));
        Assert.That(loader.LoadCount, Is.EqualTo(0));
    }

    [Test]
    public void TemplateRegistry_LoadTemplateAsync_UsesLoaderAsyncMethods()
    {
        TestTemplateLoader first = new TestTemplateLoader();
        TestTemplateLoader second = new TestTemplateLoader();
        second.SetTemplate("card", "async");
        TemplateRegistry registry = new TemplateRegistry();
        registry.AddTemplateLoader(first);
        registry.AddTemplateLoader(second);

        TemplateSource source = registry.LoadTemplateAsync("card").GetAwaiter().GetResult()!;

        Assert.That(source.Text, Is.EqualTo("async"));
        Assert.That(first.LoadAsyncCount, Is.EqualTo(1));
        Assert.That(second.LoadAsyncCount, Is.EqualTo(1));
        Assert.That(first.LoadCount, Is.EqualTo(0));
        Assert.That(second.LoadCount, Is.EqualTo(0));
    }

    [Test]
    public void TemplateRenderer_CustomMemberResolverFlowsIntoIsolatedRenders()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("child", "{{ user.display_name }}");
        AliasMemberResolver resolver = new AliasMemberResolver();
        TemplateRenderer renderer = TemplateRenderer.Create(new TemplateOptions(), registry, resolver);

        string result = renderer.RenderToString("{{ user.display_name }}|{% render \"child\" %}", CreateMemberRoot());

        Assert.That(result, Is.EqualTo("Ada|Ada"));
        Assert.That(resolver.AliasResolutionCount, Is.EqualTo(2));
    }

    [Test]
    public void TemplateRenderContext_ReplacedMemberResolverIsUsedAtRenderTime()
    {
        StringWriter writer = new StringWriter();
        TemplateRenderContext context = TemplateRenderContext.Create(CreateMemberRoot(), writer);
        AliasMemberResolver resolver = new AliasMemberResolver();
        context.MemberResolver = resolver;
        TemplateParseResult parseResult = TemplateParser.ParseNative("{{ user.display_name }}");

        new TemplateRenderer().Render(parseResult.Document, context);

        Assert.That(writer.ToString(), Is.EqualTo("Ada"));
        Assert.That(resolver.AliasResolutionCount, Is.EqualTo(1));
    }

    [Test]
    public void BuiltInServicesImplementTheirPublicInterfaces()
    {
        string root = Path.Combine(Path.GetTempPath(), "template-interface-" + Guid.NewGuid().ToString("N"));

        Assert.That(new MemoryTemplateLoader(), Is.AssignableTo<ITemplateLoader>());
        Assert.That(new FileTemplateLoader(root), Is.AssignableTo<ITemplateLoader>());
        Assert.That(new TemplateMemberResolver(), Is.AssignableTo<ITemplateMemberResolver>());
    }

    [Test]
    public void InterfaceInjectionRejectsNullServices()
    {
        TemplateRegistry registry = new TemplateRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.AddTemplateLoader(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderContext(CreateRoot(), new StringWriter(), new TemplateOptions(), registry, null!));
        Assert.Throws<ArgumentNullException>(() => TemplateRenderer.Create(new TemplateOptions(), registry, null!));
    }

    static TemplateValue CreateRoot()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(root);
    }

    static TemplateValue CreateMemberRoot()
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString("Ada");
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["user"] = TemplateValue.FromMap(user);
        return TemplateValue.FromMap(root);
    }
}

public class TestTemplateLoader : ITemplateLoader
{
    public Dictionary<string, TemplateSource> Sources;
    public int LoadCount;
    public int LoadAsyncCount;

    public TestTemplateLoader()
    {
        Sources = new Dictionary<string, TemplateSource>();
        LoadCount = 0;
        LoadAsyncCount = 0;
    }

    public void SetTemplate(string name, string text)
    {
        Sources[name] = TemplateSource.FromNamedString(name, text);
    }

    public TemplateSource? Load(string name)
    {
        LoadCount = LoadCount + 1;
        if (!Sources.ContainsKey(name))
        {
            return null;
        }

        return Sources[name];
    }

    public Task<TemplateSource?> LoadAsync(string name, CancellationToken cancellationToken)
    {
        LoadAsyncCount = LoadAsyncCount + 1;
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        if (!Sources.ContainsKey(name))
        {
            return Task.FromResult<TemplateSource?>(null);
        }

        return Task.FromResult<TemplateSource?>(Sources[name]);
    }
}

public class AliasMemberResolver : ITemplateMemberResolver
{
    public TemplateMemberResolver DefaultResolver;
    public int AliasResolutionCount;

    public AliasMemberResolver()
    {
        DefaultResolver = new TemplateMemberResolver();
        AliasResolutionCount = 0;
    }

    public TemplateValue ResolveMember(TemplateValue target, string memberName)
    {
        if (memberName == "display_name")
        {
            AliasResolutionCount = AliasResolutionCount + 1;
            return DefaultResolver.ResolveMember(target, "name");
        }

        return DefaultResolver.ResolveMember(target, memberName);
    }

    public TemplateValue ResolveIndex(TemplateValue target, TemplateValue index)
    {
        return DefaultResolver.ResolveIndex(target, index);
    }
}
