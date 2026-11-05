using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgHelpCommandParserTests
{
    [Test]
    public void Parse_HelpCommandRequestsRootHelp()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--configuration").Required().Value("name");
        root.Argument("project").Required();

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.HelpCommandRequested, Is.True);
        Assert.That(parseResult.HelpCommandTokenIndex, Is.EqualTo(0));
        Assert.That(parseResult.Command, Is.SameAs(root));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
        Assert.That(parseResult.Tokens[0].Consumed, Is.True);
        Assert.That(parseResult.Positionals, Is.Empty);
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_HelpCommandResolvesNestedCommandPath()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--configuration").Required().Value("name");

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help", "project", "build" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.HelpCommandRequested, Is.True);
        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, project, build }));
        Assert.That(parseResult.Tokens[0].Consumed, Is.True);
        Assert.That(parseResult.Tokens[1].Consumed, Is.True);
        Assert.That(parseResult.Tokens[2].Consumed, Is.True);
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_HelpCommandStopsTargetResolutionAtFirstNonMatchingPositional()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.Command("build");

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help", "project", "unknown", "build" });

        Assert.That(parseResult.HelpCommandRequested, Is.True);
        Assert.That(parseResult.Command, Is.SameAs(project));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, project }));
        Assert.That(parseResult.Tokens[0].Consumed, Is.True);
        Assert.That(parseResult.Tokens[1].Consumed, Is.True);
        Assert.That(parseResult.Tokens[2].Consumed, Is.False);
        Assert.That(parseResult.Tokens[3].Consumed, Is.False);
        Assert.That(parseResult.Positionals, Is.EqualTo(new ArgToken[] { parseResult.Tokens[2], parseResult.Tokens[3] }));
    }

    [Test]
    public void Parse_HelpCommandResolvesCommandAliases()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project").Alias("p");
        ArgCommand build = project.Command("build").Alias("b");

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help", "p", "b" });

        Assert.That(parseResult.HelpCommandRequested, Is.True);
        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, project, build }));
    }

    [Test]
    public void Parse_HelpCommandRespectsCaseInsensitiveCommandMatching()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CommandsCaseInsensitive = true;
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");

        ArgParseResult parseResult = ArgParser.WithSettings(root, settings).Parse(new string[] { "HELP", "BUILD" });

        Assert.That(parseResult.HelpCommandRequested, Is.True);
        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, build }));
    }

    [Test]
    public void Help_ForHelpCommandTargetUsesResolvedCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Description("Build projects.");

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help", "project", "build" });

        string help = new ArgHelpBuilder().Help(parseResult.Command);

        Assert.That(help, Is.EqualTo("Usage: tool project build [options]\n\nBuild projects.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Parse_ExplicitHelpSubcommandTakesPrecedence()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand help = root.Command("help");

        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "help" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.HelpCommandRequested, Is.False);
        Assert.That(parseResult.Command, Is.SameAs(help));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, help }));
        Assert.That(validation.HelpRequested, Is.False);
    }
}
