using System;
using System.Collections.Generic;
using System.IO;

namespace config.Tests;

public class ConfigPublicDiagnosticsExportTests
{
    [Test]
    public void PublicDiagnostics_ExposeEveryCollectionAndReturnEmptyItemsForInvalidIndexes()
    {
        IConfigDiagnostics diagnostics = new ConfigDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.ErrorCount(), Is.EqualTo(0));
            Assert.That(diagnostics.WarningCount(), Is.EqualTo(0));
            Assert.That(diagnostics.TraceCount(), Is.EqualTo(0));
            Assert.That(diagnostics.HasErrors(), Is.False);
            Assert.That(diagnostics.HasWarnings(), Is.False);
            Assert.That(diagnostics.HasTraces(), Is.False);
        });

        ConfigError addedError = diagnostics.AddError("settings.json", "service.port", "invalid port", 4, 7);
        ConfigWarning addedWarning = diagnostics.AddWarning("environment", "service.host", "empty host", 0, 0);
        ConfigTrace addedTrace = diagnostics.AddTrace("merge", "override", "service.port", "replaced value");
        addedTrace.SourceGroup = "overrides";
        addedTrace.PrecedenceRank = 3;
        addedTrace.Decision = ConfigMergeDecision.Replaced;

        ConfigError error = diagnostics.Error(0);
        ConfigWarning warning = diagnostics.Warning(0);
        ConfigTrace trace = diagnostics.Trace(0);
        ConfigError negativeError = diagnostics.Error(-1);
        ConfigError largeError = diagnostics.Error(1);
        ConfigWarning negativeWarning = diagnostics.Warning(-1);
        ConfigWarning largeWarning = diagnostics.Warning(1);
        ConfigTrace negativeTrace = diagnostics.Trace(-1);
        ConfigTrace largeTrace = diagnostics.Trace(1);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.SameAs(addedError));
            Assert.That(warning, Is.SameAs(addedWarning));
            Assert.That(trace, Is.SameAs(addedTrace));
            Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
            Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
            Assert.That(diagnostics.TraceCount(), Is.EqualTo(1));
            Assert.That(diagnostics.HasErrors(), Is.True);
            Assert.That(diagnostics.HasWarnings(), Is.True);
            Assert.That(diagnostics.HasTraces(), Is.True);
            Assert.That(error.SourceName, Is.EqualTo("settings.json"));
            Assert.That(error.Path, Is.EqualTo("service.port"));
            Assert.That(error.Message, Is.EqualTo("invalid port"));
            Assert.That(error.LineNumber, Is.EqualTo(4));
            Assert.That(error.ColumnNumber, Is.EqualTo(7));
            Assert.That(warning.SourceName, Is.EqualTo("environment"));
            Assert.That(trace.Action, Is.EqualTo("merge"));
            Assert.That(trace.SourceGroup, Is.EqualTo("overrides"));
            Assert.That(trace.PrecedenceRank, Is.EqualTo(3));
            Assert.That(trace.Decision, Is.EqualTo(ConfigMergeDecision.Replaced));
            Assert.That(negativeError.SourceName, Is.Empty);
            Assert.That(largeError.Message, Is.Empty);
            Assert.That(negativeWarning.Path, Is.Empty);
            Assert.That(largeWarning.LineNumber, Is.EqualTo(0));
            Assert.That(negativeTrace.Action, Is.Empty);
            Assert.That(largeTrace.Decision, Is.EqualTo(ConfigMergeDecision.None));
        });
    }

    [Test]
    public void PublicDiagnostics_RenderDeterministicallyAndRedactOnlySecretPaths()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        diagnostics.AddTrace("merge", "override", "api.token", "selected abc123");
        diagnostics.AddWarning("environment", "PUBLIC_HOST", "selected localhost", 0, 0);
        diagnostics.AddError("settings.json", "database.password", "selected hunter2", 3, 4);
        ConfigRedactor redactor = new ConfigRedactor().SetReplacement("<hidden>");

        string display = diagnostics.ToDisplayString();
        string redacted = diagnostics.ToRedactedDisplayString(redactor);

        Assert.Multiple(() =>
        {
            Assert.That(display, Is.EqualTo("error settings.json:3:4 database.password: selected hunter2\nwarning environment PUBLIC_HOST: selected localhost\ntrace merge override api.token: selected abc123"));
            Assert.That(redacted, Is.EqualTo("error settings.json:3:4 database.password: <hidden>\nwarning environment PUBLIC_HOST: selected localhost\ntrace merge override api.token: <hidden>"));
        });
    }

    [Test]
    public void PublicExports_ReturnDeterministicResolvedJsonAndIndependentFlatCopies()
    {
        Config config = BuildExportConfig();

        string json = config.ToJson();
        Dictionary<string, string> firstFlat = config.ToFlatMap();
        ConfigDiagnostics firstDiagnostics = config.Diagnostics();
        firstFlat["service.host"] = "changed";
        firstFlat.Add("added", "changed");
        firstDiagnostics.Trace(0).Message = "changed";
        Dictionary<string, string> secondFlat = config.ToFlatMap();
        ConfigDiagnostics secondDiagnostics = config.Diagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("{\"service\":{\"enabled\":true,\"host\":\"localhost\",\"password\":null,\"tags\":[\"api\",2],\"url\":\"http://localhost\"},\"z\":1}"));
            Assert.That(secondFlat.Count, Is.EqualTo(7));
            Assert.That(secondFlat["service.enabled"], Is.EqualTo("true"));
            Assert.That(secondFlat["service.host"], Is.EqualTo("localhost"));
            Assert.That(secondFlat["service.password"], Is.Empty);
            Assert.That(secondFlat["service.tags.0"], Is.EqualTo("api"));
            Assert.That(secondFlat["service.tags.1"], Is.EqualTo("2"));
            Assert.That(secondFlat["service.url"], Is.EqualTo("http://localhost"));
            Assert.That(secondFlat["z"], Is.EqualTo("1"));
            Assert.That(secondFlat.ContainsKey("added"), Is.False);
            Assert.That(secondDiagnostics.Trace(0).Message, Is.Not.EqualTo("changed"));
        });
    }

    [Test]
    public void PublicRedaction_MatchesDefaultAndCustomTokensWithSelectedReplacementText()
    {
        ConfigRedactor redactor = new ConfigRedactor();
        int defaultCount = redactor.TokenCount();
        redactor.AddToken("");
        redactor.AddToken("credential");
        ConfigRedactor returned = redactor.SetReplacement("<hidden>");
        Config config = new ConfigBuilder()
            .AddString("{\"public\":{\"host\":\"localhost\"},\"database\":{\"password\":\"hunter2\"},\"apiToken\":\"abc123\",\"credential\":\"xyz\"}", ConfigFormat.Json, "secrets.json")
            .Build();

        string json = config.ToRedactedJson(redactor);
        Dictionary<string, string> flat = config.ToRedactedFlatMap(redactor);

        Assert.Multiple(() =>
        {
            Assert.That(defaultCount, Is.EqualTo(5));
            Assert.That(redactor.TokenCount(), Is.EqualTo(6));
            Assert.That(returned, Is.SameAs(redactor));
            Assert.That(redactor.ReplacementText(), Is.EqualTo("<hidden>"));
            Assert.That(redactor.IsSecretPath("DATABASE.Password"), Is.True);
            Assert.That(redactor.IsSecretPath("api.accessTOKEN"), Is.True);
            Assert.That(redactor.IsSecretPath("service.Secret"), Is.True);
            Assert.That(redactor.IsSecretPath("auth.apiKey"), Is.True);
            Assert.That(redactor.IsSecretPath("db.ConnectionString"), Is.True);
            Assert.That(redactor.IsSecretPath("service.credential"), Is.True);
            Assert.That(redactor.IsSecretPath("public.host"), Is.False);
            Assert.That(redactor.RedactValue("api.token", "abc123"), Is.EqualTo("<hidden>"));
            Assert.That(redactor.RedactValue("public.host", "localhost"), Is.EqualTo("localhost"));
            Assert.That(json, Is.EqualTo("{\"apiToken\":\"<hidden>\",\"credential\":\"<hidden>\",\"database\":{\"password\":\"<hidden>\"},\"public\":{\"host\":\"localhost\"}}"));
            Assert.That(flat["apiToken"], Is.EqualTo("<hidden>"));
            Assert.That(flat["credential"], Is.EqualTo("<hidden>"));
            Assert.That(flat["database.password"], Is.EqualTo("<hidden>"));
            Assert.That(flat["public.host"], Is.EqualTo("localhost"));
        });
    }

    [Test]
    public void PublicWriters_CreateNewFilesAndRefuseToReplaceExistingFiles()
    {
        string directory = NewDirectory("public-safe-write");
        string jsonPath = Path.Combine(directory, "config.json");
        string flatPath = Path.Combine(directory, "config.flat");
        Config config = BuildExportConfig();

        try
        {
            config.WriteJson(jsonPath);
            config.WriteFlat(flatPath);
            string json = File.ReadAllText(jsonPath);
            string flat = File.ReadAllText(flatPath);

            Assert.Throws<IOException>(() => config.WriteJson(jsonPath));
            Assert.Throws<IOException>(() => config.WriteFlat(flatPath));

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(jsonPath), Is.EqualTo(json));
                Assert.That(File.ReadAllText(flatPath), Is.EqualTo(flat));
                Assert.That(json, Is.EqualTo(config.ToJson()));
                Assert.That(flat, Is.EqualTo("service.enabled=\"true\"\nservice.host=\"localhost\"\nservice.password=\"\"\nservice.tags.0=\"api\"\nservice.tags.1=\"2\"\nservice.url=\"http://localhost\"\nz=\"1\"\n"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicWriters_ExplicitlyOverwriteJsonAndFlatFiles()
    {
        string directory = NewDirectory("public-overwrite");
        string jsonPath = Path.Combine(directory, "config.json");
        string flatPath = Path.Combine(directory, "config.flat");
        File.WriteAllText(jsonPath, "old-json");
        File.WriteAllText(flatPath, "old-flat");
        Config config = BuildExportConfig();

        try
        {
            config.OverwriteJson(jsonPath);
            config.OverwriteFlat(flatPath);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(jsonPath), Is.EqualTo(config.ToJson()));
                Assert.That(File.ReadAllText(flatPath), Does.StartWith("service.enabled=\"true\"\n"));
                Assert.That(File.ReadAllText(jsonPath), Is.Not.EqualTo("old-json"));
                Assert.That(File.ReadAllText(flatPath), Is.Not.EqualTo("old-flat"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static Config BuildExportConfig()
    {
        return new ConfigBuilder()
            .AddString("{\"z\":1,\"service\":{\"url\":\"http://${service.host}\",\"tags\":[\"api\",2],\"password\":null,\"host\":\"localhost\",\"enabled\":true}}", ConfigFormat.Json, "export.json")
            .Build();
    }

    private static string NewDirectory(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
