using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgNegatableOptionParserTests
{
    [Test]
    public void Parse_NegatableOptionPositiveAliasReadsAsTrue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Negatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--color" });
        ArgValues values = new ArgValues(result);

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--color", 0, new string[] { });
        Assert.That(values.Bool("--color"), Is.True);
    }

    [Test]
    public void Parse_NegatableOptionNoAliasReadsAsFalse()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Negatable();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--no-color" });
        ArgValues values = new ArgValues(result);

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--no-color", 0, new string[] { "false" });
        Assert.That(values.Bool("--color"), Is.False);
    }

    [Test]
    public void Parse_InheritedGlobalNegatableOptionNoAliasIsAvailableToNestedSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--color").Negatable();
        ArgCommand build = root.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--no-color" });
        ArgValues values = new ArgValues(result);

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--color", "--no-color", 1, new string[] { "false" });
        Assert.That(values.Bool("--color"), Is.False);
    }

    [Test]
    public void Parse_NoAliasIsUnknownForNonNegatableOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--no-color" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--no-color"));
    }

    [Test]
    public void Parse_DirectAliasTakesPrecedenceOverNegatedAlias()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--cache").Negatable();
        command.Option("--no-cache").Flag();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--no-cache" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOptionOccurrence(result.Occurrences[0], "--no-cache", "--no-cache", 0, new string[] { });
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
