namespace config.Tests;

public class ConfigMapTraversalTests
{
    [Test]
    public void SetPathAndGetPath_CreateAndReadNestedObjects()
    {
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("database.primary.host", ConfigValue.String("localhost"));
        map.SetPathDefault("database.primary.port", ConfigValue.Number(5432m));

        ConfigValue database = map.Get("database");
        ConfigValue host = map.GetPathDefault("database.primary.host");
        ConfigValue port = map.GetPathDefault("database.primary.port");

        Assert.That(database.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(host.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(port.NumberValue, Is.EqualTo(5432m));
        Assert.That(map.HasPathDefault("database.primary.host"), Is.True);
        Assert.That(map.HasPathDefault("database.primary.missing"), Is.False);
    }

    [Test]
    public void PathTraversal_UsesEscapedSegments()
    {
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("database\\.primary.host", ConfigValue.String("db.internal"));

        Assert.That(map.Has("database.primary"), Is.True);
        Assert.That(map.GetPathDefault("database\\.primary.host").StringValue, Is.EqualTo("db.internal"));
        Assert.That(map.ChildKeysDefault("database\\.primary"), Is.EqualTo(new string[] { "host" }));
    }

    [Test]
    public void ChildKeys_ReturnSortedKeysForRootAndNestedObjects()
    {
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("server.port", ConfigValue.Number(8080m));
        map.SetPathDefault("server.host", ConfigValue.String("127.0.0.1"));
        map.SetPathDefault("features.cache", ConfigValue.Boolean(true));

        Assert.That(map.ChildKeysDefault(""), Is.EqualTo(new string[] { "features", "server" }));
        Assert.That(map.ChildKeysDefault("server"), Is.EqualTo(new string[] { "host", "port" }));
        Assert.That(map.ChildKeysDefault("missing"), Is.EqualTo(new string[0]));
    }

    [Test]
    public void SetPath_ReplacesScalarIntermediateValuesWithObjects()
    {
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("server", ConfigValue.String("old"));

        Assert.That(map.GetPathDefault("server.port").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(map.ChildKeysDefault("server"), Is.EqualTo(new string[0]));

        map.SetPathDefault("server.port", ConfigValue.Number(8080m));

        Assert.That(map.GetPathDefault("server").Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(map.GetPathDefault("server.port").NumberValue, Is.EqualTo(8080m));
    }

    [Test]
    public void PathTraversal_UsesCustomDelimiters()
    {
        ConfigMap map = new ConfigMap();
        map.SetPath("APP__database__host", ConfigValue.String("localhost"), "__");

        Assert.That(map.GetPath("APP__database__host", "__").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.ChildKeys("APP__database", "__"), Is.EqualTo(new string[] { "host" }));
    }
}
