using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgShortClusterParserTests
{
    [Test]
    public void Parse_PosixShortFlagCluster()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        command.Option("--brief").Alias("-b").Flag();
        command.Option("--color").Alias("-c").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-abc" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOccurrence(result.Occurrences[0], "--all", "-a", 0, new string[] { });
        AssertOccurrence(result.Occurrences[1], "--brief", "-b", 0, new string[] { });
        AssertOccurrence(result.Occurrences[2], "--color", "-c", 0, new string[] { });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_PosixShortClusterWhereFinalOptionConsumesAttachedValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        command.Option("--number").Alias("-n").Value("count");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-vn5" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOccurrence(result.Occurrences[0], "--verbose", "-v", 0, new string[] { });
        AssertOccurrence(result.Occurrences[1], "--number", "-n", 0, new string[] { "5" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_PosixShortClusterValueOptionConsumesRestOfToken()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        command.Option("--brief").Alias("-b").Value("mode");
        command.Option("--zip").Alias("-z").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-abz" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOccurrence(result.Occurrences[0], "--all", "-a", 0, new string[] { });
        AssertOccurrence(result.Occurrences[1], "--brief", "-b", 0, new string[] { "z" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_PosixShortClusterWhereFinalOptionConsumesSeparateValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        command.Option("--number").Alias("-n").Value("count");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-vn", "5" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOccurrence(result.Occurrences[0], "--verbose", "-v", 0, new string[] { });
        AssertOccurrence(result.Occurrences[1], "--number", "-n", 0, new string[] { "5" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
    }

    [Test]
    public void Parse_PosixShortClusterFinalValueDoesNotConsumeEndOfOptionsDelimiter()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        command.Option("--number").Alias("-n").Value("count");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-vn", "--", "5" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOccurrence(result.Occurrences[0], "--verbose", "-v", 0, new string[] { });
        AssertOccurrence(result.Occurrences[1], "--number", "-n", 0, new string[] { });
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Option '-n' requires a value."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Kind, Is.EqualTo(ArgTokenKind.EndOfOptions));
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Tokens[2].Kind, Is.EqualTo(ArgTokenKind.Positional));
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void Parse_PosixShortClusterAddsErrorForUnknownMembers()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-ax" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOccurrence(result.Occurrences[0], "--all", "-a", 0, new string[] { });
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '-x'."));
        Assert.That(result.Errors[0].TokenIndex, Is.EqualTo(0));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("-x"));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_PosixShortClustersCanBeDisabled()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--all").Alias("-a").Flag();
        command.Option("--brief").Alias("-b").Flag();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "-ab" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '-ab'."));
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
