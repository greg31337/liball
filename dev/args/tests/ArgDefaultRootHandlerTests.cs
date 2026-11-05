using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDefaultRootHandlerTests
{
    [Test]
    public void Run_InvokesRootHandlerWhenNoSubcommandIsResolved()
    {
        ArgApp app = new ArgApp("tool");
        app.Command("build");
        bool called = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            called = true;
            if (context.Command == app.RootCommand)
            {
                return 5;
            }

            return 1;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(called, Is.True);
        Assert.That(result.ExitCode, Is.EqualTo(5));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(app.RootCommand));
        Assert.That(result.ParseResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.RootCommand }));
    }

    [Test]
    public void Run_RootHandlerCanReadRootOptionsAndArguments()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--verbose").Flag();
        app.Argument("project").Required();
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            if (context.Values.Bool("--verbose") && context.Values.String("project") == "src")
            {
                return 8;
            }

            return 1;
        });

        ArgRunResult result = app.Run(new string[] { "--verbose", "src" });

        Assert.That(result.ExitCode, Is.EqualTo(8));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(app.RootCommand));
    }

    [Test]
    public void Run_InvokesSubcommandHandlerInsteadOfRootHandlerWhenSubcommandIsResolved()
    {
        ArgApp app = new ArgApp("tool");
        bool rootCalled = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            rootCalled = true;
            return 1;
        });
        ArgCommand build = app.Command("build");
        build.Handler(delegate(ArgContext context)
        {
            return 3;
        });

        ArgRunResult result = app.Run(new string[] { "build" });

        Assert.That(rootCalled, Is.False);
        Assert.That(result.ExitCode, Is.EqualTo(3));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(build));
    }

    [Test]
    public void Run_DoesNotInvokeRootHandlerWhenRequiredSubcommandIsMissing()
    {
        ArgApp app = new ArgApp("tool");
        app.RootCommand.RequiredSubcommand();
        app.Command("build");
        bool called = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            called = true;
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(called, Is.False);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.ValidationResult.Errors[0].Code, Is.EqualTo("missing_required_subcommand"));
    }
}
