using System;
using System.IO;

namespace config.Tests;

public class ConfigResolutionPolicyTests
{
    [Test]
    public void SubstitutionLookups_CanIndependentlySelectConfigOrEnvironment()
    {
        string key = "CONFIG_RESOLUTION_POLICY_" + Guid.NewGuid().ToString("N");
        string? previous = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, "environment");

        try
        {
            ConfigMap values = new ConfigMap();
            values.Set(key, ConfigValue.String("config"));
            values.Set("selected", ConfigValue.String("${" + key + "}"));
            ConfigResolutionOptions environmentOnly = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Error, 8, ConfigOptional.Required, 8);
            ConfigResolutionOptions configOnly = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionMissingMode.Error, 8, ConfigOptional.Required, 8);

            ConfigBuilder environmentBuilder = new ConfigBuilder().SetResolutionOptions(environmentOnly).AddMemory(values, "memory");
            ConfigReloadController controller = environmentBuilder.BuildReloading();
            environmentOnly.ConfigPathLookup = ConfigSubstitutionLookupMode.Enabled;
            environmentOnly.EnvironmentFallback = ConfigSubstitutionLookupMode.Disabled;
            Config fromEnvironment = controller.Reload();
            Config fromConfig = new ConfigBuilder().SetResolutionOptions(configOnly).AddMemory(values, "memory").Build();

            Assert.That(fromEnvironment.GetString("selected"), Is.EqualTo("environment"));
            Assert.That(fromConfig.GetString("selected"), Is.EqualTo("config"));
            Assert.That(fromEnvironment.Diagnostics().HasErrors(), Is.False);
            Assert.That(fromConfig.Diagnostics().HasErrors(), Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }

    [Test]
    public void MissingSubstitutions_ReportOnlyRequiredReferences()
    {
        string missing = "CONFIG_REQUIRED_MISSING_" + Guid.NewGuid().ToString("N");
        ConfigMap values = new ConfigMap();
        values.Set("required", ConfigValue.String("before-${" + missing + "}-after"));
        values.Set("optional", ConfigValue.String("before-${?" + missing + "}-after"));
        ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionMissingMode.Error, 8, ConfigOptional.Required, 8);

        Config config = new ConfigBuilder().SetResolutionOptions(options).AddMemory(values, "memory").Build();
        ConfigDiagnostics diagnostics = config.Diagnostics();

        Assert.That(config.GetString("required"), Is.EqualTo("before--after"));
        Assert.That(config.GetString("optional"), Is.EqualTo("before--after"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Error(0).SourceName, Is.EqualTo("memory"));
        Assert.That(diagnostics.Error(0).Path, Is.EqualTo("required"));
        Assert.That(diagnostics.Error(0).Message, Is.EqualTo("missing substitution reference: " + missing));
    }

    [Test]
    public void SubstitutionCyclesAndDepthExhaustion_AppearInBuildDiagnostics()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("cycle.a", ConfigValue.String("${cycle.b}"));
        values.SetPathDefault("cycle.b", ConfigValue.String("${cycle.a}"));
        values.SetPathDefault("chain.a", ConfigValue.String("${chain.b}"));
        values.SetPathDefault("chain.b", ConfigValue.String("${chain.c}"));
        values.SetPathDefault("chain.c", ConfigValue.String("end"));
        ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionMissingMode.Error, 1, ConfigOptional.Required, 8);

        Config config = new ConfigBuilder().SetResolutionOptions(options).AddMemory(values, "memory").Build();
        ConfigDiagnostics diagnostics = config.Diagnostics();

        Assert.That(config.GetString("cycle.a"), Is.Empty);
        Assert.That(config.GetString("chain.a"), Is.Empty);
        Assert.That(config.GetString("chain.b"), Is.EqualTo("end"));
        Assert.That(HasErrorMessage(diagnostics, "substitution cycle:"), Is.True);
        Assert.That(HasErrorMessage(diagnostics, "substitution depth limit exceeded"), Is.True);
    }

    [Test]
    public void MissingIncludes_CanBeRequiredOrOptional()
    {
        string directory = NewDirectory("include-optional");
        string mainPath = Path.Combine(directory, "main.json");
        string missingPath = Path.Combine(directory, "missing.json");
        File.WriteAllText(mainPath, "{\"include\":\"missing.json\",\"service\":{\"host\":\"main\"}}");

        try
        {
            ConfigResolutionOptions optional = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 32, ConfigOptional.Optional, 8);
            ConfigResolutionOptions required = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 32, ConfigOptional.Required, 8);

            Config optionalConfig = new ConfigBuilder().SetResolutionOptions(optional).AddFileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None).Build();
            Config requiredConfig = new ConfigBuilder().SetResolutionOptions(required).AddFileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None).Build();

            Assert.That(optionalConfig.GetString("service.host"), Is.EqualTo("main"));
            Assert.That(optionalConfig.Diagnostics().HasErrors(), Is.False);
            Assert.That(requiredConfig.Diagnostics().ErrorCount(), Is.EqualTo(1));
            Assert.That(requiredConfig.Diagnostics().Error(0).SourceName, Is.EqualTo(missingPath));
            Assert.That(requiredConfig.Diagnostics().Error(0).Message, Is.EqualTo("missing required file"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludeDepthExhaustion_StopsRecursiveLoadingAndReportsTheTarget()
    {
        string directory = NewDirectory("include-depth");
        string mainPath = Path.Combine(directory, "main.json");
        string secondPath = Path.Combine(directory, "second.json");
        string thirdPath = Path.Combine(directory, "third.json");
        File.WriteAllText(mainPath, "{\"include\":\"second.json\",\"main\":\"main\"}");
        File.WriteAllText(secondPath, "{\"include\":\"third.json\",\"second\":\"second\"}");
        File.WriteAllText(thirdPath, "{\"third\":\"third\"}");

        try
        {
            ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 32, ConfigOptional.Required, 1);
            Config config = new ConfigBuilder().SetResolutionOptions(options).AddFileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None).Build();

            Assert.That(config.GetString("main"), Is.EqualTo("main"));
            Assert.That(config.GetString("second"), Is.EqualTo("second"));
            Assert.That(config.Has("third"), Is.False);
            Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
            Assert.That(config.Diagnostics().Error(0).SourceName, Is.EqualTo(thirdPath));
            Assert.That(config.Diagnostics().Error(0).Message, Is.EqualTo("include depth limit exceeded"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludeCycles_RemainErrorsWithOptionalMissingFiles()
    {
        string directory = NewDirectory("include-cycle-policy");
        string firstPath = Path.Combine(directory, "first.json");
        string secondPath = Path.Combine(directory, "second.json");
        File.WriteAllText(firstPath, "{\"include\":\"second.json\",\"first\":\"first\"}");
        File.WriteAllText(secondPath, "{\"include\":\"first.json\",\"second\":\"second\"}");

        try
        {
            ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 32, ConfigOptional.Optional, 8);
            Config config = new ConfigBuilder().SetResolutionOptions(options).AddFileByExtension(firstPath, ConfigOptional.Required, ConfigReload.None).Build();

            Assert.That(config.GetString("first"), Is.EqualTo("first"));
            Assert.That(config.GetString("second"), Is.EqualTo("second"));
            Assert.That(config.Diagnostics().ErrorCount(), Is.EqualTo(1));
            Assert.That(config.Diagnostics().Error(0).Message, Is.EqualTo("include cycle"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void InvalidIncludeRootsAndUnsupportedFormats_AppearInDiagnostics()
    {
        string directory = NewDirectory("include-invalid-root");
        string mainPath = Path.Combine(directory, "main.json");
        string scalarPath = Path.Combine(directory, "scalar.json");
        string unsupportedPath = Path.Combine(directory, "values.custom");
        File.WriteAllText(mainPath, "{\"includes\":[\"scalar.json\",\"values.custom\"],\"main\":\"main\"}");
        File.WriteAllText(scalarPath, "42");
        File.WriteAllText(unsupportedPath, "value=unsupported");

        try
        {
            Config config = new ConfigBuilder().AddFileByExtension(mainPath, ConfigOptional.Required, ConfigReload.None).Build();
            ConfigDiagnostics diagnostics = config.Diagnostics();

            Assert.That(config.GetString("main"), Is.EqualTo("main"));
            Assert.That(HasErrorMessage(diagnostics, "invalid include root"), Is.True);
            Assert.That(HasErrorMessage(diagnostics, "unsupported file extension"), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResolutionOptions_RejectNegativeDepthLimits()
    {
        ConfigResolutionOptions substitution = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, -1, ConfigOptional.Required, 8);
        ConfigResolutionOptions includes = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 8, ConfigOptional.Required, -1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBuilder().SetResolutionOptions(substitution));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBuilder().SetResolutionOptions(includes));
    }

    private static bool HasErrorMessage(ConfigDiagnostics diagnostics, string text)
    {
        int index = 0;
        while (index < diagnostics.ErrorCount())
        {
            if (diagnostics.Error(index).Message.StartsWith(text, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    private static string NewDirectory(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
