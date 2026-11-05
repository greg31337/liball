using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicExhaustiveBehaviorTests
{
    [Test]
    public void MutatedTemplateSources_AreRejectedConsistentlyByPublicConsumers()
    {
        TemplateSource badLength = new TemplateSource("bad", null, null, null, "abc");
        badLength.Length = 2;
        TemplateSource nullText = new TemplateSource("bad", null, null, null, "abc");
        nullText.Text = null!;
        TemplateSource nullName = new TemplateSource("bad", null, null, null, "abc");
        nullName.Name = null!;
        TemplateCompilationCache cache = new TemplateCompilationCache();
        TemplateRenderer renderer = new TemplateRenderer();
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        TemplateRegistry registry = new TemplateRegistry();

        Assert.Throws<ArgumentException>(() => new TemplateLexer(badLength));
        Assert.Throws<ArgumentException>(() => new TemplateSourceLineMap(badLength));
        Assert.Throws<ArgumentException>(() => cache.GetOrCompile(badLength, new TemplateOptions()));
        Assert.Throws<ArgumentException>(() => renderer.PreparseSource(badLength));
        Assert.Throws<ArgumentException>(() => loader.SetTemplateSource("bad", badLength));
        Assert.Throws<ArgumentException>(() => registry.RegisterPartialSource("bad", badLength));
        Assert.Throws<ArgumentNullException>(() => new TemplateLexer(nullText));
        Assert.Throws<ArgumentNullException>(() => new TemplateSourceLineMap(nullName));
    }

    [Test]
    public void MemoryLoader_HandlesExactLimitsClearAndConcurrentPublicUse()
    {
        MemoryTemplateLoader loader = new MemoryTemplateLoader();
        TemplateLoaderOptions options = new TemplateLoaderOptions();
        options.MaximumTemplateLength = 4;
        loader.Configure(options);

        loader.SetTemplate("edge", "😀ab");

        Assert.That(loader.LoadRequired("edge").Text, Is.EqualTo("😀ab"));
        Assert.That(loader.LoadRequired("edge").Version, Is.EqualTo("1"));
        Assert.Throws<InvalidOperationException>(() => loader.SetTemplate("too-long", "😀abc"));

        loader.Clear();
        loader.SetTemplate("after-clear", "ok");
        Assert.That(loader.LoadRequired("after-clear").Version, Is.EqualTo("1"));
        loader.Clear();

        ConcurrentBag<string> versions = new ConcurrentBag<string>();
        Parallel.For(0, 64, (int index) =>
        {
            string name = "item-" + index.ToString(CultureInfo.InvariantCulture);
            loader.SetTemplate(name, "😀");
            versions.Add(loader.LoadRequired(name).Version!);
        });

        Assert.That(loader.GetTemplateNames().Count, Is.EqualTo(64));
        Assert.That(versions.Distinct().Count(), Is.EqualTo(64));
    }

    [Test]
    public async Task EverySyntaxProfile_PreservesUnicodeAndMixedNewlinesAcrossRenderModes()
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateValueFactory factory = new TemplateValueFactory();
        string value = "😀 e\u0301";
        TemplateValue model = factory.CreateMap(new Dictionary<string, TemplateValue>
        {
            ["value"] = factory.CreateSafeString(value)
        });

        foreach (string profileName in profiles.GetProfileNames())
        {
            TemplateOptions options = profiles.CreateOptions(profileName, TemplateCompatibilityMode.Pragmatic);
            options.WhitespaceMode = TemplateWhitespaceMode.Preserve;
            TemplateRenderer renderer = new TemplateRenderer();
            renderer.SetProfileRegistry(profiles);
            renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
            string templateText = CreateProfileTemplate(profileName);
            string expected = "α\r\nX:" + value + "\nω";

            Assert.That(renderer.RenderToString(templateText, model), Is.EqualTo(expected), profileName + " buffered");

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            renderer.RenderToWriter(templateText, model, writer);
            Assert.That(writer.ToString(), Is.EqualTo(expected), profileName + " writer");

            Assert.That(await renderer.RenderToStringAsync(templateText, model), Is.EqualTo(expected), profileName + " async buffered");

            StringWriter asyncWriter = new StringWriter(CultureInfo.InvariantCulture);
            await renderer.RenderToWriterAsync(templateText, model, asyncWriter);
            Assert.That(asyncWriter.ToString(), Is.EqualTo(expected), profileName + " async writer");
        }
    }

    [Test]
    public async Task EveryEscapingMode_UsesIdenticalBufferedWriterAndAsyncBehavior()
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateValue model = factory.CreateMap(new Dictionary<string, TemplateValue>
        {
            ["ordinary"] = factory.CreateString("<x>"),
            ["safe"] = factory.CreateSafeString("<x>")
        });

        foreach (TemplateEscapingMode mode in Enum.GetValues<TemplateEscapingMode>())
        {
            TemplateOptions options = new TemplateOptions();
            options.AutoEscape = true;
            options.EscapingMode = mode;
            options.OutputEncoder = new ExhaustiveOutputEncoder();
            TemplateRenderer renderer = new TemplateRenderer();
            renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
            string expected = mode == TemplateEscapingMode.None ? "<x>|<x>" : mode.ToString() + "(<x>)|<x>";

            Assert.That(renderer.RenderToString("{{ ordinary }}|{{ safe }}", model), Is.EqualTo(expected), mode.ToString());

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            renderer.RenderToWriter("{{ ordinary }}|{{ safe }}", model, writer);
            Assert.That(writer.ToString(), Is.EqualTo(expected), mode.ToString());

            Assert.That(await renderer.RenderToStringAsync("{{ ordinary }}|{{ safe }}", model), Is.EqualTo(expected), mode.ToString());
        }
    }

    [Test]
    public void EveryStrictSwitchCombination_AllowsValidInputAndEnforcesStrictMode()
    {
        TemplateValue model = CreateModel("Ada");
        int combination = 0;
        while (combination < 16)
        {
            TemplateOptions options = new TemplateOptions();
            options.StrictMissingValues = (combination & 1) != 0;
            options.StrictTypeConversions = (combination & 2) != 0;
            options.StrictUnknownFunctions = (combination & 4) != 0;
            options.StrictUnsafeMembers = (combination & 8) != 0;
            TemplateRenderer renderer = new TemplateRenderer();
            renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());

            Assert.That(renderer.RenderToString("{{ name }}", model), Is.EqualTo("Ada"), "combination " + combination.ToString(CultureInfo.InvariantCulture));
            combination = combination + 1;
        }

        TemplateOptions strict = new TemplateOptions();
        strict.StrictMode = true;
        TemplateRenderer strictRenderer = new TemplateRenderer();
        strictRenderer.Configure(strict, new TemplateRegistry(), new TemplateMemberResolver());
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{{ missing }}", model));
        Assert.Throws<InvalidOperationException>(() => strictRenderer.RenderToString("{{ unknown() }}", model));
    }

    [Test]
    public async Task Renderer_SupportsConcurrentCompositionAndPublishesOneAtomicLastResult()
    {
        TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TemplateValueFactory factory = new TemplateValueFactory();
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("card", "C{{ value }}");
        registry.RegisterAsyncFunction("wait", async (List<TemplateValue> arguments, CancellationToken cancellationToken) =>
        {
            await release.Task.WaitAsync(cancellationToken);
            return arguments[0];
        });
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(new TemplateOptions(), registry, new TemplateMemberResolver());
        TemplateSourceMapCollector collector = new TemplateSourceMapCollector();
        renderer.SetSourceMapCollector(collector);
        Assert.That(renderer.RenderToString("B{% include \"card\" %}", CreateModel("baseline")), Is.EqualTo("BCbaseline"));
        List<TemplateDependency> completedDependencies = renderer.GetLastDependencies();
        List<TemplateSourceMapEntry> completedMappings = renderer.GetLastSourceMap();
        List<Task<string>> tasks = new List<Task<string>>();
        int index = 0;
        while (index < 32)
        {
            TemplateValue model = CreateModel(index.ToString(CultureInfo.InvariantCulture));
            tasks.Add(renderer.RenderToStringAsync("A{{ wait(value) }}{% include \"card\" %}", model));
            index = index + 1;
        }

        Assert.That(renderer.GetLastDependencies().Count, Is.EqualTo(completedDependencies.Count));
        AssertEquivalentMappings(completedMappings, renderer.GetLastSourceMap());
        release.SetResult(true);
        string[] outputs = await Task.WhenAll(tasks);

        Assert.That(outputs.Distinct().Count(), Is.EqualTo(32));
        index = 0;
        while (index < outputs.Length)
        {
            Assert.That(outputs[index], Does.Match("^A[0-9]+C[0-9]+$"));
            index = index + 1;
        }

        Assert.That(renderer.GetLastDependencies().Count, Is.EqualTo(1));
        AssertEquivalentMappings(renderer.GetLastSourceMap(), collector.GetMappings());
    }

    [Test]
    public async Task Renderer_ReentrancyCancellationAndMustacheResetLastStateCleanly()
    {
        TemplateValue model = CreateModel("Ada");
        TemplateRegistry registry = new TemplateRegistry();
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(new TemplateOptions(), registry, new TemplateMemberResolver());
        TemplateSourceMapCollector collector = new TemplateSourceMapCollector();
        renderer.SetSourceMapCollector(collector);
        registry.RegisterFunction("nested", (List<TemplateValue> arguments) => new TemplateValueFactory().CreateString(renderer.RenderToString("inner {{ name }}", model)));

        Assert.That(renderer.RenderToString("{{ nested() }}|{{ name }}", model), Is.EqualTo("inner Ada|Ada"));
        List<TemplateSourceMapEntry> completedMap = renderer.GetLastSourceMap();
        Assert.That(completedMap.Count, Is.GreaterThan(0));

        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await renderer.RenderToStringWithCancellationAsync("ignored", model, cancellation.Token));
        AssertEquivalentMappings(completedMap, renderer.GetLastSourceMap());

        Assert.That(renderer.RenderMustacheToString("{{name}}", model), Is.EqualTo("Ada"));
        Assert.That(renderer.GetLastSourceMap(), Is.Empty);
        Assert.That(collector.GetMappings(), Is.Empty);
    }

    static string CreateProfileTemplate(string profileName)
    {
        if (profileName == "go-text-template")
        {
            return "α\r\nX:{{.value}}\nω";
        }

        if (profileName == "razor-inspired")
        {
            return "α\r\nX:@value\nω";
        }

        return "α\r\nX:{{ value }}\nω";
    }

    static TemplateValue CreateModel(string value)
    {
        TemplateValueFactory factory = new TemplateValueFactory();
        return factory.CreateMap(new Dictionary<string, TemplateValue>
        {
            ["name"] = factory.CreateString(value),
            ["value"] = factory.CreateString(value)
        });
    }

    static void AssertEquivalentMappings(List<TemplateSourceMapEntry> expected, List<TemplateSourceMapEntry> actual)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        int index = 0;
        while (index < expected.Count)
        {
            Assert.That(actual[index].OutputStart, Is.EqualTo(expected[index].OutputStart));
            Assert.That(actual[index].OutputLength, Is.EqualTo(expected[index].OutputLength));
            Assert.That(actual[index].SourceSpan.Start, Is.EqualTo(expected[index].SourceSpan.Start));
            Assert.That(actual[index].SourceSpan.Length, Is.EqualTo(expected[index].SourceSpan.Length));
            index = index + 1;
        }
    }
}

public class ExhaustiveOutputEncoder : ITemplateOutputEncoder
{
    public string Encode(string text, TemplateEscapingMode mode)
    {
        return mode.ToString() + "(" + text + ")";
    }
}
