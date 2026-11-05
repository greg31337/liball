using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlPathTests
{
    [Test]
    public void Parse_EmptyAndDollarPaths_ReturnRootPath()
    {
        global::yaml.YamlPath empty = global::yaml.YamlPathParser.Parse("");
        global::yaml.YamlPath dollar = global::yaml.YamlPathParser.Parse("$");

        Assert.That(empty.Valid, Is.True);
        Assert.That(empty.IsRoot(), Is.True);
        Assert.That(empty.ToText(), Is.EqualTo("$"));
        Assert.That(dollar.Valid, Is.True);
        Assert.That(dollar.IsRoot(), Is.True);
    }

    [Test]
    public void Parse_MappingKeysAndSequenceIndexes_CreatesOrderedSegments()
    {
        global::yaml.YamlPath path = global::yaml.YamlPathParser.Parse("$.users[12].name");

        Assert.That(path.Valid, Is.True);
        Assert.That(path.Source, Is.EqualTo("$.users[12].name"));
        Assert.That(path.Count(), Is.EqualTo(3));
        Assert.That(path.GetSegment(0)!.Kind, Is.EqualTo(global::yaml.YamlPathSegmentKind.MappingKey));
        Assert.That(path.GetSegment(0)!.Key, Is.EqualTo("users"));
        Assert.That(path.GetSegment(1)!.Kind, Is.EqualTo(global::yaml.YamlPathSegmentKind.SequenceIndex));
        Assert.That(path.GetSegment(1)!.Index, Is.EqualTo(12));
        Assert.That(path.GetSegment(2)!.Key, Is.EqualTo("name"));
        Assert.That(path.ToText(), Is.EqualTo("users[12].name"));
    }

    [Test]
    public void Parse_AllowsRootSequenceAndHyphenatedMappingKeys()
    {
        global::yaml.YamlPath path = global::yaml.YamlPathParser.Parse("[0].server-name");

        Assert.That(path.Valid, Is.True);
        Assert.That(path.Count(), Is.EqualTo(2));
        Assert.That(path.GetSegment(0)!.Index, Is.EqualTo(0));
        Assert.That(path.GetSegment(1)!.Key, Is.EqualTo("server-name"));
        Assert.That(path.ToText(), Is.EqualTo("[0].server-name"));
    }

    [Test]
    public void Path_AddCopyAndLastSegment_WorkIndependently()
    {
        global::yaml.YamlPath path = new global::yaml.YamlPath();
        path.AddKey("items");
        path.AddIndex(2);
        global::yaml.YamlPath copy = path.Copy();
        path.AddKey("name");

        Assert.That(path.Count(), Is.EqualTo(3));
        Assert.That(copy.Count(), Is.EqualTo(2));
        Assert.That(copy.LastSegment()!.IsSequenceIndex(), Is.True);
        Assert.That(copy.LastSegment()!.Index, Is.EqualTo(2));
        Assert.That(copy.ToText(), Is.EqualTo("items[2]"));
    }

    [Test]
    public void Parse_InvalidPaths_ReturnInvalidPathWithError()
    {
        AssertInvalid(".name");
        AssertInvalid("items.");
        AssertInvalid("items[]");
        AssertInvalid("items[-1]");
        AssertInvalid("items[1");
        AssertInvalid("items[2147483648]");
        AssertInvalid("items$name");
    }

    static void AssertInvalid(string text)
    {
        global::yaml.YamlPath path = global::yaml.YamlPathParser.Parse(text);

        Assert.That(path.Valid, Is.False);
        Assert.That(path.Source, Is.EqualTo(text));
        Assert.That(path.Error, Is.Not.EqualTo(""));
    }
}
