namespace config.Tests;

public class ConfigLoaderTests
{
    [Test]
    public void LoadJson_LoadsNestedValuesWithDeterministicSourceMetadata()
    {
        Config config = new ConfigLoader().LoadJson("{ \"service\": { \"host\": \"json\", \"port\": 8080 } }");

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("json"));
        Assert.That(config.GetInt("service.port"), Is.EqualTo(8080));
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("json"));
    }

    [Test]
    public void LoadDotEnv_LoadsValuesWithConservativeExpansionPolicy()
    {
        Config config = new ConfigLoader().LoadDotEnv("service.host=dotenv\nservice.url=http://$service.host\n");

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("dotenv"));
        Assert.That(config.GetString("service.url"), Is.EqualTo("http://$service.host"));
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("dotenv"));
    }

    [Test]
    public void LoadEnvironment_LoadsCurrentProcessValues()
    {
        string root = "CONFIG_T013_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        string key = root + "__HOST";

        try
        {
            Environment.SetEnvironmentVariable(key, "environment");

            Config config = new ConfigLoader().LoadEnvironment();

            Assert.That(config.Diagnostics().HasErrors(), Is.False);
            Assert.That(config.GetString(root + ".HOST"), Is.EqualTo("environment"));
            Assert.That(config.Value(root + ".HOST").SourceName, Is.EqualTo("environment"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Test]
    public void LoadCommandLine_LoadsSupportedSwitchForms()
    {
        string[] args = new string[]
        {
            "--service.host=long",
            "-service.port",
            "8080",
            "/service.enabled:true"
        };

        Config config = new ConfigLoader().LoadCommandLine(args);

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("service.host"), Is.EqualTo("long"));
        Assert.That(config.GetInt("service.port"), Is.EqualTo(8080));
        Assert.That(config.GetBool("service.enabled"), Is.True);
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("command-line"));
    }

    [Test]
    public void LoadDefaultApplicationFromDirectory_AppliesConventionalPrecedence()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "loader-defaults-" + Guid.NewGuid().ToString("N"));
        string root = "loader" + Guid.NewGuid().ToString("N");
        string environmentKey = root + "__environmentOnly";
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{ \"" + root + "\": { \"host\": \"json\", \"jsonOnly\": \"json\" } }");
        File.WriteAllText(Path.Combine(directory, "appsettings.Test.json"), "{ \"" + root + "\": { \"host\": \"environment-json\", \"environmentJsonOnly\": \"environment-json\" } }");
        File.WriteAllText(Path.Combine(directory, ".env"), root + ".host=dotenv\n" + root + ".dotEnvOnly=dotenv\n");

        try
        {
            Environment.SetEnvironmentVariable(environmentKey, "environment");
            string[] args = new string[] { "--" + root + ".host=command-line", "--" + root + ".commandOnly=command-line" };

            Config config = new ConfigLoader().LoadDefaultApplicationFromDirectory(directory, "Test", args);

            Assert.That(config.Diagnostics().HasErrors(), Is.False);
            Assert.That(config.GetString(root + ".host"), Is.EqualTo("command-line"));
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

    [Test]
    public void LoadDefaultApplication_UsesCurrentDirectoryAndAppliesCommandLineLast()
    {
        string path = "loader" + Guid.NewGuid().ToString("N") + ".value";

        Config config = new ConfigLoader().LoadDefaultApplication("", new string[] { "--" + path + "=command-line" });

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString(path), Is.EqualTo("command-line"));
        Assert.That(config.Value(path).SourceName, Is.EqualTo("command-line"));
    }
}
