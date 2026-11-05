namespace config.Tests;

public class ConfigMapCaseModeTests
{
    [Test]
    public void DefaultMap_UsesCaseSensitiveKeys()
    {
        ConfigMap map = new ConfigMap();
        map.Set("Host", ConfigValue.String("localhost"));

        Assert.That(map.CaseMode, Is.EqualTo(ConfigCaseMode.Sensitive));
        Assert.That(map.Has("Host"), Is.True);
        Assert.That(map.Has("host"), Is.False);
        Assert.That(map.Get("HOST").Kind, Is.EqualTo(ConfigValueKind.Missing));
    }

    [Test]
    public void InsensitiveMap_MatchesKeysOrdinalIgnoreCase()
    {
        ConfigMap map = ConfigMap.Create(ConfigCaseMode.Insensitive);
        map.Set("Host", ConfigValue.String("first"));
        map.Set("host", ConfigValue.String("second"));

        Assert.That(map.CaseMode, Is.EqualTo(ConfigCaseMode.Insensitive));
        Assert.That(map.Count(), Is.EqualTo(1));
        Assert.That(map.Has("HOST"), Is.True);
        Assert.That(map.Get("hOsT").StringValue, Is.EqualTo("second"));
        Assert.That(map.Keys(), Is.EqualTo(new string[] { "Host" }));
    }

    [Test]
    public void InsensitiveMap_AppliesCaseModeToPathTraversal()
    {
        ConfigMap map = ConfigMap.Create(ConfigCaseMode.Insensitive);
        map.SetPathDefault("Database.Primary.Host", ConfigValue.String("localhost"));

        Assert.That(map.HasPathDefault("database.primary.host"), Is.True);
        Assert.That(map.GetPathDefault("DATABASE.PRIMARY.HOST").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.ChildKeysDefault("database.primary"), Is.EqualTo(new string[] { "Host" }));
    }

    [Test]
    public void Copy_PreservesCaseMode()
    {
        ConfigMap map = ConfigMap.Create(ConfigCaseMode.Insensitive);
        map.SetPathDefault("Server.Port", ConfigValue.Number(8080m));

        ConfigMap copy = map.Copy();

        Assert.That(copy, Is.Not.SameAs(map));
        Assert.That(copy.CaseMode, Is.EqualTo(ConfigCaseMode.Insensitive));
        Assert.That(copy.HasPathDefault("server.port"), Is.True);
        Assert.That(copy.GetPathDefault("SERVER.PORT").NumberValue, Is.EqualTo(8080m));
    }
}
