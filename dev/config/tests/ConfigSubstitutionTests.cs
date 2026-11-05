namespace config.Tests;

public class ConfigSubstitutionTests
{
    [Test]
    public void ValueAndStringReads_ResolveSimpleAndRepeatedPathSubstitutions()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        values.SetPathDefault("service.port", ConfigValue.Number(8080m));
        values.SetPathDefault("service.url", ConfigValue.String("http://${service.host}:${service.port}/api"));
        values.SetPathDefault("service.message", ConfigValue.String("${service.host}-${service.host}"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("service.url"), Is.EqualTo("http://localhost:8080/api"));
        Assert.That(config.Value("service.url").StringValue, Is.EqualTo("http://localhost:8080/api"));
        Assert.That(config.Value("service.url").SourceName, Is.EqualTo("memory"));
        Assert.That(config.GetString("service.message"), Is.EqualTo("localhost-localhost"));
        Assert.That(config.RawValue("service.url").StringValue, Is.EqualTo("http://${service.host}:${service.port}/api"));
    }

    [Test]
    public void TypedReads_UseResolvedSubstitutionValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("source.port", ConfigValue.String("5432"));
        values.SetPathDefault("source.timeout", ConfigValue.String("5s"));
        values.SetPathDefault("source.memory", ConfigValue.String("2kb"));
        values.SetPathDefault("reads.port", ConfigValue.String("${source.port}"));
        values.SetPathDefault("reads.timeout", ConfigValue.String("${source.timeout}"));
        values.SetPathDefault("reads.memory", ConfigValue.String("${source.memory}"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetInt("reads.port"), Is.EqualTo(5432));
        Assert.That(config.GetTimeSpan("reads.timeout"), Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(config.GetByteSize("reads.memory"), Is.EqualTo(2L * 1024L));
    }

    [Test]
    public void SubstitutionResolution_FollowsNestedSubstitutionsAndAliases()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        values.SetPathDefault("endpoint.base", ConfigValue.String("http://${old.host}"));
        values.SetPathDefault("endpoint.url", ConfigValue.String("${endpoint.base}/v1"));
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .AddMemory(values, "memory")
            .AddAlias("old.host", "service.host");

        Config config = builder.Build();

        Assert.That(config.GetString("endpoint.url"), Is.EqualTo("http://localhost/v1"));
    }

