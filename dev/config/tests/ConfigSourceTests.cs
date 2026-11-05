namespace config.Tests;

public class ConfigSourceTests
{
    [Test]
    public void SourceKindEnum_ListsDescriptorKinds()
    {
        Assert.That(Enum.GetNames<ConfigSourceKind>(), Is.EqualTo(new string[] { "Memory", "Defaults", "ProgrammaticOverride", "File", "Stream", "String", "Directory", "KeyPerFile", "Environment", "CommandLine", "Search" }));
    }

    [Test]
    public void MemoryDefaultsAndOverrideSourcesCaptureDescriptorData()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("host", ConfigValue.String("localhost"));

        ConfigSource memory = ConfigSource.Memory(values, "memory");
        ConfigSource defaults = ConfigSource.Defaults(values);
        ConfigSource programmaticOverride = ConfigSource.ProgrammaticOverride("host", "override", "override");

        Assert.That(memory.Kind, Is.EqualTo(ConfigSourceKind.Memory));
        Assert.That(memory.Values, Is.SameAs(values));
        Assert.That(memory.SourceName, Is.EqualTo("memory"));
        Assert.That(defaults.Kind, Is.EqualTo(ConfigSourceKind.Defaults));
        Assert.That(defaults.SourceName, Is.EqualTo("defaults"));
        Assert.That(programmaticOverride.Kind, Is.EqualTo(ConfigSourceKind.ProgrammaticOverride));
        Assert.That(programmaticOverride.OverridePath, Is.EqualTo("host"));
        Assert.That(programmaticOverride.OverrideValue, Is.EqualTo("override"));
    }

    [Test]
    public void FileStreamStringAndDirectorySourcesCaptureDescriptorData()
    {
        MemoryStream stream = new MemoryStream();

        ConfigSource file = ConfigSource.File("appsettings.json", ConfigFormat.Json, ConfigOptional.Optional, ConfigReload.OnChange, "file");
        ConfigSource streamSource = ConfigSource.StreamSource(stream, ConfigFormat.Ini, "stream");
        ConfigSource stringSource = ConfigSource.StringSource("host=localhost", ConfigFormat.Flat, "string");
        ConfigSource directory = ConfigSource.DirectorySource("config", "*.json", ConfigOptional.Required, "directory");
        ConfigSource keyPerFile = ConfigSource.KeyPerFileDirectory("secrets", ConfigOptional.Optional, "key-per-file");

        Assert.That(file.Kind, Is.EqualTo(ConfigSourceKind.File));
        Assert.That(file.Path, Is.EqualTo("appsettings.json"));
        Assert.That(file.Format, Is.EqualTo(ConfigFormat.Json));
        Assert.That(file.UseExtensionFormat, Is.False);
        Assert.That(file.Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(file.Reload, Is.EqualTo(ConfigReload.OnChange));
        Assert.That(streamSource.Kind, Is.EqualTo(ConfigSourceKind.Stream));
        Assert.That(streamSource.StreamValue, Is.SameAs(stream));
        Assert.That(streamSource.Format, Is.EqualTo(ConfigFormat.Ini));
        Assert.That(stringSource.Kind, Is.EqualTo(ConfigSourceKind.String));
        Assert.That(stringSource.Text, Is.EqualTo("host=localhost"));
        Assert.That(directory.Kind, Is.EqualTo(ConfigSourceKind.Directory));
        Assert.That(directory.Pattern, Is.EqualTo("*.json"));
        Assert.That(keyPerFile.Kind, Is.EqualTo(ConfigSourceKind.KeyPerFile));
        Assert.That(keyPerFile.Path, Is.EqualTo("secrets"));
    }

    [Test]
    public void FileByExtensionSourceCapturesDescriptorData()
    {
        ConfigSource file = ConfigSource.FileByExtension("appsettings.json", ConfigOptional.Optional, ConfigReload.OnChange, "file");

        Assert.That(file.Kind, Is.EqualTo(ConfigSourceKind.File));
        Assert.That(file.Path, Is.EqualTo("appsettings.json"));
        Assert.That(file.UseExtensionFormat, Is.True);
        Assert.That(file.Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(file.Reload, Is.EqualTo(ConfigReload.OnChange));
        Assert.That(file.SourceName, Is.EqualTo("file"));
    }

    [Test]
    public void SearchSourceCapturesBoundEnvironmentAndFileData()
    {
        ConfigSource source = ConfigSource.SearchFile("start", "appsettings.json", 3, "Development", ConfigOptional.Optional, "search");

        Assert.That(source.Kind, Is.EqualTo(ConfigSourceKind.Search));
        Assert.That(source.Path, Is.EqualTo("start"));
        Assert.That(source.Pattern, Is.EqualTo("appsettings.json"));
        Assert.That(source.ParentSearchLimit, Is.EqualTo(3));
        Assert.That(source.EnvironmentName, Is.EqualTo("Development"));
        Assert.That(source.Optional, Is.EqualTo(ConfigOptional.Optional));
        Assert.That(source.UseExtensionFormat, Is.True);
    }

    [Test]
    public void EnvironmentAndCommandLineSourcesCaptureDescriptorData()
    {
        string[] args = new string[] { "--host", "localhost" };
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("h", ConfigValue.String("host"));
        ConfigMap envPathMap = new ConfigMap();
        envPathMap.Set("DATABASE_URL", ConfigValue.String("database.url"));
        Dictionary<string, string> envValues = new Dictionary<string, string>();
        envValues["APP_HOST"] = "localhost";

        ConfigSource environment = ConfigSource.Environment("APP_", "__", "env");
        ConfigSource environmentMap = ConfigSource.EnvironmentMap(envValues, "APP_", "__", "env-map");
        ConfigSource environmentWithMappings = ConfigSource.EnvironmentWithMappings("APP_", "__", envPathMap, "env-mapped");
        ConfigSource environmentMapWithMappings = ConfigSource.EnvironmentMapWithMappings(envValues, "APP_", "__", envPathMap, "env-map-mapped");
        ConfigSource commandLine = ConfigSource.CommandLine(args, switchMap, "args");

        Assert.That(environment.Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(environment.Prefix, Is.EqualTo("APP_"));
        Assert.That(environment.Delimiter, Is.EqualTo("__"));
        Assert.That(environment.EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Keep));
        Assert.That(environmentMap.Kind, Is.EqualTo(ConfigSourceKind.Environment));
        Assert.That(environmentMap.EnvironmentValues, Is.SameAs(envValues));
        Assert.That(environmentMap.Prefix, Is.EqualTo("APP_"));
        Assert.That(environmentMap.Delimiter, Is.EqualTo("__"));
        Assert.That(environmentMap.EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Keep));
        Assert.That(environmentWithMappings.EnvironmentPathMap, Is.SameAs(envPathMap));
        Assert.That(environmentMapWithMappings.EnvironmentValues, Is.SameAs(envValues));
        Assert.That(environmentMapWithMappings.EnvironmentPathMap, Is.SameAs(envPathMap));
        Assert.That(commandLine.Kind, Is.EqualTo(ConfigSourceKind.CommandLine));
        Assert.That(commandLine.Args, Is.SameAs(args));
        Assert.That(commandLine.SwitchMap, Is.SameAs(switchMap));
    }

    [Test]
    public void SourceResult_HoldsRootAndDiagnosticsTogether()
    {
        ConfigValue root = ConfigValue.Object(new ConfigMap());
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigSourceResult result = new ConfigSourceResult(root, diagnostics);

        Assert.That(result.Root, Is.SameAs(root));
        Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
    }

    [Test]
    public void Copy_CreatesIndependentCopiesForMutableDescriptorData()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        string[] args = new string[] { "-h", "localhost" };
        ConfigMap switchMap = new ConfigMap();
        switchMap.Set("h", ConfigValue.String("service.host"));
        Dictionary<string, string> envValues = new Dictionary<string, string>();
        envValues["APP_HOST"] = "localhost";
        ConfigMap envPathMap = new ConfigMap();
        envPathMap.Set("APP_HOST", ConfigValue.String("service.host"));
        ConfigSource source = ConfigSource.CommandLine(args, switchMap, "args");
        source.Values = values;
        source.EnvironmentValues = envValues;
        source.EnvironmentPathMap = envPathMap;
        source.EmptyEnvMode = ConfigEmptyEnvMode.Ignore;

        ConfigSource copy = source.Copy();
        values.SetPathDefault("service.host", ConfigValue.String("changed"));
        args[0] = "-p";
        switchMap.Set("h", ConfigValue.String("service.port"));
        envValues["APP_HOST"] = "changed";
        envPathMap.Set("APP_HOST", ConfigValue.String("service.port"));

        Assert.That(copy, Is.Not.SameAs(source));
        Assert.That(copy.Values, Is.Not.SameAs(values));
        Assert.That(copy.Values.GetPathDefault("service.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(copy.Args, Is.Not.SameAs(args));
        Assert.That(copy.Args, Is.EqualTo(new string[] { "-h", "localhost" }));
        Assert.That(copy.SwitchMap, Is.Not.SameAs(switchMap));
        Assert.That(copy.SwitchMap.Get("h").StringValue, Is.EqualTo("service.host"));
        Assert.That(copy.EnvironmentValues, Is.Not.SameAs(envValues));
        Assert.That(copy.EnvironmentValues["APP_HOST"], Is.EqualTo("localhost"));
        Assert.That(copy.EnvironmentPathMap, Is.Not.SameAs(envPathMap));
        Assert.That(copy.EnvironmentPathMap.Get("APP_HOST").StringValue, Is.EqualTo("service.host"));
        Assert.That(copy.EmptyEnvMode, Is.EqualTo(ConfigEmptyEnvMode.Ignore));
    }
}
