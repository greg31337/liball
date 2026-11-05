using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgRequiredOptionValidationTests
{
    [Test]
    public void Validate_MissingRequiredLocalOptionAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "--configuration");
    }

    [Test]
    public void Validate_SuppliedRequiredLocalOptionDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--configuration", "Release" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_DefaultedRequiredLocalOptionDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name").Default("Debug");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_EnvironmentRequiredLocalOptionDoesNotAddValidationError()
    {
        string name = "ARGS_TEST_REQUIRED_OPTION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name").Env(name);
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(name, "Release");
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_MissingRequiredInheritedGlobalOptionAddsValidationError()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--workspace").Required().Value("path");
        root.Command("build");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertValidationError(result.Errors[0], "--workspace");
    }

    [Test]
    public void Validate_ParentLocalRequiredOptionDoesNotApplyToSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--root").Required().Flag();
        ArgCommand build = root.Command("build");
        build.Option("--local").Required().Flag();
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--local" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_ReportsAllMissingRequiredOptions()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--first").Required().Flag();
        command.Option("--second").Required().Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        AssertValidationError(result.Errors[0], "--first");
        AssertValidationError(result.Errors[1], "--second");
    }

    static void AssertValidationError(ArgParseError error, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo("missing_required_option"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo("Missing required option '" + symbolName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(-1));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
