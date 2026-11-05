using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDefaultCommandResolutionTests
{
    [Test]
    public void Parse_NoExplicitChildResolvesDefaultCommandPath()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        app.GetRootCommand().DefaultCommand("project");
        project.DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project, build }));
        Assert.That(result.Tokens, Is.Empty);
    }

    [Test]
    public void Parse_ExplicitChildTakesPrecedenceOverDefaultCommand()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        ArgCommand test = app.Command("test");
        app.GetRootCommand().DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { "test" });

        Assert.That(result.Command, Is.SameAs(test));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), test }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Command, Is.Not.SameAs(build));
    }

    [Test]
    public void Parse_ExplicitParentResolvesItsDefaultChild()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        project.DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { "project" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
    }

    [Test]
    public void Parse_DefaultCommandUsesLocalArgumentsAndOptions()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Value("name");
        build.Argument("project").Required();
        app.GetRootCommand().DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { "--configuration", "Release", "src" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("project"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "src" }));
    }

    [Test]
    public void Parse_DefaultCommandInheritsGlobalsWithoutParentLocalOptionLeakage()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--verbose").Flag();
        app.Option("--root-only").Flag();
        ArgCommand build = app.Command("build");
        build.Option("--build-only").Flag();
        app.GetRootCommand().DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { "--verbose", "--root-only", "--build-only" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--build-only"));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("unknown_option"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--root-only"));
    }

    [Test]
    public void Validate_DefaultCommandSatisfiesParentRequiredSubcommand()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().RequiredSubcommand();
        app.Command("build");
        app.GetRootCommand().DefaultCommand("build");

        ArgValidationResult result = app.Validate(new string[] { });

        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_DefaultCommandRequiredSubcommandRuleStillApplies()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project").RequiredSubcommand();
        project.Command("build");
        app.GetRootCommand().DefaultCommand("project");

        ArgValidationResult result = app.Validate(new string[] { });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("missing_required_subcommand"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("project"));
    }

    [Test]
    public void Run_DefaultCommandSelectsHandlerAndPreservesHookPath()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        app.GetRootCommand().DefaultCommand("build");
        int calls = 0;
        app.GetRootCommand().Before(delegate(ArgContext context)
        {
            calls = calls + 1;
            return 0;
        });
        build.Handler(delegate(ArgContext context)
        {
            calls = calls + 1;
            return context.Command == build ? 7 : 1;
        });
        app.GetRootCommand().After(delegate(ArgContext context)
        {
            calls = calls + 1;
            return 0;
        });

        ArgRunResult result = app.Run(new string[] { });

        Assert.That(result.ExitCode, Is.EqualTo(7));
        Assert.That(result.HandlerInvoked, Is.True);
        Assert.That(result.Context.Command, Is.SameAs(build));
        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public void Run_HelpOptionUsesDefaultCommandWhileHelpCommandKeepsExplicitTarget()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build").Description("Default build help.");
        ArgCommand project = app.Command("project").Description("Project help.");
        project.Command("publish").Description("Publish help.");
        project.DefaultCommand("publish");
        app.GetRootCommand().DefaultCommand("build");

        ArgRunResult optionResult = app.Run(new string[] { "--help" });
        ArgRunResult commandResult = app.Run(new string[] { "help", "project" });

        Assert.That(optionResult.Context.Command, Is.SameAs(build));
        Assert.That(optionResult.ValidationResult.HelpRequested, Is.True);
        Assert.That(optionResult.HelpText, Does.Contain("Default build help."));
        Assert.That(commandResult.Context.Command, Is.SameAs(project));
        Assert.That(commandResult.ParseResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project }));
        Assert.That(commandResult.ValidationResult.HelpRequested, Is.True);
        Assert.That(commandResult.HelpText, Does.Contain("Project help."));
        Assert.That(commandResult.HelpText, Does.Not.Contain("Publish help."));
    }

    [Test]
    public void Parse_ResetsDefaultResolutionBetweenCallsWithExplicitCommands()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        ArgCommand test = app.Command("test");
        app.GetRootCommand().DefaultCommand("build");

        ArgParseResult defaultResult = app.Parse(new string[] { });
        ArgParseResult explicitResult = app.Parse(new string[] { "test" });

        Assert.That(defaultResult.Command, Is.SameAs(build));
        Assert.That(explicitResult.Command, Is.SameAs(test));
        Assert.That(explicitResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), test }));
    }
}
