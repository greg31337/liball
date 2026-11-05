namespace config.Tests;

public class ConfigMergeTests
{
    [Test]
    public void Merge_LaterObjectSourcesOverrideEarlierTopLevelKeys()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("host", ConfigValue.String("first").SetSource("first", 1, "host", 0, 0));
        firstMap.SetPathDefault("port", ConfigValue.String("5432").SetSource("first", 1, "port", 0, 0));
        ConfigSource firstSource = ConfigSource.Memory(firstMap, "first");
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(firstSource, 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("host", ConfigValue.String("second").SetSource("second", 2, "host", 0, 0));
        ConfigSource secondSource = ConfigSource.Memory(secondMap, "second");
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(secondSource, 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });
        ConfigMap merged = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(merged.GetPathDefault("host").StringValue, Is.EqualTo("second"));
        Assert.That(merged.GetPathDefault("host").SourceName, Is.EqualTo("second"));
        Assert.That(merged.GetPathDefault("host").PrecedenceRank, Is.EqualTo(2));
        Assert.That(merged.GetPathDefault("port").StringValue, Is.EqualTo("5432"));
        Assert.That(merged.GetPathDefault("port").SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_UsesInputOrderInsteadOfPrecedenceRank()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 100);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 1);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });

        Assert.That(result.Root.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("host").PrecedenceRank, Is.EqualTo(1));
    }

    [Test]
    public void Merge_AddsTraceForEachFinalValueSource()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("host", ConfigValue.String("first"));
        firstMap.SetPathDefault("port", ConfigValue.String("5432"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });

        Assert.That(result.Diagnostics.TraceCount(), Is.EqualTo(3));
        Assert.That(result.Diagnostics.Traces[0].Action, Is.EqualTo("merge"));
        Assert.That(result.Diagnostics.Traces[0].Path, Is.EqualTo("host"));
        Assert.That(result.Diagnostics.Traces[0].SourceName, Is.EqualTo("second"));
        Assert.That(result.Diagnostics.Traces[0].Decision, Is.EqualTo(ConfigMergeDecision.Replaced));
        Assert.That(result.Diagnostics.Traces[1].Message, Is.EqualTo("supplied final value"));
        Assert.That(result.Diagnostics.Traces[1].Decision, Is.EqualTo(ConfigMergeDecision.FinalSupplier));
        Assert.That(result.Diagnostics.Traces[2].Path, Is.EqualTo("port"));
        Assert.That(result.Diagnostics.Traces[2].SourceName, Is.EqualTo("first"));
        Assert.That(result.Diagnostics.Traces[2].Decision, Is.EqualTo(ConfigMergeDecision.FinalSupplier));
    }

    [Test]
    public void Merge_DeepObjectMergeKeepsEarlierNestedKeys()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        firstMap.SetPathDefault("database.port", ConfigValue.String("5432"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_ScalarValueReplacesEarlierObject()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database", ConfigValue.String("disabled"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });
        ConfigValue database = result.Root.ObjectValue!.GetPathDefault("database");

        Assert.That(database.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(database.StringValue, Is.EqualTo("disabled"));
        Assert.That(database.SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_ObjectValueReplacesEarlierScalar()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database", ConfigValue.String("disabled"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_StrictModeReportsScalarTypeConflictAndKeepsExistingValue()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.port", ConfigValue.String("5432"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.port", ConfigValue.Number(5433m));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Strict);
        ConfigValue port = result.Root.ObjectValue!.GetPathDefault("database.port");

        Assert.That(port.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(port.StringValue, Is.EqualTo("5432"));
        Assert.That(port.SourceName, Is.EqualTo("first"));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("second"));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("database.port"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("strict merge type conflict"));
    }

    [Test]
    public void Merge_StrictModeReportsObjectScalarConflictAndKeepsExistingObject()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("localhost"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database", ConfigValue.String("disabled"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Strict);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database").Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("database"));
    }

    [Test]
    public void Merge_StrictModeAllowsSameTypeReplacement()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Strict);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_FallbackModeKeepsEarlierScalarValues()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Fallback);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("first"));
        Assert.That(host.SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_FallbackModeFillsMissingNestedObjectValues()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        secondMap.SetPathDefault("database.port", ConfigValue.String("5432"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Fallback);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_FallbackModeKeepsExistingObjectWhenLaterSourceHasScalar()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database", ConfigValue.String("disabled"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Fallback);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database").Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_FallbackModeKeepsEarlierRoot()
    {
        ConfigSourceResult firstResult = new ConfigSourceResult(ConfigValue.String("first").SetSource("first", 1, "", 0, 0), new ConfigDiagnostics());
        ConfigSourceResult secondResult = new ConfigSourceResult(ConfigValue.String("second").SetSource("second", 2, "", 0, 0), new ConfigDiagnostics());

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Fallback);

        Assert.That(result.Root.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(result.Root.StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_FallbackModeTracesEarlierWinningSource()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("first"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.String("second"));
        secondMap.SetPathDefault("database.port", ConfigValue.String("5432"));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithMergeMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigMergeMode.Fallback);

        Assert.That(result.Diagnostics.TraceCount(), Is.EqualTo(3));
        Assert.That(result.Diagnostics.Traces[0].Path, Is.EqualTo("database.host"));
        Assert.That(result.Diagnostics.Traces[0].SourceName, Is.EqualTo("second"));
        Assert.That(result.Diagnostics.Traces[0].Decision, Is.EqualTo(ConfigMergeDecision.FallbackKept));
        Assert.That(result.Diagnostics.Traces[1].Path, Is.EqualTo("database.host"));
        Assert.That(result.Diagnostics.Traces[1].SourceName, Is.EqualTo("first"));
        Assert.That(result.Diagnostics.Traces[1].Decision, Is.EqualTo(ConfigMergeDecision.FinalSupplier));
        Assert.That(result.Diagnostics.Traces[2].Path, Is.EqualTo("database.port"));
        Assert.That(result.Diagnostics.Traces[2].SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_ReplacesArraysByDefault()
    {
        ConfigArray firstArray = new ConfigArray();
        firstArray.Add(ConfigValue.String("first"));
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("servers", ConfigValue.Array(firstArray));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigArray secondArray = new ConfigArray();
        secondArray.Add(ConfigValue.String("second"));
        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("servers", ConfigValue.Array(secondArray));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });
        ConfigArray servers = result.Root.ObjectValue!.GetPathDefault("servers").ArrayValue!;

        Assert.That(servers.Count(), Is.EqualTo(1));
        Assert.That(servers.Get(0).StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("servers").SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_AppendsArraysWhenConfigured()
    {
        ConfigArray firstArray = new ConfigArray();
        firstArray.Add(ConfigValue.String("first"));
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("servers", ConfigValue.Array(firstArray));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigArray secondArray = new ConfigArray();
        secondArray.Add(ConfigValue.String("second"));
        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("servers", ConfigValue.Array(secondArray));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithArrayMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigArrayMergeMode.Append);
        ConfigValue serversValue = result.Root.ObjectValue!.GetPathDefault("servers");
        ConfigArray servers = serversValue.ArrayValue!;

        Assert.That(servers.Count(), Is.EqualTo(2));
        Assert.That(servers.Get(0).StringValue, Is.EqualTo("first"));
        Assert.That(servers.Get(0).SourceName, Is.EqualTo("first"));
        Assert.That(servers.Get(1).StringValue, Is.EqualTo("second"));
        Assert.That(servers.Get(1).SourceName, Is.EqualTo("second"));
        Assert.That(serversValue.SourceName, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_AppendsNestedArraysWhenConfigured()
    {
        ConfigArray firstArray = new ConfigArray();
        firstArray.Add(ConfigValue.String("first"));
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.servers", ConfigValue.Array(firstArray));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigArray secondArray = new ConfigArray();
        secondArray.Add(ConfigValue.String("second"));
        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.servers", ConfigValue.Array(secondArray));
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithArrayMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigArrayMergeMode.Append);
        ConfigArray servers = result.Root.ObjectValue!.GetPathDefault("database.servers").ArrayValue!;

        Assert.That(servers.Count(), Is.EqualTo(2));
        Assert.That(servers.Get(0).StringValue, Is.EqualTo("first"));
        Assert.That(servers.Get(1).StringValue, Is.EqualTo("second"));
    }

    [Test]
    public void Merge_NullValuesReplaceExistingValuesByDefault()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("localhost"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.Null());
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(host.Kind, Is.EqualTo(ConfigValueKind.Null));
        Assert.That(host.SourceName, Is.EqualTo("second"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(2));
    }

    [Test]
    public void Merge_NullValuesCanBeIgnored()
    {
        ConfigMap firstMap = new ConfigMap();
        firstMap.SetPathDefault("database.host", ConfigValue.String("localhost"));
        ConfigSourceResult firstResult = ConfigSourceLoader.Load(ConfigSource.Memory(firstMap, "first"), 1);

        ConfigMap secondMap = new ConfigMap();
        secondMap.SetPathDefault("database.host", ConfigValue.Null());
        secondMap.SetPathDefault("database.port", ConfigValue.Null());
        ConfigSourceResult secondResult = ConfigSourceLoader.Load(ConfigSource.Memory(secondMap, "second"), 2);

        ConfigMergeResult result = ConfigMerger.MergeWithNullMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigNullMergeMode.Ignore);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.port").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Merge_NullRootCanBeIgnored()
    {
        ConfigSourceResult firstResult = new ConfigSourceResult(ConfigValue.String("first").SetSource("first", 1, "", 0, 0), new ConfigDiagnostics());
        ConfigSourceResult secondResult = new ConfigSourceResult(ConfigValue.Null().SetSource("second", 2, "", 0, 0), new ConfigDiagnostics());

        ConfigMergeResult result = ConfigMerger.MergeWithNullMode(new ConfigSourceResult[] { firstResult, secondResult }, ConfigNullMergeMode.Ignore);

        Assert.That(result.Root.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(result.Root.StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.SourceName, Is.EqualTo("first"));
    }

    [Test]
    public void Merge_AggregatesSourceDiagnostics()
    {
        ConfigSource missingFile = ConfigSource.File("missing-file-" + Guid.NewGuid().ToString("N") + ".json", ConfigFormat.Json, ConfigOptional.Required, ConfigReload.None, "missing-file");
        ConfigSourceResult missingResult = ConfigSourceLoader.Load(missingFile, 1);

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { missingResult });

        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required file"));
    }

    [Test]
    public void Merge_LaterScalarRootReplacesEarlierRoot()
    {
        ConfigSourceResult firstResult = new ConfigSourceResult(ConfigValue.String("first").SetSource("first", 1, "", 0, 0), new ConfigDiagnostics());
        ConfigSourceResult secondResult = new ConfigSourceResult(ConfigValue.String("second").SetSource("second", 2, "", 0, 0), new ConfigDiagnostics());

        ConfigMergeResult result = ConfigMerger.Merge(new ConfigSourceResult[] { firstResult, secondResult });

        Assert.That(result.Root.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(result.Root.StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.SourceName, Is.EqualTo("second"));
    }
}
