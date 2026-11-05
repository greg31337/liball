using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionDependencyValidationTests
{
    [Test]
    public void Validate_MissingDependentOptionAddsValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Flag().Requires("--configuration");
        command.Option("--configuration").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--publish" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDependencyError(result.Errors[0], "--publish", "--configuration", 0);
    }

    [Test]
    public void Validate_PresentDependentOptionDoesNotAddValidationError()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Flag().Requires("--configuration");
        command.Option("--configuration").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--publish", "--configuration", "Release" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_DependencyNameCanUseAlias()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Flag().Requires("-c");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--publish" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDependencyError(result.Errors[0], "--publish", "--configuration", 0);
    }

    [Test]
    public void Validate_DefaultedDependentOptionSatisfiesDependency()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Flag().Requires("--configuration");
        command.Option("--configuration").Value("name").Default("Release");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--publish" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_EnvironmentDependentOptionSatisfiesDependency()
    {
        string name = "ARGS_TEST_DEPENDENCY_OPTION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Flag().Requires("--configuration");
        command.Option("--configuration").Value("name").Env(name);
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(name, "Release");
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "--publish" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_DefaultedSourceOptionReportsMissingDependency()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--publish").Value("enabled").Default("true").Requires("--configuration");
        command.Option("--configuration").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDependencyError(result.Errors[0], "--publish", "--configuration", -1);
    }

    [Test]
    public void Validate_LocalOptionCanRequireInheritedGlobalOption()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--workspace").Value("path");
        ArgCommand build = root.Command("build");
        build.Option("--publish").Flag().Requires("--workspace");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--publish" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDependencyError(result.Errors[0], "--publish", "--workspace", 1);
    }

    [Test]
    public void Validate_RepeatedSourceOptionReportsMissingDependencyOnce()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--tag").Value("name").Repeatable().Requires("--message");
        command.Option("--message").Value("text");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--tag", "one", "--tag", "two" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertDependencyError(result.Errors[0], "--tag", "--message", 0);
    }

    static void AssertDependencyError(ArgParseError error, string symbolName, string requiredName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo("missing_dependent_option"));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo("Option '" + symbolName + "' requires option '" + requiredName + "'."));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }
}
