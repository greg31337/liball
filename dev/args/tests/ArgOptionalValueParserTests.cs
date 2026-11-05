using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionalValueParserTests
{
    [Test]
    public void Parse_OptionalLongOptionWithoutValueCapturesEmptyOccurrence()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--color", 0, new string[] { });
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
    }

    [Test]
    public void Parse_OptionalLongOptionConsumesSeparateValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode");
        command.Argument("target");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color", "always", "project" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--color", 0, new string[] { "always" });
        Assert.That(result.Occurrences[1].SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("target"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "project" }));
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_OptionalLongOptionDoesNotConsumeFollowingOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode");
        command.Option("--verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--color", 0, new string[] { });
        AssertOptionOccurrence(result.Occurrences[1], "--verbose", "--verbose", 1, new string[] { });
    }

    [Test]
    public void Parse_OptionalShortOptionWithoutAttachedValueCapturesEmptyOccurrence()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Alias("-c").OptionalValue("mode");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-c" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "-c", 0, new string[] { });
    }

    static void AssertOptionOccurrence(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(alias));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(occurrence.Values, Is.EqualTo(values));
        Assert.That(occurrence.Option, Is.Not.Null);
    }
}
