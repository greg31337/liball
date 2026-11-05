using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDefaultHelpOptionTests
{
    [Test]
    public void Parse_DefaultLongHelpOptionWithoutDefinition()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--help" });

        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--help"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("--help"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_DefaultShortHelpOptionWithoutDefinition()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-h" });

        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--help"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("-h"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
    }

    [Test]
    public void Parse_UserDefinedHelpOptionTakesPrecedence()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgOption help = command.Option("--help").Alias("-h").Flag();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--help" });

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].Option, Is.SameAs(help));
    }

    [Test]
    public void Parse_DefaultHelpOptionAppliesToSubcommands()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Required().Value("name");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--help" });

        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Command, Is.SameAs(build));
        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("--help"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Help_IncludesDefaultHelpOptionWhenNoHelpOptionIsDefined()
    {
        ArgCommand command = new ArgCommand("tool");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_DoesNotDuplicateUserDefinedHelpOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--help").Alias("-h").Flag().Description("Custom help.");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Custom help."));
    }
}
