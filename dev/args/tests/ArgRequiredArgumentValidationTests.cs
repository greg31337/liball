using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgRequiredArgumentValidationTests
{
    [Test]
    public void Validate_MissingRequiredArgumentAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Required();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "project");
    }

    [Test]
    public void Validate_SuppliedRequiredArgumentDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Required();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "src" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_UsesResolvedCommandArguments()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Argument("rootArg").Required();
        ArgCommand build = root.Command("build");
        build.Argument("project").Required();
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "project");
    }

    [Test]
    public void Validate_DefaultedRequiredArgumentDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Required().Default(".");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_EnvironmentRequiredArgumentDoesNotAddValidationError()
    {
        string name = "ARGS_TEST_REQUIRED_ARGUMENT";
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Required().Env(name);
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(name, "src");
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_ReportsAllMissingRequiredArguments()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("source").Required();
        command.Argument("destination").Required();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        AssertValidationError(result.Errors[0], "source");
        AssertValidationError(result.Errors[1], "destination");
    }

    static void AssertValidationError(ArgParseError error, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo("missing_required_argument"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(error.Message, Is.EqualTo("Missing required argument '" + symbolName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(-1));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
