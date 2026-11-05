using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCommandAliasParserTests
{
    [Test]
    public void Parse_ResolvesRootSubcommandAlias()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build").Alias("b");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "b" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Positionals, Is.Empty);
    }

    [Test]
    public void Parse_ResolvesNestedCommandAliases()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project").Alias("p");
        ArgCommand build = project.Command("build").Alias("b");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "p", "b" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, project, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_UsesAliasResolvedCommandForOptionLookup()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build").Alias("b");
        build.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "b", "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }
}
