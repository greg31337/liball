using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogConfigurationReloadTests
{
    [Test]
    public void Start_LoadsInitialConfigurationAndStartsWatcher()
    {
        string path = TempJsonPath("start");
        File.WriteAllText(path, "{ \"name\": \"Application.Reload\", \"minimumLevel\": \"Debug\", \"sinks\": [\"memory\"] }");
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path);

        LogResult result = reload.Start();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(reload.IsWatching, Is.True);
        Assert.That(reload.Watcher, Is.Not.Null);
        Assert.That(reload.Watcher!.EnableRaisingEvents, Is.True);
        Assert.That(reload.ReloadCount, Is.EqualTo(1));
        Assert.That(reload.LastConfiguration.LoggerName, Is.EqualTo("Application.Reload"));
        Assert.That(reload.LastLogger.Name, Is.EqualTo("Application.Reload"));
        Assert.That(reload.LastLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));

        reload.Stop();

        Assert.That(reload.IsWatching, Is.False);
        Assert.That(reload.Watcher, Is.Null);
    }

    [Test]
    public void Reload_UpdatesConfigurationAndInvokesCallbacks()
    {
        string path = TempJsonPath("manual");
        File.WriteAllText(path, "{ \"name\": \"Application.First\", \"sinks\": [\"memory\"] }");
        int configurationReloadedCount = 0;
        int loggerReloadedCount = 0;
        LogConfiguration? capturedConfiguration = null;
        Logger? capturedLogger = null;
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path)
            .OnConfigurationReloaded(configuration =>
            {
                configurationReloadedCount = configurationReloadedCount + 1;
                capturedConfiguration = configuration;
            })
            .OnLoggerReloaded(logger =>
            {
                loggerReloadedCount = loggerReloadedCount + 1;
                capturedLogger = logger;
            });

        reload.Reload();
        File.WriteAllText(path, "{ \"name\": \"Application.Second\", \"minimumLevel\": \"Error\", \"sinks\": [\"memory\"] }");
        LogResult result = reload.Reload();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(reload.ReloadCount, Is.EqualTo(2));
        Assert.That(configurationReloadedCount, Is.EqualTo(2));
        Assert.That(loggerReloadedCount, Is.EqualTo(2));
        Assert.That(reload.LastConfiguration.LoggerName, Is.EqualTo("Application.Second"));
        Assert.That(reload.LastLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(capturedConfiguration, Is.SameAs(reload.LastConfiguration));
        Assert.That(capturedLogger, Is.SameAs(reload.LastLogger));
    }

    [Test]
    public void OnChanged_ReloadsMatchingPathOnly()
    {
        string path = TempJsonPath("changed");
        string otherPath = TempJsonPath("other");
        File.WriteAllText(path, "{ \"name\": \"Application.Before\", \"sinks\": [\"memory\"] }");
        File.WriteAllText(otherPath, "{ \"name\": \"Application.Other\", \"sinks\": [\"memory\"] }");
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path);

        reload.Reload();
        File.WriteAllText(otherPath, "{ \"name\": \"Application.Ignored\", \"sinks\": [\"memory\"] }");
        reload.OnChanged(reload, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(otherPath)!, Path.GetFileName(otherPath)));
        File.WriteAllText(path, "{ \"name\": \"Application.After\", \"sinks\": [\"memory\"] }");
        reload.OnChanged(reload, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(path)!, Path.GetFileName(path)));

        Assert.That(reload.ReloadCount, Is.EqualTo(2));
        Assert.That(reload.LastConfiguration.LoggerName, Is.EqualTo("Application.After"));
    }

    [Test]
    public void Reload_AppliesEnvironmentOverridesWhenEnabled()
    {
        string path = TempJsonPath("environment");
        File.WriteAllText(path, "{ \"name\": \"Application.Json\", \"minimumLevel\": \"Info\", \"sinks\": [\"memory\"] }");
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_NAME"] = "Application.Environment";
        environment["LOG_LEVEL"] = "Fatal";
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path).Environment(environment);

        LogResult result = reload.Reload();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(reload.LastConfiguration.LoggerName, Is.EqualTo("Application.Environment"));
        Assert.That(reload.LastLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Fatal));
    }

    [Test]
    public void Start_ReturnsFailureWhenPathIsMissing()
    {
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile("");

        LogResult result = reload.Start();

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("configuration.reload.path.missing"));
        Assert.That(reload.FailureCount, Is.EqualTo(1));
        Assert.That(reload.IsWatching, Is.False);
    }

    [Test]
    public void JsonLoader_CreatesReloadWatcher()
    {
        string path = TempJsonPath("factory");

        LogConfigurationReload reload = LogJsonConfigurationLoader.WatchFileWithEnvironment(path);

        Assert.That(reload.FilePath, Is.EqualTo(path));
        Assert.That(reload.UseEnvironment, Is.True);
    }

    static string TempJsonPath(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-configuration-reload-" + name + "-" + Guid.NewGuid().ToString("N") + ".json");
    }
}
