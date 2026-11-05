using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgAnsiHelpStylingTests
{
    [Test]
    public void Help_AnsiStylingIsDisabledByDefault()
    {
        ArgCommand command = CompleteCommand();

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help.IndexOf("\u001b[", System.StringComparison.Ordinal), Is.EqualTo(-1));
        Assert.That(help, Does.StartWith("Usage: tool [options] [command] [input]"));
        Assert.That(help, Does.Contain("\n\nOptions:"));
    }

    [Test]
    public void Help_AnsiStylingFormatsUsageAndEverySectionHeading()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableAnsiHelpStyling = true;
        ArgCommand command = CompleteCommand();

        string help = new ArgHelpBuilder().UseSettings(settings).Help(command);

        Assert.That(help, Does.StartWith("\u001b[1mUsage:\u001b[0m tool [options] [command] [input]"));
        Assert.That(help, Does.Contain("\u001b[1mCommands:\u001b[0m"));
        Assert.That(help, Does.Contain("\u001b[1mArguments:\u001b[0m"));
        Assert.That(help, Does.Contain("\u001b[1mOptions:\u001b[0m"));
        Assert.That(help, Does.Contain("\u001b[1mOption groups:\u001b[0m"));
        Assert.That(help, Does.Contain("\u001b[1mExamples:\u001b[0m"));
    }

    [Test]
    public void Help_AnsiStylingDoesNotChangePlainTextWrapping()
    {
        ArgParserSettings plainSettings = new ArgParserSettings();
        plainSettings.HelpWidth = 32;
        ArgParserSettings styledSettings = new ArgParserSettings();
        styledSettings.HelpWidth = 32;
        styledSettings.EnableAnsiHelpStyling = true;
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Description("Select build configuration for release output");

        string plain = new ArgHelpBuilder().UseSettings(plainSettings).Help(command);
        string styled = new ArgHelpBuilder().UseSettings(styledSettings).Help(command);

        Assert.That(RemoveAnsi(styled), Is.EqualTo(plain));
        string[] lines = RemoveAnsi(styled).Split('\n');
        int index = 0;
        while (index < lines.Length)
        {
            Assert.That(lines[index].Length, Is.LessThanOrEqualTo(32));
            index = index + 1;
        }
    }

    [Test]
    public void Run_HelpRequestUsesConfiguredAnsiStyling()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableAnsiHelpStyling = true;
        ArgApp app = new ArgApp("tool").UseSettings(settings);

        ArgRunResult result = app.Run(new string[] { "--help" });

        Assert.That(result.HelpText, Does.StartWith("\u001b[1mUsage:\u001b[0m tool [options]"));
        Assert.That(result.HelpText, Does.Contain("\u001b[1mOptions:\u001b[0m"));
    }

    static ArgCommand CompleteCommand()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Command("build").Summary("Build projects.");
        command.Argument("input");
        command.Option("--json").Flag();
        command.Option("--text").Flag();
        command.MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        command.Example("tool build");
        return command;
    }

    static string RemoveAnsi(string text)
    {
        return text.Replace("\u001b[1m", "").Replace("\u001b[0m", "");
    }
}
