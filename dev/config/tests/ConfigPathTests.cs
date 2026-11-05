namespace config.Tests;

public class ConfigPathTests
{
    [Test]
    public void ParseDefault_SplitsDotDelimitedPaths()
    {
        ConfigPath path = ConfigPath.ParseDefault("database.primary.host");

        Assert.That(path.Delimiter, Is.EqualTo("."));
        Assert.That(path.Count(), Is.EqualTo(3));
        Assert.That(path.Segments, Is.EqualTo(new string[] { "database", "primary", "host" }));
        Assert.That(path.Segment(0), Is.EqualTo("database"));
        Assert.That(path.Segment(3), Is.EqualTo(""));
        Assert.That(path.IsEmpty(), Is.False);
    }

    [Test]
    public void Parse_HandlesEscapedDelimitersAndBackslashes()
    {
        ConfigPath path = ConfigPath.ParseDefault("database\\.host.path\\\\to");

        Assert.That(path.Segments, Is.EqualTo(new string[] { "database.host", "path\\to" }));
        Assert.That(path.Join(), Is.EqualTo("database\\.host.path\\\\to"));
    }

    [Test]
    public void Parse_UsesCustomDelimiter()
    {
        ConfigPath path = ConfigPath.Parse("APP__region\\__primary__host", "__");

        Assert.That(path.Delimiter, Is.EqualTo("__"));
        Assert.That(path.Segments, Is.EqualTo(new string[] { "APP", "region__primary", "host" }));
        Assert.That(path.Join(), Is.EqualTo("APP__region\\__primary__host"));
    }

    [Test]
    public void Normalize_DropsEmptySegmentsAndEscapesLiteralDelimiterCharacters()
    {
        string normalized = ConfigPath.Normalize(".database\\.host..path\\\\to.", ".");

        Assert.That(normalized, Is.EqualTo("database\\.host.path\\\\to"));
    }

    [Test]
    public void JoinSegments_EscapesSegmentsForRoundTripParsing()
    {
        string joined = ConfigPath.JoinSegments(new string[] { "database.host", "path\\to", "port" }, ".");
        ConfigPath parsed = ConfigPath.ParseDefault(joined);

        Assert.That(joined, Is.EqualTo("database\\.host.path\\\\to.port"));
        Assert.That(parsed.Segments, Is.EqualTo(new string[] { "database.host", "path\\to", "port" }));
    }

    [Test]
    public void IsDelimiterAt_ReturnsFalseForIndexesOutsideText()
    {
        Assert.That(ConfigPath.IsDelimiterAt("path", ".", -1), Is.False);
        Assert.That(ConfigPath.IsDelimiterAt("path", ".", 5), Is.False);
    }
}
