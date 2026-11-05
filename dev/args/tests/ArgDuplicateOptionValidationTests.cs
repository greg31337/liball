using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDuplicateOptionValidationTests
{
    [Test]
    public void Validate_DuplicateNonRepeatableOptionAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--verbose", "--verbose" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDuplicateError(result.Errors[0], "--verbose", 1);
    }

    [Test]
    public void Validate_DuplicateAliasesForSameOptionAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--verbose", "-v" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDuplicateError(result.Errors[0], "--verbose", 1);
    }

    [Test]
    public void Validate_RepeatableOptionDoesNotAddDuplicateValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Value("path").Repeatable();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--include", "src", "--include", "tests" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_CountOptionDoesNotAddDuplicateValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v").Count();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-v", "-v" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_DuplicateInheritedGlobalOptionAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--workspace").Value("path");
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--workspace", "src", "--workspace", "tests" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDuplicateError(result.Errors[0], "--workspace", 3);
    }

    static void AssertDuplicateError(ArgParseError error, string symbolName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo("duplicate_option"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo("Option '" + symbolName + "' cannot be specified multiple times."));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
