namespace config.Tests;

public class ConfigParserPolicyTests
{
    [Test]
    public void DuplicateKeyError_KeepsFirstJsonValueAndReportsSourceAwareError()
    {
        Config config = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.Error)
            .AddString("{\"service\":{\"host\":\"first\"},\"service\":{\"host\":\"second\"}}", ConfigFormat.Json, "settings.json")
            .Build();

        Assert.That(config.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(config.Diagnostics().Error(0).SourceName, Is.EqualTo("settings.json"));
        Assert.That(config.Diagnostics().Error(0).Path, Is.EqualTo("service"));
        Assert.That(config.Diagnostics().Error(0).Message, Is.EqualTo("duplicate JSON key"));
    }

    [Test]
    public void DuplicateKeyLastWins_ReplacesJsonAndFlatValues()
    {
        Config json = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.LastWins)
            .AddString("{\"service\":{\"host\":\"first\"},\"service\":{\"host\":\"second\"}}", ConfigFormat.Json, "settings.json")
            .Build();
        Config flat = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.LastWins)
            .AddString("host=first\nhost=second\n", ConfigFormat.Flat, "settings.flat")
            .Build();

        Assert.That(json.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(json.Diagnostics().HasErrors(), Is.False);
        Assert.That(flat.GetString("host"), Is.EqualTo("second"));
        Assert.That(flat.Diagnostics().HasErrors(), Is.False);
    }

    [Test]
    public void DuplicateKeyMergeObjects_RecursivelyCombinesJsonObjects()
    {
        string text = "{\"service\":{\"host\":\"api\",\"tls\":{\"enabled\":true}},\"service\":{\"port\":8443,\"tls\":{\"mode\":\"strict\"}}}";

        Config config = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.MergeObjects)
            .AddString(text, ConfigFormat.Json, "settings.json")
            .Build();

        Assert.That(config.GetString("service.host"), Is.EqualTo("api"));
        Assert.That(config.GetInt("service.port"), Is.EqualTo(8443));
        Assert.That(config.GetBool("service.tls.enabled"), Is.True);
        Assert.That(config.GetString("service.tls.mode"), Is.EqualTo("strict"));
        Assert.That(config.Diagnostics().HasErrors(), Is.False);
    }

    [Test]
    public void DuplicateKeyMergeObjects_RejectsNestedNonObjectDuplicates()
    {
        Config config = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.MergeObjects)
            .AddString("{\"service\":{\"host\":\"first\"},\"service\":{\"host\":\"second\"}}", ConfigFormat.Json, "settings.json")
            .Build();

        Assert.That(config.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(config.Diagnostics().Error(0).Path, Is.EqualTo("service.host"));
    }

    [Test]
    public void DotEnvExpansion_DefaultIsDisabledAndEnabledModeExpandsReferences()
    {
        string text = "HOST=localhost\nURL=http://${HOST}:8080\n";
        Config disabled = new ConfigBuilder()
            .AddString(text, ConfigFormat.DotEnv, ".env")
            .Build();
        Config enabled = new ConfigBuilder()
            .SetDotEnvExpansionMode(ConfigDotEnvExpansionMode.Enabled)
            .AddString(text, ConfigFormat.DotEnv, ".env")
            .Build();

        Assert.That(disabled.Root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://${HOST}:8080"));
        Assert.That(enabled.Root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://localhost:8080"));
        Assert.That(enabled.Diagnostics().HasErrors(), Is.False);
        Assert.That(enabled.Diagnostics().HasWarnings(), Is.False);
    }

    [Test]
    public void DotEnvMissingValuePolicies_EmptyPreserveOrRejectReferences()
    {
        string text = "URL=http://${HOST}:$PORT\n";
        Config empty = BuildDotEnv(text, ConfigDotEnvMissingValueMode.Empty);
        Config preserve = BuildDotEnv(text, ConfigDotEnvMissingValueMode.Preserve);
        Config error = BuildDotEnv(text, ConfigDotEnvMissingValueMode.Error);

        Assert.That(empty.Root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://:"));
        Assert.That(empty.Diagnostics().WarningCount(), Is.EqualTo(2));
        Assert.That(preserve.Root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://${HOST}:$PORT"));
        Assert.That(preserve.Diagnostics().WarningCount(), Is.EqualTo(2));
        Assert.That(error.Root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://:"));
        Assert.That(error.Diagnostics().ErrorCount(), Is.EqualTo(2));
        Assert.That(error.Diagnostics().Error(0).SourceName, Is.EqualTo(".env"));
        Assert.That(error.Diagnostics().Error(0).Path, Is.EqualTo("HOST"));
        Assert.That(error.Diagnostics().Error(0).LineNumber, Is.EqualTo(1));
        Assert.That(error.Diagnostics().Error(0).ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void DotEnvExpansion_MalformedReferenceReportsSourceLocation()
    {
        Config config = new ConfigBuilder()
            .SetDotEnvExpansionMode(ConfigDotEnvExpansionMode.Enabled)
            .AddString("URL=http://${HOST\n", ConfigFormat.DotEnv, ".env")
            .Build();

        Assert.That(config.GetString("URL"), Is.EqualTo("http://${HOST"));
        Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(config.Diagnostics().Error(0).SourceName, Is.EqualTo(".env"));
        Assert.That(config.Diagnostics().Error(0).Path, Is.EqualTo("URL"));
        Assert.That(config.Diagnostics().Error(0).LineNumber, Is.EqualTo(1));
        Assert.That(config.Diagnostics().Error(0).ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void ReloadController_PreservesParserPolicies()
    {
        ConfigReloadController controller = new ConfigBuilder()
            .SetDuplicateKeyMode(ConfigDuplicateKeyMode.LastWins)
            .SetDotEnvExpansionMode(ConfigDotEnvExpansionMode.Enabled)
            .AddString("HOST=first\nHOST=localhost\nURL=http://$HOST\n", ConfigFormat.DotEnv, ".env")
            .BuildReloading();

        Assert.That(controller.Current().GetString("URL"), Is.EqualTo("http://localhost"));
        Assert.That(controller.Current().Diagnostics().HasErrors(), Is.False);
    }

    [TestCase(ConfigFormat.Json, "{\"service\":", "malformed JSON input")]
    [TestCase(ConfigFormat.Ini, "missing assignment", "malformed INI line")]
    [TestCase(ConfigFormat.Ini, "name=\"unterminated", "malformed INI line")]
    [TestCase(ConfigFormat.Properties, "=missing key", "missing properties key")]
    [TestCase(ConfigFormat.Properties, "name=\\u12", "malformed properties escape")]
    [TestCase(ConfigFormat.DotEnv, "missing assignment", "malformed dotenv line")]
    [TestCase(ConfigFormat.DotEnv, "NAME=\"unterminated", "malformed dotenv line")]
    [TestCase(ConfigFormat.Flat, "missing assignment", "malformed flat key-value line")]
    [TestCase(ConfigFormat.Flat, "name=\"unterminated", "malformed flat key-value line")]
    [TestCase(ConfigFormat.Xml, "<settings><open></settings>", "malformed XML input")]
    public void MalformedText_AllParsersReturnSourceAwareDiagnostics(ConfigFormat format, string text, string message)
    {
        Config config = new ConfigBuilder()
            .AddString(text, format, "broken-source")
            .Build();

        Assert.That(config.Diagnostics().ErrorCount(), Is.GreaterThanOrEqualTo(1));
        Assert.That(config.Diagnostics().Error(0).SourceName, Is.EqualTo("broken-source"));
        Assert.That(config.Diagnostics().Error(0).Message, Is.EqualTo(message));
        Assert.That(config.Diagnostics().Error(0).LineNumber, Is.GreaterThan(0));
        Assert.That(config.Diagnostics().Error(0).ColumnNumber, Is.GreaterThan(0));
    }

    private static Config BuildDotEnv(string text, ConfigDotEnvMissingValueMode mode)
    {
        return new ConfigBuilder()
            .SetDotEnvExpansionMode(ConfigDotEnvExpansionMode.Enabled)
            .SetDotEnvMissingValueMode(mode)
            .AddString(text, ConfigFormat.DotEnv, ".env")
            .Build();
    }
}
