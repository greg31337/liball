using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgFallbackValueParserTests
{
    [Test]
    public void Parse_AppliesFallbackForPresentOptionalLongOptionWithoutValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode").Fallback("auto");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertFallbackOccurrence(result.Occurrences[0], "--color", "--color", 0, "auto");
    }

    [Test]
    public void Parse_AppliesFallbackWhenNextTokenIsAnotherOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode").Fallback("auto");
        command.Option("--verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertFallbackOccurrence(result.Occurrences[0], "--color", "--color", 0, "auto");
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[1].Source, Is.EqualTo(ArgValueSource.CommandLine));
    }

    [Test]
    public void Parse_DoesNotApplyFallbackWhenOptionalLongOptionHasValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode").Fallback("auto");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color=always" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "always" }));
    }

    [Test]
    public void Parse_AppliesFallbackForPresentOptionalShortOptionWithoutValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Alias("-c").OptionalValue("mode").Fallback("auto");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-c" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertFallbackOccurrence(result.Occurrences[0], "--color", "-c", 0, "auto");
    }

    [Test]
    public void Parse_AppliesFallbackForPresentOptionalSlashOptionWithoutValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/color").OptionalValue("mode").Fallback("auto");
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "/color" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertFallbackOccurrence(result.Occurrences[0], "/color", "/color", 0, "auto");
    }

    [Test]
    public void Parse_DoesNotApplyFallbackWhenOptionalOptionIsMissing()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("mode").Fallback("auto");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
    }

    [Test]
    public void Parse_DoesNotApplyFallbackToRequiredValueOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Fallback("Debug");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '--configuration' requires a value."));
    }

    static void AssertFallbackOccurrence(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex, string value)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(alias));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.Fallback));
        Assert.That(occurrence.Values, Is.EqualTo(new string[] { value }));
    }
}
