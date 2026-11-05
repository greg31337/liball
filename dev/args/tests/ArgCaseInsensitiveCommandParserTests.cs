using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCaseInsensitiveCommandParserTests
{
    [Test]
    public void Parse_CommandMatchingIsCaseSensitiveByDefault()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("Build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build" });

        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
    }

    [Test]
    public void Parse_CommandNameMatchingCanBeCaseInsensitive()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("Build");
        ArgParser parser = CreateCaseInsensitiveParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_CommandAliasMatchingCanBeCaseInsensitive()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build").Alias("B");
        ArgParser parser = CreateCaseInsensitiveParser(root);

        ArgParseResult result = parser.Parse(new string[] { "b" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, build }));
    }

    [Test]
    public void Parse_NestedCommandMatchingCanBeCaseInsensitive()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("Project");
        ArgCommand build = project.Command("Build");
        ArgParser parser = CreateCaseInsensitiveParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, project, build }));
    }

    static ArgParser CreateCaseInsensitiveParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CommandsCaseInsensitive = true;
        return ArgParser.WithSettings(command, settings);
    }
}
