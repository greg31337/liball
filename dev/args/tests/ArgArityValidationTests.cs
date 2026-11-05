using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgArityValidationTests
{
    [Test]
    public void Validate_OptionWithTooFewValuesAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Arity(2, 3);
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--include", "src" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertArityError(result.Errors[0], "too_few_values", ArgSymbolKind.Option, "Option '--include' expects at least 2 values.", 0, "--include");
    }

    [Test]
    public void Validate_OptionWithTooManyValuesAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode", "debug" });
        parseResult.Occurrences[0].Values.Add("extra");

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertArityError(result.Errors[0], "too_many_values", ArgSymbolKind.Option, "Option '--mode' expects at most 1 value.", 0, "--mode");
    }

    [Test]
    public void Validate_ArgumentWithTooFewValuesAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("files").Arity(2, ArgArity.Unbounded);
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "one.txt" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertArityError(result.Errors[0], "too_few_values", ArgSymbolKind.Argument, "Argument 'files' expects at least 2 values.", 0, "files");
    }

    [Test]
    public void Validate_ArgumentWithTooManyValuesAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Arity(1, 1);
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "src" });
        parseResult.Occurrences[0].Values.Add("extra");

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertArityError(result.Errors[0], "too_many_values", ArgSymbolKind.Argument, "Argument 'project' expects at most 1 value.", 0, "project");
    }

    [Test]
    public void Validate_AllowedOptionAndArgumentAritiesDoNotAddValidationErrors()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--color").OptionalValue("name");
        command.Argument("files").Arity(1, ArgArity.Unbounded);
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--color", "blue", "one.txt", "two.txt" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    static void AssertArityError(ArgParseError error, string code, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.Message, Is.EqualTo(message));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
