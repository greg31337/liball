using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPositionalArgumentParserTests
{
    [Test]
    public void Parse_AssignsSinglePositionalArgument()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "project.csproj" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(result.Occurrences[0], "project", 0, new string[] { "project.csproj" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_OptionalPositionalArgumentWithoutValueCreatesNoOccurrence()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Optional();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.Empty);
    }

    [Test]
    public void Parse_AssignsPositionalArgumentsByDeclarationOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("source");
        command.Argument("destination");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "src", "dist" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertArgumentOccurrence(result.Occurrences[0], "source", 0, new string[] { "src" });
        AssertArgumentOccurrence(result.Occurrences[1], "destination", 1, new string[] { "dist" });
    }

    [Test]
    public void Parse_LeavesExtraPositionalsUnassigned()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "project.csproj", "extra" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(result.Occurrences[0], "project", 0, new string[] { "project.csproj" });
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[1] }));
        Assert.That(result.Tokens[1].Consumed, Is.False);
    }

    [Test]
    public void Parse_UsesResolvedCommandArguments()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Argument("rootArg");
        ArgCommand build = root.Command("build");
        build.Argument("project");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "project.csproj" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(result.Occurrences[0], "project", 1, new string[] { "project.csproj" });
        Assert.That(result.Positionals, Is.Empty);
    }

    [Test]
    public void Parse_ConsumedOptionValuesAreNotAssignedAsArguments()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name");
        command.Argument("project");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration", "Release", "project.csproj" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
        AssertArgumentOccurrence(result.Occurrences[1], "project", 2, new string[] { "project.csproj" });
        Assert.That(result.Positionals, Is.Empty);
    }

    static void AssertArgumentOccurrence(ArgOccurrence occurrence, string symbolName, int tokenIndex, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(""));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(occurrence.Values, Is.EqualTo(values));
        Assert.That(occurrence.Argument, Is.Not.Null);
    }
}
