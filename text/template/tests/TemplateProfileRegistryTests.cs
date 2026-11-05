using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateProfileRegistryTests
{
    [Test]
    public void Registry_ContainsBuiltInsAndStoresDefensiveCustomCopies()
    {
        TemplateProfileRegistry registry = new TemplateProfileRegistry();
        TemplateProfile profile = CreateBracketProfile();

        registry.Register(profile);
        profile.OutputStartDelimiter = "changed";
        TemplateProfile resolved = registry.GetProfile("bracket");
        resolved.OutputEndDelimiter = "changed";

        Assert.That(registry.Contains("native"), Is.True);
        Assert.That(registry.Contains("mustache"), Is.True);
        Assert.That(registry.Contains("handlebars"), Is.True);
        Assert.That(registry.Contains("liquid"), Is.True);
        Assert.That(registry.Contains("jinja"), Is.True);
        Assert.That(registry.Contains("django"), Is.True);
        Assert.That(registry.GetProfileNames(), Does.Contain("bracket"));
        Assert.That(registry.GetProfile("bracket").OutputStartDelimiter, Is.EqualTo("[["));
        Assert.That(registry.GetProfile("bracket").OutputEndDelimiter, Is.EqualTo("]]"));
        Assert.That(registry.Remove("native"), Is.False);
        Assert.That(registry.Remove("bracket"), Is.True);
        Assert.That(registry.Contains("bracket"), Is.False);
    }

    [Test]
    public void ParseAndRender_UseRegisteredCustomDelimiters()
    {
        TemplateProfileRegistry profiles = CreateRegistry();
        TemplateParseResult parsed = profiles.Parse("Hello [[ name ]][# note #]", "bracket");
        TemplateOptions options = profiles.CreateOptions("bracket", TemplateCompatibilityMode.Pragmatic);
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        renderer.SetProfileRegistry(profiles);

        string output = renderer.RenderToString("Hello [[ name ]][# note #]", CreateModel());

        Assert.That(parsed.HasDiagnostics(), Is.False);
        Assert.That(parsed.Document.Children.Count, Is.EqualTo(3));
        Assert.That(parsed.Document.Children[1].Kind, Is.EqualTo(TemplateSyntaxKind.Output));
        Assert.That(parsed.Document.Children[2].Kind, Is.EqualTo(TemplateSyntaxKind.Comment));
        Assert.That(output, Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void Renderer_ComposesRegisteredTemplatesWithCustomDelimiters()
    {
        TemplateProfileRegistry profiles = CreateRegistry();
        TemplateRegistry templates = new TemplateRegistry();
        templates.RegisterTemplate("child", "Child [[ name ]][% include \"leaf\" %]");
        templates.RegisterTemplate("leaf", "!");
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(profiles.CreateOptions("bracket", TemplateCompatibilityMode.Pragmatic), templates, new TemplateMemberResolver());
        renderer.SetProfileRegistry(profiles);

        string output = renderer.RenderToString("Root:[% include \"child\" %]", CreateModel());

        Assert.That(output, Is.EqualTo("Root:Child Ada!"));
    }

    [Test]
    public void CompilationCache_UsesRegisteredProfileAndDelimiterFingerprint()
    {
        TemplateProfileRegistry profiles = CreateRegistry();
        TemplateCompilationCache cache = new TemplateCompilationCache();
        cache.SetProfileRegistry(profiles);
        TemplateOptions options = profiles.CreateOptions("bracket", TemplateCompatibilityMode.Pragmatic);
        TemplateSource source = new TemplateSource("sample", null, "1", "hash", "[[ name ]]");

        string firstKey = cache.CreateKey(source, options);
        TemplateCompiledTemplate first = cache.GetOrCompile(source, options);
        cache.GetOrCompile(source, options);
        TemplateProfile replacement = CreateBracketProfile();
        replacement.OutputStartDelimiter = "((";
        replacement.OutputEndDelimiter = "))";
        profiles.Register(replacement);
        string secondKey = cache.CreateKey(source, options);
        TemplateCompiledTemplate second = cache.GetOrCompile(source, options);

        Assert.That(first.Diagnostics.Count, Is.EqualTo(0));
        Assert.That(first.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Output));
        Assert.That(cache.GetHitCount(), Is.EqualTo(1));
        Assert.That(secondKey, Is.Not.EqualTo(firstKey));
        Assert.That(second.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Text));
        Assert.That(cache.Count(), Is.EqualTo(2));
    }

    [Test]
    public void AnalysisAndDependencyGraph_ResolveCustomProfilesEndToEnd()
    {
        TemplateProfileRegistry profiles = CreateRegistry();
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();
        analyzer.SetProfileRegistry(profiles);
        TemplateCompatibilityAnalysis analysis = analyzer.Analyze("[[ name ]]", "bracket", "native");
        TemplateRegistry templates = new TemplateRegistry();
        templates.RegisterTemplate("child", "[% include \"leaf\" %]");
        templates.RegisterTemplate("leaf", "Leaf");
        TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        builder.Configure(profiles.CreateOptions("bracket", TemplateCompatibilityMode.Pragmatic), templates);
        builder.SetProfileRegistry(profiles);

        TemplateDependencyGraph graph = builder.Build("[% include \"child\" %]");

        Assert.That(analysis.SourceProfileName, Is.EqualTo("bracket"));
        Assert.That(analysis.TargetProfileName, Is.EqualTo("native"));
        Assert.That(analysis.NativeTemplateText, Is.EqualTo("{{ name }}"));
        Assert.That(HasDiagnostic(analysis, "COMPAT001"), Is.False);
        Assert.That(graph.HasDiagnostics(), Is.False);
        Assert.That(graph.Edges.Count, Is.EqualTo(2));
        Assert.That(graph.Edges[0].Name, Is.EqualTo("child"));
        Assert.That(graph.Edges[1].Name, Is.EqualTo("leaf"));
    }

    [Test]
    public void CompatibilityModes_CreatePragmaticAndStrictRuntimeBehavior()
    {
        TemplateProfileRegistry profiles = CreateRegistry();
        TemplateOptions pragmatic = profiles.CreateOptions("bracket", TemplateCompatibilityMode.Pragmatic);
        TemplateOptions strict = profiles.CreateOptions("bracket", TemplateCompatibilityMode.Strict);
        TemplateRenderer pragmaticRenderer = new TemplateRenderer();
        pragmaticRenderer.Configure(pragmatic, new TemplateRegistry(), new TemplateMemberResolver());
        pragmaticRenderer.SetProfileRegistry(profiles);
        TemplateRenderer strictRenderer = new TemplateRenderer();
        strictRenderer.Configure(strict, new TemplateRegistry(), new TemplateMemberResolver());
        strictRenderer.SetProfileRegistry(profiles);

        Assert.That(pragmatic.CompatibilityMode, Is.EqualTo(TemplateCompatibilityMode.Pragmatic));
        Assert.That(pragmatic.StrictMode, Is.False);
        Assert.That(pragmaticRenderer.RenderToString("[[ missing ]]", TemplateValue.FromMap(new Dictionary<string, TemplateValue>())), Is.EqualTo(""));
        Assert.That(strict.CompatibilityMode, Is.EqualTo(TemplateCompatibilityMode.Strict));
        Assert.That(strict.StrictMode, Is.True);
        Assert.That(strict.StrictMissingValues, Is.True);
        Assert.That(strict.StrictTypeConversions, Is.True);
        Assert.That(strict.StrictUnknownFunctions, Is.True);
        Assert.That(strict.StrictUnsafeMembers, Is.True);
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("[[ missing ]]", TemplateValue.FromMap(new Dictionary<string, TemplateValue>())));
    }

    [Test]
    public void UnknownProfiles_AreRejectedByEveryProfileSelectingService()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateOptions unknownOptions = new TemplateOptions();
        unknownOptions.SyntaxProfile = "unknown";
        TemplateSource source = new TemplateSource("sample", null, null, null, "text");
        TemplateCompilationCache cache = new TemplateCompilationCache();
        cache.SetProfileRegistry(profiles);
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();
        analyzer.SetProfileRegistry(profiles);
        TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        builder.Configure(unknownOptions, new TemplateRegistry());
        builder.SetProfileRegistry(profiles);
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(unknownOptions, new TemplateRegistry(), new TemplateMemberResolver());
        renderer.SetProfileRegistry(profiles);

        Assert.Throws<ArgumentException>(() => profiles.GetProfile("unknown"));
        Assert.Throws<ArgumentException>(() => profiles.CreateOptions("unknown", TemplateCompatibilityMode.Pragmatic));
        Assert.Throws<ArgumentException>(() => profiles.Parse("text", "unknown"));
        Assert.Throws<ArgumentException>(() => cache.GetOrCompile(source, unknownOptions));
        Assert.Throws<ArgumentException>(() => analyzer.Analyze("text", "unknown", "native"));
        Assert.Throws<ArgumentException>(() => analyzer.Analyze("text", "native", "unknown"));
        Assert.Throws<ArgumentException>(() => builder.Build("text"));
        Assert.Throws<ArgumentException>(() => renderer.RenderToString("text", CreateModel()));
    }

    [Test]
    public void RegistryAndServices_RejectInvalidArgumentsAndBuiltInReplacement()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateProfile native = new TemplateProfileCatalog().CreateNativeProfile();

        Assert.Throws<ArgumentNullException>(() => profiles.Register(null!));
        Assert.Throws<InvalidOperationException>(() => profiles.Register(native));
        Assert.Throws<ArgumentNullException>(() => profiles.Contains(null!));
        Assert.Throws<ArgumentException>(() => profiles.Contains(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => profiles.CreateOptions("native", (TemplateCompatibilityMode)99));
        Assert.Throws<ArgumentNullException>(() => profiles.Parse(null!, "native"));
        Assert.Throws<ArgumentNullException>(() => new TemplateRenderer().SetProfileRegistry(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateCompilationCache().SetProfileRegistry(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateCompatibilityAnalyzer().SetProfileRegistry(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateDependencyGraphBuilder().SetProfileRegistry(null!));
    }

    static TemplateProfileRegistry CreateRegistry()
    {
        TemplateProfileRegistry registry = new TemplateProfileRegistry();
        registry.Register(CreateBracketProfile());
        return registry;
    }

    static TemplateProfile CreateBracketProfile()
    {
        TemplateProfile profile = new TemplateProfileCatalog().CreateNativeProfile();
        profile.Name = "bracket";
        profile.OutputStartDelimiter = "[[";
        profile.OutputEndDelimiter = "]]";
        profile.StatementStartDelimiter = "[%";
        profile.StatementEndDelimiter = "%]";
        profile.CommentStartDelimiter = "[#";
        profile.CommentEndDelimiter = "#]";
        return profile;
    }

    static TemplateValue CreateModel()
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(values);
    }

    static bool HasDiagnostic(TemplateCompatibilityAnalysis analysis, string code)
    {
        int index = 0;
        while (index < analysis.Diagnostics.Count)
        {
            if (analysis.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
