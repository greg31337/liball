using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgLoneDashParserTests
{
    [Test]
    public void Parse_LoneDashIsCapturedAsPositional()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("-v"));
        Assert.That(result.Positionals.Count, Is.EqualTo(1));
        Assert.That(result.Positionals[0].RawText, Is.EqualTo("-"));
        Assert.That(result.Positionals[0].Index, Is.EqualTo(0));
        Assert.That(result.Positionals[0].Kind, Is.EqualTo(ArgTokenKind.Positional));
        Assert.That(result.Positionals[0].Consumed, Is.False);
    }

    [Test]
    public void Parse_LoneDashCanBeConsumedAsOptionValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--input").Value("path");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--input", "-" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "-" }));
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Tokens[1].Kind, Is.EqualTo(ArgTokenKind.Positional));
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_PositionalsAfterEndOfOptionsIncludeOptionLikeTokensButNotDelimiter()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--", "-", "-v", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals.Count, Is.EqualTo(3));
        Assert.That(result.Positionals[0].RawText, Is.EqualTo("-"));
        Assert.That(result.Positionals[1].RawText, Is.EqualTo("-v"));
        Assert.That(result.Positionals[2].RawText, Is.EqualTo("--verbose"));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }
}
