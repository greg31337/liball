using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgChoiceValidationTests
{
    [Test]
    public void Validate_InvalidOptionChoiceAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name").Choice("debug").Choice("release");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode", "fast" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertInvalidChoice(result.Errors[0], ArgSymbolKind.Option, "Option '--mode' value 'fast' is not one of the allowed choices.", 0, "--mode");
    }

    [Test]
    public void Validate_ValidOptionChoiceDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name").Choice("debug").Choice("release");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode", "debug" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_InvalidArgumentChoiceAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("action").Choice("build").Choice("test");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "deploy" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertInvalidChoice(result.Errors[0], ArgSymbolKind.Argument, "Argument 'action' value 'deploy' is not one of the allowed choices.", 0, "action");
    }

    [Test]
    public void Validate_RepeatableOptionValidatesEachOccurrenceChoice()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--target").Value("name").Repeatable().Choice("linux").Choice("macos");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--target", "linux", "--target", "windows" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertInvalidChoice(result.Errors[0], ArgSymbolKind.Option, "Option '--target' value 'windows' is not one of the allowed choices.", 2, "--target");
    }

    [Test]
    public void Validate_InvalidDefaultOptionChoiceAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").Value("name").Default("fast").Choice("debug").Choice("release");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertInvalidChoice(result.Errors[0], ArgSymbolKind.Option, "Option '--mode' value 'fast' is not one of the allowed choices.", -1, "--mode");
    }

    [Test]
    public void Validate_InvalidVariadicArgumentChoiceAddsValidationErrorForEachInvalidValue()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("targets").Variadic().Choice("linux").Choice("macos");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "linux", "windows", "freebsd" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        AssertInvalidChoice(result.Errors[0], ArgSymbolKind.Argument, "Argument 'targets' value 'windows' is not one of the allowed choices.", 0, "targets");
        AssertInvalidChoice(result.Errors[1], ArgSymbolKind.Argument, "Argument 'targets' value 'freebsd' is not one of the allowed choices.", 0, "targets");
    }

    static void AssertInvalidChoice(ArgParseError error, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo("invalid_choice"));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.Message, Is.EqualTo(message));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
