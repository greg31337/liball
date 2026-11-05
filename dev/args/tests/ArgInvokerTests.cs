using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgInvokerTests
{
    [Test]
    public void Invoke_RejectsNullArguments()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);
        ArgInvoker invoker = new ArgInvoker();

        Assert.Throws<System.ArgumentNullException>(delegate { invoker.Invoke(null!, validationResult); });
        Assert.Throws<System.ArgumentNullException>(delegate { invoker.Invoke(parseResult, null!); });
    }

    [Test]
    public void Invoke_CallsResolvedCommandHandlerAndUsesReturnedExitCode()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Value("name");
        bool called = false;
        ArgContext? seenContext = null;
        build.Handler(delegate(ArgContext context)
        {
            called = true;
            seenContext = context;
            return context.Values.String("--configuration").Length;
        });
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--configuration", "Release" });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(called, Is.True);
        Assert.That(runResult.ExitCode, Is.EqualTo(7));
        Assert.That(runResult.HandlerInvoked, Is.True);
        Assert.That(runResult.ParseResult, Is.SameAs(parseResult));
        Assert.That(runResult.ValidationResult, Is.SameAs(validationResult));
        Assert.That(runResult.Context, Is.SameAs(seenContext));
        Assert.That(runResult.Context.Command, Is.SameAs(build));
    }

    [Test]
    public void Invoke_ReturnsOneAndSkipsHandlerWhenParseErrorsExist()
    {
        ArgCommand command = new ArgCommand("tool");
        bool called = false;
        command.Handler(delegate(ArgContext context)
        {
            called = true;
            return 0;
        });
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--missing" });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(called, Is.False);
        Assert.That(runResult.ExitCode, Is.EqualTo(1));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.HasErrors(), Is.True);
        Assert.That(runResult.ValidationResult.Errors[0].Code, Is.EqualTo("unknown_option"));
    }

    [Test]
    public void Invoke_ReturnsOneAndSkipsHandlerWhenValidationHasErrors()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Required().Value("name");
        bool called = false;
        command.Handler(delegate(ArgContext context)
        {
            called = true;
            return 0;
        });
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(called, Is.False);
        Assert.That(runResult.ExitCode, Is.EqualTo(1));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.HasErrors(), Is.True);
    }

    [Test]
    public void Invoke_ReturnsZeroAndSkipsHandlerForShortCircuitRequests()
    {
        ArgCommand command = new ArgCommand("tool");
        bool called = false;
        command.Handler(delegate(ArgContext context)
        {
            called = true;
            return 7;
        });
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--help" });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(called, Is.False);
        Assert.That(runResult.ExitCode, Is.EqualTo(0));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.ValidationResult.HelpRequested, Is.True);
    }

    [Test]
    public void Invoke_ReturnsZeroAndSkipsHandlerForVersionRequests()
    {
        ArgApp app = new ArgApp("tool").Version("1.2.3");
        bool called = false;
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            called = true;
            return 7;
        });
        ArgParseResult parseResult = new ArgParser(app.RootCommand).Parse(new string[] { "--version" });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(called, Is.False);
        Assert.That(runResult.ExitCode, Is.EqualTo(0));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.ValidationResult.VersionRequested, Is.True);
        Assert.That(runResult.ValidationResult.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(runResult.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(runResult.HelpText, Is.EqualTo(""));
    }

    [Test]
    public void Invoke_ReturnsZeroWhenNoHandlerExists()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        ArgRunResult runResult = new ArgInvoker().Invoke(parseResult, validationResult);

        Assert.That(runResult.ExitCode, Is.EqualTo(0));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.HasErrors(), Is.False);
    }

    [Test]
    public void App_RunParsesValidatesAndInvokesResolvedCommandHandler()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Argument("project").Required();
        build.Handler(delegate(ArgContext context)
        {
            if (context.Command.Name == "build" && context.Values.String("project") == "src")
            {
                return 12;
            }

            return 1;
        });

        ArgRunResult runResult = app.Run(new string[] { "build", "src" });

        Assert.That(runResult.ExitCode, Is.EqualTo(12));
        Assert.That(runResult.HandlerInvoked, Is.True);
        Assert.That(runResult.ParseResult.Command, Is.SameAs(build));
        Assert.That(runResult.Context.Values.String("project"), Is.EqualTo("src"));
    }

    [Test]
    public void RunResult_EmptyCreatesSuccessfulEmptyResult()
    {
        ArgRunResult runResult = ArgRunResult.Empty();

        Assert.That(runResult.ExitCode, Is.EqualTo(0));
        Assert.That(runResult.HandlerInvoked, Is.False);
        Assert.That(runResult.HasErrors(), Is.False);
        Assert.That(runResult.Context.ParseResult, Is.SameAs(runResult.ParseResult));
        Assert.That(runResult.ValidationResult.ParseResult, Is.SameAs(runResult.ParseResult));
    }

    [Test]
    public void RunResult_ConstructorRejectsNullArgumentsAndSetHelpTextStoresText()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);
        ArgContext context = new ArgContext(parseResult);

        Assert.Throws<System.ArgumentNullException>(delegate { new ArgRunResult(null!, validationResult, context, 0, false); });
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgRunResult(parseResult, null!, context, 0, false); });
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgRunResult(parseResult, validationResult, null!, 0, false); });

        ArgRunResult runResult = new ArgRunResult(parseResult, validationResult, context, 0, false);
        runResult.SetHelpText("help text");

        Assert.That(runResult.HelpText, Is.EqualTo("help text"));
    }
}
