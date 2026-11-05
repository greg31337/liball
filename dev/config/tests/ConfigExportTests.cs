namespace config.Tests;

public class ConfigExportTests
{
    [Test]
    public void ToJson_ExportsMergedObjectWithSortedKeysAndResolvedValues()
    {
        ConfigArray tags = new ConfigArray();
        tags.Add(ConfigValue.String("api"));
        tags.Add(ConfigValue.Number(2m));

        ConfigMap defaults = new ConfigMap();
        defaults.SetPathDefault("service.host", ConfigValue.String("base"));
        defaults.SetPathDefault("service.port", ConfigValue.Number(5000m));
        defaults.SetPathDefault("service.enabled", ConfigValue.Boolean(true));
        defaults.SetPathDefault("service.tags", ConfigValue.Array(tags));
        defaults.SetPathDefault("service.password", ConfigValue.Null());
        defaults.SetPathDefault("service.url", ConfigValue.String("http://${service.host}"));
        defaults.SetPathDefault("z", ConfigValue.Number(1m));

        ConfigMap overrides = new ConfigMap();
        overrides.SetPathDefault("service.host", ConfigValue.String("override"));

        Config config = Config.CreateBuilder()
            .AddMemory(defaults, "defaults")
            .AddMemory(overrides, "overrides")
            .Build();

        string json = config.ToJson();

        Assert.That(
            json,
            Is.EqualTo("{\"service\":{\"enabled\":true,\"host\":\"override\",\"password\":null,\"port\":5000,\"tags\":[\"api\",2],\"url\":\"http://override\"},\"z\":1}"));
    }

    [Test]
    public void ToJson_EscapesObjectKeysAndStringValues()
    {
        ConfigMap values = new ConfigMap();
        values.Set("quote\"slash\\", ConfigValue.String("line\nTabbed\t" + ((char)1).ToString()));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        string json = config.ToJson();

        Assert.That(json, Is.EqualTo("{\"quote\\\"slash\\\\\":\"line\\nTabbed\\t" + "\\u0001\"}"));
    }

