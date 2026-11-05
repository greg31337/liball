using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgHelpBuilderTests
{
    [Test]
    public void RootHelp_IncludesUsageAndRootDescription()
    {
        ArgApp app = new ArgApp("tool");
        app.Description("Example command line application.");

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nExample command line application.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void CommandHelp_IncludesUsageSummaryAndDescription()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Value("name");
        build.Argument("project").Required();
        build.Summary("Build projects.");
        build.Description("Runs the compiler for a project.");

        string help = new ArgHelpBuilder().CommandHelp(build);

        Assert.That(help, Is.EqualTo("Usage: tool build [options] <project>\n\nBuild projects.\n\nRuns the compiler for a project.\n\nArguments:\n  <project> (required)\n\nOptions:\n  --configuration <name>\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_ForCommandWithoutTextReturnsOnlyUsage()
    {
        ArgCommand command = new ArgCommand("tool");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_ForAppUsesRootCommand()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--verbose").Flag();

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Is.EqualTo("Usage: tool [options]\n\nOptions:\n  --verbose\n  --help, -h - Show help."));
    }

    [Test]
    public void Help_UsesExplicitUsageTextInUsageBlock()
    {
        ArgCommand command = new ArgCommand("tool");
        command.UsageText = "tool custom";
        command.Description("Custom usage command.");

        string help = new ArgHelpBuilder().Help(command);

        Assert.That(help, Is.EqualTo("Usage: tool custom\n\nCustom usage command.\n\nOptions:\n  --help, -h - Show help."));
    }
}
