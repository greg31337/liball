using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgUnmatchedTokenParserTests
{
    [Test]
    public void Parse_UnknownOptionProducesErrorByDefault()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--missing" });

        Assert.That(result.UnmatchedTokens, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--missing'."));
    }

    [Test]
    public void Parse_CollectUnmatchedStoresUnknownLongOptionWithoutError()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParser parser = CreateCollectingParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--missing" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_CollectUnmatchedStoresUnknownShortOptionAndContinues()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = CreateCollectingParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-x", "-v" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
    }

    [Test]
    public void Parse_CollectUnmatchedStoresUnknownSlashOptionWhenSlashParsingIsEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        settings.CollectUnmatched = true;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "/missing" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_CollectUnmatchedStoresClusterTokenOnceForUnknownClusterMembers()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        ArgParser parser = CreateCollectingParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-axy" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--all"));
    }

    static ArgParser CreateCollectingParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        return ArgParser.WithSettings(command, settings);
    }
}
