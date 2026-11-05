namespace config.Tests;

public class ConfigEnvironmentOptionsTests
{
    [Test]
    public void EnvironmentMapOptions_ApplyMappingsEmptyPolicyAndStableNameOrder()
    {
        ConfigMap values = new ConfigMap();
        values.Set("APP_B", ConfigValue.String("second"));
        values.Set("OUTSIDE_VALUE", ConfigValue.String("mapped"));
        values.Set("APP_EMPTY", ConfigValue.String(""));
        values.Set("APP_DATABASE__PORT", ConfigValue.String("5432"));
        values.Set("APP_A", ConfigValue.String("first"));
        ConfigMap pathMap = PathMap("APP_A", "APP_B", "APP_EMPTY", "OUTSIDE_VALUE");

        Config config = new ConfigBuilder()
            .AddEnvironmentMapWithOptions(values, "APP_", "__", ConfigEmptyEnvMode.Ignore, pathMap)
            .Build();

        AssertControlledValues(config);
    }

    [Test]
    public void EnvironmentDictionaryOptions_ApplyMappingsEmptyPolicyAndStableNameOrder()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP_B"] = "second";
        values["OUTSIDE_VALUE"] = "mapped";
        values["APP_EMPTY"] = "";
        values["APP_DATABASE__PORT"] = "5432";
        values["APP_A"] = "first";
        ConfigMap pathMap = PathMap("APP_A", "APP_B", "APP_EMPTY", "OUTSIDE_VALUE");

        Config config = new ConfigBuilder()
            .AddEnvironmentDictionaryWithOptions(values, "APP_", "__", ConfigEmptyEnvMode.Ignore, pathMap)
            .Build();

        AssertControlledValues(config);
    }

    [Test]
    public void ProcessEnvironmentOptions_ApplyMappingsEmptyPolicyAndStableNameOrder()
    {
        string id = Guid.NewGuid().ToString("N").ToUpperInvariant();
        string prefix = "CONFIG_T003_" + id + "_";
        string firstKey = prefix + "A";
        string secondKey = prefix + "B";
        string emptyKey = prefix + "EMPTY";
        string portKey = prefix + "DATABASE__PORT";
        string outsideKey = "CONFIG_T003_OUTSIDE_" + id;
        ConfigMap pathMap = PathMap(firstKey, secondKey, emptyKey, outsideKey);

        try
        {
            Environment.SetEnvironmentVariable(secondKey, "second");
            Environment.SetEnvironmentVariable(outsideKey, "mapped");
            Environment.SetEnvironmentVariable(emptyKey, "");
            Environment.SetEnvironmentVariable(portKey, "5432");
            Environment.SetEnvironmentVariable(firstKey, "first");

            Config config = new ConfigBuilder()
                .AddEnvironmentWithOptions(prefix, "__", ConfigEmptyEnvMode.Ignore, pathMap)
                .Build();

            AssertControlledValues(config);
        }
        finally
        {
            Environment.SetEnvironmentVariable(firstKey, null);
            Environment.SetEnvironmentVariable(secondKey, null);
            Environment.SetEnvironmentVariable(emptyKey, null);
            Environment.SetEnvironmentVariable(portKey, null);
            Environment.SetEnvironmentVariable(outsideKey, null);
        }
    }

    [Test]
    public void EnvironmentOptions_KeepEmptyValuesAsPresentStrings()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP_EMPTY"] = "";
        ConfigMap pathMap = new ConfigMap();
        pathMap.Set("APP_EMPTY", ConfigValue.String("service.empty"));

        Config config = new ConfigBuilder()
            .AddEnvironmentDictionaryWithOptions(values, "APP_", "__", ConfigEmptyEnvMode.Keep, pathMap)
            .Build();
        ConfigValue empty = config.Value("service.empty");

        Assert.That(empty.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(empty.StringValue, Is.EqualTo(""));
        Assert.That(empty.SourceName, Is.EqualTo("environment"));
        Assert.That(empty.OriginalKey, Is.EqualTo("service.empty"));
    }

    [Test]
    public void ProcessEnvironmentOptions_KeepEmptyValuesAsPresentStrings()
    {
        string key = "CONFIG_T003_EMPTY_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        ConfigMap pathMap = new ConfigMap();
        pathMap.Set(key, ConfigValue.String("service.empty"));

        try
        {
            Environment.SetEnvironmentVariable(key, "");
            Config config = new ConfigBuilder()
                .AddEnvironmentWithOptions("UNMATCHED_", "__", ConfigEmptyEnvMode.Keep, pathMap)
                .Build();
            ConfigValue empty = config.Value("service.empty");

            Assert.That(empty.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(empty.StringValue, Is.EqualTo(""));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    private static ConfigMap PathMap(string firstKey, string secondKey, string emptyKey, string outsideKey)
    {
        ConfigMap pathMap = new ConfigMap();
        pathMap.Set(firstKey, ConfigValue.String("service.host"));
        pathMap.Set(secondKey, ConfigValue.String("service.host"));
        pathMap.Set(emptyKey, ConfigValue.String("service.empty"));
        pathMap.Set(outsideKey, ConfigValue.String("service.external"));
        return pathMap;
    }

    private static void AssertControlledValues(Config config)
    {
        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(config.GetString("service.external"), Is.EqualTo("mapped"));
        Assert.That(config.GetString("DATABASE.PORT"), Is.EqualTo("5432"));
        Assert.That(config.Has("service.empty"), Is.False);
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("environment"));
        Assert.That(config.Value("service.host").OriginalKey, Is.EqualTo("service.host"));
    }
}
