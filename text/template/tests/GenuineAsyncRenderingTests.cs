using System.Text;

namespace template.Tests;

[TestFixture]
public class GenuineAsyncRenderingTests
{
    [Test]
    public async Task RenderAsync_AwaitsAsynchronousFunctionsFiltersTestsAndHelpers()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncFunction("make", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateValue.FromString(arguments[0].StringValue + "F");
        });
        registry.RegisterAsyncFilter("decorate", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateValue.FromString("[" + arguments[0].StringValue + "]");
        });
        registry.RegisterAsyncTest("accepted", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateValue.FromBoolean(arguments[0].StringValue == "yes");
        });
        registry.RegisterAsyncHelper("assist", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateValue.FromString("H");
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        string output = await renderer.RenderToStringAsync("{{ make(\"A\") | decorate }}|{% if \"yes\" is accepted %}true{% endif %}|{{ assist() }}", EmptyModel());

        Assert.That(output, Is.EqualTo("[AF]|true|H"));
    }

    [Test]
    public async Task RenderAsync_AsynchronousTestsCanObserveMissingValuesInStrictMode()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictMissingValues = true;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncTest("missing_async", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateValue.FromBoolean(arguments[0].IsMissing());
        });
        TemplateRenderer renderer = TemplateRenderer.Create(options, registry);

        string output = await renderer.RenderToStringAsync("{{ missing is missing_async }}", EmptyModel());

        Assert.That(output, Is.EqualTo("true"));
    }

    [Test]
    public async Task RenderAsync_UsesAsynchronousOnlyLoaderForComposition()
    {
        AsyncOnlyTemplateLoader loader = new AsyncOnlyTemplateLoader();
        TemplateRegistry registry = new TemplateRegistry();
        registry.AddTemplateLoader(loader);
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        Task<string> renderTask = renderer.RenderToStringAsync("before-{% include \"child\" %}-after", EmptyModel());
        await loader.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(renderTask.IsCompleted, Is.False);
        Assert.That(loader.SynchronousCallCount, Is.EqualTo(0));

        loader.Completion.SetResult(TemplateSource.FromNamedString("child", "loaded"));
        Assert.That(await renderTask, Is.EqualTo("before-loaded-after"));
    }

    [Test]
    public async Task RenderAsync_UsesAsynchronousOnlyWriterAndAwaitsOutput()
    {
        AsyncOnlyTextWriter writer = new AsyncOnlyTextWriter();
        TemplateRenderer renderer = new TemplateRenderer();

        Task renderTask = renderer.RenderToWriterAsync("Hello {{ name }}", CreateModel(), writer);
        await writer.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(renderTask.IsCompleted, Is.False);
        Assert.That(writer.SynchronousCallCount, Is.EqualTo(0));

        writer.Release.SetResult();
        await renderTask;
        Assert.That(writer.GetText(), Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void RenderAsync_PropagatesCancellationToCompositionLoader()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        AsyncOnlyTemplateLoader loader = new AsyncOnlyTemplateLoader();
        TemplateRegistry registry = new TemplateRegistry();
        registry.AddTemplateLoader(loader);
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        Task<string> renderTask = renderer.RenderToStringWithCancellationAsync("{% include \"child\" %}", EmptyModel(), cancellation.Token);
        loader.Invoked.Task.GetAwaiter().GetResult();
        cancellation.Cancel();

        Assert.That(loader.ObservedToken, Is.EqualTo(cancellation.Token));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await renderTask);
    }

    [Test]
    public void RenderAsync_PropagatesCancellationToWriterOutput()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        AsyncOnlyTextWriter writer = new AsyncOnlyTextWriter();
        TemplateRenderer renderer = new TemplateRenderer();

        Task renderTask = renderer.RenderToWriterWithCancellationAsync("output", EmptyModel(), writer, cancellation.Token);
        writer.Invoked.Task.GetAwaiter().GetResult();
        cancellation.Cancel();

        Assert.That(writer.ObservedToken, Is.EqualTo(cancellation.Token));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await renderTask);
    }

    [Test]
    public void RenderAsync_PropagatesCancellationToEvaluation()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        TaskCompletionSource invoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observedToken = CancellationToken.None;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncFunction("wait", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            observedToken = cancellationToken;
            invoked.SetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return TemplateValue.FromString("unreachable");
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);

        Task<string> renderTask = renderer.RenderToStringWithCancellationAsync("{{ wait() }}", EmptyModel(), cancellation.Token);
        invoked.Task.GetAwaiter().GetResult();
        cancellation.Cancel();

        Assert.That(observedToken, Is.EqualTo(cancellation.Token));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await renderTask);
    }

    [Test]
    public void RenderAsync_ChecksCancellationBetweenLoopIterations()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        int calls = 0;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncHelper("step", async delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            calls = calls + 1;
            await Task.Yield();
            if (calls == 2)
            {
                cancellation.Cancel();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return arguments[0];
        });
        TemplateRenderer renderer = TemplateRenderer.Create(registry);
        Dictionary<string, TemplateValue> model = new Dictionary<string, TemplateValue>();
        model["items"] = TemplateValue.FromList(new List<TemplateValue>
        {
            TemplateValue.FromString("a"),
            TemplateValue.FromString("b"),
            TemplateValue.FromString("c")
        });

        Task<string> renderTask = renderer.RenderToStringWithCancellationAsync("{% for item in items %}{{ step(item) }}{% endfor %}", TemplateValue.FromMap(model), cancellation.Token);

        Assert.ThrowsAsync<TaskCanceledException>(async () => await renderTask);
        Assert.That(calls, Is.EqualTo(2));
    }

    static TemplateValue EmptyModel()
    {
        return TemplateValue.FromMap(new Dictionary<string, TemplateValue>());
    }

    static TemplateValue CreateModel()
    {
        Dictionary<string, TemplateValue> model = new Dictionary<string, TemplateValue>();
        model["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(model);
    }
}

