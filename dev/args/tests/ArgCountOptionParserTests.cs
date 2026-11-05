using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCountOptionParserTests
{
    [Test]
    public void Parse_CountShortClusterCapturesOneCountPerFlag()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Count();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-vvv" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertCountOccurrence(result.Occurrences[0], "--verbose", "-v", 0);
        AssertCountOccurrence(result.Occurrences[1], "--verbose", "-v", 0);
        AssertCountOccurrence(result.Occurrences[2], "--verbose", "-v", 0);
        Assert.That(result.CountOption("--verbose"), Is.EqualTo(3));
    }

    [Test]
    public void Parse_RepeatedLongCountOptionCapturesCounts()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Count();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose", "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertCountOccurrence(result.Occurrences[0], "--verbose", "--verbose", 0);
        AssertCountOccurrence(result.Occurrences[1], "--verbose", "--verbose", 1);
        Assert.That(result.CountOption("--verbose"), Is.EqualTo(2));
    }

    [Test]
    public void Parse_CountAndNonCountFlagsCanShareACluster()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Count();
        command.Option("--quiet").Alias("-q").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-vvq" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertCountOccurrence(result.Occurrences[0], "--verbose", "-v", 0);
        AssertCountOccurrence(result.Occurrences[1], "--verbose", "-v", 0);
        Assert.That(result.Occurrences[2].SymbolName, Is.EqualTo("--quiet"));
        Assert.That(result.Occurrences[2].Count, Is.EqualTo(0));
        Assert.That(result.CountOption("--verbose"), Is.EqualTo(2));
        Assert.That(result.CountOption("--quiet"), Is.EqualTo(0));
    }

    static void AssertCountOccurrence(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(alias));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Values, Is.Empty);
        Assert.That(occurrence.Count, Is.EqualTo(1));
        Assert.That(occurrence.Option, Is.Not.Null);
    }
}
