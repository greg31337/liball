using System;
using System.IO;

namespace config.Tests;

public class ConfigMergeTraceTests
{
    [Test]
    public void SourceGroupLabels_AnnotateConventionalSourcesAndRanks()
    {
        string path = Path.Combine(Path.GetTempPath(), "config-groups-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{\"service\":{\"value\":\"file\"}}");

        try
        {
            ConfigSourceGroupLabels labels = new ConfigSourceGroupLabels("base", "local", "process", "arguments", "explicit");
            ConfigMap defaults = new ConfigMap();
            defaults.SetPathDefault("defaults.only", ConfigValue.String("present"));
            defaults.SetPathDefault("service.value", ConfigValue.String("default"));
            ConfigMap environment = new ConfigMap();
            environment.Set("APP__service__value", ConfigValue.String("environment"));

            Config config = new ConfigBuilder()
                .SetSourceGroupLabels(labels)
                .AddDefaults(defaults)
                .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
                .AddEnvironmentMap(environment, "APP__", "__")
                .AddCommandLine(new string[] { "--service.value=command-line" })
                .AddOverride("service.value", "override")
                .Build();

            ConfigDiagnostics diagnostics = config.Diagnostics();
            AssertTrace(diagnostics, "service.value", ConfigMergeDecision.Replaced, 2, "local");
            AssertTrace(diagnostics, "service.value", ConfigMergeDecision.Replaced, 3, "process");
            AssertTrace(diagnostics, "service.value", ConfigMergeDecision.Replaced, 4, "arguments");
            AssertTrace(diagnostics, "service.value", ConfigMergeDecision.Replaced, 5, "explicit");
            AssertTrace(diagnostics, "defaults.only", ConfigMergeDecision.FinalSupplier, 1, "base");
            AssertTrace(diagnostics, "service.value", ConfigMergeDecision.FinalSupplier, 5, "explicit");
            Assert.That(config.GetString("service.value"), Is.EqualTo("override"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void SourceGroupLabels_AreOptionalAndCopiedByBuilder()
    {
        ConfigMap defaults = new ConfigMap();
        defaults.Set("value", ConfigValue.String("first"));
        ConfigSourceGroupLabels labels = new ConfigSourceGroupLabels("base", "", "", "", "");
        ConfigBuilder builder = new ConfigBuilder();
        builder.SetSourceGroupLabels(labels).AddDefaults(defaults);
        ConfigReloadController controller = builder.BuildReloading();
        labels.Defaults = "changed";
        builder.SetSourceGroupLabels(new ConfigSourceGroupLabels("builder-changed", "", "", "", ""));

        ConfigTrace configured = builder.Build().Diagnostics().Trace(0);
        ConfigTrace reloaded = controller.Reload().Diagnostics().Trace(0);
        ConfigTrace omitted = new ConfigBuilder().AddDefaults(defaults).Build().Diagnostics().Trace(0);

        Assert.That(configured.SourceGroup, Is.EqualTo("builder-changed"));
        Assert.That(reloaded.SourceGroup, Is.EqualTo("base"));
        Assert.That(omitted.SourceGroup, Is.Empty);
    }

    [Test]
    public void CustomSources_CanSupplyTheirOwnGroupLabel()
    {
        Config config = new ConfigBuilder().AddSource(new GroupedTraceSource()).Build();

        ConfigTrace trace = config.Diagnostics().Trace(0);

        Assert.That(trace.SourceName, Is.EqualTo("custom"));
        Assert.That(trace.SourceGroup, Is.EqualTo("plugin"));
        Assert.That(trace.PrecedenceRank, Is.EqualTo(1));
        Assert.That(trace.Decision, Is.EqualTo(ConfigMergeDecision.FinalSupplier));
    }

    [Test]
    public void FallbackAndStrictModes_RecordDecisionsAndFinalSuppliers()
    {
        ConfigMap first = new ConfigMap();
        first.Set("value", ConfigValue.String("first"));
        ConfigMap second = new ConfigMap();
        second.Set("value", ConfigValue.Number(2m));
        ConfigSourceGroupLabels labels = new ConfigSourceGroupLabels("base", "", "", "", "explicit");

        ConfigDiagnostics fallback = new ConfigBuilder()
            .SetSourceGroupLabels(labels)
            .SetMergeMode(ConfigMergeMode.Fallback)
            .AddDefaults(first)
            .AddOverride("value", "second")
            .BuildDiagnostics();
        ConfigDiagnostics strict = new ConfigBuilder()
            .SetSourceGroupLabels(labels)
            .SetMergeMode(ConfigMergeMode.Strict)
            .AddDefaults(first)
            .AddMemory(second, "second")
            .BuildDiagnostics();

        AssertTrace(fallback, "value", ConfigMergeDecision.FallbackKept, 2, "explicit");
        AssertTrace(fallback, "value", ConfigMergeDecision.FinalSupplier, 1, "base");
        AssertTrace(strict, "value", ConfigMergeDecision.StrictConflict, 2, "");
        AssertTrace(strict, "value", ConfigMergeDecision.FinalSupplier, 1, "base");
        Assert.That(strict.ErrorCount(), Is.EqualTo(1));
    }

    [Test]
    public void ArrayAppendAndIgnoredNull_RecordTheirMergeDecisions()
    {
        ConfigArray firstValues = new ConfigArray();
        firstValues.Add(ConfigValue.String("first"));
        ConfigArray secondValues = new ConfigArray();
        secondValues.Add(ConfigValue.String("second"));
        ConfigMap first = new ConfigMap();
        first.Set("items", ConfigValue.Array(firstValues));
        first.Set("kept", ConfigValue.String("value"));
        ConfigMap second = new ConfigMap();
        second.Set("items", ConfigValue.Array(secondValues));
        second.Set("kept", ConfigValue.Null());

        ConfigDiagnostics diagnostics = new ConfigBuilder()
            .SetArrayMergeMode(ConfigArrayMergeMode.Append)
            .SetNullMergeMode(ConfigNullMergeMode.Ignore)
            .AddMemory(first, "first")
            .AddMemory(second, "second")
            .BuildDiagnostics();

        AssertTrace(diagnostics, "items", ConfigMergeDecision.Appended, 2, "");
        AssertTrace(diagnostics, "kept", ConfigMergeDecision.Ignored, 2, "");
    }

    [Test]
    public void DiagnosticsCopies_PreserveMergeTraceDetails()
    {
        ConfigMap first = new ConfigMap();
        first.Set("value", ConfigValue.String("first"));
        ConfigMap second = new ConfigMap();
        second.Set("value", ConfigValue.String("second"));
        ConfigSourceGroupLabels labels = new ConfigSourceGroupLabels("", "", "", "", "");
        Config config = new ConfigBuilder()
            .SetSourceGroupLabels(labels)
            .AddMemory(first, "first")
            .AddMemory(second, "second")
            .Build();

        ConfigDiagnostics firstCopy = config.Diagnostics();
        ConfigDiagnostics secondCopy = config.Diagnostics();
        ConfigTrace trace = firstCopy.Trace(0);
        trace.SourceGroup = "mutated";
        trace.PrecedenceRank = 99;
        trace.Decision = ConfigMergeDecision.None;

        Assert.That(secondCopy.Trace(0).SourceGroup, Is.Empty);
        Assert.That(secondCopy.Trace(0).PrecedenceRank, Is.EqualTo(2));
        Assert.That(secondCopy.Trace(0).Decision, Is.EqualTo(ConfigMergeDecision.Replaced));
    }

    private static void AssertTrace(ConfigDiagnostics diagnostics, string path, ConfigMergeDecision decision, int precedenceRank, string sourceGroup)
    {
        int index = 0;
        while (index < diagnostics.TraceCount())
        {
            ConfigTrace trace = diagnostics.Trace(index);
            if (trace.Path == path && trace.Decision == decision && trace.PrecedenceRank == precedenceRank && trace.SourceGroup == sourceGroup)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected trace was not found for " + path + " at rank " + precedenceRank.ToString());
    }

    private class GroupedTraceSource : IConfigSource
    {
        public ConfigSourceResult Load(int precedenceRank)
        {
            ConfigMap values = new ConfigMap();
            values.Set("value", ConfigValue.String("custom").SetSource("custom", precedenceRank, "value", 0, 0));
            ConfigValue root = ConfigValue.Object(values).SetSource("custom", precedenceRank, "", 0, 0);
            ConfigSourceResult result = new ConfigSourceResult(root, new ConfigDiagnostics());
            result.SourceGroup = "plugin";
            return result;
        }
    }
}
