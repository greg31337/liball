using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgHelpVersionShortCircuitValidationTests
{
    [Test]
    public void Validate_HelpOptionSkipsRequiredOptionAndArgumentValidation()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--help").Alias("-h").Flag();
        command.Option("--configuration").Required().Value("name");
        command.Argument("project").Required();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-h" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.VersionRequested, Is.False);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_VersionOptionSkipsRequiredSubcommandValidation()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--version").Alias("-V").Flag();
        command.RequiredSubcommand();
        command.Command("build");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-V" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.False);
        Assert.That(result.VersionRequested, Is.True);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_HelpOptionSkipsConflictAndDependencyValidation()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--help").Flag();
        command.Option("--fast").Flag().Conflicts("--safe").Requires("--configuration");
        command.Option("--safe").Flag();
        command.Option("--configuration").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--help", "--fast", "--safe" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void Validate_ShortCircuitPreservesParseErrors()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--help").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--missing", "--help" });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--missing"));
    }

    [Test]
    public void Validate_WithoutHelpOrVersionRunsValidationRules()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--help").Flag();
        command.Option("--configuration").Required().Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });

        ArgValidationResult result = new ArgValidator().Validate(parseResult);

        Assert.That(result.ShortCircuited, Is.False);
        Assert.That(result.HelpRequested, Is.False);
        Assert.That(result.VersionRequested, Is.False);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("missing_required_option"));
    }
}
