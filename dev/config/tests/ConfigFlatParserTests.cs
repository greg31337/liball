using System.Text;

namespace config.Tests;

public class ConfigFlatParserTests
{
    [Test]
    public void Parse_ReadsKeyValueLinesCommentsQuotesAndNestedPaths()
    {
        string text = "# comment\napp.name=config\ndatabase.host = localhost\nmessage = \"hello # not comment\"\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigFlatParser.Parse(text, "flat.txt", 2, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(map.GetPathDefault("app.name").StringValue, Is.EqualTo("config"));
        Assert.That(map.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("message").StringValue, Is.EqualTo("hello # not comment"));
    }

    [Test]
    public void ParseStream_ReadsUtf8StreamAndLeavesStreamOpen()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("name=config\n");
        MemoryStream stream = new MemoryStream(bytes);
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigFlatParser.ParseStream(stream, "stream", 3, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("name").StringValue, Is.EqualTo("config"));
        Assert.That(stream.CanRead, Is.True);
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_StoresSourceMetadata()
    {
        string text = "  database.host = localhost\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigFlatParser.Parse(text, "flat.txt", 7, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigValue host = root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(host.SourceName, Is.EqualTo("flat.txt"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(7));
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

        ConfigValue errorRoot = ConfigFlatParser.Parse(text, "flat.txt", 1, ConfigDuplicateKeyMode.Error, errorDiagnostics);
        ConfigValue lastWinsRoot = ConfigFlatParser.Parse(text, "flat.txt", 1, ConfigDuplicateKeyMode.LastWins, lastWinsDiagnostics);

        Assert.That(errorRoot.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("first"));
        Assert.That(errorDiagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(errorDiagnostics.Errors[0].Message, Is.EqualTo("duplicate flat key"));
        Assert.That(lastWinsRoot.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("second"));
        Assert.That(lastWinsDiagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_MalformedLinesReportDiagnosticsAndContinue()
    {
        string text = "missing assignment\n=missing key\nvalid=value\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigFlatParser.Parse(text, "flat.txt", 1, ConfigDuplicateKeyMode.Error, diagnostics);

        Assert.That(root.ObjectValue!.GetPathDefault("valid").StringValue, Is.EqualTo("value"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(2));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("malformed flat key-value line"));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("missing flat key"));
    }

    [Test]
    public void Parse_ComplexInvalidFileReportsAllDiagnosticsAndContinues()
    {
        string text = "service.host=first\nservice.host=second\n\"quoted = but no assignment\"\n=missing key\nservice.port=5432\n";
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();

        ConfigValue root = ConfigFlatParser.Parse(text, "broken.flat", 6, ConfigDuplicateKeyMode.Error, diagnostics);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("service.host").StringValue, Is.EqualTo("first"));
        Assert.That(map.GetPathDefault("service.port").StringValue, Is.EqualTo("5432"));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(3));
        Assert.That(diagnostics.Errors[0].Path, Is.EqualTo("service.host"));
        Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("duplicate flat key"));
        Assert.That(diagnostics.Errors[0].LineNumber, Is.EqualTo(2));
        Assert.That(diagnostics.Errors[1].Message, Is.EqualTo("malformed flat key-value line"));
        Assert.That(diagnostics.Errors[1].LineNumber, Is.EqualTo(3));
        Assert.That(diagnostics.Errors[2].Message, Is.EqualTo("missing flat key"));
        Assert.That(diagnostics.Errors[2].LineNumber, Is.EqualTo(4));
    }
}
