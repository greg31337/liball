using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgShortOptionParserTests
{
    [Test]
    public void Parse_ShortOptionWithSeparateValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-c", "Release", "project.csproj" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "--configuration", "-c", 0, new string[] { "Release" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void Parse_ShortFlagWithoutValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "--verbose", "-v", 0, new string[] { });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_AttachedShortValueWhenEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = true;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "-cRelease" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "--configuration", "-c", 0, new string[] { "Release" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_AttachedShortValueIsUnknownWhenDisabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "-cRelease" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '-cRelease'."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("-cRelease"));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_MissingRequiredShortOptionValueAddsError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-c", "-v" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '-c' requires a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("-c"));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_ShortFlagWithAttachedValueAddsErrorWhenAttachedValuesAreEnabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = true;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "-vtrue" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '-v' does not accept a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
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
