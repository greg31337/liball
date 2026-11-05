using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCaseInsensitiveOptionParserTests
{
    [Test]
    public void Parse_OptionMatchingIsCaseSensitiveByDefault()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--Verbose").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--verbose'."));
    }

    [Test]
    public void Parse_LongOptionMatchingCanBeCaseInsensitive()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--Configuration").Value("name");
        ArgParser parser = CreateCaseInsensitiveParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration=Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--Configuration"));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_ShortOptionMatchingCanBeCaseInsensitive()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-V").Flag();
        ArgParser parser = CreateCaseInsensitiveParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("-v"));
    }

    [Test]
    public void Parse_SlashOptionMatchingCanBeCaseInsensitiveWhenSlashOptionsAreEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("/Verbose").Flag();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        settings.OptionsCaseInsensitive = true;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "/verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("/Verbose"));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("/verbose"));
    }

    static ArgParser CreateCaseInsensitiveParser(ArgCommand command)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.OptionsCaseInsensitive = true;
        return ArgParser.WithSettings(command, settings);
    }
}
