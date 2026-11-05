namespace config.Tests;

public class ConfigPropertiesParserTests
{
    [Test]
    public void Parse_ReadsCommentsSeparatorsAndNestedPaths()
    {
        string text = "# comment\n! another comment\napp.name=config\ndatabase.host localhost\ndatabase.port: 5432\nfeatures.cache.enabled = true\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 3, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("app.name").StringValue, Is.EqualTo("config"));
        Assert.That(map.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(map.GetPathDefault("features.cache.enabled").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Parse_UnescapesKeysValuesAndUnicodeSequences()
    {
        string text = "database\\.host=localhost\npath=c\\:\\\\temp\\\\files\nletter=\\u0041\nspace\\ key = spaced\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.Has("database.host"), Is.True);
        Assert.That(map.GetPathDefault("database\\.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("path").StringValue, Is.EqualTo("c:\\temp\\files"));
        Assert.That(map.GetPathDefault("letter").StringValue, Is.EqualTo("A"));
        Assert.That(map.GetPathDefault("space\\ key").StringValue, Is.EqualTo("spaced"));
    }

    [Test]
    public void UnicodeLookahead_ReturnsFalseForIndexesOutsideText()
    {
        Assert.That(ConfigPropertiesParser.CanReadUnicode("0041", -1), Is.False);
        Assert.That(ConfigPropertiesParser.CanReadUnicode("0041", 5), Is.False);
    }

    [Test]
    public void Parse_JoinsLineContinuations()
    {
        string text = "message = first \\\n    second \\\n    third\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(root.ObjectValue!.GetPathDefault("message").StringValue, Is.EqualTo("first second third"));
    }

    [Test]
    public void Parse_StoresSourceMetadata()
    {
        string text = "  database.host = localhost\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 9, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigValue host = root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(host.SourceName, Is.EqualTo("app.properties"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(9));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
        Assert.That(host.LineNumber, Is.EqualTo(1));
        Assert.That(host.ColumnNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_DuplicatePoliciesHandleRepeatedKeys()
    {
        string text = "host=first\nhost=second\n";
        ConfigDiagnostics errorDiagnostics = new ConfigDiagnostics();
        ConfigDiagnostics lastWinsDiagnostics = new ConfigDiagnostics();

        ConfigValue errorRoot = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, errorDiagnostics);
        ConfigValue lastWinsRoot = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.LastWins, lastWinsDiagnostics);

        Assert.That(errorRoot.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("first"));
        Assert.That(errorDiagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(errorDiagnostics.Errors[0].Path, Is.EqualTo("host"));
        Assert.That(errorDiagnostics.Errors[0].Message, Is.EqualTo("duplicate properties key"));
        Assert.That(lastWinsRoot.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("second"));
        Assert.That(lastWinsDiagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_EscapedCommentAndSeparatorCharactersCanAppearInKeys()
    {
        string text = "\\#literal\\.key\\:name=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.Has("#literal.key:name"), Is.True);
        Assert.That(map.GetPathDefault("\\#literal\\.key:name").StringValue, Is.EqualTo("value"));
    }

    [Test]
    public void Parse_EvenTrailingBackslashesDoNotContinueLine()
    {
        string text = "path = c:\\\\\nnext=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("path").StringValue, Is.EqualTo("c:\\"));
        Assert.That(map.GetPathDefault("next").StringValue, Is.EqualTo("value"));
    }

    [Test]
    public void Parse_DuplicateMergeObjectsModeReportsScalarDuplicate()
    {
        string text = "server.host=first\nserver.host=second\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.MergeObjects, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("server.host").StringValue, Is.EqualTo("first"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("server.host"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("duplicate properties key"));
    }

    [Test]
    public void Parse_MalformedEmptyKeysReportDiagnosticsAndContinue()
    {
        string text = "=missing\n  : also missing\nvalid=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("valid").StringValue, Is.EqualTo("value"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("missing properties key"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].ColumnNumber, Is.EqualTo(1));
        Assert.That(diagnostics.Errors[1].LineNumber, Is.EqualTo(2));
        Assert.That(diagnostics.Errors[1].ColumnNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_ComplexValidFileCombinesContinuationsEscapesAndNestedPaths()
    {
        string text = "service.url = https\\://example.test/api\\\n  ?retry\\=true\nservice.headers.X\\ Trace = enabled\nservice.paths.api\\.v1 : /v1\\:stable\nservice.message = line1\\nline2\\tready\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "app.properties", 16, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;
        ConfigValue url = map.GetPathDefault("service.url");

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(url.StringValue, Is.EqualTo("https://example.test/api?retry=true"));
        Assert.That(map.GetPathDefault("service.headers.X Trace").StringValue, Is.EqualTo("enabled"));
        Assert.That(map.GetPathDefault("service.paths.api\\.v1").StringValue, Is.EqualTo("/v1:stable"));
        Assert.That(map.GetPathDefault("service.message").StringValue, Is.EqualTo("line1\nline2\tready"));
        Assert.That(url.SourceName, Is.EqualTo("app.properties"));
        Assert.That(url.PrecedenceRank, Is.EqualTo(16));
        Assert.That(url.OriginalKey, Is.EqualTo("service.url"));
        Assert.That(url.LineNumber, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ComplexInvalidFileReportsAllDiagnosticsAndContinues()
    {
        string text = "=missing\nservice.host=first\nservice.host: second\n  : also missing\nservice.port 5432\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigPropertiesParser.Parse(text, "broken.properties", 7, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(map.GetPathDefault("service.port").StringValue, Is.EqualTo("5432"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(3));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("missing properties key"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(1));
        Assert.That(diagnostics.Errors[1].Path, Is.EqualTo("service.host"));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("duplicate properties key"));
        Assert.That(diagnostics.Errors[1].LineNumber, Is.EqualTo(3));
        Assert.That(diagnostics.Errors[2].Message, Is.EqualTo("missing properties key"));
        Assert.That(diagnostics.Errors[2].LineNumber, Is.EqualTo(4));
    }
}
