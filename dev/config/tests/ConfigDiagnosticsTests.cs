namespace config.Tests;

public class ConfigDiagnosticsTests
{
    [Test]
    public void DiagnosticItems_StoreLocationPathAndMessage()
    {
        ConfigError error = new ConfigError("appsettings.json", "database.host", "invalid host", 12, 5);
        ConfigWarning warning = new ConfigWarning("env", "PORT", "empty value ignored", 0, 0);
        ConfigTrace trace = new ConfigTrace("merge", "env", "database.host", "overrode file value");

        Assert.That(error.Location(), Is.EqualTo("appsettings.json:12:5"));
        Assert.That(error.PathLabel(), Is.EqualTo("database.host"));
        Assert.That(error.ToDisplayString(), Is.EqualTo("error appsettings.json:12:5 database.host: invalid host"));
        Assert.That(warning.Location(), Is.EqualTo("env"));
        Assert.That(warning.ToDisplayString(), Is.EqualTo("warning env PORT: empty value ignored"));
        Assert.That(trace.ToDisplayString(), Is.EqualTo("trace merge env database.host: overrode file value"));
        Assert.That(trace.SourceGroup, Is.Empty);
        Assert.That(trace.PrecedenceRank, Is.EqualTo(0));
        Assert.That(trace.Decision, Is.EqualTo(ConfigMergeDecision.None));
    }

    [Test]
    public void DiagnosticItems_UseStableFallbackLabels()
    {
        ConfigError error = new ConfigError("", "", "missing source", 0, 0);
        ConfigTrace trace = new ConfigTrace("read", "", "", "missing value");

        Assert.That(error.Location(), Is.EqualTo("(unknown)"));
        Assert.That(error.PathLabel(), Is.EqualTo("(root)"));
        Assert.That(error.ToDisplayString(), Is.EqualTo("error (unknown) (root): missing source"));
        Assert.That(trace.SourceLabel(), Is.EqualTo("(unknown)"));
        Assert.That(trace.PathLabel(), Is.EqualTo("(root)"));
    }

    [Test]
    public void ConfigDiagnostics_CollectsErrorsWarningsAndTracesInStableOrder()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigError error = diagnostics.AddError("appsettings.json", "database.host", "invalid host", 12, 5);
        ConfigWarning warning = diagnostics.AddWarning("env", "PORT", "empty value ignored", 0, 0);
        ConfigTrace trace = diagnostics.AddTrace("merge", "env", "database.host", "overrode file value");

        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(diagnostics.TraceCount(), Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.HasWarnings(), Is.True);
        Assert.That(diagnostics.HasTraces(), Is.True);
        Assert.That(diagnostics.Errors[0], Is.SameAs(error));
        Assert.That(diagnostics.Warnings[0], Is.SameAs(warning));
        Assert.That(diagnostics.Traces[0], Is.SameAs(trace));
        Assert.That(
            diagnostics.ToDisplayString(),
            Is.EqualTo("error appsettings.json:12:5 database.host: invalid host\nwarning env PORT: empty value ignored\ntrace merge env database.host: overrode file value"));
    }
}
