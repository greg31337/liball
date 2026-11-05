namespace config.Tests;

public class ConfigJsonParserTests
{
    [Test]
    public void Parse_ConvertsObjectTreesToConfigValues()
    {
        ConfigValue root = ConfigJsonParser.Parse("{\"database\":{\"host\":\"localhost\",\"port\":5432,\"enabled\":true,\"tags\":[\"primary\",2],\"password\":null}}", "appsettings.json", 10);
        ConfigMap map = root.ObjectValue!;

        Assert.That(root.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(root.SourceName, Is.EqualTo("appsettings.json"));
        Assert.That(root.PrecedenceRank, Is.EqualTo(10));
        Assert.That(map.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.port").NumberValue, Is.EqualTo(5432m));
        Assert.That(map.GetPathDefault("database.enabled").BooleanValue, Is.True);
        Assert.That(map.GetPathDefault("database.password").Kind, Is.EqualTo(ConfigValueKind.Null));
        Assert.That(map.GetPathDefault("database.tags").Kind, Is.EqualTo(ConfigValueKind.Array));
        Assert.That(map.GetPathDefault("database.tags").ArrayValue!.Get(0).StringValue, Is.EqualTo("primary"));
        Assert.That(map.GetPathDefault("database.tags").ArrayValue!.Get(1).NumberValue, Is.EqualTo(2m));
    }

    [Test]
    public void Parse_PreservesSourceMetadataOnNestedValues()
    {
        ConfigValue root = ConfigJsonParser.Parse("{\"database.host\":{\"port\":5432}}", "settings.json", 30);
        ConfigMap map = root.ObjectValue!;
        ConfigValue port = map.GetPathDefault("database\\.host.port");

        Assert.That(port.SourceName, Is.EqualTo("settings.json"));
        Assert.That(port.PrecedenceRank, Is.EqualTo(30));
        Assert.That(port.OriginalKey, Is.EqualTo("database\\.host.port"));
        Assert.That(port.HasLocation(), Is.False);
    }

    [Test]
    public void Parse_AcceptsArrayAndScalarRoots()
    {
        ConfigValue arrayRoot = ConfigJsonParser.Parse("[true,false,null,\"text\",12.5]", "array.json", 1);
        ConfigValue stringRoot = ConfigJsonParser.Parse("\"hello\"", "value.json", 2);
        ConfigValue numberRoot = ConfigJsonParser.Parse("42", "value.json", 2);
        ConfigValue boolRoot = ConfigJsonParser.Parse("true", "value.json", 2);
        ConfigValue nullRoot = ConfigJsonParser.Parse("null", "value.json", 2);

        Assert.That(arrayRoot.Kind, Is.EqualTo(ConfigValueKind.Array));
        Assert.That(arrayRoot.ArrayValue!.Get(0).BooleanValue, Is.True);
        Assert.That(arrayRoot.ArrayValue!.Get(1).BooleanValue, Is.False);
        Assert.That(arrayRoot.ArrayValue!.Get(2).Kind, Is.EqualTo(ConfigValueKind.Null));
        Assert.That(arrayRoot.ArrayValue!.Get(3).StringValue, Is.EqualTo("text"));
        Assert.That(arrayRoot.ArrayValue!.Get(4).NumberValue, Is.EqualTo(12.5m));
        Assert.That(stringRoot.StringValue, Is.EqualTo("hello"));
        Assert.That(numberRoot.NumberValue, Is.EqualTo(42m));
        Assert.That(boolRoot.Kind, Is.EqualTo(ConfigValueKind.Boolean));
        Assert.That(boolRoot.BooleanValue, Is.True);
        Assert.That(nullRoot.Kind, Is.EqualTo(ConfigValueKind.Null));
    }

    [Test]
    public void ParseWithCaseMode_CreatesCaseInsensitiveObjectMaps()
    {
        ConfigValue root = ConfigJsonParser.ParseWithCaseMode("{\"Database\":{\"Host\":\"localhost\"}}", "settings.json", 1, ConfigCaseMode.Insensitive);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.CaseMode, Is.EqualTo(ConfigCaseMode.Insensitive));
        Assert.That(map.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Parse_ComplexArraysPreserveEscapedMetadataPaths()
    {
        string text = """
            {
              "service.api": {
                "endpoints": [
                  { "name": "primary", "timeouts.ms": [100, 250] },
                  { "name": "backup", "timeouts.ms": [500] }
                ],
                "headers": { "x.trace": "enabled" }
              }
            }
            """;

        ConfigValue root = ConfigJsonParser.Parse(text, "complex.json", 13);
        ConfigMap service = root.ObjectValue!.GetPathDefault("service\\.api").ObjectValue!;
        ConfigArray endpoints = service.GetPathDefault("endpoints").ArrayValue!;
        ConfigValue firstTimeout = endpoints.Get(0).ObjectValue!.GetPathDefault("timeouts\\.ms").ArrayValue!.Get(0);

        Assert.That(endpoints.Count(), Is.EqualTo(2));
        Assert.That(endpoints.Get(0).ObjectValue!.GetPathDefault("name").StringValue, Is.EqualTo("primary"));
        Assert.That(endpoints.Get(1).ObjectValue!.GetPathDefault("name").StringValue, Is.EqualTo("backup"));
        Assert.That(firstTimeout.NumberValue, Is.EqualTo(100m));
        Assert.That(firstTimeout.SourceName, Is.EqualTo("complex.json"));
        Assert.That(firstTimeout.PrecedenceRank, Is.EqualTo(13));
        Assert.That(firstTimeout.OriginalKey, Is.EqualTo("service\\.api.endpoints.0.timeouts\\.ms.0"));
        Assert.That(service.GetPathDefault("headers.x\\.trace").StringValue, Is.EqualTo("enabled"));
    }

    [Test]
    public void Parse_InvalidJsonThrowsJsonException()
    {
        Exception first = Assert.Catch<Exception>(() => ConfigJsonParser.Parse("{\"database\":", "broken.json", 1));
        Exception second = Assert.Catch<Exception>(() => ConfigJsonParser.Parse("[1, 2,, 3]", "broken.json", 1));

        Assert.That(first!.GetType().FullName, Does.StartWith("System.Text.Json."));
        Assert.That(second!.GetType().FullName, Does.StartWith("System.Text.Json."));
    }

    [Test]
    public void Parse_ComplexInvalidJsonThrowsJsonException()
    {
        string trailingCommaInNestedArray = "{\"services\":[{\"name\":\"api\",\"ports\":[80,]}]}";
        string invalidEscapeInNestedString = "{\"service\":{\"path\":\"c:\\xinvalid\"}}";

        Exception first = Assert.Catch<Exception>(() => ConfigJsonParser.Parse(trailingCommaInNestedArray, "broken.json", 1));
        Exception second = Assert.Catch<Exception>(() => ConfigJsonParser.Parse(invalidEscapeInNestedString, "broken.json", 1));

        Assert.That(first!.GetType().FullName, Does.StartWith("System.Text.Json."));
        Assert.That(second!.GetType().FullName, Does.StartWith("System.Text.Json."));
    }
}
