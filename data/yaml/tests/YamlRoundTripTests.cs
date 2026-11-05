using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlRoundTripTests
{
    [Test]
    public void RoundTrip_PreservesCommentsAndBlankLines()
    {
        string source = "\n# document\n# pair\nname: Ada # inline\n";

        global::yaml.YamlDocument first = global::yaml.Yaml.Parse(source);
        string emitted = global::yaml.YamlEmitter.EmitDocument(first);
        global::yaml.YamlDocument second = global::yaml.Yaml.Parse(emitted);

        Assert.That(emitted, Is.EqualTo("\n# document\n# pair\nname: Ada # inline\n"));
        Assert.That(second.LeadingBlankLines.Count, Is.EqualTo(1));
        Assert.That(second.LeadingComments, Is.EqualTo(new List<string> { "# document", "# pair" }));
        Assert.That(second.Root!.Pairs[0].Value!.TrailingComments, Is.EqualTo(new List<string> { "# inline" }));
    }

    [Test]
    public void RoundTrip_PreservesTagsAnchorsAliasesAndAliasTargets()
    {
        string source = "person: &person !person Ada\ncopy: *person\n";

        global::yaml.YamlDocument first = global::yaml.Yaml.Parse(source);
        string emitted = global::yaml.YamlEmitter.EmitDocument(first);
        global::yaml.YamlDocument second = global::yaml.Yaml.Parse(emitted);
        global::yaml.YamlNode root = second.Root!;
        global::yaml.YamlNode person = root.Pairs[0].Value!;
        global::yaml.YamlNode copy = root.Pairs[1].Value!;

        Assert.That(emitted, Is.EqualTo("person: &person !person Ada\ncopy: *person\n"));
        Assert.That(person.Anchor, Is.EqualTo("person"));
        Assert.That(person.Tag, Is.EqualTo("!person"));
        Assert.That(copy.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(copy.AliasName, Is.EqualTo("person"));
        Assert.That(copy.AliasTarget, Is.SameAs(person));
    }

    [Test]
    public void RoundTrip_PreservesMappingOrder()
    {
        string source = "zeta: last\nalpha: first\nmiddle: kept\n";

        global::yaml.YamlDocument first = global::yaml.Yaml.Parse(source);
        string emitted = global::yaml.YamlEmitter.EmitDocument(first);
        global::yaml.YamlDocument second = global::yaml.Yaml.Parse(emitted);

        Assert.That(emitted, Is.EqualTo(source));
        Assert.That(second.Root!.Pairs[0].Key!.Value, Is.EqualTo("zeta"));
        Assert.That(second.Root!.Pairs[1].Key!.Value, Is.EqualTo("alpha"));
        Assert.That(second.Root!.Pairs[2].Key!.Value, Is.EqualTo("middle"));
    }

    [Test]
    public void RoundTrip_PreservesSelectedScalarStyles()
    {
        string source =
            "single: 'Ada''s note'\n" +
            "double: \"line\\nnext\"\n" +
            "literal: |\n" +
            "  one\n" +
            "  two\n" +
            "folded: >\n" +
            "  one\n" +
            "  two\n";

        global::yaml.YamlDocument first = global::yaml.Yaml.Parse(source);
        string emitted = global::yaml.YamlEmitter.EmitDocument(first);
        global::yaml.YamlDocument second = global::yaml.Yaml.Parse(emitted);
        global::yaml.YamlNode root = second.Root!;

        Assert.That(root.FindMappingValue("single")!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
        Assert.That(root.FindMappingValue("double")!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
        Assert.That(root.FindMappingValue("literal")!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Literal));
        Assert.That(root.FindMappingValue("folded")!.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Folded));
        Assert.That(emitted, Does.Contain("single: 'Ada''s note'\n"));
        Assert.That(emitted, Does.Contain("double: \"line\\nnext\"\n"));
        Assert.That(emitted, Does.Contain("literal: |\n"));
        Assert.That(emitted, Does.Contain("folded: >\n"));
    }
}
