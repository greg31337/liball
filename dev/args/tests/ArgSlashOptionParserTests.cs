using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgSlashOptionParserTests
{
    [Test]
    public void Parse_SlashFlagWhenEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/verbose").Flag();
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "/verbose", "/verbose", 0, new string[] { });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_SlashOptionWithColonValueWhenEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/configuration").Value("name");
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/configuration:Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "/configuration", "/configuration", 0, new string[] { "Release" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_SlashOptionWithColonValueContainingColon()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/define").Value("pair");
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/define:name:value" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "/define", "/define", 0, new string[] { "name:value" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_SlashOptionWithSeparateValueWhenEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/configuration").Value("name");
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "/configuration", "/configuration", 0, new string[] { "Release" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_SlashOptionIsPositionalWhenDisabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Tokens[0].Kind, Is.EqualTo(ArgTokenKind.Positional));
        Assert.That(result.Tokens[0].Consumed, Is.False);
    }

    [Test]
    public void Parse_UnknownSlashOptionAddsStructuredError()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/missing" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '/missing'."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("/missing"));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_SlashFlagWithColonValueAddsErrorButKeepsOccurrence()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/verbose").Flag();
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/verbose:true" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '/verbose' does not accept a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
    }

    [Test]
    public void Parse_SlashOptionMissingRequiredValueAddsError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/configuration").Value("name");
        ArgParser parser = CreateSlashParser(command);

        ArgParseResult result = parser.Parse(new string[] { "/configuration" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '/configuration' requires a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
    }

    static ArgParser CreateSlashParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        return ArgParser.WithSettings(command, settings);
    }

    static void AssertOccurrence(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex, string[] values)
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
