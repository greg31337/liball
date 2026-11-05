namespace config.Tests;

public class ConfigDotEnvParserTests
{
    [Test]
    public void Parse_ReadsCommentsExportsQuotedStringsAndEmptyValues()
    {
        string text = "# comment\nexport HOST=localhost\nEMPTY=\nMESSAGE=\"hello # not comment\"\nSINGLE='it\\'s ok'\nPLAIN=value # inline comment\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 4, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("HOST").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("EMPTY").StringValue, Is.EqualTo(""));
        Assert.That(map.GetPathDefault("MESSAGE").StringValue, Is.EqualTo("hello # not comment"));
        Assert.That(map.GetPathDefault("SINGLE").StringValue, Is.EqualTo("it's ok"));
        Assert.That(map.GetPathDefault("PLAIN").StringValue, Is.EqualTo("value"));
    }

    [Test]
    public void Parse_UnescapesDoubleQuotedValuesAndStoresMetadata()
    {
        string text = "MULTILINE=\"first\\nsecond\"\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 8, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigValue value = root.ObjectValue!.GetPathDefault("MULTILINE");

        Assert.That(value.StringValue, Is.EqualTo("first\nsecond"));
        Assert.That(value.SourceName, Is.EqualTo(".env"));
        Assert.That(value.PrecedenceRank, Is.EqualTo(8));
        Assert.That(value.OriginalKey, Is.EqualTo("MULTILINE"));
        Assert.That(value.LineNumber, Is.EqualTo(1));
        Assert.That(value.ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void Parse_UsesDuplicateKeyPolicy()
    {
        string text = "HOST=first\nHOST=second\n";
        ConfigDiagnostics errorDiagnostics = new ConfigDiagnostics();
        ConfigDiagnostics lastWinsDiagnostics = new ConfigDiagnostics();

        ConfigValue errorRoot = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, errorDiagnostics);
        ConfigValue lastWinsRoot = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.LastWins, lastWinsDiagnostics);

        Assert.That(errorRoot.ObjectValue!.GetPathDefault("HOST").StringValue, Is.EqualTo("first"));
        Assert.That(errorDiagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(errorDiagnostics.Errors[0].Path, Is.EqualTo("HOST"));
        Assert.That(errorDiagnostics.Errors[0].Message, Is.EqualTo("duplicate dotenv key"));
        Assert.That(lastWinsRoot.ObjectValue!.GetPathDefault("HOST").StringValue, Is.EqualTo("second"));
        Assert.That(lastWinsDiagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_ReportsMalformedLinesAndContinues()
    {
        string text = "missing assignment\n=missing key\nVALID=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("VALID").StringValue, Is.EqualTo("value"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("malformed dotenv line"));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("missing dotenv key"));
    }

    [Test]
    public void ParseOptionalFile_ReturnsEmptyMapForMissingFile()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-dotenv-file.env");
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseOptionalFile(path, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(root.ObjectValue!.Count(), Is.EqualTo(0));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_HandlesTabbedExportAndQuotedEmptyValues()
    {
        string text = "export\tTABBED=value\nEMPTY_QUOTED=\"\"\nSINGLE_HASH='value # not comment'\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("TABBED").StringValue, Is.EqualTo("value"));
        Assert.That(map.GetPathDefault("EMPTY_QUOTED").StringValue, Is.EqualTo(""));
        Assert.That(map.GetPathDefault("SINGLE_HASH").StringValue, Is.EqualTo("value # not comment"));
    }

    [Test]
    public void Parse_DottedKeysCreateNestedPaths()
    {
        string text = "database.host=localhost\ndatabase.port=5432\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(root.ObjectValue!.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(root.ObjectValue!.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
    }

    [Test]
    public void Parse_DuplicateMergeObjectsModeReportsScalarDuplicate()
    {
        string text = "HOST=first\nHOST=second\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.MergeObjects, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("HOST").StringValue, Is.EqualTo("first"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("duplicate dotenv key"));
    }

    [Test]
    public void Parse_MalformedExportLinesReportDiagnostics()
    {
        string text = "export\nexport =missing\nVALID=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("VALID").StringValue, Is.EqualTo("value"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("malformed dotenv line"));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("missing dotenv key"));
    }

    [Test]
    public void Parse_DoesNotExpandVariablesUnlessRequested()
    {
        string text = "HOST=localhost\nURL=http://${HOST}:8080\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://${HOST}:8080"));
        Assert.That(diagnostics.HasWarnings(), Is.False);
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void ParseAndExpand_ExpandsBraceAndBareVariableReferences()
    {
        string text = "HOST=localhost\nPORT=8080\nBASE=http://${HOST}:$PORT\nAPI=$BASE/api\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseAndExpand(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("BASE").StringValue, Is.EqualTo("http://localhost:8080"));
        Assert.That(root.ObjectValue!.GetPathDefault("API").StringValue, Is.EqualTo("http://localhost:8080/api"));
        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(diagnostics.HasWarnings(), Is.False);
    }

    [Test]
    public void ParseAndExpand_ExpandsNestedPathReferences()
    {
        string text = "database.host=localhost\nDATABASE_URL=postgres://${database.host}:5432/app\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseAndExpand(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("DATABASE_URL").StringValue, Is.EqualTo("postgres://localhost:5432/app"));
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void ParseAndExpand_MissingVariablesBecomeEmptyAndWarn()
    {
        string text = "URL=http://${MISSING}:8080\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseAndExpand(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("URL").StringValue, Is.EqualTo("http://:8080"));
        Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(diagnostics.Warnings[0].Path, Is.EqualTo("MISSING"));
        Assert.That(diagnostics.Warnings[0].Message, Is.EqualTo("missing dotenv variable expansion"));
    }

    [Test]
    public void ParseAndExpand_CyclesReportErrorsWithoutLooping()
    {
        string text = "A=${B}\nB=${A}\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseAndExpand(text, ".env", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("A").StringValue, Is.Not.Null);
        Assert.That(root.ObjectValue!.GetPathDefault("B").StringValue, Is.Not.Null);
        Assert.That(diagnostics.ErrorCount(), Is.GreaterThanOrEqualTo(1));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("cycle in dotenv variable expansion"));
    }

    [Test]
    public void ParseAndExpand_ComplexValidFileExpandsChainedNestedReferences()
    {
        string text = "service.host=api.internal\nservice.port=8443\nBASE_URL=https://${service.host}:${service.port}\nHEALTH_URL=$BASE_URL/health\nMESSAGE=\"ready # not comment\"\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.ParseAndExpand(text, ".env", 15, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;
        ConfigValue healthUrl = map.GetPathDefault("HEALTH_URL");

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(diagnostics.HasWarnings(), Is.False);
        Assert.That(map.GetPathDefault("BASE_URL").StringValue, Is.EqualTo("https://api.internal:8443"));
        Assert.That(healthUrl.StringValue, Is.EqualTo("https://api.internal:8443/health"));
        Assert.That(map.GetPathDefault("MESSAGE").StringValue, Is.EqualTo("ready # not comment"));
        Assert.That(healthUrl.SourceName, Is.EqualTo(".env"));
        Assert.That(healthUrl.PrecedenceRank, Is.EqualTo(15));
        Assert.That(healthUrl.OriginalKey, Is.EqualTo("HEALTH_URL"));
    }

    [Test]
    public void Parse_ComplexInvalidFileReportsAllDiagnosticsAndContinues()
    {
        string text = "export\nexport =missing\nHOST=first\nHOST=second\nQUERY=value=with=equals\nBROKEN LINE\nVALID=ok\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigDotEnvParser.Parse(text, ".env", 5, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("HOST").StringValue, Is.EqualTo("first"));
        Assert.That(map.GetPathDefault("QUERY").StringValue, Is.EqualTo("value=with=equals"));
        Assert.That(map.GetPathDefault("VALID").StringValue, Is.EqualTo("ok"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(4));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("malformed dotenv line"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(1));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("missing dotenv key"));
        Assert.That(diagnostics.Errors[1].LineNumber, Is.EqualTo(2));
        Assert.That(diagnostics.Errors[2].Path, Is.EqualTo("HOST"));
        Assert.That(diagnostics.Errors[2].Message, Is.EqualTo("duplicate dotenv key"));
        Assert.That(diagnostics.Errors[2].LineNumber, Is.EqualTo(4));
        Assert.That(diagnostics.Errors[3].Message, Is.EqualTo("malformed dotenv line"));
        Assert.That(diagnostics.Errors[3].LineNumber, Is.EqualTo(6));
    }
}
