namespace config.Tests;

public class ConfigRedactorTests
{
    [Test]
    public void Redactor_DetectsDefaultSecretLikePathTokensCaseInsensitively()
    {
        ConfigRedactor redactor = new ConfigRedactor();

        Assert.That(redactor.TokenCount(), Is.EqualTo(5));
        Assert.That(redactor.IsSecretPath("database.password"), Is.True);
        Assert.That(redactor.IsSecretPath("api.accessToken"), Is.True);
        Assert.That(redactor.IsSecretPath("service.connectionString"), Is.True);
        Assert.That(redactor.IsSecretPath("public.host"), Is.False);
    }

    [Test]
    public void Redactor_RedactsValuesOnlyForSecretLikePaths()
    {
        ConfigRedactor redactor = new ConfigRedactor();

        Assert.That(redactor.RedactValue("database.password", "hunter2"), Is.EqualTo("[redacted]"));
        Assert.That(redactor.RedactValue("public.host", "localhost"), Is.EqualTo("localhost"));
    }

    [Test]
    public void Redactor_AllowsAdditionalTokens()
    {
        ConfigRedactor redactor = new ConfigRedactor();
        redactor.AddToken("credential");

        Assert.That(redactor.TokenCount(), Is.EqualTo(6));
        Assert.That(redactor.IsSecretPath("service.credential"), Is.True);
        Assert.That(redactor.RedactValue("service.credential", "abc"), Is.EqualTo("[redacted]"));
    }

    [Test]
    public void Diagnostics_CanRenderRedactedMessagesForSecretLikePaths()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        diagnostics.AddError("appsettings.json", "database.password", "value was hunter2", 7, 13);
        diagnostics.AddWarning("env", "PUBLIC_HOST", "value was localhost", 0, 0);
        diagnostics.AddTrace("merge", "env", "api.token", "value was abc123");

        ConfigRedactor redactor = new ConfigRedactor();

        Assert.That(
            diagnostics.ToRedactedDisplayString(redactor),
            Is.EqualTo("error appsettings.json:7:13 database.password: [redacted]\nwarning env PUBLIC_HOST: value was localhost\ntrace merge env api.token: [redacted]"));
        Assert.That(
            diagnostics.ToDisplayString(),
            Is.EqualTo("error appsettings.json:7:13 database.password: value was hunter2\nwarning env PUBLIC_HOST: value was localhost\ntrace merge env api.token: value was abc123"));
    }
}
