using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgStopAtFirstUnmatchedParserTests
{
    [Test]
    public void Parse_StopAtFirstUnmatchedCollectsUnknownAndRemainingTokens()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParser parser = CreateStopAtFirstUnmatchedParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--missing", "--verbose", "file.txt" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0], result.Tokens[1], result.Tokens[2] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.False);
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void Parse_StopAtFirstUnmatchedKeepsOccurrencesBeforeUnknownToken()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParser parser = CreateStopAtFirstUnmatchedParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose", "--missing", "--verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[1], result.Tokens[2] }));
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void Parse_StopAtFirstUnmatchedStopsInsideShortCluster()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        command.Option("--brief").Alias("-b").Flag();
        ArgParser parser = CreateStopAtFirstUnmatchedParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-xab", "--brief" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0], result.Tokens[1] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.False);
    }

    [Test]
    public void Parse_StopAtFirstUnmatchedOverridesErrorModeWithoutCollectUnmatched()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstUnmatched = true;
        settings.CollectUnmatched = false;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "--missing" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
    }

    static ArgParser CreateStopAtFirstUnmatchedParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstUnmatched = true;
        return ArgParser.WithSettings(command, settings);
    }
}
