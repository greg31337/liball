using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogEnvironmentConfigurationTests
{
    [Test]
    public void Apply_OverridesScalarSettings()
    {
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("Application.Old")
            .MinimumLevel(LogLevel.Info)
            .RuntimeMetadata(true)
            .Activity(true)
            .AddSink(MemoryLogSink.Create());
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["log_name"] = "Application.Env";
        environment["log_level"] = "Error";
        environment["log_runtime_metadata"] = "false";
        environment["log_activity"] = "off";

        LogConfiguration returned = configuration.ApplyEnvironmentValues(environment);

        Assert.That(returned, Is.SameAs(configuration));
        Assert.That(configuration.LoggerName, Is.EqualTo("Application.Env"));
        Assert.That(configuration.ConfiguredLevelSwitch.GetLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(configuration.IncludeRuntimeMetadata, Is.False);
        Assert.That(configuration.IncludeActivity, Is.False);
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Apply_ReplacesSinksFromEnvironmentList()
    {
        string path = Path.Combine(Path.GetTempPath(), "log-env-configuration.log");
        LogConfiguration configuration = LogConfiguration.Create().AddSink(MemoryLogSink.Named("old"));
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_SINKS"] = "file, memory, null";
        environment["LOG_FILE_PATH"] = path;
        environment["LOG_FORMATTER"] = "json";
        environment["LOG_FILE_APPEND"] = "false";

        configuration.ApplyEnvironmentValues(environment);

        Assert.That(configuration.Sinks.Length, Is.EqualTo(3));
        Assert.That(configuration.Sinks[0].IsFileSink, Is.True);
        Assert.That(configuration.Sinks[0].FilePath, Is.EqualTo(path));
        Assert.That(configuration.Sinks[0].SinkFormatter.Name, Is.EqualTo(LogFormatter.JsonName));
        Assert.That(configuration.Sinks[0].FileAppend, Is.False);
        Assert.That(configuration.Sinks[1].IsMemorySink, Is.True);
        Assert.That(configuration.Sinks[2].IsNullSink, Is.True);
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Apply_FilePathAddsFileSinkWhenNoFileSinkExists()
    {
        string path = Path.Combine(Path.GetTempPath(), "log-env-added-file.log");
        LogConfiguration configuration = LogConfiguration.Create().AddSink(MemoryLogSink.Create());
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_FILE_PATH"] = path;
        environment["LOG_FORMATTER"] = "compact-json";

        configuration.ApplyEnvironmentValues(environment);

        Assert.That(configuration.Sinks.Length, Is.EqualTo(2));
        Assert.That(configuration.Sinks[1].IsFileSink, Is.True);
        Assert.That(configuration.Sinks[1].FilePath, Is.EqualTo(path));
        Assert.That(configuration.Sinks[1].SinkFormatter.Name, Is.EqualTo(LogFormatter.CompactJsonName));
    }

    [Test]
    public void Apply_ReplacesAndAddsGlobalFields()
    {
        LogConfiguration configuration = LogConfiguration.Create()
            .AddSink(MemoryLogSink.Create())
            .AddGlobalField("TenantId", "old");
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_FIELDS"] = "TenantId=tenant-2;Region=us";
        environment["LOG_FIELD__Build__Number"] = "42";

        configuration.ApplyEnvironmentValues(environment);

        Assert.That(configuration.GlobalFields.Length, Is.EqualTo(3));
        Assert.That(configuration.GlobalFields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(configuration.GlobalFields[0].Value.TextValue, Is.EqualTo("tenant-2"));
        Assert.That(configuration.GlobalFields[1].Name, Is.EqualTo("Region"));
        Assert.That(configuration.GlobalFields[1].Value.TextValue, Is.EqualTo("us"));
        Assert.That(configuration.GlobalFields[2].Name, Is.EqualTo("Build.Number"));
        Assert.That(configuration.GlobalFields[2].Value.TextValue, Is.EqualTo("42"));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Apply_OverridesCategoryLevels()
    {
        LogConfiguration configuration = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .CategoryLevel("Application.Noisy", LogLevel.Warn)
            .AddSink(MemoryLogSink.Create());
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_CATEGORY_LEVELS"] = "Application.Noisy=Fatal;Application.Worker=Debug";
        environment["LOG_CATEGORY_LEVEL__Application__Api"] = "Error";

        configuration.ApplyEnvironmentValues(environment);

        Assert.That(configuration.CategoryLevelSwitches["Application.Noisy"].GetLevel(), Is.EqualTo(LogLevel.Fatal));
        Assert.That(configuration.CategoryLevelSwitches["Application.Worker"].GetLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(configuration.CategoryLevelSwitches["Application.Api"].GetLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Apply_PopulatesDiagnosticsAfterOverrides()
    {
        LogConfiguration configuration = LogConfiguration.Create().AddSink(MemoryLogSink.Create());
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_SINKS"] = "file";

        configuration.ApplyEnvironmentValues(environment);

        Assert.That(configuration.Sinks[0].IsFileSink, Is.True);
        Assert.That(ContainsCode(configuration.LastDiagnostics, "sink.file.path.missing"), Is.True);
    }

    static bool ContainsCode(LogConfigurationDiagnostic[] diagnostics, string code)
    {
        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
