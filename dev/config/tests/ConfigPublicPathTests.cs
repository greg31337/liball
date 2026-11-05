namespace config.Tests;

public class ConfigPublicPathTests
{
    [Test]
    public void Clean_ReturnsDefaultOrSelectedDelimiter()
    {
        IConfigPath paths = new ConfigPath();

        Assert.That(paths.Clean(""), Is.EqualTo("."));
        Assert.That(paths.Clean("__"), Is.EqualTo("__"));
        Assert.That(paths.Clean("/"), Is.EqualTo("/"));
    }

    [Test]
    public void EscapeAndParseSegments_RoundTripDelimitersAndBackslashes()
    {
        IConfigPath paths = new ConfigPath();
        string escaped = paths.Escape("region__primary\\host", "__");
        string overlapping = paths.Escape("___", "__");
        string path = "APP__" + escaped + "__port";

        string[] segments = paths.ParseSegments(path, "__");

        Assert.That(escaped, Is.EqualTo("region\\__primary\\\\host"));
        Assert.That(paths.ParseSegments(overlapping, "__"), Is.EqualTo(new string[] { "___" }));
        Assert.That(segments, Is.EqualTo(new string[] { "APP", "region__primary\\host", "port" }));
    }

    [Test]
    public void ParseSegments_OmitsEmptySegmentsAndPreservesTrailingBackslash()
    {
        IConfigPath paths = new ConfigPath();

        string[] segments = paths.ParseSegments("..database...path\\", ".");

        Assert.That(segments, Is.EqualTo(new string[] { "database", "path\\" }));
    }

    [Test]
    public void ParseSegments_ReturnsIndependentArrays()
    {
        IConfigPath paths = new ConfigPath();
        string[] first = paths.ParseSegments("service.host", ".");
        first[0] = "changed";

        string[] second = paths.ParseSegments("service.host", ".");

        Assert.That(second, Is.EqualTo(new string[] { "service", "host" }));
    }

    [Test]
    public void NormalizePath_CleansEmptySegmentsAndCanonicalizesEscaping()
    {
        IConfigPath paths = new ConfigPath();

        string normalized = paths.NormalizePath(".database\\.primary..path\\\\to.", ".");
        string defaultDelimiter = paths.NormalizePath(".service..host.", "");

        Assert.That(normalized, Is.EqualTo("database\\.primary.path\\\\to"));
        Assert.That(defaultDelimiter, Is.EqualTo("service.host"));
    }

    [Test]
    public void JoinPath_ConstructsPathsForKeysContainingDelimiter()
    {
        IConfigPath paths = new ConfigPath();
        string path = paths.JoinPath(new string[] { "database.primary", "", "host\\name" }, ".");
        ConfigMap map = new ConfigMap();
        map.SetPathDefault(path, ConfigValue.String("localhost"));

        Assert.That(path, Is.EqualTo("database\\.primary.host\\\\name"));
        Assert.That(map.Has("database.primary"), Is.True);
        Assert.That(map.GetPathDefault(path).StringValue, Is.EqualTo("localhost"));
        Assert.That(paths.ParseSegments(path, "."), Is.EqualTo(new string[] { "database.primary", "host\\name" }));
    }

    [Test]
    public void Compare_UsesParsedSegmentsCleanupAndCaseMode()
    {
        IConfigPath paths = new ConfigPath();

        Assert.That(paths.Compare(".service..host.", "service.host", ".", ConfigCaseMode.Sensitive), Is.EqualTo(0));
        Assert.That(paths.Compare("Service.Host", "service.host", ".", ConfigCaseMode.Insensitive), Is.EqualTo(0));
        Assert.That(paths.Compare("Service.Host", "service.host", ".", ConfigCaseMode.Sensitive), Is.LessThan(0));
        Assert.That(paths.Compare("service", "service.host", ".", ConfigCaseMode.Sensitive), Is.LessThan(0));
        Assert.That(paths.Compare("service.port", "service.host", ".", ConfigCaseMode.Sensitive), Is.GreaterThan(0));
    }

    [Test]
    public void Compare_HandlesEscapedCustomDelimiterSegments()
    {
        IConfigPath paths = new ConfigPath();

        int comparison = paths.Compare("APP__region\\__primary__HOST", "APP__region\\__primary__host", "__", ConfigCaseMode.Insensitive);

        Assert.That(comparison, Is.EqualTo(0));
    }
}
