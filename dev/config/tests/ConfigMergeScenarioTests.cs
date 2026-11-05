namespace config.Tests;

public class ConfigMergeScenarioTests
{
    [Test]
    public void MergeScenario_PrecedenceUsesOrderAndPreservesWinningMetadata()
    {
        ConfigMap lowerMap = new ConfigMap();
        lowerMap.SetPathDefault("service.host", ConfigValue.String("lower"));
        ConfigSourceResult lower = ConfigSourceLoader.Load(ConfigSource.Memory(lowerMap, "lower"), 100);

        ConfigMap higherMap = new ConfigMap();
        higherMap.SetPathDefault("service.host", ConfigValue.String("higher"));
        ConfigSourceResult higher = ConfigSourceLoader.Load(ConfigSource.Memory(higherMap, "higher"), 1);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { lower, higher });
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("service.host");

        Assert.That(host.StringValue, Is.EqualTo("higher"));
        Assert.That(host.SourceName, Is.EqualTo("higher"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Traces[0].Path, Is.EqualTo("service.host"));
        Assert.That(result.Diagnostics.Traces[0].SourceName, Is.EqualTo("higher"));
    }

    [Test]
    public void MergeScenario_DeepObjectsArraysAndNullsUseConfiguredPolicies()
    {
        ConfigArray baseServers = new ConfigArray();
        baseServers.Add(ConfigValue.String("base"));
        ConfigMap baseMap = new ConfigMap();
        baseMap.SetPathDefault("database.host", ConfigValue.String("base-host"));
        baseMap.SetPathDefault("database.port", ConfigValue.String("5432"));
        baseMap.SetPathDefault("database.servers", ConfigValue.Array(baseServers));
        baseMap.SetPathDefault("database.password", ConfigValue.String("secret"));
        ConfigSourceResult lower = ConfigSourceLoader.Load(ConfigSource.Memory(baseMap, "base"), 1);

        ConfigArray overrideServers = new ConfigArray();
        overrideServers.Add(ConfigValue.String("override"));
        ConfigMap overrideMap = new ConfigMap();
        overrideMap.SetPathDefault("database.host", ConfigValue.String("override-host"));
        overrideMap.SetPathDefault("database.servers", ConfigValue.Array(overrideServers));
        overrideMap.SetPathDefault("database.password", ConfigValue.Null());
        ConfigSourceResult higher = ConfigSourceLoader.Load(ConfigSource.Memory(overrideMap, "override"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithOptions(new ConfigSourceResult[] { lower, higher }, ConfigMergeMode.Override, ConfigArrayMergeMode.Append, ConfigNullMergeMode.Ignore);
        ConfigArray servers = result.Root.ObjectValue!.GetPathDefault("database.servers").ArrayValue!;

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("override-host"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.password").StringValue, Is.EqualTo("secret"));
        Assert.That(servers.Count(), Is.EqualTo(2));
        Assert.That(servers.Get(0).StringValue, Is.EqualTo("base"));
        Assert.That(servers.Get(1).StringValue, Is.EqualTo("override"));
    }

    [Test]
    public void MergeScenario_StrictConflictsReportDiagnosticsAndKeepExistingMetadata()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("service.port", ConfigValue.String("5432"));
        ConfigSourceResult first = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("service.port", ConfigValue.Number(5432m));
        ConfigSourceResult second = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { first, second }, ConfigMergeMode.Strict);
        ConfigValue port = result.Root.ObjectValue!.GetPathDefault("service.port");

        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("service.port"));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("second"));
        Assert.That(port.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(port.SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void MergeScenario_FallbackFillsMissingValuesWithoutReplacingExistingValues()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("service.host", ConfigValue.String("first"));
        ConfigSourceResult first = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("service.host", ConfigValue.String("second"));
        secondMap.SetPathDefault("service.port", ConfigValue.String("5432"));
        ConfigSourceResult second = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { first, second }, ConfigMergeMode.Fallback);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").SourceName, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").StringValue, Is.EqualTo("5432"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").SourceName, Is.EqualTo("second"));
    }
}
