namespace config.Tests;

public class ConfigExceptionTests
{
    [Test]
    public void ThrowingEntryPoints_ReturnSuccessfulResults()
    {
        ConfigMap values = new ConfigMap();
        values.Set("port", ConfigValue.Number(8080m));
        ConfigBuilder builder = new ConfigBuilder().AddMemory(values, "memory");
        Config built = builder.BuildOrThrow();
        ConfigValidationResult validation = new ConfigValidator().RequireInt("port").ValidateOrThrow(built);
        ConfigReloadController controller = builder.BuildReloading();

        Config reloaded = controller.ReloadOrThrow();

        Assert.That(built.GetInt("port"), Is.EqualTo(8080));
        Assert.That(validation.IsValid, Is.True);
        Assert.That(reloaded.GetInt("port"), Is.EqualTo(8080));
        Assert.That(controller.LastDiagnostics().HasErrors(), Is.False);
    }

    [Test]
    public void BuildOrThrow_ClassifiesLoadFailuresAndBuildRetainsDiagnostics()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-load-" + Guid.NewGuid().ToString("N") + ".json");
        ConfigBuilder builder = new ConfigBuilder().AddJsonFile(path, ConfigOptional.Required, ConfigReload.None);

        ConfigLoadException? error = Assert.Throws<ConfigLoadException>(() => builder.BuildOrThrow());
        Config diagnosticConfig = builder.Build();

        Assert.That(error!.Message, Is.EqualTo("missing required file"));
        Assert.That(error.SourceName, Is.EqualTo(path));
        Assert.That(error.Path, Is.EqualTo(path));
        Assert.That(error.LineNumber, Is.Zero);
        Assert.That(error.ColumnNumber, Is.Zero);
        Assert.That(diagnosticConfig.Diagnostics().Error(0).Message, Is.EqualTo(error.Message));
    }

    [Test]
    public void BuildOrThrow_ClassifiesParseFailuresAndPreservesTextLocation()
    {
        IConfigBuilder builder = new ConfigBuilder().AddString("{\n  \"service\":", ConfigFormat.Json, "settings.json");

        ConfigParseException? error = Assert.Throws<ConfigParseException>(() => builder.BuildOrThrow());

        Assert.That(error!.Message, Is.EqualTo("malformed JSON input"));
        Assert.That(error.SourceName, Is.EqualTo("settings.json"));
        Assert.That(error.Path, Is.Empty);
        Assert.That(error.LineNumber, Is.EqualTo(2));
        Assert.That(error.ColumnNumber, Is.GreaterThan(0));
    }

    [Test]
    public void BuildOrThrow_ClassifiesMergeFailuresAndPreservesIncomingContext()
    {
        ConfigMap first = new ConfigMap();
        first.Set("value", ConfigValue.String("first"));
        ConfigMap second = new ConfigMap();
        second.Set("value", ConfigValue.Number(2m));
        ConfigBuilder builder = new ConfigBuilder()
            .SetMergeMode(ConfigMergeMode.Strict)
            .AddMemory(first, "first")
            .AddMemory(second, "second");

        ConfigMergeException? error = Assert.Throws<ConfigMergeException>(() => builder.BuildOrThrow());

        Assert.That(error!.Message, Is.EqualTo("strict merge type conflict"));
        Assert.That(error.SourceName, Is.EqualTo("second"));
        Assert.That(error.Path, Is.EqualTo("value"));
        Assert.That(error.LineNumber, Is.Zero);
        Assert.That(error.ColumnNumber, Is.Zero);
        Assert.That(builder.BuildDiagnostics().Error(0).Message, Is.EqualTo(error.Message));
    }

    [Test]
    public void RequiredRead_ThrowsConversionExceptionWithSourceLocation()
    {
        IConfig config = new ConfigBuilder()
            .AddString("service.port=not-an-int\n", ConfigFormat.Flat, "settings.flat")
            .Build();

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.RequireInt("service.port"));
        ConfigReadResult result = config.ReadInt("service.port");

        Assert.That(error!.Message, Does.Contain("value cannot be converted to 32-bit integer"));
        Assert.That(error.SourceName, Is.EqualTo("settings.flat"));
        Assert.That(error.Path, Is.EqualTo("service.port"));
        Assert.That(error.LineNumber, Is.EqualTo(1));
        Assert.That(error.ColumnNumber, Is.EqualTo(1));
        Assert.That(result.Status, Is.EqualTo(ConfigReadStatus.Malformed));
    }

    [Test]
    public void Binding_ThrowsConversionExceptionWithValueLocation()
    {
        Config config = new ConfigBuilder()
            .AddString("Port=not-an-int\n", ConfigFormat.Flat, "binding.flat")
            .Build();
        ExceptionBindingTarget target = new ExceptionBindingTarget();

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.BindInstance(target));

        Assert.That(error!.SourceName, Is.EqualTo("binding.flat"));
        Assert.That(error.Path, Is.EqualTo("Port"));
        Assert.That(error.LineNumber, Is.EqualTo(1));
        Assert.That(error.ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void ValidateOrThrow_ThrowsValidationExceptionAndValidateRetainsResult()
    {
        Config config = new ConfigBuilder()
            .AddString("service.port=not-a-number\n", ConfigFormat.Flat, "validation.flat")
            .Build();
        IConfigValidator validator = new ConfigValidator().RequireInt("service.port");

        ConfigValidationException? error = Assert.Throws<ConfigValidationException>(() => validator.ValidateOrThrow(config));
        ConfigValidationResult result = validator.Validate(config);

        Assert.That(error!.Message, Is.EqualTo("expected integer value"));
        Assert.That(error.SourceName, Is.EqualTo("validation.flat"));
        Assert.That(error.Path, Is.EqualTo("service.port"));
        Assert.That(error.LineNumber, Is.EqualTo(1));
        Assert.That(error.ColumnNumber, Is.EqualTo(1));
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorCount, Is.EqualTo(1));
    }

    [Test]
    public void ReloadOrThrow_ThrowsReloadExceptionAndPreservesActiveSnapshot()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reload-exception-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{ \"service\": { \"host\": \"first\" } }");
        IConfigReloadController controller = new ConfigBuilder()
            .AddJsonFile(path, ConfigOptional.Required, ConfigReload.None)
            .BuildReloading();

        try
        {
            File.Delete(path);

            ConfigReloadException? error = Assert.Throws<ConfigReloadException>(() => controller.ReloadOrThrow());

            Assert.That(error!.Message, Is.EqualTo("missing required file"));
            Assert.That(error.SourceName, Is.EqualTo(path));
            Assert.That(error.Path, Is.EqualTo(path));
            Assert.That(error.LineNumber, Is.Zero);
            Assert.That(error.ColumnNumber, Is.Zero);
            Assert.That(controller.Current().GetString("service.host"), Is.EqualTo("first"));
            Assert.That(controller.LastDiagnostics().Error(0).Message, Is.EqualTo(error.Message));
            Assert.That(controller.ReloadEventCount(), Is.EqualTo(1));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}

public class ExceptionBindingTarget
{
    public int Port;
}
