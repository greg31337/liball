using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCustomValidationTests
{
    [Test]
    public void Configuration_StoresCommandAndCrossOptionValidators()
    {
        ArgCommand command = new ArgCommand("build");
        System.Func<ArgContext, ArgParseError?> commandValidator = delegate(ArgContext context) { return null; };
        System.Func<ArgValues, ArgParseError?> optionValidator = delegate(ArgValues values) { return null; };

        Assert.That(command.ValidateCommand(commandValidator), Is.SameAs(command));
        Assert.That(command.ValidateOptions(optionValidator), Is.SameAs(command));
        Assert.That(command.GetCommandValidators(), Is.EqualTo(new System.Func<ArgContext, ArgParseError?>[] { commandValidator }));
        Assert.That(command.GetCrossOptionValidators(), Is.EqualTo(new System.Func<ArgValues, ArgParseError?>[] { optionValidator }));
        Assert.Throws<System.ArgumentNullException>(delegate { command.ValidateCommand(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { command.ValidateOptions(null!); });
    }

    [Test]
    public void Validate_AggregatesBuiltInAndSelectedCommandPathErrorsInDeterministicOrder()
    {
        ArgApp app = new ArgApp("tool");
        app.RootCommand.GlobalOption("--required").Required().Value("value");
        app.RootCommand.GlobalOption("--left").Flag();
        app.RootCommand.GlobalOption("--right").Flag();
        ArgCommand build = app.Command("build");
        ArgCommand test = app.Command("test");
        int siblingCalls = 0;
        ArgParseError rootCommandError = new ArgParseError("root_command_rule", ArgSymbolKind.Command, "Root command rule failed.", -1, "tool");
        ArgParseError rootOptionError = new ArgParseError("root_option_rule", ArgSymbolKind.Option, "Root option rule failed.", 1, "--left");
        ArgParseError buildCommandErrorOne = new ArgParseError("build_command_rule_one", ArgSymbolKind.Command, "First build rule failed.", 0, "build");
        ArgParseError buildCommandErrorTwo = new ArgParseError("build_command_rule_two", ArgSymbolKind.Command, "Second build rule failed.", 0, "build");
        ArgParseError buildOptionError = new ArgParseError("build_option_rule", ArgSymbolKind.Option, "Build option rule failed.", 2, "--right");
        app.RootCommand.ValidateCommand(delegate(ArgContext context)
        {
            Assert.That(context.Command, Is.SameAs(build));
            return rootCommandError;
        });
        app.RootCommand.ValidateOptions(delegate(ArgValues values)
        {
            if (values.Has("--left") && values.Has("--right"))
            {
                return rootOptionError;
            }

            return null;
        });
        build.ValidateCommand(delegate(ArgContext context) { return buildCommandErrorOne; });
        build.ValidateCommand(delegate(ArgContext context) { return buildCommandErrorTwo; });
        build.ValidateOptions(delegate(ArgValues values) { return buildOptionError; });
        test.ValidateCommand(delegate(ArgContext context)
        {
            siblingCalls = siblingCalls + 1;
            return new ArgParseError("sibling_rule", ArgSymbolKind.Command, "Sibling rule failed.", -1, "test");
        });

        ArgValidationResult result = app.Validate(new string[] { "build", "--left", "--right" });

        Assert.That(result.Errors.Count, Is.EqualTo(6));
        Assert.That(result.Errors[0].Code, Is.EqualTo("missing_required_option"));
        Assert.That(result.Errors[1], Is.SameAs(rootCommandError));
        Assert.That(result.Errors[2], Is.SameAs(rootOptionError));
        Assert.That(result.Errors[3], Is.SameAs(buildCommandErrorOne));
        Assert.That(result.Errors[4], Is.SameAs(buildCommandErrorTwo));
        Assert.That(result.Errors[5], Is.SameAs(buildOptionError));
        Assert.That(siblingCalls, Is.EqualTo(0));
    }

    [Test]
    public void Validate_NullCallbackResultsDoNotAddErrors()
    {
        ArgApp app = new ArgApp("tool");
        app.RootCommand.ValidateCommand(delegate(ArgContext context) { return null; });
        app.RootCommand.ValidateOptions(delegate(ArgValues values) { return null; });

        ArgValidationResult result = app.Validate(new string[] { });

        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_HelpAndVersionSkipAllCustomCallbacksAndBuiltInRules()
    {
        ArgApp app = new ArgApp("tool");
        app.Version("1.2.3");
        app.Option("--required").Required().Value("value");
        int commandCalls = 0;
        int optionCalls = 0;
        app.RootCommand.ValidateCommand(delegate(ArgContext context)
        {
            commandCalls = commandCalls + 1;
            return new ArgParseError("command_rule", ArgSymbolKind.Command, "Command rule failed.", -1, "tool");
        });
        app.RootCommand.ValidateOptions(delegate(ArgValues values)
        {
            optionCalls = optionCalls + 1;
            return new ArgParseError("option_rule", ArgSymbolKind.Option, "Option rule failed.", -1, "--required");
        });

        ArgValidationResult help = app.Validate(new string[] { "--help" });
        ArgValidationResult version = app.Validate(new string[] { "--version" });

        Assert.That(help.ShortCircuited, Is.True);
        Assert.That(help.HelpRequested, Is.True);
        Assert.That(help.Errors, Is.Empty);
        Assert.That(version.ShortCircuited, Is.True);
        Assert.That(version.VersionRequested, Is.True);
        Assert.That(version.Errors, Is.Empty);
        Assert.That(commandCalls, Is.EqualTo(0));
        Assert.That(optionCalls, Is.EqualTo(0));
    }

    [Test]
    public void Run_CustomValidationErrorPreventsHandlerInvocation()
    {
        ArgApp app = new ArgApp("tool");
        bool handlerCalled = false;
        app.RootCommand.ValidateCommand(delegate(ArgContext context)
        {
            return new ArgParseError("custom_failure", ArgSymbolKind.Command, "Custom validation failed.", -1, "tool");
        });
        app.RootCommand.Handler(delegate(ArgContext context)
        {
            handlerCalled = true;
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(result.ValidationResult.Errors.Count, Is.EqualTo(1));
        Assert.That(result.ValidationResult.Errors[0].Code, Is.EqualTo("custom_failure"));
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(handlerCalled, Is.False);
    }
}
