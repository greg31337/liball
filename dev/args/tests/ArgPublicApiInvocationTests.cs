using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiInvocationTests
{
    [Test]
    public void ParseAndValidate_DoNotInvokeHandlersOrHooks()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--configuration").Required().Value("name");
        int validationCalls = 0;
        int lifecycleCalls = 0;
        app.GetRootCommand().ValidateCommand(delegate(ArgContext context)
        {
            validationCalls = validationCalls + 1;
            return null;
        });
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        app.GetRootCommand().Handler(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        app.GetRootCommand().After(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });

        ArgParseResult parseResult = app.Parse(new string[] { });

        Assert.That(parseResult.Errors, Is.Empty);
        Assert.That(validationCalls, Is.EqualTo(0));
        Assert.That(lifecycleCalls, Is.EqualTo(0));

        ArgValidationResult validationResult = app.Validate(new string[] { });

        Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
        Assert.That(validationResult.Errors[0].Code, Is.EqualTo("missing_required_option"));
        Assert.That(validationCalls, Is.EqualTo(1));
        Assert.That(lifecycleCalls, Is.EqualTo(0));
    }

    [Test]
    public void Run_InvokesTheSelectedHandlerWithItsContextAndValues()
    {
        IArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--verbose").Flag();
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Value("name");
        build.Argument("project").Required();
        ArgContext? receivedContext = null;
        string receivedConfiguration = "";
        string receivedProject = "";
        bool receivedVerbose = false;
        build.Handler(delegate(ArgContext context)
        {
            receivedContext = context;
            receivedConfiguration = context.Values.String("--configuration");
            receivedProject = context.Values.String("project");
            receivedVerbose = context.Values.Bool("--verbose");
            return 23;
        });

        ArgRunResult result = app.Run(new string[] { "build", "--verbose", "--configuration", "Release", "src" });

        Assert.That(result.ExitCode, Is.EqualTo(23));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context, Is.SameAs(receivedContext));
        Assert.That(result.Context.Command, Is.SameAs(build));
        Assert.That(result.Context.ParseResult, Is.SameAs(result.ParseResult));
        Assert.That(result.ValidationResult.ParseResult, Is.SameAs(result.ParseResult));
        Assert.That(receivedConfiguration, Is.EqualTo("Release"));
        Assert.That(receivedProject, Is.EqualTo("src"));
        Assert.That(receivedVerbose, Is.True);
    }

    [Test]
    public void Run_UsesTheRootHandlerAsTheDefaultHandler()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand root = app.GetRootCommand();
        app.Command("build");
        root.Handler(delegate(ArgContext context)
        {
            if (context.Command == root)
            {
                return 11;
            }

            return 1;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(result.ExitCode, Is.EqualTo(11));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(root));
        Assert.That(result.ParseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root }));
    }

    [Test]
    public void Run_InvokesTheConfiguredDefaultCommandHandler()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        app.GetRootCommand().DefaultCommand("build");
        build.Handler(delegate(ArgContext context)
        {
            return context.Command == build ? 12 : 1;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(result.ExitCode, Is.EqualTo(12));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(build));
        Assert.That(result.ParseResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), build }));
    }

    [Test]
    public void Run_ExplicitBranchWithoutAHandlerShowsItsHelp()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project").Branch().Description("Project commands.");

        ArgRunResult result = app.Run(new string[] { "project" });

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.Context.Command, Is.SameAs(project));
        Assert.That(result.ValidationResult.ShortCircuited, Is.True);
        Assert.That(result.ValidationResult.HelpRequested, Is.True);
        Assert.That(result.HelpText, Does.StartWith("Usage: tool project [options]"));
        Assert.That(result.HelpText, Does.Contain("Project commands."));
    }

    [Test]
    public void Run_ExecutesHooksInPathOrderAndUsesTheLastNonzeroAfterExitCode()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        List<string> calls = new List<string>();
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            calls.Add("root-before");
            return 0;
        });
        project.Before(delegate(ArgContext context)
        {
            calls.Add("project-before");
            return 0;
        });
        build.Before(delegate(ArgContext context)
        {
            calls.Add("build-before");
            return 0;
        });
        build.Handler(delegate(ArgContext context)
        {
            calls.Add("handler");
            return 2;
        });
        build.After(delegate(ArgContext context)
        {
            calls.Add("build-after");
            return 6;
        });
        project.After(delegate(ArgContext context)
        {
            calls.Add("project-after");
            return 0;
        });
        app.GetRootCommand().After(delegate(ArgContext context)
        {
            calls.Add("root-after");
            return 7;
        });

        ArgRunResult result = app.Run(new string[] { "project", "build" });

        Assert.That(result.ExitCode, Is.EqualTo(7));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(calls, Is.EqualTo(new string[]
        {
            "root-before",
            "project-before",
            "build-before",
            "handler",
            "build-after",
            "project-after",
            "root-after"
        }));
    }

    [Test]
    public void Run_NonzeroBeforeExitCodeStopsTheRemainingLifecycle()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        List<string> calls = new List<string>();
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            calls.Add("root-before");
            return 0;
        });
        build.Before(delegate(ArgContext context)
        {
            calls.Add("build-before");
            return 5;
        });
        build.Handler(delegate(ArgContext context)
        {
            calls.Add("handler");
            return 0;
        });
        build.After(delegate(ArgContext context)
        {
            calls.Add("build-after");
            return 0;
        });
        app.GetRootCommand().After(delegate(ArgContext context)
        {
            calls.Add("root-after");
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(5));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(calls, Is.EqualTo(new string[] { "root-before", "build-before" }));
    }

    [Test]
    public void Run_ValidationFailuresPreventTheInvocationLifecycle()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Required().Value("name");
        int lifecycleCalls = 0;
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        build.Handler(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        build.After(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        IArgResultStatus status = new ArgResultStatus();

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.ValidationResult.Errors.Count, Is.EqualTo(1));
        Assert.That(result.ValidationResult.Errors[0].Code, Is.EqualTo("missing_required_option"));
        Assert.That(status.RunFailed(result), Is.True);
        Assert.That(lifecycleCalls, Is.EqualTo(0));
    }

    [Test]
    public void Run_HelpAndVersionShortCircuitsSkipHandlersAndHooks()
    {
        IArgApp app = new ArgApp("tool").Version("4.5.6");
        app.Option("--configuration").Required().Value("name");
        int lifecycleCalls = 0;
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });
        app.GetRootCommand().Handler(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 9;
        });
        app.GetRootCommand().After(delegate(ArgContext context)
        {
            lifecycleCalls = lifecycleCalls + 1;
            return 0;
        });

        ArgRunResult help = app.Run(new string[] { "--help" });
        ArgRunResult version = app.Run(new string[] { "--version" });

        Assert.That(help.ExitCode, Is.EqualTo(0));
        Assert.That(help.HandlerInvoked, Is.False);
        Assert.That(help.ValidationResult.ShortCircuited, Is.True);
        Assert.That(help.ValidationResult.HelpRequested, Is.True);
        Assert.That(help.ValidationResult.Errors, Is.Empty);
        Assert.That(help.HelpText, Does.StartWith("Usage: tool"));
        Assert.That(help.VersionText, Is.EqualTo(""));
        Assert.That(version.ExitCode, Is.EqualTo(0));
        Assert.That(version.HandlerInvoked, Is.False);
        Assert.That(version.ValidationResult.ShortCircuited, Is.True);
        Assert.That(version.ValidationResult.VersionRequested, Is.True);
        Assert.That(version.ValidationResult.Errors, Is.Empty);
        Assert.That(version.HelpText, Is.EqualTo(""));
        Assert.That(version.VersionText, Is.EqualTo("4.5.6"));
        Assert.That(lifecycleCalls, Is.EqualTo(0));
    }
}
