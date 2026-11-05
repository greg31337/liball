using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCommandPathParserTests
{
    [Test]
    public void Parse_ResolvesDeepestLeadingCommandPath()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "build", "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, project, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_ResolvesHiddenSubcommands()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand internalCommand = root.Command("internal").Hidden();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "internal" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(internalCommand));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, internalCommand }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Positionals, Is.Empty);
    }

    [Test]
    public void Parse_StopsCommandResolutionAtFirstNonMatchingPositional()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "unknown", "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(project));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root, project }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.False);
        Assert.That(result.Tokens[2].Consumed, Is.False);
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[1], result.Tokens[2] }));
    }

    [Test]
    public void Parse_DoesNotResolveCommandsAfterEndOfOptionsDelimiter()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        root.Argument("values").Variadic();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "--", "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("values"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "build" }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_DoesNotResolveCommandsAfterAnOption()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--verbose").Flag();
        root.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "--verbose", "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[1] }));
    }

    [Test]
    public void Parse_UsesResolvedCommandForOptionLookup()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--root").Flag();
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--local", "--root" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--local"));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--root'."));
    }

    [Test]
    public void Parse_ResetsResolvedCommandBetweenParseCalls()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        root.Option("--root").Flag();
        build.Option("--local").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult buildResult = parser.Parse(new string[] { "build", "--local" });
        ArgParseResult rootResult = parser.Parse(new string[] { "--root" });

        Assert.That(buildResult.HasErrors(), Is.False);
        Assert.That(buildResult.Command, Is.SameAs(build));
        Assert.That(buildResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, build }));
        Assert.That(rootResult.HasErrors(), Is.False);
        Assert.That(rootResult.Command, Is.SameAs(root));
        Assert.That(rootResult.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
        Assert.That(rootResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(rootResult.Occurrences[0].SymbolName, Is.EqualTo("--root"));
    }
}
