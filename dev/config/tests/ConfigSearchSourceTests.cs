namespace config.Tests;

[NonParallelizable]
public class ConfigSearchSourceTests
{
    [Test]
    public void CurrentDirectorySources_LoadNamedFileAndDotEnvAtBuildTime()
    {
        string firstDirectory = NewDirectory("current-first");
        string secondDirectory = NewDirectory("current-second");
        File.WriteAllText(Path.Combine(firstDirectory, "settings.json"), "{\"location\":\"first\"}");
        File.WriteAllText(Path.Combine(secondDirectory, "settings.json"), "{\"location\":\"second\"}");
        File.WriteAllText(Path.Combine(secondDirectory, ".env"), "DOTENV_VALUE=loaded\n");
        ConfigBuilder builder = new ConfigBuilder()
            .AddCurrentDirectoryFile("settings.json", ConfigOptional.Required)
            .AddCurrentDirectoryDotEnv(ConfigOptional.Required);

        Config config = BuildFromCurrentDirectory(builder, secondDirectory);

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("location"), Is.EqualTo("second"));
        Assert.That(config.GetString("DOTENV_VALUE"), Is.EqualTo("loaded"));
        Assert.That(config.Value("location").SourceName, Is.EqualTo(Path.Combine(secondDirectory, "settings.json")));
    }

    [Test]
    public void ParentSearch_LoadsOnlyNearestMatchWithinBound()
    {
        string root = NewDirectory("nearest");
        string child = Path.Combine(root, "child");
        string grandchild = Path.Combine(child, "grandchild");
        Directory.CreateDirectory(grandchild);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"location\":\"root\"}");
        string childPath = Path.Combine(child, "settings.json");
        File.WriteAllText(childPath, "{\"location\":\"child\"}");

        Config config = new ConfigBuilder()
            .AddParentDirectoryFileFromDirectory(grandchild, "settings.json", 2, ConfigOptional.Required)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("location"), Is.EqualTo("child"));
        Assert.That(config.Value("location").SourceName, Is.EqualTo(childPath));
    }

    [Test]
    public void ParentSearch_EnforcesExactBoundAndOptionalBehavior()
    {
        string root = NewDirectory("bounds");
        string child = Path.Combine(root, "child");
        string grandchild = Path.Combine(child, "grandchild");
        Directory.CreateDirectory(grandchild);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"found\":true}");

        Config tooShallow = new ConfigBuilder()
            .AddParentDirectoryFileFromDirectory(grandchild, "settings.json", 1, ConfigOptional.Required)
            .Build();
        Config exact = new ConfigBuilder()
            .AddParentDirectoryFileFromDirectory(grandchild, "settings.json", 2, ConfigOptional.Required)
            .Build();
        Config optional = new ConfigBuilder()
            .AddParentDirectoryFileFromDirectory(grandchild, "missing.json", 2, ConfigOptional.Optional)
            .Build();

        Assert.That(tooShallow.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(tooShallow.Diagnostics().Error(0).SourceName, Is.EqualTo("settings.json"));
        Assert.That(tooShallow.Diagnostics().Error(0).Path, Is.EqualTo("settings.json"));
        Assert.That(tooShallow.Diagnostics().Error(0).Message, Is.EqualTo("missing required search file"));
        Assert.That(exact.GetBool("found"), Is.True);
        Assert.That(exact.Diagnostics().HasErrors(), Is.False);
        Assert.That(optional.Diagnostics().HasErrors(), Is.False);
        Assert.That(optional.Keys(), Is.Empty);
    }

    [Test]
    public void ParentDotEnvSources_SelectNearestFileForCurrentAndExplicitStarts()
    {
        string root = NewDirectory("dotenv-nearest");
        string child = Path.Combine(root, "child");
        string grandchild = Path.Combine(child, "grandchild");
        Directory.CreateDirectory(grandchild);
        File.WriteAllText(Path.Combine(root, ".env"), "LOCATION=root\n");
        string childPath = Path.Combine(child, ".env");
        File.WriteAllText(childPath, "LOCATION=child\n");
        Config fromExplicit = new ConfigBuilder()
            .AddParentDirectoryDotEnvFromDirectory(grandchild, 2, ConfigOptional.Required)
            .Build();
        ConfigBuilder currentBuilder = new ConfigBuilder()
            .AddParentDirectoryDotEnv(2, ConfigOptional.Required);

        Config fromCurrent = BuildFromCurrentDirectory(currentBuilder, grandchild);

        Assert.That(fromExplicit.GetString("LOCATION"), Is.EqualTo("child"));
        Assert.That(fromCurrent.GetString("LOCATION"), Is.EqualTo("child"));
        Assert.That(fromCurrent.Value("LOCATION").SourceName, Is.EqualTo(childPath));
    }

    [Test]
    public void EnvironmentLayers_UseSelectedBaseDirectoryAndLaterEnvironmentPrecedence()
    {
        string root = NewDirectory("layers");
        string child = Path.Combine(root, "child");
        string grandchild = Path.Combine(child, "grandchild");
        Directory.CreateDirectory(grandchild);
        File.WriteAllText(Path.Combine(root, "appsettings.json"), "{\"service\":{\"host\":\"root\"}}");
        File.WriteAllText(Path.Combine(root, "appsettings.Test.json"), "{\"rootEnvironment\":true}");
        string basePath = Path.Combine(child, "appsettings.json");
        string environmentPath = Path.Combine(child, "appsettings.Test.json");
        File.WriteAllText(basePath, "{\"service\":{\"host\":\"base\",\"port\":5000},\"baseOnly\":true}");
        File.WriteAllText(environmentPath, "{\"service\":{\"host\":\"environment\"},\"environmentOnly\":true}");

        Config config = new ConfigBuilder()
            .AddEnvironmentFileLayersFromDirectory(grandchild, "appsettings.json", "Test", 2, ConfigOptional.Required)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("environment"));
        Assert.That(config.GetInt("service.port"), Is.EqualTo(5000));
        Assert.That(config.GetBool("baseOnly"), Is.True);
        Assert.That(config.GetBool("environmentOnly"), Is.True);
        Assert.That(config.Has("rootEnvironment"), Is.False);
        Assert.That(config.Value("service.port").SourceName, Is.EqualTo(basePath));
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo(environmentPath));
    }

    [Test]
    public void CurrentEnvironmentLayers_LoadBaseWhenEnvironmentSiblingIsMissing()
    {
        string root = NewDirectory("current-layers");
        string child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        File.WriteAllText(Path.Combine(root, "appsettings.json"), "{\"baseOnly\":true}");
        ConfigBuilder builder = new ConfigBuilder()
            .AddEnvironmentFileLayers("appsettings.json", "Missing", 1, ConfigOptional.Required);

        Config config = BuildFromCurrentDirectory(builder, child);

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetBool("baseOnly"), Is.True);
    }

    [Test]
    public void SearchReload_SelectsNewlyCreatedNearerFile()
    {
        string root = NewDirectory("reload-nearest");
        string child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"location\":\"root\"}");
        ConfigReloadController controller = new ConfigBuilder()
            .AddParentDirectoryFileFromDirectory(child, "settings.json", 1, ConfigOptional.Required)
            .BuildReloading();

        File.WriteAllText(Path.Combine(child, "settings.json"), "{\"location\":\"child\"}");
        Config reloaded = controller.Reload();

        Assert.That(reloaded.Diagnostics().HasErrors(), Is.False);
        Assert.That(reloaded.GetString("location"), Is.EqualTo("child"));
    }

    private static Config BuildFromCurrentDirectory(ConfigBuilder builder, string directory)
    {
        string previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directory);
            return builder.Build();
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private static string NewDirectory(string label)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-search-" + label + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
