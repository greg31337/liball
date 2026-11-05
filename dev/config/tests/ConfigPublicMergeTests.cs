using System;
using System.Collections.Generic;
using System.IO;

namespace config.Tests;

public class ConfigPublicMergeTests
{
    [Test]
    public void PublicMerge_UsesRegistrationPrecedenceAndPreservesDeepValueMetadata()
    {
        IConfig config = new ConfigBuilder()
            .AddString("service.host=base\nservice.port=5000", ConfigFormat.Flat, "base.flat")
            .AddString("\n  service.host=override\nservice.enabled=true", ConfigFormat.Flat, "override.flat")
            .Build();

        ConfigValue host = config.Value("service.host");
        ConfigValue port = config.Value("service.port");
        ConfigValue enabled = config.Value("service.enabled");

        Assert.Multiple(() =>
        {
            Assert.That(host.StringValue, Is.EqualTo("override"));
            Assert.That(host.SourceName, Is.EqualTo("override.flat"));
            Assert.That(host.PrecedenceRank, Is.EqualTo(2));
            Assert.That(host.OriginalKey, Is.EqualTo("service.host"));
            Assert.That(host.LineNumber, Is.EqualTo(2));
            Assert.That(host.ColumnNumber, Is.EqualTo(3));
            Assert.That(port.StringValue, Is.EqualTo("5000"));
            Assert.That(port.SourceName, Is.EqualTo("base.flat"));
            Assert.That(port.PrecedenceRank, Is.EqualTo(1));
            Assert.That(port.OriginalKey, Is.EqualTo("service.port"));
            Assert.That(port.LineNumber, Is.EqualTo(2));
            Assert.That(port.ColumnNumber, Is.EqualTo(1));
            Assert.That(enabled.StringValue, Is.EqualTo("true"));
            Assert.That(enabled.SourceName, Is.EqualTo("override.flat"));
        });
    }

