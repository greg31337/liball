namespace config.Tests;

public class ConfigXmlParserTests
{
    [Test]
    public void Parse_LoadsElementsAsNestedConfigValues()
    {
        string text = "<settings><database><host>localhost</host><port>5432</port></database><enabled>true</enabled></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 5);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("database.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.port").StringValue, Is.EqualTo("5432"));
        Assert.That(map.GetPathDefault("enabled").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Parse_LoadsAttributesAndDirectTextForComplexElements()
    {
        string text = "<settings><server port=\"8080\">localhost</server></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 5);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("server.@port").StringValue, Is.EqualTo("8080"));
        Assert.That(map.GetPathDefault("server.#text").StringValue, Is.EqualTo("localhost"));
    }

    [Test]
    public void Parse_RepeatedElementsBecomeArrays()
    {
        string text = "<settings><endpoint>one</endpoint><endpoint>two</endpoint></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 5);
        ConfigValue endpoints = root.ObjectValue!.GetPathDefault("endpoint");

        Assert.That(endpoints.Kind, Is.EqualTo(ConfigValueKind.Array));
        Assert.That(endpoints.ArrayValue!.Get(0).StringValue, Is.EqualTo("one"));
        Assert.That(endpoints.ArrayValue!.Get(1).StringValue, Is.EqualTo("two"));
    }

    [Test]
    public void Parse_StoresSourceMetadata()
    {
        string text = "<settings><database><host>localhost</host></database></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 11);
        ConfigValue host = root.ObjectValue!.GetPathDefault("database.host");

        Assert.That(host.SourceName, Is.EqualTo("settings.xml"));
        Assert.That(host.PrecedenceRank, Is.EqualTo(11));
        Assert.That(host.OriginalKey, Is.EqualTo("database.host"));
    }

    [Test]
    public void ParseOptionalFile_ReturnsEmptyMapForMissingFile()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-settings.xml");

        ConfigValue root = ConfigXmlParser.ParseOptionalFile(path, "settings.xml", 1);

        Assert.That(root.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(root.ObjectValue!.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_RootAttributesUseAtPrefixedKeys()
    {
        string text = "<settings environment=\"development\"><enabled>true</enabled></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 1);
        ConfigMap map = root.ObjectValue!;

        Assert.That(map.GetPathDefault("@environment").StringValue, Is.EqualTo("development"));
        Assert.That(map.GetPathDefault("enabled").StringValue, Is.EqualTo("true"));
    }

    [Test]
    public void Parse_RepeatedComplexElementsBecomeObjectArrays()
    {
        string text = "<settings><server name=\"one\"><host>a</host></server><server name=\"two\"><host>b</host></server></settings>";

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 1);
        ConfigValue servers = root.ObjectValue!.GetPathDefault("server");
        ConfigValue first = servers.ArrayValue!.Get(0);
        ConfigValue second = servers.ArrayValue!.Get(1);

        Assert.That(servers.Kind, Is.EqualTo(ConfigValueKind.Array));
        Assert.That(first.ObjectValue!.GetPathDefault("@name").StringValue, Is.EqualTo("one"));
        Assert.That(first.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("a"));
        Assert.That(second.ObjectValue!.GetPathDefault("@name").StringValue, Is.EqualTo("two"));
        Assert.That(second.ObjectValue!.GetPathDefault("host").StringValue, Is.EqualTo("b"));
    }

    [Test]
    public void Parse_NamespacedMixedRepeatedElementsUseLocalNames()
    {
        string text = """
            <cfg:settings xmlns:cfg="urn:cfg" xmlns:sec="urn:sec" sec:environment="prod">
              <cfg:endpoint sec:name="primary">https://primary.example</cfg:endpoint>
              <cfg:endpoint sec:name="backup">https://backup.example</cfg:endpoint>
              <cfg:database>
                <sec:credential sec:type="password">secret</sec:credential>
              </cfg:database>
            </cfg:settings>
            """;

        ConfigValue root = ConfigXmlParser.Parse(text, "settings.xml", 17);
        ConfigMap map = root.ObjectValue!;
        ConfigArray endpoints = map.GetPathDefault("endpoint").ArrayValue!;
        ConfigValue credential = map.GetPathDefault("database.credential.#text");

        Assert.That(map.GetPathDefault("@environment").StringValue, Is.EqualTo("prod"));
        Assert.That(endpoints.Count(), Is.EqualTo(2));
        Assert.That(endpoints.Get(0).ObjectValue!.GetPathDefault("@name").StringValue, Is.EqualTo("primary"));
        Assert.That(endpoints.Get(0).ObjectValue!.GetPathDefault("#text").StringValue, Is.EqualTo("https://primary.example"));
        Assert.That(endpoints.Get(1).ObjectValue!.GetPathDefault("@name").StringValue, Is.EqualTo("backup"));
        Assert.That(map.GetPathDefault("database.credential.@type").StringValue, Is.EqualTo("password"));
        Assert.That(credential.StringValue, Is.EqualTo("secret"));
        Assert.That(credential.SourceName, Is.EqualTo("settings.xml"));
        Assert.That(credential.PrecedenceRank, Is.EqualTo(17));
        Assert.That(credential.OriginalKey, Is.EqualTo("database.credential.#text"));
    }

    [Test]
    public void Parse_InvalidXmlThrowsSystemXmlException()
    {
        Exception exception = Assert.Catch<Exception>(() => ConfigXmlParser.Parse("<settings><unclosed></settings>", "broken.xml", 1));

        Assert.That(exception!.GetType().FullName, Does.StartWith("System.Xml."));
    }

    [Test]
    public void Parse_ComplexInvalidXmlThrowsSystemXmlException()
    {
        string undeclaredPrefix = "<settings><sec:credential>secret</sec:credential></settings>";
        string duplicateAttribute = "<settings><server name=\"first\" name=\"second\" /></settings>";

        Exception first = Assert.Catch<Exception>(() => ConfigXmlParser.Parse(undeclaredPrefix, "broken.xml", 1));
        Exception second = Assert.Catch<Exception>(() => ConfigXmlParser.Parse(duplicateAttribute, "broken.xml", 1));

        Assert.That(first!.GetType().FullName, Does.StartWith("System.Xml."));
        Assert.That(second!.GetType().FullName, Does.StartWith("System.Xml."));
    }
}
