namespace config.Tests;

public class ConfigReloadTests
{
    [Test]
    public void ManualReload_RebuildsFromRegisteredFileSources()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-manual-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        Config reloaded = controller.Reload();

        Assert.That(reloaded.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("second"));
    }

    [Test]
    public void ManualReload_PreservesBuilderOptionsAndAliases()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-options-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigBuilder builder = Config.CreateBuilder();
        builder
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .AddAlias("legacy.host", "service.host");
        ConfigReloadController controller = builder.BuildReloading();

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        Config reloaded = controller.Reload();

        Assert.That(reloaded.GetString("legacy.host"), Is.EqualTo("second"));
        Assert.That(reloaded.Keys(), Is.EqualTo(new string[] { "service" }));
    }

    [Test]
    public void ManualReload_UsesSourceListRegisteredWhenControllerWasBuilt()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-registered-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigBuilder builder = Config.CreateBuilder();
        builder.AddJsonFile(path, ConfigOptional.Required, ConfigReload.None);
        ConfigReloadController controller = builder.BuildReloading();
        builder.AddOverride("service.host", "builder-mutation");

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        Config reloaded = controller.Reload();

        Assert.That(controller.SourceCount(), Is.EqualTo(1));
        Assert.That(reloaded.GetString("service.host"), Is.EqualTo("second"));
    }

    [Test]
    public void ManualReload_DoesNotReplaceSnapshotWhenReloadHasErrors()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-failed-preserve-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        File.Delete(path);
        Config reloaded = controller.Reload();

        Assert.That(reloaded.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(controller.LastDiagnostics().Errors[0].Message, Is.EqualTo("missing required file"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        Config recovered = controller.Reload();

        Assert.That(recovered.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
    }

    [Test]
    public void ManualReload_SaturatesTheReloadCountAtIntMaximum()
    {
        ConfigReloadController controller = Config.CreateBuilder().BuildReloading();
        controller.ReloadCount = int.MaxValue;

        controller.Reload();

        Assert.That(controller.ReloadEventCount(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ManualReload_InvokesRegisteredCallbacksAfterSuccessfulReload()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-callback-success-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();
        int callbackCount = 0;
        string callbackHost = "";

        ConfigReloadController returned = controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
            callbackHost = config.GetString("service.host");
            config.Root = ConfigValue.String("callback-mutation");
        });
        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.Reload();

        Assert.That(returned, Is.SameAs(controller));
        Assert.That(controller.ChangeCallbackCount(), Is.EqualTo(1));
        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(callbackHost, Is.EqualTo("second"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("second"));
    }

    [Test]
    public void ManualReload_DoesNotInvokeCallbacksWhenReloadHasErrors()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-callback-failed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();
        int callbackCount = 0;
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
        });

        File.Delete(path);
        controller.Reload();

        Assert.That(callbackCount, Is.EqualTo(0));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().HasErrors(), Is.True);
    }

    [Test]
    public void FileWatching_StartCreatesWatchersForReloadableFileSources()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-watchers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string watchedPath = Path.Combine(directory, "watched.json");
        string unwatchedPath = Path.Combine(directory, "unwatched.json");
        File.WriteAllText(watchedPath, "{ \"watched\": \"yes\" }");
        File.WriteAllText(unwatchedPath, "{ \"unwatched\": \"yes\" }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(watchedPath, ConfigOptional.Required, ConfigReload.OnChange)
            .AddJsonFile(unwatchedPath, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        controller.Start();
        Assert.That(controller.IsWatching(), Is.True);
        Assert.That(controller.WatcherCount(), Is.EqualTo(1));

        controller.Stop();
        Assert.That(controller.IsWatching(), Is.False);
        Assert.That(controller.WatcherCount(), Is.EqualTo(0));
    }

    [Test]
    public void FileWatching_ChangedEventReloadsWatchedSource()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-watch-change-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(0);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.OnFileChanged(controller, args);
        Config current = controller.Current();

        Assert.That(current.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

    [Test]
    public void FileWatching_DoesNotReplaceSnapshotWhenWatchedReloadHasErrors()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-watch-failed-preserve-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(0);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, directory, "settings.json");

        File.Delete(path);
        controller.OnFileChanged(controller, args);

        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(controller.LastDiagnostics().Errors[0].Message, Is.EqualTo("missing required file"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

    [Test]
    public void FileWatching_InvokesRegisteredCallbacksAfterSuccessfulReload()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-watch-callback-success-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(0);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");
        int callbackCount = 0;
        string callbackHost = "";
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
            callbackHost = config.GetString("service.host");
        });

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.OnFileChanged(controller, args);

        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(callbackHost, Is.EqualTo("second"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("second"));
    }

    [Test]
    public void FileWatching_DebouncedReloadInvokesCallbacksOnceWithLatestSnapshot()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-debounce-callback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(100);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");
        int callbackCount = 0;
        string callbackHost = "";
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
            callbackHost = config.GetString("service.host");
        });

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.OnFileChanged(controller, args);
        File.WriteAllText(path, "{ \"service\": { \"host\": \"third\" } }");
        controller.OnFileChanged(controller, args);
        bool changed = SpinWait.SpinUntil(
            () => callbackCount == 1,
            TimeSpan.FromSeconds(2));
        Thread.Sleep(200);

        Assert.That(changed, Is.True);
        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(callbackHost, Is.EqualTo("third"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("third"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

    [Test]
    public void FileWatching_DebouncedFailedReloadPreservesSnapshotAndSuppressesCallbacks()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-debounce-failed-callback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(100);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, directory, "settings.json");
        int callbackCount = 0;
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
        });

        File.Delete(path);
        controller.OnFileChanged(controller, args);
        controller.OnFileChanged(controller, args);
        bool reloaded = SpinWait.SpinUntil(
            () => controller.ReloadEventCount() == 1,
            TimeSpan.FromSeconds(2));
        Thread.Sleep(200);

        Assert.That(reloaded, Is.True);
        Assert.That(callbackCount, Is.EqualTo(0));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

    [Test]
    public void FileWatching_DebouncesMultipleFileEventsIntoSingleReload()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-debounce-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(100);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.OnFileChanged(controller, args);
        controller.OnFileChanged(controller, args);
        controller.OnFileChanged(controller, args);
        bool changed = SpinWait.SpinUntil(
            () => controller.Current().GetString("service.host") == "second",
            TimeSpan.FromSeconds(2));
        Thread.Sleep(200);

        Assert.That(changed, Is.True);
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

    [Test]
    public void FileWatching_StopCancelsPendingDebouncedReload()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-debounce-stop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading()
            .SetDebounceMilliseconds(150);
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\" } }");
        controller.OnFileChanged(controller, args);
        controller.Stop();
        Thread.Sleep(250);

        Assert.That(controller.ReloadEventCount(), Is.EqualTo(0));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
    }

    [Test]
    public void FreshKeys_ReturnsOnlyNewlyLoadedSelectionsWithoutReplacingCurrentSnapshot()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "fresh-keys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\", \"port\": 80 }, \"other\": \"old\" }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();
        Config original = controller.Current();
        int callbackCount = 0;
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
        });

        File.WriteAllText(path, "{ \"service\": { \"host\": \"second\", \"port\": 81 }, \"other\": \"new\" }");
        Config fresh = controller.FreshKeys(new string[] { "service.host", "service.port" });
        Config fullFresh = controller.FreshKeys(new string[] { "" });

        Assert.That(fresh.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(fresh.GetInt("service.port"), Is.EqualTo(81));
        Assert.That(fresh.Has("other"), Is.False);
        Assert.That(fullFresh.GetString("other"), Is.EqualTo("new"));
        Assert.That(original.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.Current().GetString("other"), Is.EqualTo("old"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(0));
        Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public void FreshKeys_ReturnsFreshErrorsWithoutChangingControllerDiagnosticsOrSnapshot()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "fresh-errors-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        File.Delete(path);
        Config fresh = controller.FreshKeys(new string[] { "service.host" });

        Assert.That(fresh.Has("service.host"), Is.False);
        Assert.That(fresh.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(fresh.Diagnostics().Error(0).Message, Is.EqualTo("missing required file"));
        Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(0));
    }

    [Test]
    public void FreshSources_LoadsSelectedRegistrationsInOriginalPrecedenceOrder()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "fresh-sources-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "first.json");
        string secondPath = Path.Combine(directory, "second.json");
        File.WriteAllText(firstPath, "{ \"first\": \"old-one\", \"shared\": \"old-first\" }");
        File.WriteAllText(secondPath, "{ \"second\": \"old-two\", \"shared\": \"old-second\" }");
        ConfigReloadController controller = Config.CreateBuilder()
            .AddJsonFile(firstPath, ConfigOptional.Required, ConfigReload.None)
            .AddJsonFile(secondPath, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        File.WriteAllText(firstPath, "{ \"first\": \"new-one\", \"shared\": \"new-first\" }");
        File.WriteAllText(secondPath, "{ \"second\": \"new-two\", \"shared\": \"new-second\" }");
        Config first = controller.FreshSources(new int[] { 0 });
        File.Delete(secondPath);
        Config isolatedFirst = controller.FreshSources(new int[] { 0 });
        File.WriteAllText(secondPath, "{ \"second\": \"new-two\", \"shared\": \"new-second\" }");
        Config second = controller.FreshSources(new int[] { 1 });
        Config both = controller.FreshSources(new int[] { 1, 0, 1 });
        Config none = controller.FreshSources(new int[0]);

        Assert.That(controller.SourceCount(), Is.EqualTo(2));
        Assert.That(first.GetString("first"), Is.EqualTo("new-one"));
        Assert.That(first.Has("second"), Is.False);
        Assert.That(first.GetString("shared"), Is.EqualTo("new-first"));
        Assert.That(first.Value("shared").PrecedenceRank, Is.EqualTo(1));
        Assert.That(isolatedFirst.Diagnostics().HasErrors(), Is.False);
        Assert.That(isolatedFirst.GetString("first"), Is.EqualTo("new-one"));
        Assert.That(second.Has("first"), Is.False);
        Assert.That(second.GetString("second"), Is.EqualTo("new-two"));
        Assert.That(second.Value("shared").PrecedenceRank, Is.EqualTo(2));
        Assert.That(both.GetString("shared"), Is.EqualTo("new-second"));
        Assert.That(none.Keys(), Is.Empty);
        Assert.That(controller.Current().GetString("first"), Is.EqualTo("old-one"));
        Assert.That(controller.Current().GetString("second"), Is.EqualTo("old-two"));
        Assert.That(controller.Current().GetString("shared"), Is.EqualTo("old-second"));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(0));
    }

    [Test]
    public void FreshSelection_ValidatesArraysEntriesAndSourceIndexes()
    {
        ConfigReloadController controller = Config.CreateBuilder()
            .AddMemory(new ConfigMap(), "memory")
            .BuildReloading();

        Assert.Throws<ArgumentNullException>(() => controller.FreshKeys(null!));
        Assert.Throws<ArgumentNullException>(() => controller.FreshKeys(new string[] { null! }));
        Assert.Throws<ArgumentNullException>(() => controller.FreshSources(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.FreshSources(new int[] { -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.FreshSources(new int[] { 1 }));
    }

    [Test]
    public void FreshReadsAndReloads_SerializeSourceLoadingAndSnapshotReplacement()
    {
        CoordinatedFreshSource source = new CoordinatedFreshSource();
        ConfigReloadController controller = Config.CreateBuilder()
            .AddSource(source)
            .BuildReloading();
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task<Config> freshTask = Task.Factory.StartNew(
            () =>
            {
                start.Wait();
                return controller.FreshKeys(new string[] { "value" });
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Task<Config> reloadTask = Task.Factory.StartNew(
            () =>
            {
                start.Wait();
                return controller.Reload();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        start.Set();
        bool completed = Task.WaitAll(new Task[] { freshTask, reloadTask }, TimeSpan.FromSeconds(5));

        Assert.That(completed, Is.True);
        Assert.That(source.LoadCount, Is.EqualTo(3));
        Assert.That(source.MaximumConcurrentLoads, Is.EqualTo(1));
        Assert.That(freshTask.Result.GetInt("value"), Is.AnyOf(2, 3));
        Assert.That(controller.Current().GetInt("value"), Is.AnyOf(2, 3));
        Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
    }

}

public class CoordinatedFreshSource : IConfigSource
{
    public int LoadCount;
    public int ActiveLoads;
    public int MaximumConcurrentLoads;
    public object Gate;

    public CoordinatedFreshSource()
    {
        LoadCount = 0;
        ActiveLoads = 0;
        MaximumConcurrentLoads = 0;
        Gate = new object();
    }

    public ConfigSourceResult Load(int precedenceRank)
    {
        int loadedValue = 0;
        lock (Gate)
        {
            LoadCount = LoadCount + 1;
            loadedValue = LoadCount;
            ActiveLoads = ActiveLoads + 1;
            if (ActiveLoads > MaximumConcurrentLoads)
            {
                MaximumConcurrentLoads = ActiveLoads;
            }
        }

        Thread.Sleep(100);

        lock (Gate)
        {
            ActiveLoads = ActiveLoads - 1;
        }

        ConfigMap map = new ConfigMap();
        map.Set("value", ConfigValue.Number(loadedValue).SetSource("coordinated", precedenceRank, "value", 0, 0));
        ConfigValue root = ConfigValue.Object(map).SetSource("coordinated", precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, new ConfigDiagnostics());
    }
}
