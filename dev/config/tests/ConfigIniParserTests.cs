namespace config.Tests;

public class ConfigIniParserTests
{
    [Test]
    public void Parse_ReadsSectionsCommentsAndNestedKeys()
    {
        string text = "# global comment\napp.name = config\n[database.primary]\nhost = localhost ; inline comment\nport: 5432\n[features]\ncache.enabled = true\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 20, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("app.name").StringValue, Is.EqualTo("config"));
        Assert.That(map.GetPathDefault("database.primary.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.primary.port").StringValue, Is.EqualTo("5432"));
        Assert.That(map.GetPathDefault("features.cache.enabled").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Parse_UnquotesValuesAndKeepsCommentMarkersInsideQuotes()
    {
        string text = "[server]\nmessage = \"hello # still text\"\npath = 'c:\\\\temp\\tfiles'\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("server.message").StringValue, Is.EqualTo("hello # still text"));
        Assert.That(map.GetPathDefault("server.path").StringValue, Is.EqualTo("c:\\temp\tfiles"));
    }

    [Test]
    public void Parse_StoresSourceMetadataOnParsedValues()
    {
        string text = "[database]\n  host = localhost\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 12, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigValue host = root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(host.SourceName, Is.EqualTo("settings.ini"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(12));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
        Assert.That(host.LineNumber, Is.EqualTo(2));
        Assert.That(host.ColumnNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_DuplicateKeyErrorModeKeepsFirstValueAndReportsDiagnostic()
    {
        string text = "host = first\nhost = second\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("first"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("host"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("duplicate INI key"));
    }

    [Test]
    public void Parse_DuplicateKeyLastWinsModeReplacesEarlierValue()
    {
        string text = "host = first\nhost = second\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.LastWins, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("second"));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_SupportsSemicolonCommentsAroundSections()
    {
        string text = "; file comment\n[server] ; section comment\nhost = localhost ; value comment\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(root.ObjectValue!.GetPathDefault("server.host").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Parse_DuplicateKeysInsideSectionsReportFullNestedPath()
    {
        string text = "[server]\nhost = first\nhost = second\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("server.host").StringValue, Is.EqualTo("first"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("server.host"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_MalformedLinesReportDiagnosticsAndContinue()
    {
        string text = "[server\n  missing assignment\n  = missing key\nhost = localhost\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("localhost"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(3));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("malformed INI line"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(1));
        Assert.That(diagnostics.Errors[1].ColumnNumber, Is.EqualTo(3));
        Assert.That(diagnostics.Errors[2].Message, Is.EqualTo("missing INI key"));
    }

    [Test]
    public void Parse_NestedPathsCanUseEscapedSectionSegments()
    {
        string text = "[database\\.primary]\nhost = localhost\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.Has("database.primary"), Is.True);
        Assert.That(map.GetPathDefault("database\\.primary.host").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Parse_ComplexValidFileHandlesCrlfEscapedKeysAndQuotedSeparators()
    {
        string text = "[service]\r\nurl = \"https://example.test/path?a=1#fragment\"\r\nmessage: 'ready; still part of value'\r\n[service.routes]\r\napi\\.v1 = \"/v1:stable\" ; inline comment\r\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "settings.ini", 14, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;
        ConfigValue route = map.GetPathDefault("service.routes.api\\.v1");

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("service.url").StringValue, Is.EqualTo("https://example.test/path?a=1#fragment"));
        Assert.That(map.GetPathDefault("service.message").StringValue, Is.EqualTo("ready; still part of value"));
        Assert.That(route.StringValue, Is.EqualTo("/v1:stable"));
        Assert.That(route.SourceName, Is.EqualTo("settings.ini"));
        Assert.That(route.PrecedenceRank, Is.EqualTo(14));
        Assert.That(route.OriginalKey, Is.EqualTo("service.routes.api\\.v1"));
        Assert.That(route.LineNumber, Is.EqualTo(5));
    }

    [Test]
    public void Parse_ComplexInvalidFileReportsAllDiagnosticsAndContinues()
    {
        string text = "[service]\nhost=first\nhost=second\n[broken.section\n  : missing key\norphan without assignment\n[service.valid]\nport=5432\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigIniParser.Parse(text, "broken.ini", 4, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(map.GetPathDefault("service.valid.port").StringValue, Is.EqualTo("5432"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(4));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("service.host"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("duplicate INI key"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(3));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("malformed INI line"));
        Assert.That(diagnostics.Errors[1].LineNumber, Is.EqualTo(4));
        Assert.That(diagnostics.Errors[2].Message, Is.EqualTo("missing INI key"));
        Assert.That(diagnostics.Errors[2].LineNumber, Is.EqualTo(5));
        Assert.That(diagnostics.Errors[3].Message, Is.EqualTo("malformed INI line"));
        Assert.That(diagnostics.Errors[3].LineNumber, Is.EqualTo(6));
    }
}
