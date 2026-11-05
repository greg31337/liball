using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgStopAtFirstPositionalParserTests
{
    [Test]
    public void Parse_ParsesOptionsAfterPositionalByDefault()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "file.txt", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
    }

    [Test]
    public void Parse_StopAtFirstPositionalPreventsLaterOptionParsing()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParser parser = CreateStopAtFirstPositionalParser(command);

        ArgParseResult result = parser.Parse(new string[] { "file.txt", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[0], result.Tokens[1] }));
        Assert.That(result.Tokens[0].Consumed, Is.False);
        Assert.That(result.Tokens[1].Consumed, Is.False);
    }

    [Test]
    public void Parse_StopAtFirstPositionalStillAssignsDeclaredArguments()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        command.Argument("first");
        command.Argument("second");
        ArgParser parser = CreateStopAtFirstPositionalParser(command);

        ArgParseResult result = parser.Parse(new string[] { "file.txt", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("first"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "file.txt" }));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("second"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "--verbose" }));
    }

    [Test]
    public void Parse_StopAtFirstPositionalRunsAfterCommandResolution()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag();
        ArgParser parser = CreateStopAtFirstPositionalParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "file.txt", "--local" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[1], result.Tokens[2] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    static ArgParser CreateStopAtFirstPositionalParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstPositional = true;
        return ArgParser.WithSettings(command, settings);
    }
}
