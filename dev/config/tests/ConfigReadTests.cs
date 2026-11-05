namespace config.Tests;

public class ConfigReadTests
{
    [Test]
    public void ReadMethods_ResolveAliasesAndUseConfiguredDelimiter()
    {
        ConfigMap values = new ConfigMap();
        values.SetPath("service__host", ConfigValue.String("localhost"), "__");
        values.SetPath("service__port", ConfigValue.String("5432"), "__");
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .SetDelimiter("__")
            .AddMemory(values, "memory")
            .AddAlias("old__host", "service__host");

        Config config = builder.Build();

        Assert.That(config.Has("service__host"), Is.True);
        Assert.That(config.Has("old__host"), Is.True);
        Assert.That(config.Has("missing__value"), Is.False);
        Assert.That(config.Value("old__host").StringValue, Is.EqualTo("localhost"));
        Assert.That(config.Value("old__host").SourceName, Is.EqualTo("memory"));
        Assert.That(config.Keys(), Is.EqualTo(new string[] { "service" }));
        Assert.That(config.Children("service"), Is.EqualTo(new string[] { "host", "port" }));
        Assert.That(config.Section("service").Get("host").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void ReadMethods_ReturnCopiesForValuesSectionsAndDiagnostics()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();

        ConfigValue value = config.Value("service.host");
        ConfigMap section = config.Section("service");
        ConfigDiagnostics diagnostics = config.Diagnostics();
        value.StringValue = "changed";
        section.Set("host", ConfigValue.String("changed"));
        diagnostics.AddError("test", "service.host", "mutated", 0, 0);

        Assert.That(config.Value("service.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(config.Section("service").Get("host").StringValue, Is.EqualTo("localhost"));
        Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadMethods_ReturnMissingAndEmptyCollectionsForAbsentPaths()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();

        Assert.That(config.Value("service.missing").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(config.Section("service.host").Count(), Is.EqualTo(0));
        Assert.That(config.Section("service.missing").Count(), Is.EqualTo(0));
        Assert.That(config.Children("service.host"), Is.EqualTo(new string[0]));
        Assert.That(config.Children("service.missing"), Is.EqualTo(new string[0]));
    }

    [Test]
    public void ReadMethods_HandleScalarRoot()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        diagnostics.AddWarning("scalar", "", "warning", 0, 0);
        Config config = new Config(ConfigValue.String("root").SetSource("scalar", 1, "", 0, 0), diagnostics, new ConfigAliasTable(ConfigCaseMode.Sensitive), ".", ConfigCaseMode.Sensitive, ConfigResolutionOptions.DefaultOptions());

        Assert.That(config.Has(""), Is.True);
        Assert.That(config.Has("child"), Is.False);
        Assert.That(config.Value("").StringValue, Is.EqualTo("root"));
        Assert.That(config.Value("child").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(config.Section("").Count(), Is.EqualTo(0));
        Assert.That(config.Keys(), Is.EqualTo(new string[0]));
        Assert.That(config.Children(""), Is.EqualTo(new string[0]));
        Assert.That(config.Diagnostics().WarningCount(), Is.EqualTo(1));
    }
}