internal class AsyncOnlyTemplateLoader : ITemplateLoader
{
    internal TaskCompletionSource Invoked;
    internal TaskCompletionSource<TemplateSource?> Completion;
    internal int SynchronousCallCount;
    internal CancellationToken ObservedToken;

    internal AsyncOnlyTemplateLoader()
    {
        Invoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Completion = new TaskCompletionSource<TemplateSource?>(TaskCreationOptions.RunContinuationsAsynchronously);
        SynchronousCallCount = 0;
        ObservedToken = CancellationToken.None;
    }

    public TemplateSource? Load(string name)
    {
        SynchronousCallCount = SynchronousCallCount + 1;
        throw new InvalidOperationException("The synchronous loader must not be used.");
    }

    public Task<TemplateSource?> LoadAsync(string name, CancellationToken cancellationToken)
    {
        ObservedToken = cancellationToken;
        Invoked.SetResult();
        return Completion.Task.WaitAsync(cancellationToken);
    }
}

internal class AsyncOnlyTextWriter : TextWriter
{
    internal StringBuilder Text;
    internal TaskCompletionSource Invoked;
    internal TaskCompletionSource Release;
    internal int SynchronousCallCount;
    internal CancellationToken ObservedToken;

    internal AsyncOnlyTextWriter()
    {
        Text = new StringBuilder();
        Invoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SynchronousCallCount = 0;
        ObservedToken = CancellationToken.None;
    }

    public override Encoding Encoding
    {
        get
        {
            return Encoding.UTF8;
        }
    }

    public override void Write(char value)
    {
        SynchronousCallCount = SynchronousCallCount + 1;
        throw new InvalidOperationException("Synchronous writer output must not be used.");
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken)
    {
        ObservedToken = cancellationToken;
        Invoked.SetResult();
        await Release.Task.WaitAsync(cancellationToken);
        Text.Append(buffer.Span);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal string GetText()
    {
        return Text.ToString();
    }
}
