using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDetailedHelpTests
{
    [Test]
    public void Help_DefaultViewOmitsHiddenCommandsOptionsAndOptionGroups()
    {
        ArgCommand root = DetailedCommand();

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Does.Not.Contain("internal"));
        Assert.That(help, Does.Not.Contain("--secret"));
        Assert.That(help, Does.Not.Contain("--global-secret"));
        Assert.That(help, Does.Not.Contain("hidden-only"));
        Assert.That(help, Does.Contain("mixed (mutually exclusive): --public"));
    }

    [Test]
    public void Help_DetailedViewIncludesHiddenCommandsOptionsAndOptionGroups()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.IncludeHiddenInHelp = true;
        ArgCommand root = DetailedCommand();

        string help = new ArgHelpBuilder().UseSettings(settings).Help(root);

        Assert.That(help, Does.Contain("Commands:\n  Administration:\n    internal - Internal command."));
        Assert.That(help, Does.Contain("--secret - Internal option."));
        Assert.That(help, Does.Contain("--global-secret - Internal global option."));
        Assert.That(help, Does.Contain("mixed (mutually exclusive): --public, --secret"));
        Assert.That(help, Does.Contain("hidden-only (at least one required): --secret"));
    }

    [Test]
    public void Run_HelpRequestUsesConfiguredDetailedView()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.IncludeHiddenInHelp = true;
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Command("internal").Hidden();
        app.Option("--secret").Flag().Hidden();

        ArgRunResult result = app.Run(new string[] { "--help" });

        Assert.That(result.HelpText, Does.Contain("internal"));
        Assert.That(result.HelpText, Does.Contain("--secret"));
    }

    [Test]
    public void Help_DetailedViewUsesHiddenCustomHelpAndVersionOptionsWithoutDuplicates()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.IncludeHiddenInHelp = true;
        ArgApp app = new ArgApp("tool").Version("1.2.3");
        app.Option("--help").Alias("-h").Flag().Hidden().Description("Internal help.");
        app.Option("--version").Alias("-V").Flag().Hidden().Description("Internal version.");

        string help = new ArgHelpBuilder().UseSettings(settings).RootHelp(app);

        Assert.That(Occurrences(help, "--help, -h"), Is.EqualTo(1));
        Assert.That(Occurrences(help, "--version, -V"), Is.EqualTo(1));
        Assert.That(help, Does.Contain("--help, -h - Internal help."));
        Assert.That(help, Does.Contain("--version, -V - Internal version."));
    }

    static ArgCommand DetailedCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("internal").Summary("Internal command.").Group("Administration").Hidden();
        root.Option("--public").Flag();
        root.Option("--secret").Flag().Description("Internal option.").Hidden();
        root.GlobalOption("--global-secret").Flag().Description("Internal global option.").Hidden();
        root.MutuallyExclusiveGroup("mixed", new string[] { "--public", "--secret" });
        root.AtLeastOneRequiredGroup("hidden-only", new string[] { "--secret" });
        return root;
    }

    static int Occurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = text.IndexOf(value, index, System.StringComparison.Ordinal);
            if (index >= 0)
            {
                count = count + 1;
                index = index + value.Length;
            }
        }

        return count;
    }
}
