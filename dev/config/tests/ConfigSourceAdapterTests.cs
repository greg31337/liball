namespace config.Tests;

public class ConfigSourceAdapterTests
{
    [Test]
    public void MemoryAdapter_LoadsCopiedValuesWithMetadata()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("app.name", ConfigValue.String("demo"));
        ConfigSource source = ConfigSource.Memory(values, "memory-adapter");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 1);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("app.name").StringValue, Is.EqualTo("demo"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("app.name").SourceName, Is.EqualTo("memory-adapter"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("app.name").PrecedenceRank, Is.EqualTo(1));
        Assert.That(values.GetPathDefault("app.name").SourceName, Is.Null);
    }

    [Test]
    public void FileAdapter_LoadsRequiredFileAndOptionalMissingFile()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "adapter-file-" + Guid.NewGuid().ToString("N") + ".json");
        string missingPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "adapter-missing-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{ \"app\": { \"name\": \"demo\" } }");
        ConfigSource fileSource = ConfigSource.FileByExtension(path, ConfigOptional.Required, ConfigReload.None, "file-adapter");
        ConfigSource optionalSource = ConfigSource.File(missingPath, ConfigFormat.Json, ConfigOptional.Optional, ConfigReload.None, "optional-file-adapter");

        ConfigSourceResult fileResult = ConfigSourceLoader.Load(fileSource, 2);
        ConfigSourceResult optionalResult = ConfigSourceLoader.Load(optionalSource, 2);

        Assert.That(fileResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(fileResult.Root.ObjectValue!.GetPathDefault("app.name").StringValue, Is.EqualTo("demo"));
        Assert.That(optionalResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(optionalResult.Root.ObjectValue!.Count(), Is.EqualTo(0));
    }

    [Test]
    public void DirectoryAndKeyPerFileAdapters_LoadDirectoryContents()
    {
        string configDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "adapter-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(Path.Combine(configDirectory, "a.json"), "{ \"app\": { \"name\": \"first\", \"port\": 5000 } }");
        File.WriteAllText(Path.Combine(configDirectory, "b.json"), "{ \"app\": { \"name\": \"second\" } }");
        File.WriteAllText(Path.Combine(configDirectory, "ignored.ini"), "app.name=ignored\n");
        ConfigSource directorySource = ConfigSource.DirectorySource(configDirectory, "*.json", ConfigOptional.Required, "directory-adapter");

        string secretsDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "adapter-secrets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(secretsDirectory);
        File.WriteAllText(Path.Combine(secretsDirectory, "app.secret"), "secret");
        ConfigSource keyPerFileSource = ConfigSource.KeyPerFileDirectory(secretsDirectory, ConfigOptional.Required, "key-per-file-adapter");

        ConfigSourceResult directoryResult = ConfigSourceLoader.Load(directorySource, 3);
        ConfigSourceResult keyPerFileResult = ConfigSourceLoader.Load(keyPerFileSource, 4);

        Assert.That(directoryResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(directoryResult.Root.ObjectValue!.GetPathDefault("app.name").StringValue, Is.EqualTo("second"));
        Assert.That(directoryResult.Root.ObjectValue!.GetPathDefault("app.port").NumberValue, Is.EqualTo(5000m));
        Assert.That(directoryResult.Root.ObjectValue!.GetPathDefault("ignored").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(keyPerFileResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(keyPerFileResult.Root.ObjectValue!.GetPathDefault("app.secret").StringValue, Is.EqualTo("secret"));
    }

    [Test]
    public void EnvironmentAndCommandLineAdapters_LoadMappedValues()
    {
        Dictionary<string, string> envValues = new Dictionary<string, string>();
        envValues["APP__HOST"] = "localhost";
        envValues["DATABASE_URL"] = "postgres://localhost";
        ConfigMap envPathMap = new ConfigMap();
        envPathMap.Set("DATABASE_URL", ConfigValue.String("database.url"));
        ConfigSource environmentSource = ConfigSource.EnvironmentMapWithMappings(envValues, "APP__", "__", envPathMap, "environment-adapter");

        string[] args = new string[] { "-p", "5432", "--enabled" };
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("p", ConfigValue.String("database.port"));
        switchMap.Set("enabled", ConfigValue.String("feature.enabled"));
        ConfigSource commandLineSource = ConfigSource.CommandLine(args, switchMap, "command-line-adapter");

        ConfigSourceResult environmentResult = ConfigSourceLoader.Load(environmentSource, 5);
        ConfigSourceResult commandLineResult = ConfigSourceLoader.Load(commandLineSource, 6);

        Assert.That(environmentResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(environmentResult.Root.ObjectValue!.GetPathDefault("HOST").StringValue, Is.EqualTo("localhost"));
        Assert.That(environmentResult.Root.ObjectValue!.GetPathDefault("database.url").StringValue, Is.EqualTo("postgres://localhost"));
        Assert.That(commandLineResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(commandLineResult.Root.ObjectValue!.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(commandLineResult.Root.ObjectValue!.GetPathDefault("feature.enabled").StringValue, Is.EqualTo("true"));
    }
}
