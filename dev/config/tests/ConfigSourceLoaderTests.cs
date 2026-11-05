namespace config.Tests;

public class ConfigSourceLoaderTests
{
    [Test]
    public void Load_MemorySourceReturnsCopiedRootWithSourceMetadata()
    {
        ConfigArray endpoints = new ConfigArray();
        endpoints.Add(ConfigValue.String("one"));

        ConfigMap values = new ConfigMap();
        values.SetPathDefault("database.host", ConfigValue.String("localhost"));
        values.SetPathDefault("endpoints", ConfigValue.Array(endpoints));

        ConfigSource source = ConfigSource.Memory(values, "memory");
        ConfigSourceResult result = ConfigSourceLoader.Load(source, 5);
        ConfigMap loaded = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded, Is.Not.SameAs(values));
        Assert.That(loaded.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(loaded.GetPathDefault("database.host").SourceName, Is.EqualTo("memory"));
        Assert.That(loaded.GetPathDefault("database.host").PrecedenceRank, Is.EqualTo(5));
        Assert.That(loaded.GetPathDefault("database.host").OriginalKey, Is.EqualTo("database.host"));
        Assert.That(loaded.GetPathDefault("endpoints").ArrayValue!.Get(0).SourceName, Is.EqualTo("memory"));
        Assert.That(values.GetPathDefault("database.host").SourceName, Is.Null);
    }

    [Test]
    public void Load_DefaultsSourceUsesDefaultsSourceName()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("host", ConfigValue.String("localhost"));

