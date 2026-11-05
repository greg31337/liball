namespace config.Tests;

public class ConfigAliasTableTests
{
    [Test]
    public void AddAndResolveDefault_NormalizesRegisteredPaths()
    {
        ConfigAliasTable aliases = new ConfigAliasTable(ConfigCaseMode.Sensitive);
        aliases.AddDefault(".old\\.database..host", "database.primary.host");

        Assert.That(aliases.Count(), Is.EqualTo(1));
        Assert.That(aliases.HasDefault("old\\.database.host"), Is.True);
        Assert.That(aliases.ResolveDefault("old\\.database.host"), Is.EqualTo("database.primary.host"));
        Assert.That(aliases.ResolveDefault("database.primary.port"), Is.EqualTo("database.primary.port"));
    }

    [Test]
    public void Resolve_FollowsAliasChainsAndStopsAtCycles()
    {
        ConfigAliasTable aliases = new ConfigAliasTable(ConfigCaseMode.Sensitive);
        aliases.AddDefault("old.host", "new.host");
        aliases.AddDefault("new.host", "current.host");
        aliases.AddDefault("cycle.a", "cycle.b");
        aliases.AddDefault("cycle.b", "cycle.a");

        Assert.That(aliases.ResolveDefault("old.host"), Is.EqualTo("current.host"));
        Assert.That(aliases.ResolveDefault("cycle.a"), Is.EqualTo("cycle.a"));
    }

    [Test]
    public void AliasTable_CanUseCaseInsensitiveLookup()
    {
        ConfigAliasTable aliases = new ConfigAliasTable(ConfigCaseMode.Insensitive);
        aliases.AddDefault("Old.Path", "New.Path");

        Assert.That(aliases.HasDefault("old.path"), Is.True);
        Assert.That(aliases.ResolveDefault("OLD.PATH"), Is.EqualTo("New.Path"));
    }

    [Test]
    public void ConfigMap_ReadsAndWritesThroughAliases()
    {
        ConfigAliasTable aliases = new ConfigAliasTable(ConfigCaseMode.Sensitive);
        aliases.AddDefault("database.host", "database.primary.host");

        ConfigMap map = new ConfigMap();
        map.SetPathDefaultWithAliases("database.host", ConfigValue.String("localhost"), aliases);

        Assert.That(map.HasPathDefaultWithAliases("database.host", aliases), Is.True);
        Assert.That(map.GetPathDefaultWithAliases("database.host", aliases).StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.primary.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPathDefault("database.host").Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(map.ChildKeysDefaultWithAliases("database", aliases), Is.EqualTo(new string[] { "primary" }));
    }

    [Test]
    public void ConfigMap_AppliesAliasesWithCustomDelimiters()
    {
        ConfigAliasTable aliases = new ConfigAliasTable(ConfigCaseMode.Sensitive);
        aliases.Add("APP__HOST", "APP__DATABASE__HOST", "__");

        ConfigMap map = new ConfigMap();
        map.SetPathWithAliases("APP__HOST", ConfigValue.String("localhost"), "__", aliases);

        Assert.That(map.GetPathWithAliases("APP__HOST", "__", aliases).StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPath("APP__DATABASE__HOST", "__").StringValue, Is.EqualTo("localhost"));
    }
}
