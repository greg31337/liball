using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgConfigurableHelpOptionTests
{
    [Test]
    public void Parse_ConfiguredLongHelpAliasShortCircuitsValidation()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name");

        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "--assist" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--assist"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("--assist"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Parse_ConfiguredShortHelpAliasReplacesDefaultShortAlias()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        ArgCommand command = new ArgCommand("tool");

        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "-?" });

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("--assist"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("-?"));
    }

    [Test]
    public void Parse_RemovedDefaultHelpAliasIsUnknown()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        ArgCommand command = new ArgCommand("tool");

        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "--help" });

        Assert.That(parseResult.Occurrences, Is.Empty);
        Assert.That(parseResult.Errors.Count, Is.EqualTo(1));
        Assert.That(parseResult.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(parseResult.Errors[0].SymbolName, Is.EqualTo("--help"));
    }

    [Test]
    public void Parse_ConfiguredSlashHelpAliasShortCircuitsValidation()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("/?");
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Required();

        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { "/?" });
        ArgValidationResult validation = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(parseResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(parseResult.Occurrences[0].SymbolName, Is.EqualTo("/?"));
        Assert.That(parseResult.Occurrences[0].Alias, Is.EqualTo("/?"));
        Assert.That(validation.ShortCircuited, Is.True);
        Assert.That(validation.HelpRequested, Is.True);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void Help_UsesConfiguredDefaultHelpAliases()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        ArgCommand command = new ArgCommand("tool");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --assist, -? - Show help."));
    }

    [Test]
    public void Help_DoesNotDuplicateUserDefinedConfiguredHelpAlias()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        ArgCommand command = new ArgCommand("tool");
        command.Option("--assist").Alias("-?").Flag().Description("Custom assistance.");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --assist, -? - Custom assistance."));
    }

    [Test]
    public void Help_EmptyConfiguredHelpAliasesDisableBuiltInHelpOption()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.HelpAliases.Clear();
        ArgCommand command = new ArgCommand("tool");

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool"));
    }
}
