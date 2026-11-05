using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgVariadicPositionalArgumentParserTests
{
    [Test]
    public void Parse_VariadicArgumentConsumesAllPositionals()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Variadic();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "a.txt", "b.txt", "c.txt" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(result.Occurrences[0], "files", 0, new string[] { "a.txt", "b.txt", "c.txt" });
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Tokens[2].Consumed, Is.True);
    }

    [Test]
    public void Parse_VariadicArgumentAfterScalarArgumentConsumesRemainingPositionals()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("source");
        command.Argument("files").Variadic();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "src", "a.txt", "b.txt" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertArgumentOccurrence(result.Occurrences[0], "source", 0, new string[] { "src" });
        AssertArgumentOccurrence(result.Occurrences[1], "files", 1, new string[] { "a.txt", "b.txt" });
    }

    [Test]
    public void Parse_RequiredVariadicArgumentConsumesAllValuesAndPassesValidation()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Required().Variadic();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "a.txt", "b.txt" });

        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.HasErrors(), Is.False);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(parseResult.Occurrences[0], "files", 0, new string[] { "a.txt", "b.txt" });
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_RequiredVariadicArgumentWithoutValuesAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Required().Variadic();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Occurrences, Is.Empty);
        Assert.That(validation.Errors.Count, Is.EqualTo(1));
        Assert.That(validation.Errors[0].Code, Is.EqualTo("missing_required_argument"));
        Assert.That(validation.Errors[0].SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(validation.Errors[0].SymbolName, Is.EqualTo("files"));
    }

    [Test]
    public void Parse_OptionalVariadicArgumentWithoutValuesCreatesNoOccurrence()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Variadic();
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.Empty);
    }

    [Test]
    public void Parse_UsesResolvedCommandVariadicArgument()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Argument("rootArg");
        ArgCommand run = root.Command("run");
        run.Argument("args").Variadic();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "run", "one", "two" });

        Assert.That(result.Command, Is.SameAs(run));
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertArgumentOccurrence(result.Occurrences[0], "args", 1, new string[] { "one", "two" });
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
