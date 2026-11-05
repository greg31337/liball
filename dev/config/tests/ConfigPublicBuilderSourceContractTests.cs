using System.Text;

namespace config.Tests;

public class ConfigPublicBuilderSourceContractTests
{
    [TestCase(ConfigFormat.Json, ".json", "{ \"value\": \"expected\" }")]
    [TestCase(ConfigFormat.Ini, ".ini", "value=expected\n")]
    [TestCase(ConfigFormat.Properties, ".properties", "value=expected\n")]
    [TestCase(ConfigFormat.DotEnv, ".env", "value=expected\n")]
    [TestCase(ConfigFormat.Flat, ".flat", "value=expected\n")]
    [TestCase(ConfigFormat.Xml, ".xml", "<settings><value>expected</value></settings>")]
    public void ExplicitAndDetectedFiles_ParseEveryFormat(ConfigFormat format, string extension, string text)
    {
        string directory = NewDirectory("formats");
        string explicitPath = Path.Combine(directory, "explicit.custom");
        string detectedPath = Path.Combine(directory, "detected" + extension);

        try
        {
            File.WriteAllText(explicitPath, text, Encoding.UTF8);
            File.WriteAllText(detectedPath, text, Encoding.UTF8);

            IConfig explicitConfig = new ConfigBuilder()
                .AddFile(explicitPath, format, ConfigOptional.Required, ConfigReload.None)
                .Build();
            IConfig detectedConfig = new ConfigBuilder()
                .AddFileByExtension(detectedPath, ConfigOptional.Required, ConfigReload.None)
                .Build();

            Assert.That(explicitConfig.Diagnostics().HasErrors(), Is.False);
            Assert.That(detectedConfig.Diagnostics().HasErrors(), Is.False);
            Assert.That(explicitConfig.GetString("value"), Is.EqualTo("expected"));
            Assert.That(detectedConfig.GetString("value"), Is.EqualTo("expected"));
            Assert.That(explicitConfig.Value("value").SourceName, Is.EqualTo(explicitPath));
            Assert.That(detectedConfig.Value("value").SourceName, Is.EqualTo(detectedPath));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FormatSpecificFileEntryPoints_LoadEveryFormat()
    {
        string directory = NewDirectory("specific-formats");
        string jsonPath = Path.Combine(directory, "settings.json");
        string iniPath = Path.Combine(directory, "settings.ini");
        string propertiesPath = Path.Combine(directory, "settings.properties");
        string dotEnvPath = Path.Combine(directory, ".env");
        string flatPath = Path.Combine(directory, "settings.flat");
        string xmlPath = Path.Combine(directory, "settings.xml");

        try
        {
            File.WriteAllText(jsonPath, "{ \"json\": \"loaded\" }");
            File.WriteAllText(iniPath, "ini=loaded\n");
            File.WriteAllText(propertiesPath, "properties=loaded\n");
            File.WriteAllText(dotEnvPath, "dotenv=loaded\n");
            File.WriteAllText(flatPath, "flat=loaded\n");
            File.WriteAllText(xmlPath, "<settings><xml>loaded</xml></settings>");

            IConfig config = new ConfigBuilder()
                .AddJsonFile(jsonPath, ConfigOptional.Required, ConfigReload.None)
                .AddIniFile(iniPath, ConfigOptional.Required, ConfigReload.None)
                .AddPropertiesFile(propertiesPath, ConfigOptional.Required, ConfigReload.None)
                .AddDotEnvFile(dotEnvPath, ConfigOptional.Required)
                .AddFlatFile(flatPath, ConfigOptional.Required, ConfigReload.None)
                .AddXmlFile(xmlPath, ConfigOptional.Required, ConfigReload.None)
                .Build();

            Assert.That(config.Diagnostics().HasErrors(), Is.False);
            Assert.That(config.GetString("json"), Is.EqualTo("loaded"));
            Assert.That(config.GetString("ini"), Is.EqualTo("loaded"));
            Assert.That(config.GetString("properties"), Is.EqualTo("loaded"));
            Assert.That(config.GetString("dotenv"), Is.EqualTo("loaded"));
            Assert.That(config.GetString("flat"), Is.EqualTo("loaded"));
            Assert.That(config.GetString("xml"), Is.EqualTo("loaded"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("{ \"value\": 1 }", ConfigValueKind.Object)]
    [TestCase("[1, 2]", ConfigValueKind.Array)]
    [TestCase("\"text\"", ConfigValueKind.String)]
    [TestCase("42.5", ConfigValueKind.Number)]
    [TestCase("true", ConfigValueKind.Boolean)]
    [TestCase("null", ConfigValueKind.Null)]
    public void JsonStringSources_PreserveEveryRootKind(string text, ConfigValueKind expectedKind)
    {
        IConfig config = new ConfigBuilder().AddString(text, ConfigFormat.Json, "root.json").Build();

        ConfigValue root = config.Value("");

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(root.Kind, Is.EqualTo(expectedKind));
        Assert.That(root.SourceName, Is.EqualTo("root.json"));
    }

    [Test]
    public void StringAndStreamSources_LoadTextAndHonorSelectedEncoding()
    {
        MemoryStream utf8Stream = new MemoryStream(Encoding.UTF8.GetBytes("stream.value=utf8\n"));
        MemoryStream unicodeStream = new MemoryStream(Encoding.Unicode.GetBytes("encoded.value=français\n"));
        IConfigBuilder builder = new ConfigBuilder();
        builder
            .AddString("string.value=memory\n", ConfigFormat.Flat, "string")
            .AddStream(utf8Stream, ConfigFormat.Flat, "utf8-stream")
            .AddStreamWithEncoding(unicodeStream, ConfigFormat.Flat, "unicode-stream", Encoding.Unicode);

        IConfig config = builder.Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("string.value"), Is.EqualTo("memory"));
        Assert.That(config.GetString("stream.value"), Is.EqualTo("utf8"));
        Assert.That(config.GetString("encoded.value"), Is.EqualTo("français"));
        Assert.That(config.Value("string.value").SourceName, Is.EqualTo("string"));
        Assert.That(config.Value("stream.value").SourceName, Is.EqualTo("utf8-stream"));
        Assert.That(config.Value("encoded.value").SourceName, Is.EqualTo("unicode-stream"));
        Assert.That(utf8Stream.CanRead, Is.True);
        Assert.That(unicodeStream.CanRead, Is.True);
    }

    [Test]
    public void DirectoryAndKeyPerFileSources_UseStableOrderPatternsAndMetadata()
    {
        string root = NewDirectory("directories");
        string configDirectory = Path.Combine(root, "config");
        string keyDirectory = Path.Combine(root, "keys");
        Directory.CreateDirectory(configDirectory);
        Directory.CreateDirectory(keyDirectory);
        string firstPath = Path.Combine(configDirectory, "a.json");
        string secondPath = Path.Combine(configDirectory, "b.json");
        string ignoredPath = Path.Combine(configDirectory, "ignored.ini");
        string passwordPath = Path.Combine(keyDirectory, "database.password");

        try
        {
            File.WriteAllText(firstPath, "{ \"service\": { \"host\": \"first\", \"port\": 8080 } }");
            File.WriteAllText(secondPath, "{ \"service\": { \"host\": \"second\" } }");
            File.WriteAllText(ignoredPath, "service.host=ignored\n");
            File.WriteAllText(passwordPath, "secret");
            File.WriteAllText(Path.Combine(keyDirectory, "token"), "abc123");

            IConfig config = new ConfigBuilder()
                .AddDirectory(configDirectory, "*.json", ConfigOptional.Required)
                .AddKeyPerFileDirectory(keyDirectory, ConfigOptional.Required)
                .Build();

            Assert.That(config.Diagnostics().HasErrors(), Is.False);
            Assert.That(config.GetString("service.host"), Is.EqualTo("second"));
            Assert.That(config.GetInt("service.port"), Is.EqualTo(8080));
            Assert.That(config.GetString("database.password"), Is.EqualTo("secret"));
            Assert.That(config.GetString("token"), Is.EqualTo("abc123"));
            Assert.That(config.Has("ignored"), Is.False);
            Assert.That(config.Value("service.host").SourceName, Is.EqualTo(secondPath));
            Assert.That(config.Value("database.password").SourceName, Is.EqualTo(passwordPath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void OptionalAndRequiredFileDirectoryKeyAndSearchSources_ReportConsistently()
    {
        string root = NewDirectory("missing-sources");
        string missingFile = Path.Combine(root, "missing.json");
        string missingDirectory = Path.Combine(root, "missing-directory");
        string missingKeys = Path.Combine(root, "missing-keys");

        try
        {
            IConfig optional = new ConfigBuilder()
                .AddFileByExtension(missingFile, ConfigOptional.Optional, ConfigReload.None)
                .AddDirectory(missingDirectory, "*.json", ConfigOptional.Optional)
                .AddKeyPerFileDirectory(missingKeys, ConfigOptional.Optional)
                .AddParentDirectoryFileFromDirectory(root, "absent.json", 0, ConfigOptional.Optional)
                .Build();
            IConfig required = new ConfigBuilder()
                .AddFileByExtension(missingFile, ConfigOptional.Required, ConfigReload.None)
                .AddDirectory(missingDirectory, "*.json", ConfigOptional.Required)
                .AddKeyPerFileDirectory(missingKeys, ConfigOptional.Required)
                .AddParentDirectoryFileFromDirectory(root, "absent.json", 0, ConfigOptional.Required)
                .Build();

            Assert.That(optional.Diagnostics().HasErrors(), Is.False);
            Assert.That(optional.Keys(), Is.Empty);
            Assert.That(required.Diagnostics().ErrorCount(), Is.EqualTo(4));
            Assert.That(required.Diagnostics().Error(0).Message, Is.EqualTo("missing required file"));
            Assert.That(required.Diagnostics().Error(1).Message, Is.EqualTo("missing required directory"));
            Assert.That(required.Diagnostics().Error(2).Message, Is.EqualTo("missing required key-per-file directory"));
            Assert.That(required.Diagnostics().Error(3).Message, Is.EqualTo("missing required search file"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void EnvironmentAndCommandLineSources_ApplyControlsMappingsAndSwitchForms()
    {
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["APP_service__host"] = "environment";
        environment["APP_service__empty"] = "";
        environment["EXTERNAL_URL"] = "https://example.test";
        ConfigMap environmentMap = new ConfigMap();
        environmentMap.Set("EXTERNAL_URL", new ConfigValues().String("service.external"));
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("host", new ConfigValues().String("service.host"));
        switchMap.Set("p", new ConfigValues().String("service.port"));
        switchMap.Set("enabled", new ConfigValues().String("service.enabled"));
        string[] args = new string[] { "--host=command-line", "-p", "8080", "/enabled:true", "--unmapped=value" };

        IConfig config = new ConfigBuilder()
            .AddEnvironmentDictionaryWithOptions(environment, "APP_", "__", ConfigEmptyEnvMode.Ignore, environmentMap)
            .AddMappedCommandLine(args, switchMap)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("command-line"));
        Assert.That(config.GetInt("service.port"), Is.EqualTo(8080));
        Assert.That(config.GetBool("service.enabled"), Is.True);
        Assert.That(config.GetString("service.external"), Is.EqualTo("https://example.test"));
        Assert.That(config.Has("service.empty"), Is.False);
        Assert.That(config.GetString("unmapped"), Is.EqualTo("value"));
        Assert.That(config.Value("service.external").SourceName, Is.EqualTo("environment"));
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("command-line"));
    }

    [Test]
    public void CustomInterfaceSource_LoadsThroughPublicBuilderContract()
    {
        PublicBuilderContractSource source = new PublicBuilderContractSource();
        IConfigBuilder builder = new ConfigBuilder();
        builder.AddSource(source);

        IConfig config = builder.Build();

        Assert.That(source.LoadCount, Is.EqualTo(1));
        Assert.That(source.PrecedenceRank, Is.EqualTo(1));
        Assert.That(config.GetString("custom.value"), Is.EqualTo("loaded"));
        Assert.That(config.Value("custom.value").SourceName, Is.EqualTo("custom-source"));
        Assert.That(config.Value("custom.value").PrecedenceRank, Is.EqualTo(1));
        Assert.That(config.Diagnostics().WarningCount(), Is.EqualTo(1));
        Assert.That(config.Diagnostics().Warning(0).Message, Is.EqualTo("custom source loaded"));
    }

    [Test]
    public void ConventionalStack_AppliesEveryLayerInDocumentedOrder()
    {
        string directory = NewDirectory("conventional");
        string root = "stack" + Guid.NewGuid().ToString("N");
        string environmentKey = root + "__environmentOnly";
        ConfigValues values = new ConfigValues();
        ConfigMap defaults = new ConfigMap();
        defaults.SetPathDefault(root + ".host", values.String("defaults"));
        defaults.SetPathDefault(root + ".defaultOnly", values.String("defaults"));
        File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{ \"" + root + "\": { \"host\": \"json\", \"jsonOnly\": \"json\" } }");
        File.WriteAllText(Path.Combine(directory, "appsettings.Test.json"), "{ \"" + root + "\": { \"host\": \"environment-json\", \"environmentJsonOnly\": \"environment-json\" } }");
        File.WriteAllText(Path.Combine(directory, ".env"), root + ".host=dotenv\n" + root + ".dotEnvOnly=dotenv\n");

        try
        {
            Environment.SetEnvironmentVariable(environmentKey, "environment");
            string[] args = new string[] { "--" + root + ".host=command-line", "--" + root + ".commandOnly=command-line" };

            IConfig config = new ConfigBuilder()
                .AddDefaultAppSourcesWithDefaultsFromDirectory(defaults, directory, "Test", args)
                .Build();

            Assert.That(config.Diagnostics().HasErrors(), Is.False);
            Assert.That(config.GetString(root + ".host"), Is.EqualTo("command-line"));
            Assert.That(config.GetString(root + ".defaultOnly"), Is.EqualTo("defaults"));
            Assert.That(config.GetString(root + ".jsonOnly"), Is.EqualTo("json"));
            Assert.That(config.GetString(root + ".environmentJsonOnly"), Is.EqualTo("environment-json"));
            Assert.That(config.GetString(root + ".dotEnvOnly"), Is.EqualTo("dotenv"));
            Assert.That(config.GetString(root + ".environmentOnly"), Is.EqualTo("environment"));
            Assert.That(config.GetString(root + ".commandOnly"), Is.EqualTo("command-line"));
            Assert.That(config.Value(root + ".host").SourceName, Is.EqualTo("command-line"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, null);
            Directory.Delete(directory, true);
        }
    }

    private static string NewDirectory(string label)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-builder-" + label + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

public class PublicBuilderContractSource : IConfigSource
{
    public int LoadCount;
    public int PrecedenceRank;

    public PublicBuilderContractSource()
    {
        LoadCount = 0;
        PrecedenceRank = 0;
    }

    public ConfigSourceResult Load(int precedenceRank)
    {
        ConfigValues values = new ConfigValues();
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("custom.value", values.SetSource(values.String("loaded"), "custom-source", precedenceRank, "custom.value", 1, 1));
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        diagnostics.AddWarning("custom-source", "custom.value", "custom source loaded", 1, 1);
        LoadCount = LoadCount + 1;
        PrecedenceRank = precedenceRank;
        return new ConfigSourceResult(values.SetSource(values.Object(map), "custom-source", precedenceRank, "", 1, 1), diagnostics);
    }
}