        ConfigSource source = ConfigSource.Defaults(values);
        ConfigSourceResult result = ConfigSourceLoader.Load(source, 1);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("host").SourceName, Is.EqualTo("defaults"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("host").PrecedenceRank, Is.EqualTo(1));
    }

    [Test]
    public void Load_MemorySourceWithoutValuesReportsDiagnostic()
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Memory;
        source.SourceName = "broken-memory";

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 3);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing memory source values"));
    }

    [Test]
    public void Load_UnsupportedSourceKindReportsDiagnostic()
    {
        ConfigSource source = new ConfigSource();
        source.Kind = (ConfigSourceKind)999;
        source.SourceName = "invalid";

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 2);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("unsupported source kind"));
    }

    [Test]
    public void Load_CommandLineSourceParsesLongSwitchesEqualsAssignmentsAndValueTokens()
    {
        string[] args = new string[] { "--database.host=localhost", "--database.port", "5432", "--enabled" };
        ConfigSource source = ConfigSource.CommandLine(args, new ConfigMap(), "args");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);
        ConfigMap loaded = result.Root.ObjectValue!;
        ConfigValue host = loaded.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(loaded.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(loaded.GetPathDefault("enabled").StringValue, Is.EqualTo("true"));
        Assert.That(host.SourceName, Is.EqualTo("args"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(9));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
    }

    [Test]
    public void Load_CommandLineSourceParsesSlashAssignments()
    {
        string[] args = new string[] { "/database.host:localhost", "/database.port=5432", "/enabled" };
        ConfigSource source = ConfigSource.CommandLine(args, new ConfigMap(), "args");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);
        ConfigMap loaded = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(loaded.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(loaded.GetPathDefault("enabled").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Load_CommandLineSourceParsesShortSwitchesAndValueTokens()
    {
        string[] args = new string[] { "-h", "localhost", "-p=5432", "-v" };
        ConfigSource source = ConfigSource.CommandLine(args, new ConfigMap(), "args");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);
        ConfigMap loaded = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded.GetPathDefault("h").StringValue, Is.EqualTo("localhost"));
        Assert.That(loaded.GetPathDefault("p").StringValue, Is.EqualTo("5432"));
        Assert.That(loaded.GetPathDefault("v").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Load_CommandLineSourceAppliesSwitchMapAliases()
    {
        string[] args = new string[] { "-h", "localhost", "--port=5432", "/verbose", "--unmapped", "value" };
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("h", ConfigValue.String("database.host"));
        switchMap.Set("port", ConfigValue.String("database.port"));
        switchMap.Set("verbose", ConfigValue.String("logging.verbose"));
        ConfigSource source = ConfigSource.CommandLine(args, switchMap, "args");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);
        ConfigMap loaded = result.Root.ObjectValue!;
        ConfigValue host = loaded.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
        Assert.That(loaded.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(loaded.GetPathDefault("logging.verbose").StringValue, Is.EqualTo("true"));
        Assert.That(loaded.GetPathDefault("h").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(loaded.GetPathDefault("unmapped").StringValue, Is.EqualTo("value"));
    }

    [Test]
    public void Load_CommandLineSourceWithoutArgsReportsDiagnostic()
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.CommandLine;
        source.SourceName = "args";

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing command-line args"));
    }

    [Test]
    public void Load_CommandLineSourceReportsDiagnosticForSwitchMapAliasToEmptyPath()
    {
        string[] args = new string[] { "--blank", "ignored", "--host", "localhost" };
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("blank", ConfigValue.String(""));
        ConfigSource source = ConfigSource.CommandLine(args, switchMap, "args");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("blank").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("args"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing command-line key"));
    }

    [Test]
    public void Load_StringSourceParsesTextWithConfiguredFormat()
    {
        ConfigSource source = ConfigSource.StringSource("database.host=localhost\n", ConfigFormat.Flat, "string");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 3);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.SourceName, Is.EqualTo("string"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(3));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
    }

    [Test]
    public void Load_StreamSourceParsesUtf8TextWithConfiguredFormat()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("database.host=localhost\n");
        MemoryStream stream = new MemoryStream(bytes);
        ConfigSource source = ConfigSource.StreamSource(stream, ConfigFormat.Properties, "stream");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 4);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.SourceName, Is.EqualTo("stream"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(4));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void Load_StreamSourceWithoutStreamReportsDiagnostic()
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Stream;
        source.SourceName = "broken-stream";
        source.Format = ConfigFormat.Flat;

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 4);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing stream source"));
    }

    [Test]
    public void Load_DirectorySourceLoadsMatchingFilesInStableOrder()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "a.json");
        string secondPath = Path.Combine(directory, "b.json");
        string ignoredPath = Path.Combine(directory, "c.ini");
        File.WriteAllText(secondPath, "{ \"database\": { \"host\": \"second\" } }");
        File.WriteAllText(ignoredPath, "ignored=true\n");
        File.WriteAllText(firstPath, "{ \"database\": { \"host\": \"first\", \"port\": 5432 }, \"feature\": { \"enabled\": true } }");
        ConfigSource source = ConfigSource.DirectorySource(directory, "*.json", ConfigOptional.Required, "directory");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 5);
        ConfigMap loaded = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded.GetPathDefault("database.host").StringValue, Is.EqualTo("second"));
        Assert.That(loaded.GetPathDefault("database.host").SourceName, Is.EqualTo(secondPath));
        Assert.That(loaded.GetPathDefault("database.port").NumberValue, Is.EqualTo(5432m));
        Assert.That(loaded.GetPathDefault("feature.enabled").BooleanValue, Is.True);
        Assert.That(loaded.GetPathDefault("ignored").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Load_DirectorySourceWithEmptyPatternLoadsAllSupportedFiles()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-dir-all-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "a.flat"), "from.flat=yes\n");
        File.WriteAllText(Path.Combine(directory, "b.properties"), "from.properties=yes\n");
        ConfigSource source = ConfigSource.DirectorySource(directory, "", ConfigOptional.Required, "directory");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 5);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.flat").StringValue, Is.EqualTo("yes"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.properties").StringValue, Is.EqualTo("yes"));
    }

    [Test]
    public void Load_OptionalDirectorySourceReturnsEmptyRootWhenDirectoryIsMissing()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-optional-dir-" + Guid.NewGuid().ToString("N"));
        ConfigSource source = ConfigSource.DirectorySource(directory, "*.json", ConfigOptional.Optional, "optional-directory");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 5);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Load_RequiredDirectorySourceReportsDiagnosticWhenDirectoryIsMissing()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-required-dir-" + Guid.NewGuid().ToString("N"));
        ConfigSource source = ConfigSource.DirectorySource(directory, "*.json", ConfigOptional.Required, "required-directory");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 5);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("required-directory"));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo(directory));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required directory"));
    }

    [Test]
    public void Load_KeyPerFileDirectorySourceLoadsFileNamesAsStringKeys()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-kpf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string passwordPath = Path.Combine(directory, "database.password");
        string hostPath = Path.Combine(directory, "database.host");
        File.WriteAllText(passwordPath, "secret\n");
        File.WriteAllText(hostPath, "localhost");
        ConfigSource source = ConfigSource.KeyPerFileDirectory(directory, ConfigOptional.Required, "secrets");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 6);
        ConfigMap loaded = result.Root.ObjectValue!;
        ConfigValue password = loaded.GetPathDefault("database.password");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(password.StringValue, Is.EqualTo("secret\n"));
        Assert.That(password.SourceName, Is.EqualTo(passwordPath));
        Assert.That(password.PrecedenceRank, Is.EqualTo(6));
        Assert.That(password.OriginalKey, Is.EqualTo("database.password"));
    }

    [Test]
    public void Load_KeyPerFileDirectorySourceUsesStableFileOrderForDuplicatePaths()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-kpf-order-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "database..host");
        string secondPath = Path.Combine(directory, "database.host");
        File.WriteAllText(secondPath, "second");
        File.WriteAllText(firstPath, "first");
        ConfigSource source = ConfigSource.KeyPerFileDirectory(directory, ConfigOptional.Required, "secrets");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 6);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo(secondPath));
    }

    [Test]
    public void Load_OptionalKeyPerFileDirectorySourceReturnsEmptyRootWhenDirectoryIsMissing()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-optional-kpf-" + Guid.NewGuid().ToString("N"));
        ConfigSource source = ConfigSource.KeyPerFileDirectory(directory, ConfigOptional.Optional, "optional-secrets");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 6);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Load_RequiredKeyPerFileDirectorySourceReportsDiagnosticWhenDirectoryIsMissing()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-required-kpf-" + Guid.NewGuid().ToString("N"));
        ConfigSource source = ConfigSource.KeyPerFileDirectory(directory, ConfigOptional.Required, "required-secrets");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 6);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("required-secrets"));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo(directory));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required key-per-file directory"));
    }

    [Test]
    public void Load_EnvironmentSourceReadsProcessEnvironmentVariables()
    {
        string key = "CONFIG_TEST_T031_" + Guid.NewGuid().ToString("N");
        string? previousValue = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "from-env");

        try
        {
            ConfigSource source = ConfigSource.Environment("", "__", "env");

            ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);
            ConfigValue value = result.Root.ObjectValue!.GetPathDefault(key);

            Assert.That(result.Diagnostics.HasErrors(), Is.False);
            Assert.That(value.StringValue, Is.EqualTo("from-env"));
            Assert.That(value.SourceName, Is.EqualTo("env"));
            Assert.That(value.PrecedenceRank, Is.EqualTo(7));
            Assert.That(value.OriginalKey, Is.EqualTo(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public void Load_EnvironmentMapSourceReadsSuppliedValues()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP_HOST"] = "localhost";
        values["APP_PORT"] = "5432";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", "__", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("APP_HOST");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP_PORT").StringValue, Is.EqualTo("5432"));
        Assert.That(host.SourceName, Is.EqualTo("env-map"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(8));
        Assert.That(host.OriginalKey, Is.EqualTo("APP_HOST"));
    }

    [Test]
    public void Load_EnvironmentMapSourceUsesStableOrderForDuplicateNormalizedPaths()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP____HOST"] = "first";
        values["APP__HOST"] = "second";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", "__", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.HOST").StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.HOST").OriginalKey, Is.EqualTo("APP.HOST"));
    }

    [Test]
    public void Load_EnvironmentMapSourceFiltersAndStripsPrefix()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP_HOST"] = "localhost";
        values["APP_DATABASE__HOST"] = "database";
        values["OTHER_HOST"] = "ignored";
        values["APP_"] = "empty-key";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "APP_", "__", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);
        ConfigMap loaded = result.Root.ObjectValue!;
        ConfigValue host = loaded.GetPathDefault("HOST");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.OriginalKey, Is.EqualTo("HOST"));
        Assert.That(loaded.GetPathDefault("DATABASE.HOST").StringValue, Is.EqualTo("database"));
        Assert.That(loaded.GetPathDefault("APP_HOST").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(loaded.GetPathDefault("OTHER_HOST").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(loaded.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Load_EnvironmentMapSourceNormalizesDoubleUnderscoreDelimiter()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP__DATABASE__HOST"] = "localhost";
        values["APP__DATABASE__PORT"] = "5432";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", "__", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.DATABASE.HOST").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.DATABASE.PORT").StringValue, Is.EqualTo("5432"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.DATABASE.HOST").OriginalKey, Is.EqualTo("APP.DATABASE.HOST"));
    }

    [Test]
    public void Load_EnvironmentMapSourceNormalizesCustomDelimiter()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP:DATABASE:HOST"] = "localhost";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", ":", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.DATABASE.HOST").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Load_EnvironmentMapSourceKeepsEmptyValuesByDefault()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP__EMPTY"] = "";
        values["APP__HOST"] = "localhost";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", "__", "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.EMPTY").StringValue, Is.EqualTo(""));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.EMPTY").Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.HOST").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Load_EnvironmentMapSourceIgnoresEmptyValuesWhenConfigured()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["APP__EMPTY"] = "";
        values["APP__HOST"] = "localhost";
        ConfigSource source = ConfigSource.EnvironmentMap(values, "", "__", "env-map");
        source.EmptyEnvMode = ConfigEmptyEnvMode.Ignore;

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.EMPTY").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("APP.HOST").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Load_EnvironmentMapSourceUsesExplicitPathMappings()
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        values["DATABASE_URL"] = "postgres://localhost";
        values["APP__HOST"] = "localhost";
        values["IGNORED"] = "ignored";
        ConfigMap pathMap = new ConfigMap();
        pathMap.Set("DATABASE_URL", ConfigValue.String("database.url"));
        pathMap.Set("APP__HOST", ConfigValue.String("service.host"));
        ConfigSource source = ConfigSource.EnvironmentMapWithMappings(values, "APP__", "__", pathMap, "env-map");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);
        ConfigMap loaded = result.Root.ObjectValue!;

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(loaded.GetPathDefault("database.url").StringValue, Is.EqualTo("postgres://localhost"));
        Assert.That(loaded.GetPathDefault("database.url").OriginalKey, Is.EqualTo("database.url"));
        Assert.That(loaded.GetPathDefault("service.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(loaded.GetPathDefault("HOST").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(loaded.GetPathDefault("IGNORED").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(loaded.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Load_FileSourceUsesExplicitFormat()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-explicit.conf");
        File.WriteAllText(path, "[database]\nhost=localhost\n");
        ConfigSource source = ConfigSource.File(path, ConfigFormat.Ini, ConfigOptional.Required, ConfigReload.None, "explicit-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 6);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.SourceName, Is.EqualTo("explicit-file"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(6));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
    }

    [Test]
    public void Load_FileSourceDetectsJsonFormatFromExtension()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-detected.json");
        File.WriteAllText(path, "{ \"database\": { \"host\": \"localhost\" }, \"enabled\": true }");
        ConfigSource source = ConfigSource.FileByExtension(path, ConfigOptional.Required, ConfigReload.None, "detected-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("enabled").BooleanValue, Is.True);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").SourceName, Is.EqualTo("detected-file"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database.host").PrecedenceRank, Is.EqualTo(7));
    }

    [Test]
    public void Load_FileSourceProcessesSingleIncludeBeforeIncludingFile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-single-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string includedPath = Path.Combine(directory, "included.json");
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(includedPath, "{ \"service\": { \"host\": \"included\", \"port\": \"5000\" } }");
        File.WriteAllText(mainPath, "{ \"include\": \"" + includedPath + "\", \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").SourceName, Is.EqualTo("main-file"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").StringValue, Is.EqualTo("5000"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").SourceName, Is.EqualTo(includedPath));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("include").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Load_FileSourceProcessesIncludeArraysAcrossSupportedFormats()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-many-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string jsonPath = Path.Combine(directory, "included.json");
        string iniPath = Path.Combine(directory, "included.ini");
        string propertiesPath = Path.Combine(directory, "included.properties");
        string dotenvPath = Path.Combine(directory, "included.env");
        string flatPath = Path.Combine(directory, "included.flat");
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(jsonPath, "{ \"from\": { \"json\": \"json\" } }");
        File.WriteAllText(iniPath, "[from]\nini=ini\n");
        File.WriteAllText(propertiesPath, "from.properties=properties\n");
        File.WriteAllText(dotenvPath, "from.dotenv=dotenv\n");
        File.WriteAllText(flatPath, "from.flat=flat\n");
        File.WriteAllText(mainPath, "{ \"includes\": [\"" + jsonPath + "\", \"" + iniPath + "\", \"" + propertiesPath + "\", \"" + dotenvPath + "\", \"" + flatPath + "\"], \"from\": { \"main\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.json").StringValue, Is.EqualTo("json"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.ini").StringValue, Is.EqualTo("ini"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.properties").StringValue, Is.EqualTo("properties"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.dotenv").StringValue, Is.EqualTo("dotenv"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.flat").StringValue, Is.EqualTo("flat"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("from.main").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("includes").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Load_FileSourceProcessesIncludeArraysInOrderBeforeIncludingFile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-order-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "first.json");
        string secondPath = Path.Combine(directory, "second.json");
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(firstPath, "{ \"service\": { \"host\": \"first\", \"port\": \"5000\", \"shared\": \"first\", \"firstOnly\": \"first\" } }");
        File.WriteAllText(secondPath, "{ \"service\": { \"host\": \"second\", \"shared\": \"second\", \"secondOnly\": \"second\" } }");
        File.WriteAllText(mainPath, "{ \"includes\": [\"" + firstPath + "\", \"" + secondPath + "\"], \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").SourceName, Is.EqualTo("main-file"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").StringValue, Is.EqualTo("5000"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.firstOnly").StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.shared").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.shared").SourceName, Is.EqualTo(secondPath));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.secondOnly").StringValue, Is.EqualTo("second"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("includes").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Load_FileSourceReportsDiagnosticsForMissingIncludes()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string missingPath = Path.Combine(directory, "missing.json");
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(mainPath, "{ \"include\": \"" + missingPath + "\", \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo(missingPath));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo(missingPath));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required file"));
    }

    [Test]
    public void Load_FileSourceReportsDiagnosticsForUnsupportedIncludeFormats()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-unsupported-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string unsupportedPath = Path.Combine(directory, "settings.custom");
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(unsupportedPath, "service.port=5000\n");
        File.WriteAllText(mainPath, "{ \"include\": \"" + unsupportedPath + "\", \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.port").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("include").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo(unsupportedPath));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("unsupported file extension"));
    }

    [Test]
    public void Load_FileSourceResolvesRelativeIncludesFromIncludingFileDirectory()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-relative-" + Guid.NewGuid().ToString("N"));
        string appDirectory = Path.Combine(directory, "app");
        string sharedDirectory = Path.Combine(appDirectory, "shared");
        string secretsDirectory = Path.Combine(appDirectory, "secrets");
        Directory.CreateDirectory(sharedDirectory);
        Directory.CreateDirectory(secretsDirectory);
        string mainPath = Path.Combine(appDirectory, "main.json");
        string basePath = Path.Combine(sharedDirectory, "base.json");
        string secretPath = Path.Combine(secretsDirectory, "secret.properties");
        File.WriteAllText(secretPath, "service.secret=secret\n");
        File.WriteAllText(basePath, "{ \"include\": \"../secrets/secret.properties\", \"service\": { \"host\": \"base\" } }");
        File.WriteAllText(mainPath, "{ \"include\": \"shared/base.json\", \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.secret").StringValue, Is.EqualTo("secret"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.secret").SourceName, Is.EqualTo(secretPath));
    }

    [Test]
    public void Load_FileSourceReportsDiagnosticsForInvalidIncludeValuesAndKeepsMainValues()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-invalid-values-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string mainPath = Path.Combine(directory, "main.json");
        File.WriteAllText(mainPath, "{ \"include\": { \"path\": \"bad\" }, \"includes\": [\" \", 7, null], \"service\": { \"host\": \"main\" } }");
        ConfigSource source = ConfigSource.FileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None, "main-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("main"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("include").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("includes").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(4));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("main-file"));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("include"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("invalid include path"));
        Assert.That(result.Diagnostics.Errors[1].SourceName, Is.EqualTo(""));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo(""));
        Assert.That(result.Diagnostics.Errors[1].Message, Is.EqualTo("invalid include path"));
        Assert.That(result.Diagnostics.Errors[2].Path, Is.EqualTo("includes"));
        Assert.That(result.Diagnostics.Errors[2].Message, Is.EqualTo("invalid include path"));
        Assert.That(result.Diagnostics.Errors[3].Path, Is.EqualTo("includes"));
        Assert.That(result.Diagnostics.Errors[3].Message, Is.EqualTo("invalid include path"));
    }

    [Test]
    public void Load_FileSourceDetectsIncludeCycles()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "include-cycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "first.json");
        string secondPath = Path.Combine(directory, "second.json");
        File.WriteAllText(firstPath, "{ \"include\": \"second.json\", \"first\": \"first\" }");
        File.WriteAllText(secondPath, "{ \"include\": \"first.json\", \"second\": \"second\" }");
        ConfigSource source = ConfigSource.FileByExtension(firstPath, ConfigOptional.Required, ConfigReload.None, "first-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Root.ObjectValue!.GetPathDefault("first").StringValue, Is.EqualTo("first"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("second").StringValue, Is.EqualTo("second"));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo(firstPath));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo(firstPath));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("include cycle"));
    }

    [Test]
    public void Load_FileSourceDetectsDotEnvFormatFromExtension()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, ".env");
        File.WriteAllText(path, "DATABASE_HOST=localhost\n");
        ConfigSource source = ConfigSource.FileByExtension(path, ConfigOptional.Required, ConfigReload.None, "dotenv-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 8);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("DATABASE_HOST").StringValue, Is.EqualTo("localhost"));
        Assert.That(result.Root.ObjectValue!.GetPathDefault("DATABASE_HOST").SourceName, Is.EqualTo("dotenv-file"));
    }

    [Test]
    public void Load_FileSourceWithUnknownExtensionReportsDiagnostic()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-source-unknown.custom");
        File.WriteAllText(path, "host=localhost\n");
        ConfigSource source = ConfigSource.FileByExtension(path, ConfigOptional.Required, ConfigReload.None, "unknown-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 7);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("unsupported file extension"));
    }

    [Test]
    public void Load_OptionalFileSourceReturnsEmptyRootWhenFileIsMissing()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-optional-config.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        ConfigSource source = ConfigSource.File(path, ConfigFormat.Json, ConfigOptional.Optional, ConfigReload.None, "optional-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 4);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Root.SourceName, Is.EqualTo("optional-file"));
        Assert.That(result.Root.PrecedenceRank, Is.EqualTo(4));
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Load_RequiredFileSourceReportsDiagnosticWhenFileIsMissing()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-required-config.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        ConfigSource source = ConfigSource.File(path, ConfigFormat.Json, ConfigOptional.Required, ConfigReload.None, "required-file");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 4);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("required-file"));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo(path));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required file"));
    }

    [Test]
    public void Load_ProgrammaticOverrideCreatesSingleStringValue()
    {
        ConfigSource source = ConfigSource.ProgrammaticOverride("database.host", "localhost", "override");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);
        ConfigValue host = result.Root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(host.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(host.SourceName, Is.EqualTo("override"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(9));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
    }

    [Test]
    public void Load_ProgrammaticOverridePreservesEscapedPathSegments()
    {
        ConfigSource source = ConfigSource.ProgrammaticOverride("database\\.primary.host", "localhost", "override");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);

        Assert.That(result.Root.ObjectValue!.Has("database.primary"), Is.True);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("database\\.primary.host").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Load_ProgrammaticOverrideAllowsEmptyStringValues()
    {
        ConfigSource source = ConfigSource.ProgrammaticOverride("feature.flag", "", "override");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Root.ObjectValue!.GetPathDefault("feature.flag").StringValue, Is.EqualTo(""));
    }

    [Test]
    public void Load_ProgrammaticOverrideWithoutPathReportsDiagnostic()
    {
        ConfigSource source = ConfigSource.ProgrammaticOverride("", "localhost", "override");

        ConfigSourceResult result = ConfigSourceLoader.Load(source, 9);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing override path"));
    }

    [Test]
    public void LoadEnvironmentMapSourceWithoutMapReportsDiagnostic()
    {
        ConfigSource source = new ConfigSource();
        source.Kind = ConfigSourceKind.Environment;
        source.SourceName = "env-map";

        ConfigSourceResult result = ConfigSourceLoader.LoadEnvironmentMapSource(source, 8);

        Assert.That(result.Root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo("env-map"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing environment map"));
    }

    [Test]
    public void ParseText_UnsupportedFormatReportsDiagnosticAndReturnsEmptyRoot()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigSourceLoader.ParseText("host=localhost", (ConfigFormat)999, "unknown-format", 12, diagnostics);

        Assert.That(root.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(root.SourceName, Is.EqualTo("unknown-format"));
        Assert.That(root.PrecedenceRank, Is.EqualTo(12));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].SourceName, Is.EqualTo("unknown-format"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("unsupported file format"));
    }
}
