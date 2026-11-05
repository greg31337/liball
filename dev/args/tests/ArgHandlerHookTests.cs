using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgHandlerHookTests
{
    [Test]
    public void Run_ExecutesBeforeHooksHandlerAndAfterHooksInCommandPathOrder()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        List<string> calls = new List<string>();
        app.RootCommand.Before(delegate(ArgContext context)
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
            return 4;
        });
        build.After(delegate(ArgContext context)
        {
            calls.Add("build-after");
            return 0;
        });
        project.After(delegate(ArgContext context)
        {
            calls.Add("project-after");
            return 0;
        });
        app.RootCommand.After(delegate(ArgContext context)
        {
            calls.Add("root-after");
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { "project", "build" });

        Assert.That(result.ExitCode, Is.EqualTo(4));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(calls, Is.EqualTo(new string[] { "root-before", "project-before", "build-before", "handler", "build-after", "project-after", "root-after" }));
    }

    [Test]
    public void Run_NonZeroBeforeHookSkipsHandlerAndAfterHooks()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        List<string> calls = new List<string>();
        app.RootCommand.Before(delegate(ArgContext context)
        {
            calls.Add("root-before");
            return 9;
        });
        build.Handler(delegate(ArgContext context)
        {
            calls.Add("handler");
            return 0;
        });
        app.RootCommand.After(delegate(ArgContext context)
        {
            calls.Add("root-after");
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(9));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(calls, Is.EqualTo(new string[] { "root-before" }));
    }

    [Test]
    public void Run_NonZeroChildBeforeHookSkipsHandlerAndAfterHooks()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        List<string> calls = new List<string>();
        app.RootCommand.Before(delegate(ArgContext context)
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

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(5));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(calls, Is.EqualTo(new string[] { "root-before", "build-before" }));
    }

    [Test]
    public void Run_NonZeroAfterHookOverridesCurrentExitCode()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Handler(delegate(ArgContext context)
        {
            return 2;
        });
        build.After(delegate(ArgContext context)
        {
            return 6;
        });

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(6));
        Assert.That(result.HandlerInvoked, Is.True);
    }

    [Test]
    public void Run_AllAfterHooksRunAndLastNonZeroExitCodeWins()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        List<string> calls = new List<string>();
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
        app.RootCommand.After(delegate(ArgContext context)
        {
            calls.Add("root-after");
            return 7;
        });

        ArgRunResult result = app.Run(new string[] { "project", "build" });

        Assert.That(result.ExitCode, Is.EqualTo(7));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(calls, Is.EqualTo(new string[] { "handler", "build-after", "project-after", "root-after" }));
    }

    [Test]
    public void Run_HooksDoNotExecuteWhenValidationFails()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Required().Value("name");
        List<string> calls = new List<string>();
        app.RootCommand.Before(delegate(ArgContext context)
        {
            calls.Add("root-before");
            return 0;
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

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(calls, Is.Empty);
    }
}