    [Test]
    public void SubstitutionResolution_ResolvesInsideArraysAndObjects()
    {
        ConfigArray array = new ConfigArray();
        array.Add(ConfigValue.String("${service.host}"));
        array.Add(ConfigValue.String("https://${service.host}"));
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        values.SetPathDefault("service.urls", ConfigValue.Array(array));
        values.SetPathDefault("service.section.url", ConfigValue.String("http://${service.host}"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();
        ConfigMap section = config.Section("service.section");

        Assert.That(config.GetStringArray("service.urls"), Is.EqualTo(new string[] { "localhost", "https://localhost" }));
        Assert.That(section.Get("url").StringValue, Is.EqualTo("http://localhost"));
    }

    [Test]
    public void SubstitutionResolution_ReplacesMissingReferencesWithEmptyTextAndLeavesUnclosedReferences()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.url", ConfigValue.String("http://${missing.host}/api ${unclosed"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("service.url"), Is.EqualTo("http:///api ${unclosed"));
    }

    [Test]
    public void SubstitutionResolution_OptionalMissingReferencesResolveToEmptyText()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        values.SetPathDefault("service.url", ConfigValue.String("http://${?missing.host}${?service.host}/api"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("service.url"), Is.EqualTo("http://localhost/api"));
    }

    [Test]
    public void SubstitutionResolution_OptionalReferencesCanReadEnvironmentVariables()
    {
        string key = "CONFIG_OPTIONAL_SUBSTITUTION_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "env-host");

        try
        {
            ConfigMap values = new ConfigMap();
            values.SetPathDefault("service.url", ConfigValue.String("http://${?" + key + "}/api"));
            ConfigBuilder builder = new ConfigBuilder();
            builder.AddMemory(values, "memory");

            Config config = builder.Build();

            Assert.That(config.GetString("service.url"), Is.EqualTo("http://env-host/api"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void SubstitutionResolution_FallsBackToEnvironmentVariablesForMissingConfigPaths()
    {
        string key = "CONFIG_SUBSTITUTION_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "env-host");

        try
        {
            ConfigMap values = new ConfigMap();
            values.SetPathDefault("service.url", ConfigValue.String("http://${" + key + "}/api"));
            ConfigBuilder builder = new ConfigBuilder();
            builder.AddMemory(values, "memory");

            Config config = builder.Build();

            Assert.That(config.GetString("service.url"), Is.EqualTo("http://env-host/api"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void SubstitutionResolution_ConfigValuesTakePrecedenceOverEnvironmentVariables()
    {
        string key = "CONFIG_SUBSTITUTION_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "env-host");

        try
        {
            ConfigMap values = new ConfigMap();
            values.SetPathDefault(key, ConfigValue.String("config-host"));
            values.SetPathDefault("service.url", ConfigValue.String("http://${" + key + "}/api"));
            ConfigBuilder builder = new ConfigBuilder();
            builder.AddMemory(values, "memory");

            Config config = builder.Build();

            Assert.That(config.GetString("service.url"), Is.EqualTo("http://config-host/api"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void TypedReads_UseEnvironmentSubstitutionValues()
    {
        string key = "CONFIG_SUBSTITUTION_SIZE_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "4kb");

        try
        {
            ConfigMap values = new ConfigMap();
            values.SetPathDefault("limits.memory", ConfigValue.String("${" + key + "}"));
            ConfigBuilder builder = new ConfigBuilder();
            builder.AddMemory(values, "memory");

            Config config = builder.Build();

            Assert.That(config.GetByteSize("limits.memory"), Is.EqualTo(4L * 1024L));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void SubstitutionResolution_NullConfigValuesDoNotFallBackToEnvironmentVariables()
    {
        string key = "CONFIG_SUBSTITUTION_NULL_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "env-host");

        try
        {
            ConfigMap values = new ConfigMap();
            values.SetPathDefault(key, ConfigValue.Null());
            values.SetPathDefault("service.url", ConfigValue.String("http://${" + key + "}/api"));
            ConfigBuilder builder = new ConfigBuilder();
            builder.AddMemory(values, "memory");

            Config config = builder.Build();

            Assert.That(config.GetString("service.url"), Is.EqualTo("http:///api"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void SubstitutionResolution_ReplacesSelfCyclesWithEmptyText()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("before-${service.host}-after"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("service.host"), Is.EqualTo("before--after"));
    }

    [Test]
    public void SubstitutionResolution_ReplacesLongerCyclesWithEmptyText()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("cycle.a", ConfigValue.String("${cycle.b}"));
        values.SetPathDefault("cycle.b", ConfigValue.String("${cycle.c}"));
        values.SetPathDefault("cycle.c", ConfigValue.String("${cycle.a}"));
        values.SetPathDefault("message", ConfigValue.String("x-${cycle.a}-y"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("cycle.a"), Is.EqualTo(""));
        Assert.That(config.GetString("message"), Is.EqualTo("x--y"));
    }

    [Test]
    public void SubstitutionResolution_StopsAtDepthLimit()
    {
        ConfigMap values = new ConfigMap();

        int index = 0;
        while (index < 40)
        {
            values.SetPathDefault("chain." + index.ToString(), ConfigValue.String("${chain." + (index + 1).ToString() + "}"));
            index = index + 1;
        }

        values.SetPathDefault("chain.40", ConfigValue.String("end"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("chain.0"), Is.EqualTo(""));
        Assert.That(config.GetString("chain.30"), Is.EqualTo("end"));
    }
}
