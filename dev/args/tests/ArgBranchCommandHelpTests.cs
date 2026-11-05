using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgBranchCommandHelpTests
{
    [Test]
    public void Run_RootBranchWithoutHandlerReturnsHelp()
    {
        ArgApp app = new ArgApp("tool");
        app.Command("build").Summary("Build projects.");
        app.Command("test").Summary("Run tests.");

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.ValidationResult.HelpRequested, Is.True);
        Assert.That(result.HelpText, Is.EqualTo("Usage: tool [options] [command]\n\nCommands:\n  build - Build projects.\n  test - Run tests.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Run_NestedBranchWithoutHandlerReturnsHelpForResolvedCommand()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project").Summary("Project commands.");
        project.Command("build").Summary("Build projects.");
        project.Command("clean").Summary("Clean outputs.");

        ArgRunResult result = app.Run(new string[] { "project" });

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.Context.Command, Is.SameAs(project));
        Assert.That(result.ValidationResult.HelpRequested, Is.True);
        Assert.That(result.HelpText, Is.EqualTo("Usage: tool project [options] [command]\n\nProject commands.\n\nCommands:\n  build - Build projects.\n  clean - Clean outputs.\n\nOptions:\n  --help, -h - Show help."));
    }

    [Test]
    public void Run_LeafCommandWithoutHandlerDoesNotReturnBranchHelp()
    {
        ArgApp app = new ArgApp("tool");
        app.Command("build");

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.ValidationResult.HelpRequested, Is.False);
        Assert.That(result.HelpText, Is.EqualTo(""));
    }

    [Test]
    public void Run_ExplicitHelpRequestReturnsHelpForResolvedTarget()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build").Summary("Build projects.");
        build.Option("--configuration").Value("name");

        ArgRunResult result = app.Run(new string[] { "help", "build" });

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.Context.Command, Is.SameAs(build));
        Assert.That(result.ValidationResult.HelpRequested, Is.True);
        Assert.That(result.HelpText, Is.EqualTo("Usage: tool build [options]\n\nBuild projects.\n\nOptions:\n  --configuration <name>\n  --help, -h - Show help."));
    }
}
