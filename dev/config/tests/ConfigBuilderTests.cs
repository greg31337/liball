using System.Text;

namespace config.Tests;

public class ConfigBuilderTests
{
    [Test]
    public void CreateBuilder_ReturnsBuilderWithConservativeDefaults()
    {
        ConfigBuilder builder = Config.CreateBuilder();

        Assert.That(builder.SourceCount(), Is.EqualTo(0));
        Assert.That(builder.Delimiter, Is.EqualTo("."));
        Assert.That(builder.CaseMode, Is.EqualTo(ConfigCaseMode.Sensitive));
        Assert.That(builder.MergeMode, Is.EqualTo(ConfigMergeMode.Override));
        Assert.That(builder.ArrayMergeMode, Is.EqualTo(ConfigArrayMergeMode.Replace));
        Assert.That(builder.NullMergeMode, Is.EqualTo(ConfigNullMergeMode.Replace));
        Assert.That(builder.DuplicateKeyMode, Is.EqualTo(ConfigDuplicateKeyMode.Error));
        Assert.That(builder.DotEnvExpansionMode, Is.EqualTo(ConfigDotEnvExpansionMode.Disabled));
        Assert.That(builder.DotEnvMissingValueMode, Is.EqualTo(ConfigDotEnvMissingValueMode.Empty));
        Assert.That(builder.Aliases.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Setters_UpdateBuilderOptionsAndReturnSameBuilder()
    {
        ConfigBuilder builder = new ConfigBuilder();

        ConfigBuilder returned = builder
            .SetDelimiter("__")
            .SetCaseMode(ConfigCaseMode.Insensitive)
            .SetMergeMode(ConfigMergeMode.Fallback)
            .SetArrayMergeMode(ConfigArrayMergeMode.Append)
            .SetNullMergeMode(ConfigNullMergeMode.Ignore)
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.MergeObjects)
            .SetDotEnvExpansionMode(ConfigDotEnvExpansionMode.Enabled)
            .SetDotEnvMissingValueMode(ConfigDotEnvMissingValueMode.Preserve);

        Assert.That(returned, Is.SameAs(builder));
        Assert.That(builder.Delimiter, Is.EqualTo("__"));
        Assert.That(builder.CaseMode, Is.EqualTo(ConfigCaseMode.Insensitive));
        Assert.That(builder.MergeMode, Is.EqualTo(ConfigMergeMode.Fallback));
        Assert.That(builder.ArrayMergeMode, Is.EqualTo(ConfigArrayMergeMode.Append));
        Assert.That(builder.NullMergeMode, Is.EqualTo(ConfigNullMergeMode.Ignore));
        Assert.That(builder.DuplicateKeyMode, Is.EqualTo(ConfigDuplicateKeyMode.MergeObjects));
        Assert.That(builder.DotEnvExpansionMode, Is.EqualTo(ConfigDotEnvExpansionMode.Enabled));
        Assert.That(builder.DotEnvMissingValueMode, Is.EqualTo(ConfigDotEnvMissingValueMode.Preserve));
    }

    [Test]
    public void AddMemoryDefaultsOverrideStringAndStream_RegisterSources()
    {
        ConfigMap values = new ConfigMap();
        MemoryStream stream = new MemoryStream();
        ConfigBuilder builder = new ConfigBuilder();

        builder
            .AddMemory(values, "memory")
            .AddDefaults(values)
            .AddOverride("host", "localhost")
            .AddString("host=localhost", ConfigFormat.Flat, "string")
            .AddStream(stream, ConfigFormat.Properties, "stream");

        Assert.That(builder.SourceCount(), Is.EqualTo(5));
        Assert.That(builder.SourceAt(0).Kind, Is.EqualTo(ConfigSourceKind.Memory));
        Assert.That(builder.SourceAt(0).Values, Is.SameAs(values));
        Assert.That(builder.SourceAt(1).Kind, Is.EqualTo(ConfigSourceKind.Defaults));
        Assert.That(builder.SourceAt(2).Kind, Is.EqualTo(ConfigSourceKind.ProgrammaticOverride));
        Assert.That(builder.SourceAt(2).OverridePath, Is.EqualTo("host"));
        Assert.That(builder.SourceAt(3).Kind, Is.EqualTo(ConfigSourceKind.String));
        Assert.That(builder.SourceAt(4).StreamValue, Is.SameAs(stream));
    }

    [Test]
    public void AddFileAndDirectorySources_RegisterFormatsAndOptions()
    {
        ConfigBuilder builder = new ConfigBuilder();

        builder
            .AddJsonFile("app.json", ConfigOptional.Required, ConfigReload.OnChange)
            .AddIniFile("app.ini", ConfigOptional.Optional, ConfigReload.None)
            .AddPropertiesFile("app.properties", ConfigOptional.Required, ConfigReload.None)
            .AddDotEnvFile(".env", ConfigOptional.Optional)
            .AddXmlFile("app.xml", ConfigOptional.Required, ConfigReload.OnChange)
            .AddDirectory("config", "*.json", ConfigOptional.Required)
            .AddKeyPerFileDirectory("secrets", ConfigOptional.Optional);

        Assert.That(builder.SourceCount(), Is.EqualTo(7));
        Assert.That(builder.SourceAt(0).Format, Is.EqualTo(ConfigFormat.Json));
        Assert.That(builder.SourceAt(0).Reload, Is.EqualTo(ConfigReload.OnChange));
        Assert.That(builder.SourceAt(1).Format, Is.EqualTo(ConfigFormat.Ini));
        Assert.That(builder.SourceAt(1).Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(builder.SourceAt(2).Format, Is.EqualTo(ConfigFormat.Properties));
        Assert.That(builder.SourceAt(3).Format, Is.EqualTo(ConfigFormat.DotEnv));
        Assert.That(builder.SourceAt(4).Format, Is.EqualTo(ConfigFormat.Xml));
        Assert.That(builder.SourceAt(5).Kind, Is.EqualTo(ConfigSourceKind.Directory));
        Assert.That(builder.SourceAt(5).Pattern, Is.EqualTo("*.json"));
        Assert.That(builder.SourceAt(6).Kind, Is.EqualTo(ConfigSourceKind.KeyPerFile));
    }

    [Test]
    public void SearchEntryPoints_RegisterCurrentParentDotEnvAndLayeredSources()
    {
        ConfigBuilder builder = new ConfigBuilder();

        builder
            .AddCurrentDirectoryFile("settings.json", ConfigOptional.Required)
            .AddParentDirectoryFile("parent.json", 2, ConfigOptional.Optional)
            .AddParentDirectoryFileFromDirectory("start", "selected.json", 3, ConfigOptional.Required)
            .AddCurrentDirectoryDotEnv(ConfigOptional.Optional)
            .AddParentDirectoryDotEnv(4, ConfigOptional.Required)
            .AddParentDirectoryDotEnvFromDirectory("start", 5, ConfigOptional.Optional)
            .AddEnvironmentFileLayers("appsettings.json", "Development", 6, ConfigOptional.Required)
            .AddEnvironmentFileLayersFromDirectory("start", "config.ini", "Test", 7, ConfigOptional.Optional);

        Assert.That(builder.SourceCount(), Is.EqualTo(8));
        Assert.That(builder.SourceAt(0).Kind, Is.EqualTo(ConfigSourceKind.Search));
        Assert.That(builder.SourceAt(0).Path, Is.EqualTo(""));
        Assert.That(builder.SourceAt(0).Pattern, Is.EqualTo("settings.json"));
        Assert.That(builder.SourceAt(0).ParentSearchLimit, Is.EqualTo(0));
        Assert.That(builder.SourceAt(1).ParentSearchLimit, Is.EqualTo(2));
        Assert.That(builder.SourceAt(2).Path, Is.EqualTo("start"));
        Assert.That(builder.SourceAt(3).Pattern, Is.EqualTo(".env"));
        Assert.That(builder.SourceAt(4).ParentSearchLimit, Is.EqualTo(4));
        Assert.That(builder.SourceAt(5).Path, Is.EqualTo("start"));
        Assert.That(builder.SourceAt(6).EnvironmentName, Is.EqualTo("Development"));
        Assert.That(builder.SourceAt(7).EnvironmentName, Is.EqualTo("Test"));
        Assert.That(builder.SourceAt(7).Optional, Is.EqualTo(ConfigOptional.Optional));
    }

    [Test]
    public void ParentSearchEntryPoints_RejectNegativeBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBuilder().AddParentDirectoryFile("settings.json", -1, ConfigOptional.Required));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBuilder().AddParentDirectoryDotEnv(-1, ConfigOptional.Required));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBuilder().AddEnvironmentFileLayers("settings.json", "Test", -1, ConfigOptional.Required));
    }

    [Test]
    public void GeneralAndEncodedSourceEntryPoints_RegisterDescriptorOptions()
    {
        MemoryStream stream = new MemoryStream();
        ConfigBuilder builder = new ConfigBuilder();

        builder
            .AddFile("config.custom", ConfigFormat.Ini, ConfigOptional.Optional, ConfigReload.OnChange)
            .AddFileWithEncoding("config.data", ConfigFormat.Flat, ConfigOptional.Required, ConfigReload.None, Encoding.Unicode)
            .AddFileByExtension("config.json", ConfigOptional.Required, ConfigReload.None)
            .AddFileByExtensionWithEncoding("config.properties", ConfigOptional.Optional, ConfigReload.OnChange, Encoding.BigEndianUnicode)
            .AddFlatFile("config.txt", ConfigOptional.Required, ConfigReload.OnChange)
            .AddStreamWithEncoding(stream, ConfigFormat.Properties, "stream", Encoding.Unicode)
            .AddDirectoryWithEncoding("files", "*.flat", ConfigOptional.Required, Encoding.Unicode)
            .AddKeyPerFileDirectoryWithEncoding("secrets", ConfigOptional.Optional, Encoding.Unicode);

        Assert.That(builder.SourceCount(), Is.EqualTo(8));
        Assert.That(builder.SourceAt(0).Format, Is.EqualTo(ConfigFormat.Ini));
        Assert.That(builder.SourceAt(0).UseExtensionFormat, Is.False);
        Assert.That(builder.SourceAt(0).Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(builder.SourceAt(0).Reload, Is.EqualTo(ConfigReload.OnChange));
        Assert.That(builder.SourceAt(0).TextEncoding, Is.SameAs(Encoding.UTF8));
        Assert.That(builder.SourceAt(1).TextEncoding, Is.SameAs(Encoding.Unicode));
        Assert.That(builder.SourceAt(2).UseExtensionFormat, Is.True);
        Assert.That(builder.SourceAt(3).TextEncoding, Is.SameAs(Encoding.BigEndianUnicode));
        Assert.That(builder.SourceAt(4).Format, Is.EqualTo(ConfigFormat.Flat));
        Assert.That(builder.SourceAt(5).StreamValue, Is.SameAs(stream));
        Assert.That(builder.SourceAt(5).TextEncoding, Is.SameAs(Encoding.Unicode));
        Assert.That(builder.SourceAt(6).Kind, Is.EqualTo(ConfigSourceKind.Directory));
        Assert.That(builder.SourceAt(6).TextEncoding, Is.SameAs(Encoding.Unicode));
        Assert.That(builder.SourceAt(7).Kind, Is.EqualTo(ConfigSourceKind.KeyPerFile));
        Assert.That(builder.SourceAt(7).TextEncoding, Is.SameAs(Encoding.Unicode));
    }

    [Test]
    public void AddEnvironmentAndCommandLineSources_RegisterMapsAndSwitches()
    {
        ConfigBuilder builder = new ConfigBuilder();
        ConfigMap envMap = new ConfigMap();
        envMap.Set("APP_HOST", ConfigValue.String("localhost"));
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("h", ConfigValue.String("host"));
        string[] args = new string[] { "-h", "localhost" };

        builder
            .AddEnvironment()
            .AddEnvironmentWithPrefix("APP_", "__")
            .AddEnvironmentMap(envMap, "APP_", "__")
            .AddCommandLine(args)
            .AddMappedCommandLine(args, switchMap);

        Assert.That(builder.SourceCount(), Is.EqualTo(5));
        Assert.That(builder.SourceAt(0).Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(builder.SourceAt(0).Prefix, Is.EqualTo(""));
        Assert.That(builder.SourceAt(1).Prefix, Is.EqualTo("APP_"));
        Assert.That(builder.SourceAt(2).EnvironmentValues!["APP_HOST"], Is.EqualTo("localhost"));
        Assert.That(builder.SourceAt(3).Kind, Is.EqualTo(ConfigSourceKind.CommandLine));
        Assert.That(builder.SourceAt(3).Args, Is.SameAs(args));
        Assert.That(builder.SourceAt(4).SwitchMap, Is.SameAs(switchMap));
    }

    [Test]
    public void EnvironmentOptionEntryPoints_RegisterModesMappingsAndInputKinds()
    {
        ConfigMap values = new ConfigMap();
        values.Set("APP_HOST", ConfigValue.String("localhost"));
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        dictionary["APP_HOST"] = "localhost";
        ConfigMap pathMap = new ConfigMap();
        pathMap.Set("APP_HOST", ConfigValue.String("service.host"));
        ConfigBuilder builder = new ConfigBuilder();

        builder
            .AddEnvironmentWithOptions("APP_", "__", ConfigEmptyEnvMode.Ignore, pathMap)
            .AddEnvironmentMapWithOptions(values, "APP_", "__", ConfigEmptyEnvMode.Keep, pathMap)
            .AddEnvironmentDictionaryWithOptions(dictionary, "APP_", "__", ConfigEmptyEnvMode.Ignore, pathMap);

        Assert.That(builder.SourceCount(), Is.EqualTo(3));
        Assert.That(builder.SourceAt(0).EnvironmentValues, Is.Null);
        Assert.That(builder.SourceAt(0).EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Ignore));
        Assert.That(builder.SourceAt(0).EnvironmentPathMap, Is.SameAs(pathMap));
        Assert.That(builder.SourceAt(1).EnvironmentValues!["APP_HOST"], Is.EqualTo("localhost"));
        Assert.That(builder.SourceAt(1).EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Keep));
        Assert.That(builder.SourceAt(1).EnvironmentPathMap, Is.SameAs(pathMap));
        Assert.That(builder.SourceAt(2).EnvironmentValues, Is.SameAs(dictionary));
        Assert.That(builder.SourceAt(2).EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Ignore));
        Assert.That(builder.SourceAt(2).EnvironmentPathMap, Is.SameAs(pathMap));
    }

    [Test]
    public void AddAlias_RegistersAliasesUsingCurrentDelimiter()
    {
        ConfigBuilder builder = new ConfigBuilder();

        builder.SetDelimiter("__").AddAlias("OLD__HOST", "NEW__HOST");

        Assert.That(builder.Aliases.Count(), Is.EqualTo(1));
        Assert.That(builder.Aliases.Resolve("OLD__HOST", "__"), Is.EqualTo("NEW__HOST"));
    }

    [Test]
    public void Build_LoadsSourcesAndMergesInRegistrationOrder()
    {
        ConfigMap first = new ConfigMap();
        first.SetPathDefault("service.host", ConfigValue.String("first"));
        first.SetPathDefault("service.port", ConfigValue.String("5432"));
        ConfigMap second = new ConfigMap();
        second.SetPathDefault("service.host", ConfigValue.String("second"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(first, "first").AddMemory(second, "second");

        Config config = builder.Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("second"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.host").SourceName, Is.EqualTo("second"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.host").PrecedenceRank, Is.EqualTo(2));
        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.port").StringValue, Is.EqualTo("5432"));
    }

    [Test]
    public void Build_UsesConfiguredMergeOptions()
    {
        ConfigArray firstServers = new ConfigArray();
        firstServers.Add(ConfigValue.String("first"));
        ConfigMap first = new ConfigMap();
        first.SetPathDefault("service.host", ConfigValue.String("first"));
        first.SetPathDefault("service.servers", ConfigValue.Array(firstServers));

        ConfigArray secondServers = new ConfigArray();
        secondServers.Add(ConfigValue.String("second"));
        ConfigMap second = new ConfigMap();
        second.SetPathDefault("service.host", ConfigValue.String("second"));
        second.SetPathDefault("service.servers", ConfigValue.Array(secondServers));
        second.SetPathDefault("service.password", ConfigValue.Null());

        ConfigBuilder builder = new ConfigBuilder();
        builder
            .SetMergeMode(ConfigMergeMode.Fallback)
            .SetArrayMergeMode(ConfigArrayMergeMode.Append)
            .SetNullMergeMode(ConfigNullMergeMode.Ignore)
            .AddMemory(first, "first")
            .AddMemory(second, "second");

        Config config = builder.Build();
        ConfigArray servers = config.Root.ObjectValue!.GetPathDefault("service.servers").ArrayValue!;

        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(servers.Count(), Is.EqualTo(1));
        Assert.That(servers.Get(0).StringValue, Is.EqualTo("first"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.password").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void Build_ReturnsSnapshotWithCopiedRootAndAliases()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("first"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory").AddAlias("old.host", "service.host");

        Config config = builder.Build();
        values.SetPathDefault("service.host", ConfigValue.String("changed"));
        builder.AddAlias("other.host", "service.host");

        Assert.That(config.Root.ObjectValue!.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(config.Aliases.Count(), Is.EqualTo(1));
        Assert.That(config.Aliases.ResolveDefault("old.host"), Is.EqualTo("service.host"));
        Assert.That(config.Aliases.ResolveDefault("other.host"), Is.EqualTo("other.host"));
    }

    [Test]
    public void Build_CarriesBuilderSettingsAndDiagnostics()
    {
        string missingPath = "missing-builder-file-" + Guid.NewGuid().ToString("N") + ".json";
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .SetDelimiter("__")
            .SetCaseMode(ConfigCaseMode.Insensitive)
            .AddJsonFile(missingPath, ConfigOptional.Required, ConfigReload.None);

        Config config = builder.Build();

        Assert.That(config.Delimiter, Is.EqualTo("__"));
        Assert.That(config.CaseMode, Is.EqualTo(ConfigCaseMode.Insensitive));
        Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(config.Diagnostics().Errors[0].Message, Is.EqualTo("missing required file"));
        Assert.That(config.Diagnostics().Errors[0].Path, Is.EqualTo(missingPath));
    }

    [Test]
    public void BuildDiagnostics_AggregatesSourceMergeAndTraceDiagnostics()
    {
        string missingPath = "missing-builder-diagnostics-file-" + Guid.NewGuid().ToString("N") + ".json";
        ConfigMap first = new ConfigMap();
        first.SetPathDefault("service.port", ConfigValue.String("5432"));
        ConfigMap second = new ConfigMap();
        ConfigMap port = new ConfigMap();
        port.Set("value", ConfigValue.String("6543"));
        second.SetPathDefault("service.port", ConfigValue.Object(port));
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .SetMergeMode(ConfigMergeMode.Strict)
            .AddMemory(first, "first")
            .AddMemory(second, "second")
            .AddJsonFile(missingPath, ConfigOptional.Required, ConfigReload.None);

        ConfigDiagnostics diagnostics = builder.BuildDiagnostics();

        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Errors[0].SourceName, Is.EqualTo("second"));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("service.port"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("strict merge type conflict"));
        Assert.That(diagnostics.Errors[1].Path, Is.EqualTo(missingPath));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("missing required file"));
        Assert.That(diagnostics.TraceCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Traces[0].Path, Is.EqualTo("service.port"));
        Assert.That(diagnostics.Traces[0].SourceName, Is.EqualTo("second"));
        Assert.That(diagnostics.Traces[0].Decision, Is.EqualTo(ConfigMergeDecision.StrictConflict));
        Assert.That(diagnostics.Traces[1].Path, Is.EqualTo("service.port"));
        Assert.That(diagnostics.Traces[1].SourceName, Is.EqualTo("first"));
        Assert.That(diagnostics.Traces[1].Decision, Is.EqualTo(ConfigMergeDecision.FinalSupplier));
    }

    [Test]
    public void BuildDiagnostics_ReturnsIndependentDiagnosticsSnapshot()
    {
        string missingPath = "missing-builder-diagnostics-copy-" + Guid.NewGuid().ToString("N") + ".json";
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddJsonFile(missingPath, ConfigOptional.Required, ConfigReload.None);

        ConfigDiagnostics diagnostics = builder.BuildDiagnostics();
        diagnostics.AddError("test", "extra", "mutated", 0, 0);

        ConfigDiagnostics secondDiagnostics = builder.BuildDiagnostics();

        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(secondDiagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(secondDiagnostics.Errors[0].Path, Is.EqualTo(missingPath));
    }

    [Test]
    public void AddDefaultAppSources_RegistersCommonSourcesInOverrideOrder()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "builder-app-sources-" + Guid.NewGuid().ToString("N"));
        string[] args = new string[] { "--service.host", "cli" };
        ConfigBuilder builder = new ConfigBuilder();

        builder.AddDefaultAppSourcesFromDirectory(directory, "Development", args);

        Assert.That(builder.SourceCount(), Is.EqualTo(5));
        Assert.That(builder.SourceAt(0).Kind, Is.EqualTo(ConfigSourceKind.File));
        Assert.That(builder.SourceAt(0).Path, Is.EqualTo(Path.Combine(directory, "appsettings.json")));
        Assert.That(builder.SourceAt(0).Format, Is.EqualTo(ConfigFormat.Json));
        Assert.That(builder.SourceAt(0).Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(builder.SourceAt(1).Path, Is.EqualTo(Path.Combine(directory, "appsettings.Development.json")));
        Assert.That(builder.SourceAt(2).Format, Is.EqualTo(ConfigFormat.DotEnv));
        Assert.That(builder.SourceAt(2).Path, Is.EqualTo(Path.Combine(directory, ".env")));
        Assert.That(builder.SourceAt(3).Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(builder.SourceAt(3).Delimiter, Is.EqualTo("__"));
        Assert.That(builder.SourceAt(4).Kind, Is.EqualTo(ConfigSourceKind.CommandLine));
        Assert.That(builder.SourceAt(4).Args, Is.SameAs(args));
    }

    [Test]
    public void AddDefaultAppSources_CanSkipEnvironmentSpecificFileAndIncludeDefaults()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "builder-app-sources-empty-env-" + Guid.NewGuid().ToString("N"));
        ConfigMap defaults = new ConfigMap();
        ConfigBuilder builder = new ConfigBuilder();

        builder.AddDefaultAppSourcesWithDefaultsFromDirectory(defaults, directory, "", new string[0]);

        Assert.That(builder.SourceCount(), Is.EqualTo(5));
        Assert.That(builder.SourceAt(0).Kind, Is.EqualTo(ConfigSourceKind.Defaults));
        Assert.That(builder.SourceAt(0).Values, Is.SameAs(defaults));
        Assert.That(builder.SourceAt(1).Path, Is.EqualTo(Path.Combine(directory, "appsettings.json")));
        Assert.That(builder.SourceAt(2).Path, Is.EqualTo(Path.Combine(directory, ".env")));
        Assert.That(builder.SourceAt(3).Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(builder.SourceAt(4).Kind, Is.EqualTo(ConfigSourceKind.CommandLine));
    }

    [Test]
    public void AddDefaultAppSources_BuildUsesCommonPrecedence()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "builder-app-build-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string root = "app" + Guid.NewGuid().ToString("N");
        ConfigMap defaults = new ConfigMap();
        defaults.SetPathDefault(root + ".host", ConfigValue.String("defaults"));
        File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{ \"" + root + "\": { \"host\": \"json\", \"port\": \"5000\" } }");
        File.WriteAllText(Path.Combine(directory, "appsettings.Development.json"), "{ \"" + root + "\": { \"host\": \"development-json\" } }");
        File.WriteAllText(Path.Combine(directory, ".env"), root + ".host=dotenv\n" + root + ".name=dotenv\n");
        string[] args = new string[] { "--" + root + ".host", "cli" };
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddDefaultAppSourcesWithDefaultsFromDirectory(defaults, directory, "Development", args);

        Config config = builder.Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".host").StringValue, Is.EqualTo("cli"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".host").SourceName, Is.EqualTo("command-line"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".port").StringValue, Is.EqualTo("5000"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".name").StringValue, Is.EqualTo("dotenv"));
    }

    [Test]
    public void Build_UsesExplicitSourceOrderAcrossCommonBuilderSourceTypes()
    {
        string root = "order" + Guid.NewGuid().ToString("N");
        ConfigMap defaults = new ConfigMap();
        defaults.SetPathDefault(root + ".defaultOnly", ConfigValue.String("defaults"));
        defaults.SetPathDefault(root + ".host", ConfigValue.String("defaults"));
        Dictionary<string, string> envMap = new Dictionary<string, string>();
        envMap["APP_" + root + "__host"] = "environment";
        envMap["APP_" + root + "__envOnly"] = "environment";
        string json = "{ \"" + root + "\": { \"host\": \"json\", \"jsonOnly\": \"json\" } }";
        string[] args = new string[] { "--" + root + ".host", "command-line", "--" + root + ".cliOnly", "command-line" };
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .AddDefaults(defaults)
            .AddString(json, ConfigFormat.Json, "json")
            .AddEnvironmentDictionary(envMap, "APP_", "__")
            .AddCommandLine(args);

        Config config = builder.Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".host").StringValue, Is.EqualTo("command-line"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".host").PrecedenceRank, Is.EqualTo(4));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".envOnly").SourceName, Is.EqualTo("environment"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".envOnly").PrecedenceRank, Is.EqualTo(3));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".jsonOnly").SourceName, Is.EqualTo("json"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".jsonOnly").PrecedenceRank, Is.EqualTo(2));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".defaultOnly").SourceName, Is.EqualTo("defaults"));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".defaultOnly").PrecedenceRank, Is.EqualTo(1));
        Assert.That(config.Root.ObjectValue!.GetPathDefault(root + ".cliOnly").SourceName, Is.EqualTo("command-line"));
    }

    [Test]
    public void BuildDiagnostics_DefaultAppSourcesTreatMissingFilesAsOptional()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-common-defaults-" + Guid.NewGuid().ToString("N"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddDefaultAppSourcesFromDirectory(directory, "Production", new string[0]);

        ConfigDiagnostics diagnostics = builder.BuildDiagnostics();

        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void AddDefaultAppSources_CurrentDirectoryOverloadUsesWorkingDirectory()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        ConfigBuilder builder = new ConfigBuilder();

        builder.AddDefaultAppSources("Staging", new string[0]);

        Assert.That(builder.SourceCount(), Is.EqualTo(5));
        Assert.That(builder.SourceAt(0).Path, Is.EqualTo(Path.Combine(currentDirectory, "appsettings.json")));
        Assert.That(builder.SourceAt(1).Path, Is.EqualTo(Path.Combine(currentDirectory, "appsettings.Staging.json")));
        Assert.That(builder.SourceAt(2).Path, Is.EqualTo(Path.Combine(currentDirectory, ".env")));
        Assert.That(builder.SourceAt(3).Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(builder.SourceAt(4).Kind, Is.EqualTo(ConfigSourceKind.CommandLine));
    }
}
