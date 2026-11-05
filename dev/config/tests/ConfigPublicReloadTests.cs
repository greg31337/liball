using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace config.Tests;

public class ConfigPublicReloadTests
{
    [Test]
    public void PublicReload_ManuallyReplacesSnapshotsAndInvokesCallbacksInRegistrationOrder()
    {
        string directory = NewDirectory("public-manual-reload");
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{\"service\":{\"host\":\"first\"}}");
        IConfigReloadController controller = new ConfigBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();
        Config original = controller.Current();
        List<string> callbacks = new List<string>();

        controller.OnChange(config =>
        {
            callbacks.Add("first:" + config.GetString("service.host"));
            ConfigValue value = config.Value("service.host");
            value.StringValue = "mutated-copy";
        });
        controller.OnChange(config =>
        {
            callbacks.Add("second:" + config.GetString("service.host"));
        });

        try
        {
            File.WriteAllText(path, "{\"service\":{\"host\":\"second\"}}");
            Config reloaded = controller.Reload();

            Assert.Multiple(() =>
            {
                Assert.That(original.GetString("service.host"), Is.EqualTo("first"));
                Assert.That(reloaded.GetString("service.host"), Is.EqualTo("second"));
                Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("second"));
                Assert.That(callbacks, Is.EqualTo(new string[] { "first:second", "second:second" }));
                Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
                Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
                Assert.That(controller.SourceCount(), Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicReload_PreservesTheActiveSnapshotAfterFailureAndRecoversAtomically()
    {
        string directory = NewDirectory("public-failed-reload");
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{\"service\":{\"host\":\"first\"}}");
        IConfigReloadController controller = new ConfigBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();
        int callbackCount = 0;
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
        });

        try
        {
            File.WriteAllText(path, "{\"service\":");
            Config retained = controller.Reload();
            ConfigDiagnostics failedDiagnostics = controller.LastDiagnostics();
            failedDiagnostics.Error(0).Message = "mutated-copy";

            Assert.Multiple(() =>
            {
                Assert.That(retained.GetString("service.host"), Is.EqualTo("first"));
                Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
                Assert.That(controller.LastDiagnostics().ErrorCount(), Is.EqualTo(1));
                Assert.That(controller.LastDiagnostics().Error(0).Message, Is.EqualTo("malformed JSON input"));
                Assert.That(callbackCount, Is.EqualTo(0));
                Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
            });

            File.WriteAllText(path, "{\"service\":{\"host\":\"recovered\"}}");
            Config recovered = controller.Reload();

            Assert.Multiple(() =>
            {
                Assert.That(recovered.GetString("service.host"), Is.EqualTo("recovered"));
                Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("recovered"));
                Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(controller.ReloadEventCount(), Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicReload_WatchesFilesDebouncesDuplicateEventsAndStopsCleanly()
    {
        string directory = NewDirectory("public-watched-reload");
        string path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{\"service\":{\"host\":\"first\"}}");
        ConfigReloadController controller = new ConfigBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.OnChange)
            .BuildReloading();
        ConfigReloadController returned = controller.SetDebounceMilliseconds(250);
        int callbackCount = 0;
        string callbackHost = "";
        controller.OnChange(config =>
        {
            callbackHost = config.GetString("service.host");
            Interlocked.Increment(ref callbackCount);
        });

        try
        {
            Assert.That(returned, Is.SameAs(controller));
            Assert.That(controller.IsWatching(), Is.False);
            controller.Start();
            controller.Start();
            Assert.That(controller.IsWatching(), Is.True);

            File.WriteAllText(path, "{\"service\":{\"host\":\"second\"}}");
            File.WriteAllText(path, "{\"service\":{\"host\":\"third\"}}");
            File.WriteAllText(path, "{\"service\":{\"host\":\"final\"}}");
            FileSystemEventArgs changed = new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, "settings.json");
            controller.OnFileChanged(controller, changed);
            controller.OnFileChanged(controller, changed);
            controller.OnFileChanged(controller, changed);

            bool reloaded = SpinWait.SpinUntil(
                () => Volatile.Read(ref callbackCount) >= 1,
                TimeSpan.FromSeconds(5));
            Thread.Sleep(600);

            Assert.Multiple(() =>
            {
                Assert.That(reloaded, Is.True);
                Assert.That(Volatile.Read(ref callbackCount), Is.EqualTo(1));
                Assert.That(Volatile.Read(ref callbackHost), Is.EqualTo("final"));
                Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("final"));
                Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
            });

            File.WriteAllText(path, "{\"service\":{\"host\":\"after-stop\"}}");
            controller.OnFileChanged(controller, changed);
            controller.Stop();
            controller.Stop();
            Assert.That(controller.IsWatching(), Is.False);
            Thread.Sleep(500);

            Assert.Multiple(() =>
            {
                Assert.That(Volatile.Read(ref callbackCount), Is.EqualTo(1));
                Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("final"));
                Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
            });
        }
        finally
        {
            controller.Stop();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicReload_CustomSourcesSupportFreshKeysAndFreshSourceSelectionsWithoutChangingState()
    {
        PublicSequenceReloadSource source = new PublicSequenceReloadSource(0);
        IConfigReloadController controller = new ConfigBuilder()
            .AddSource(source)
            .BuildReloading();
        int callbackCount = 0;
        controller.OnChange(config =>
        {
            callbackCount = callbackCount + 1;
        });

        Config original = controller.Current();
        Config freshKey = controller.FreshKeys(new string[] { "version" });
        Config freshSource = controller.FreshSources(new int[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.That(original.GetInt("version"), Is.EqualTo(1));
            Assert.That(freshKey.GetInt("version"), Is.EqualTo(2));
            Assert.That(freshKey.Has("label"), Is.False);
            Assert.That(freshSource.GetInt("version"), Is.EqualTo(3));
            Assert.That(freshSource.GetString("label"), Is.EqualTo("version-3"));
            Assert.That(controller.Current().GetInt("version"), Is.EqualTo(1));
            Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
            Assert.That(controller.ReloadEventCount(), Is.EqualTo(0));
            Assert.That(callbackCount, Is.EqualTo(0));
            Assert.That(controller.SourceCount(), Is.EqualTo(1));
        });

        Config reloaded = controller.Reload();

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.GetInt("version"), Is.EqualTo(4));
            Assert.That(controller.Current().GetString("label"), Is.EqualTo("version-4"));
            Assert.That(source.LoadCount, Is.EqualTo(4));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void PublicReload_ConcurrentReadsObserveOnlyCompleteSnapshotsDuringReload()
    {
        PublicSequenceReloadSource source = new PublicSequenceReloadSource(150);
        IConfigReloadController controller = new ConfigBuilder()
            .AddSource(source)
            .BuildReloading();
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        int inconsistentReads = 0;
        Task<Config> reloadTask = Task.Factory.StartNew(
            () =>
            {
                start.Wait();
                return controller.Reload();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Task[] tasks = new Task[9];
        tasks[0] = reloadTask;

        int taskIndex = 1;
        while (taskIndex < tasks.Length)
        {
            tasks[taskIndex] = Task.Factory.StartNew(
                () =>
                {
                    start.Wait();
                    int readIndex = 0;
                    while (readIndex < 30)
                    {
                        Config snapshot = controller.Current();
                        int version = snapshot.GetInt("version");
                        string label = snapshot.GetString("label");
                        if ((version != 1 && version != 2) || label != "version-" + version.ToString())
                        {
                            Interlocked.Increment(ref inconsistentReads);
                        }

                        readIndex = readIndex + 1;
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            taskIndex = taskIndex + 1;
        }

        start.Set();
        bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True);
            Assert.That(inconsistentReads, Is.EqualTo(0));
            Assert.That(reloadTask.Result.GetInt("version"), Is.EqualTo(2));
            Assert.That(controller.Current().GetInt("version"), Is.EqualTo(2));
            Assert.That(controller.Current().GetString("label"), Is.EqualTo("version-2"));
            Assert.That(source.LoadCount, Is.EqualTo(2));
            Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
        });
    }

    private static string NewDirectory(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

}

public class PublicSequenceReloadSource : IConfigSource
{
    public int LoadCount;
    public int DelayAfterFirstMilliseconds;
    public object Gate;

    public PublicSequenceReloadSource(int delayAfterFirstMilliseconds)
    {
        LoadCount = 0;
        DelayAfterFirstMilliseconds = delayAfterFirstMilliseconds;
        Gate = new object();
    }

    public ConfigSourceResult Load(int precedenceRank)
    {
        int version = 0;
        lock (Gate)
        {
            LoadCount = LoadCount + 1;
            version = LoadCount;
        }

        if (version > 1 && DelayAfterFirstMilliseconds > 0)
        {
            Thread.Sleep(DelayAfterFirstMilliseconds);
        }

        ConfigValues values = new ConfigValues();
        ConfigMap map = new ConfigMap();
        map.Set("version", values.SetSource(values.Number(version), "sequence", precedenceRank, "version", 0, 0));
        map.Set("label", values.SetSource(values.String("version-" + version.ToString()), "sequence", precedenceRank, "label", 0, 0));
        ConfigValue root = values.SetSource(values.Object(map), "sequence", precedenceRank, "", 0, 0);
        return new ConfigSourceResult(root, new ConfigDiagnostics());
    }
}