    [Test]
    public void PublicMerge_AppliesReplaceAndIgnoreNullPolicies()
    {
        IConfig replaced = new ConfigBuilder()
            .SetNullMergeMode(ConfigNullMergeMode.Replace)
            .AddString("{\"value\":\"base\"}", ConfigFormat.Json, "base.json")
            .AddString("{\"value\":null}", ConfigFormat.Json, "null.json")
            .Build();
        IConfig ignored = new ConfigBuilder()
            .SetNullMergeMode(ConfigNullMergeMode.Ignore)
            .AddString("{\"value\":\"base\"}", ConfigFormat.Json, "base.json")
            .AddString("{\"value\":null}", ConfigFormat.Json, "null.json")
            .Build();

        ConfigValue replacedValue = replaced.Value("value");
        ConfigValue ignoredValue = ignored.Value("value");

        Assert.Multiple(() =>
        {
            Assert.That(replacedValue.Kind, Is.EqualTo(ConfigValueKind.Null));
            Assert.That(replacedValue.SourceName, Is.EqualTo("null.json"));
            Assert.That(replacedValue.PrecedenceRank, Is.EqualTo(2));
            Assert.That(ignoredValue.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(ignoredValue.StringValue, Is.EqualTo("base"));
            Assert.That(ignoredValue.SourceName, Is.EqualTo("base.json"));
            Assert.That(ignoredValue.PrecedenceRank, Is.EqualTo(1));
        });

        AssertMergeTrace(replaced.Diagnostics(), "value", "null.json", 2, ConfigMergeDecision.Replaced, "");
        AssertMergeTrace(ignored.Diagnostics(), "value", "null.json", 2, ConfigMergeDecision.Ignored, "");
        AssertMergeTrace(ignored.Diagnostics(), "value", "base.json", 1, ConfigMergeDecision.FinalSupplier, "");
    }

    [Test]
    public void PublicMerge_ReplacesOrAppendsArraysAndPreservesItemSuppliers()
    {
        IConfig replaced = BuildArrayConfig(ConfigArrayMergeMode.Replace);
        IConfig appended = BuildArrayConfig(ConfigArrayMergeMode.Append);

        ConfigArray replacedItems = replaced.Value("items").ArrayValue!;
        ConfigValue appendedValue = appended.Value("items");
        ConfigArray appendedItems = appendedValue.ArrayValue!;

        Assert.Multiple(() =>
        {
            Assert.That(replacedItems.Count(), Is.EqualTo(1));
            Assert.That(replacedItems.Get(0).StringValue, Is.EqualTo("override"));
            Assert.That(replacedItems.Get(0).SourceName, Is.EqualTo("override.json"));
            Assert.That(appendedItems.Count(), Is.EqualTo(2));
            Assert.That(appendedItems.Get(0).StringValue, Is.EqualTo("base"));
            Assert.That(appendedItems.Get(0).SourceName, Is.EqualTo("base.json"));
            Assert.That(appendedItems.Get(0).PrecedenceRank, Is.EqualTo(1));
            Assert.That(appendedItems.Get(1).StringValue, Is.EqualTo("override"));
            Assert.That(appendedItems.Get(1).SourceName, Is.EqualTo("override.json"));
            Assert.That(appendedItems.Get(1).PrecedenceRank, Is.EqualTo(2));
            Assert.That(appendedValue.SourceName, Is.EqualTo("override.json"));
            Assert.That(appendedValue.PrecedenceRank, Is.EqualTo(2));
        });

        AssertMergeTrace(replaced.Diagnostics(), "items", "override.json", 2, ConfigMergeDecision.Replaced, "");
        AssertMergeTrace(appended.Diagnostics(), "items", "override.json", 2, ConfigMergeDecision.Appended, "");
    }

    [Test]
    public void PublicMerge_StrictConflictsKeepExistingValuesAndSupportThrowingBuilds()
    {
        ConfigBuilder builder = new ConfigBuilder()
            .SetMergeMode(ConfigMergeMode.Strict)
            .AddString("{\"service\":{\"port\":\"5000\"}}", ConfigFormat.Json, "base.json")
            .AddString("{\"service\":{\"port\":6000}}", ConfigFormat.Json, "override.json");

        IConfig config = builder.Build();
        ConfigDiagnostics diagnostics = config.Diagnostics();
        ConfigError error = diagnostics.Error(0);
        ConfigValue port = config.Value("service.port");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
            Assert.That(error.SourceName, Is.EqualTo("override.json"));
            Assert.That(error.Path, Is.EqualTo("service.port"));
            Assert.That(error.Message, Does.Contain("strict merge type conflict"));
            Assert.That(port.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(port.StringValue, Is.EqualTo("5000"));
            Assert.That(port.SourceName, Is.EqualTo("base.json"));
        });

        AssertMergeTrace(diagnostics, "service.port", "override.json", 2, ConfigMergeDecision.StrictConflict, "");
        ConfigMergeException? exception = Assert.Throws<ConfigMergeException>(() => builder.BuildOrThrow());
        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.SourceName, Is.EqualTo("override.json"));
            Assert.That(exception.Path, Is.EqualTo("service.port"));
        });
    }

    [Test]
    public void PublicMerge_FallbackKeepsExistingValuesAndFillsMissingDeepValues()
    {
        IConfig config = new ConfigBuilder()
            .SetMergeMode(ConfigMergeMode.Fallback)
            .AddString("{\"service\":{\"host\":\"base\",\"nested\":{\"first\":\"one\"}}}", ConfigFormat.Json, "base.json")
            .AddString("{\"service\":{\"host\":\"fallback\",\"port\":5000,\"nested\":{\"second\":\"two\"}}}", ConfigFormat.Json, "fallback.json")
            .Build();

        ConfigValue host = config.Value("service.host");
        ConfigValue port = config.Value("service.port");

        Assert.Multiple(() =>
        {
            Assert.That(host.StringValue, Is.EqualTo("base"));
            Assert.That(host.SourceName, Is.EqualTo("base.json"));
            Assert.That(port.NumberValue, Is.EqualTo(5000m));
            Assert.That(port.SourceName, Is.EqualTo("fallback.json"));
            Assert.That(config.GetString("service.nested.first"), Is.EqualTo("one"));
            Assert.That(config.GetString("service.nested.second"), Is.EqualTo("two"));
        });

        ConfigDiagnostics diagnostics = config.Diagnostics();
        AssertMergeTrace(diagnostics, "service.host", "fallback.json", 2, ConfigMergeDecision.FallbackKept, "");
        AssertMergeTrace(diagnostics, "service.host", "base.json", 1, ConfigMergeDecision.FinalSupplier, "");
        AssertMergeTrace(diagnostics, "service.port", "fallback.json", 2, ConfigMergeDecision.FinalSupplier, "");
    }

    [Test]
    public void PublicMerge_ReportsEverySourceDecisionWithConfiguredGroups()
    {
        string path = Path.Combine(Path.GetTempPath(), "config-public-merge-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{\"value\":\"file\"}");

        try
        {
            ConfigValues values = new ConfigValues();
            ConfigMap defaults = new ConfigMap();
            defaults.Set("value", values.String("default"));
            defaults.Set("defaults-only", values.String("present"));
            Dictionary<string, string> environment = new Dictionary<string, string>();
            environment.Add("APP__value", "environment");
            ConfigSourceGroupLabels groups = new ConfigSourceGroupLabels("defaults-group", "files-group", "environment-group", "command-line-group", "overrides-group");

            IConfig config = new ConfigBuilder()
                .SetSourceGroupLabels(groups)
                .AddDefaults(defaults)
                .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
                .AddEnvironmentDictionary(environment, "APP__", "__")
                .AddCommandLine(new string[] { "--value=command-line" })
                .AddOverride("value", "override")
                .Build();
            ConfigDiagnostics diagnostics = config.Diagnostics();

            Assert.Multiple(() =>
            {
                Assert.That(config.GetString("value"), Is.EqualTo("override"));
                Assert.That(diagnostics.TraceCount(), Is.EqualTo(6));
            });

            AssertTraceAt(diagnostics, 0, "value", path, 2, ConfigMergeDecision.Replaced, "files-group");
            AssertTraceAt(diagnostics, 1, "value", "environment", 3, ConfigMergeDecision.Replaced, "environment-group");
            AssertTraceAt(diagnostics, 2, "value", "command-line", 4, ConfigMergeDecision.Replaced, "command-line-group");
            AssertTraceAt(diagnostics, 3, "value", "override", 5, ConfigMergeDecision.Replaced, "overrides-group");
            AssertTraceAt(diagnostics, 4, "defaults-only", "defaults", 1, ConfigMergeDecision.FinalSupplier, "defaults-group");
            AssertTraceAt(diagnostics, 5, "value", "override", 5, ConfigMergeDecision.FinalSupplier, "overrides-group");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void PublicMerge_AppliesPoliciesToNonObjectRoots()
    {
        IConfig overridden = new ConfigBuilder()
            .AddString("\"base\"", ConfigFormat.Json, "base.json")
            .AddString("42", ConfigFormat.Json, "override.json")
            .Build();
        IConfig fallback = new ConfigBuilder()
            .SetMergeMode(ConfigMergeMode.Fallback)
            .AddString("\"base\"", ConfigFormat.Json, "base.json")
            .AddString("42", ConfigFormat.Json, "fallback.json")
            .Build();
        IConfig appended = new ConfigBuilder()
            .SetArrayMergeMode(ConfigArrayMergeMode.Append)
            .AddString("[\"base\"]", ConfigFormat.Json, "base.json")
            .AddString("[\"override\"]", ConfigFormat.Json, "override.json")
            .Build();
        IConfig ignoredNull = new ConfigBuilder()
            .SetNullMergeMode(ConfigNullMergeMode.Ignore)
            .AddString("\"base\"", ConfigFormat.Json, "base.json")
            .AddString("null", ConfigFormat.Json, "null.json")
            .Build();

        ConfigValue overriddenRoot = overridden.Value("");
        ConfigValue fallbackRoot = fallback.Value("");
        ConfigValue appendedRoot = appended.Value("");
        ConfigValue ignoredNullRoot = ignoredNull.Value("");

        Assert.Multiple(() =>
        {
            Assert.That(overriddenRoot.Kind, Is.EqualTo(ConfigValueKind.Number));
            Assert.That(overriddenRoot.NumberValue, Is.EqualTo(42m));
            Assert.That(overriddenRoot.SourceName, Is.EqualTo("override.json"));
            Assert.That(overriddenRoot.PrecedenceRank, Is.EqualTo(2));
            Assert.That(fallbackRoot.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(fallbackRoot.StringValue, Is.EqualTo("base"));
            Assert.That(fallbackRoot.SourceName, Is.EqualTo("base.json"));
            Assert.That(appendedRoot.Kind, Is.EqualTo(ConfigValueKind.Array));
            Assert.That(appendedRoot.ArrayValue!.Count(), Is.EqualTo(2));
            Assert.That(appendedRoot.ArrayValue.Get(0).StringValue, Is.EqualTo("base"));
            Assert.That(appendedRoot.ArrayValue.Get(1).StringValue, Is.EqualTo("override"));
            Assert.That(appendedRoot.SourceName, Is.EqualTo("override.json"));
            Assert.That(ignoredNullRoot.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(ignoredNullRoot.StringValue, Is.EqualTo("base"));
        });

        AssertMergeTrace(overridden.Diagnostics(), "", "override.json", 2, ConfigMergeDecision.Replaced, "");
        AssertMergeTrace(fallback.Diagnostics(), "", "fallback.json", 2, ConfigMergeDecision.FallbackKept, "");
        AssertMergeTrace(appended.Diagnostics(), "", "override.json", 2, ConfigMergeDecision.Appended, "");
        AssertMergeTrace(ignoredNull.Diagnostics(), "", "null.json", 2, ConfigMergeDecision.Ignored, "");
    }

    private static IConfig BuildArrayConfig(ConfigArrayMergeMode mode)
    {
        return new ConfigBuilder()
            .SetArrayMergeMode(mode)
            .AddString("{\"items\":[\"base\"]}", ConfigFormat.Json, "base.json")
            .AddString("{\"items\":[\"override\"]}", ConfigFormat.Json, "override.json")
            .Build();
    }

    private static void AssertMergeTrace(ConfigDiagnostics diagnostics, string path, string sourceName, int precedenceRank, ConfigMergeDecision decision, string sourceGroup)
    {
        int index = 0;
        while (index < diagnostics.TraceCount())
        {
            ConfigTrace trace = diagnostics.Trace(index);
            if (trace.Action == "merge" && trace.Path == path && trace.SourceName == sourceName && trace.PrecedenceRank == precedenceRank && trace.Decision == decision && trace.SourceGroup == sourceGroup)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected merge trace for " + sourceName + " at " + path + " with decision " + decision.ToString());
    }

    private static void AssertTraceAt(ConfigDiagnostics diagnostics, int index, string path, string sourceName, int precedenceRank, ConfigMergeDecision decision, string sourceGroup)
    {
        ConfigTrace trace = diagnostics.Trace(index);
        Assert.Multiple(() =>
        {
            Assert.That(trace.Action, Is.EqualTo("merge"));
            Assert.That(trace.Path, Is.EqualTo(path));
            Assert.That(trace.SourceName, Is.EqualTo(sourceName));
            Assert.That(trace.PrecedenceRank, Is.EqualTo(precedenceRank));
            Assert.That(trace.Decision, Is.EqualTo(decision));
            Assert.That(trace.SourceGroup, Is.EqualTo(sourceGroup));
        });
    }
}
