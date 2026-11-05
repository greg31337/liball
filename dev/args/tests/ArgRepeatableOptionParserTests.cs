using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgRepeatableOptionParserTests
{
    [Test]
    public void Parse_RepeatableLongOptionCapturesEachOccurrenceInOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Value("path").Repeatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--include", "src", "--include=tests" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOptionOccurrence(result.Occurrences[0], "--include", "--include", 0, new string[] { "src" });
        AssertOptionOccurrence(result.Occurrences[1], "--include", "--include", 2, new string[] { "tests" });
        Assert.That(result.OptionOccurrences("--include"), Is.EqualTo(new ArgOccurrence[] { result.Occurrences[0], result.Occurrences[1] }));
    }

    [Test]
    public void Parse_RepeatableShortOptionCapturesEachOccurrenceInOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Alias("-I").Value("path").Repeatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "-I", "src", "-I", "tests" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOptionOccurrence(result.Occurrences[0], "--include", "-I", 0, new string[] { "src" });
        AssertOptionOccurrence(result.Occurrences[1], "--include", "-I", 2, new string[] { "tests" });
    }

    [Test]
    public void Parse_RepeatableFlagCapturesEachEncounter()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag().Repeatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--verbose", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOptionOccurrence(result.Occurrences[0], "--verbose", "--verbose", 0, new string[] { });
        AssertOptionOccurrence(result.Occurrences[1], "--verbose", "-v", 1, new string[] { });
    }

    [Test]
    public void Parse_RepeatableOptionalValueOptionCapturesValueAndNoValueOccurrences()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--tag").OptionalValue("name").Repeatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--tag", "one", "--tag", "--tag=three" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOptionOccurrence(result.Occurrences[0], "--tag", "--tag", 0, new string[] { "one" });
        AssertOptionOccurrence(result.Occurrences[1], "--tag", "--tag", 2, new string[] { });
        AssertOptionOccurrence(result.Occurrences[2], "--tag", "--tag", 3, new string[] { "three" });
        Assert.That(result.OptionOccurrences("--tag"), Is.EqualTo(new ArgOccurrence[] { result.Occurrences[0], result.Occurrences[1], result.Occurrences[2] }));
    }

    [Test]
    public void Parse_RepeatableOptionCaptureInterleavesWithOtherOccurrences()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Value("path").Repeatable();
        command.Argument("project");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--include", "src", "project.csproj", "--include", "tests" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOptionOccurrence(result.Occurrences[0], "--include", "--include", 0, new string[] { "src" });
        Assert.That(result.Occurrences[1].SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        AssertOptionOccurrence(result.Occurrences[1], "--include", "--include", 3, new string[] { "tests" });
        Assert.That(result.Occurrences[2].SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(result.Occurrences[2].SymbolName, Is.EqualTo("project"));
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