    [Test]
    public void ToJsonValue_ExportsScalarValues()
    {
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.String("hello")), Is.EqualTo("\"hello\""));
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.Number(42.5m)), Is.EqualTo("42.5"));
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.Boolean(true)), Is.EqualTo("true"));
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.Boolean(false)), Is.EqualTo("false"));
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.Null()), Is.EqualTo("null"));
        Assert.That(ConfigExporter.ToJsonValue(ConfigValue.Missing()), Is.EqualTo("null"));
    }

    [Test]
    public void ToFlatMap_ExportsResolvedMergedLeafValuesAndArrayIndexes()
    {
        ConfigArray tags = new ConfigArray();
        tags.Add(ConfigValue.String("api"));
        tags.Add(ConfigValue.Number(2m));

        ConfigMap defaults = new ConfigMap();
        defaults.SetPathDefault("service.host", ConfigValue.String("base"));
        defaults.SetPathDefault("service.port", ConfigValue.Number(5000m));
        defaults.SetPathDefault("service.enabled", ConfigValue.Boolean(true));
        defaults.SetPathDefault("service.tags", ConfigValue.Array(tags));
        defaults.SetPathDefault("service.password", ConfigValue.Null());
        defaults.SetPathDefault("service.url", ConfigValue.String("http://${service.host}"));
        defaults.SetPathDefault("z", ConfigValue.Number(1m));

        ConfigMap overrides = new ConfigMap();
        overrides.SetPathDefault("service.host", ConfigValue.String("override"));

        Config config = Config.CreateBuilder()
            .AddMemory(defaults, "defaults")
            .AddMemory(overrides, "overrides")
            .Build();

        Dictionary<string, string> flat = config.ToFlatMap();

        Assert.That(flat.Count, Is.EqualTo(8));
        Assert.That(flat["service.enabled"], Is.EqualTo("true"));
        Assert.That(flat["service.host"], Is.EqualTo("override"));
        Assert.That(flat["service.password"], Is.EqualTo(""));
        Assert.That(flat["service.port"], Is.EqualTo("5000"));
        Assert.That(flat["service.tags.0"], Is.EqualTo("api"));
        Assert.That(flat["service.tags.1"], Is.EqualTo("2"));
        Assert.That(flat["service.url"], Is.EqualTo("http://override"));
        Assert.That(flat["z"], Is.EqualTo("1"));
    }

    [Test]
    public void ToFlatMap_UsesConfiguredDelimiterAndEscapesKeySegments()
    {
        ConfigMap values = new ConfigMap();
        values.SetPath("service__host", ConfigValue.String("localhost"), "__");
        values.Set("literal__key", ConfigValue.String("literal"));
        Config config = Config.CreateBuilder()
            .SetDelimiter("__")
            .AddMemory(values, "memory")
            .Build();

        Dictionary<string, string> flat = config.ToFlatMap();

        Assert.That(flat.Count, Is.EqualTo(2));
        Assert.That(flat["service__host"], Is.EqualTo("localhost"));
        Assert.That(flat["literal\\__key"], Is.EqualTo("literal"));
    }

    [Test]
    public void ToFlatMapValue_ExportsScalarRootAtEmptyPath()
    {
        Dictionary<string, string> flat = ConfigExporter.ToFlatMapValue(ConfigValue.String("root"), ".");

        Assert.That(flat.Count, Is.EqualTo(1));
        Assert.That(flat[""], Is.EqualTo("root"));
    }

    [Test]
    public void RedactedJsonExport_RedactsSecretLikePaths()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("database.host", ConfigValue.String("localhost"));
        values.SetPathDefault("database.password", ConfigValue.String("hunter2"));
        values.SetPathDefault("api.token", ConfigValue.String("abc123"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        ConfigRedactor redactor = new ConfigRedactor();

        string json = config.ToRedactedJson(redactor);

        Assert.That(json, Is.EqualTo("{\"api\":{\"token\":\"[redacted]\"},\"database\":{\"host\":\"localhost\",\"password\":\"[redacted]\"}}"));
    }

    [Test]
    public void RedactedFlatExport_RedactsSecretLikeLeafValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("database.host", ConfigValue.String("localhost"));
        values.SetPathDefault("database.password", ConfigValue.String("hunter2"));
        values.SetPathDefault("api.token", ConfigValue.String("abc123"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        ConfigRedactor redactor = new ConfigRedactor();

        Dictionary<string, string> flat = config.ToRedactedFlatMap(redactor);

        Assert.That(flat["api.token"], Is.EqualTo("[redacted]"));
        Assert.That(flat["database.host"], Is.EqualTo("localhost"));
        Assert.That(flat["database.password"], Is.EqualTo("[redacted]"));
    }

    [Test]
    public void WriteJson_CreatesNewFileAndDoesNotOverwriteExistingFile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "export-json-safe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.json");
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        config.WriteJson(path);
        IOException error = Assert.Throws<IOException>(() => config.WriteJson(path));

        Assert.That(error, Is.Not.Null);
        Assert.That(File.ReadAllText(path), Is.EqualTo("{\"service\":{\"host\":\"localhost\"}}"));
    }

    [Test]
    public void OverwriteJson_ReplacesExistingFile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "export-json-overwrite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.json");
        File.WriteAllText(path, "old");
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        config.OverwriteJson(path);

        Assert.That(File.ReadAllText(path), Is.EqualTo("{\"service\":{\"host\":\"localhost\"}}"));
    }

    [Test]
    public void WriteFlat_WritesDeterministicFlatSnapshot()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "export-flat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.flat");
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.port", ConfigValue.Number(5000m));
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        config.WriteFlat(path);

        Assert.That(File.ReadAllText(path), Is.EqualTo("service.host=\"localhost\"\nservice.port=\"5000\"\n"));
    }

    [Test]
    public void OverwriteFlat_ReplacesExistingFile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "export-flat-overwrite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.flat");
        File.WriteAllText(path, "old");
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("message", ConfigValue.String("hello\nthere"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        config.OverwriteFlat(path);

        Assert.That(File.ReadAllText(path), Is.EqualTo("message=\"hello\\nthere\"\n"));
    }
}
