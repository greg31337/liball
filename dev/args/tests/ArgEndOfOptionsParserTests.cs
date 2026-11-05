using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgEndOfOptionsParserTests
{
    [Test]
    public void Parse_EndOfOptionsPreventsLaterOptionParsing()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--", "--verbose", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        AssertToken(result.Tokens[0], "--", ArgTokenKind.EndOfOptions, true);
        AssertToken(result.Tokens[1], "--verbose", ArgTokenKind.Positional, false);
        AssertToken(result.Tokens[2], "-v", ArgTokenKind.Positional, false);
    }

    [Test]
    public void Parse_EndOfOptionsAllowsFollowingTokensAsArgumentValues()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        command.Argument("files").Variadic();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--", "--verbose", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("files"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "--verbose", "-v" }));
        AssertToken(result.Tokens[0], "--", ArgTokenKind.EndOfOptions, true);
        AssertToken(result.Tokens[1], "--verbose", ArgTokenKind.Positional, true);
        AssertToken(result.Tokens[2], "-v", ArgTokenKind.Positional, true);
    }

    [Test]
    public void Parse_ParsesOptionsBeforeEndOfOptionsOnly()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose", "--", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("--verbose"));
        AssertToken(result.Tokens[0], "--verbose", ArgTokenKind.LongOption, true);
        AssertToken(result.Tokens[1], "--", ArgTokenKind.EndOfOptions, true);
        AssertToken(result.Tokens[2], "-v", ArgTokenKind.Positional, false);
    }

    [Test]
    public void Parse_UsesConfiguredEndOfOptionsDelimiter()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EndOfOptionsDelimiter = "++";
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "++", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        AssertToken(result.Tokens[0], "++", ArgTokenKind.EndOfOptions, true);
        AssertToken(result.Tokens[1], "--verbose", ArgTokenKind.Positional, false);
    }

    [Test]
    public void Parse_EndOfOptionsDoesNotSatisfyMissingOptionValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration", "--" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '--configuration' requires a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        AssertToken(result.Tokens[0], "--configuration", ArgTokenKind.LongOption, true);
        AssertToken(result.Tokens[1], "--", ArgTokenKind.EndOfOptions, true);
    }

    static void AssertToken(ArgToken token, string rawText, ArgTokenKind kind, bool consumed)
    {
        Assert.That(token.RawText, Is.EqualTo(rawText));
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Consumed, Is.EqualTo(consumed));
    }
}
