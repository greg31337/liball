using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiValidationTests
{
    [Test]
    public void RequiredSymbolsAndSubcommands_ValidateForTheSelectedCommand()
    {
        IArgApp app = new ArgApp("tool");
        app.GetRootCommand().RequiredSubcommand();
        ArgCommand build = app.Command("build");
        build.Option("--configuration").Required().Value("name");
        build.Argument("project").Required();

        ArgValidationResult missingCommand = app.Validate(new string[] { });
        ArgValidationResult missingSymbols = app.Validate(new string[] { "build" });
        ArgValidationResult valid = app.Validate(new string[] { "build", "--configuration", "Release", "src" });

        Assert.That(missingCommand.Errors.Count, Is.EqualTo(1));
        AssertError(missingCommand.Errors[0], "missing_required_subcommand", ArgSymbolKind.Command, "tool", -1);
        Assert.That(missingSymbols.Errors.Count, Is.EqualTo(2));
        AssertError(missingSymbols.Errors[0], "missing_required_option", ArgSymbolKind.Option, "--configuration", -1);
        AssertError(missingSymbols.Errors[1], "missing_required_argument", ArgSymbolKind.Argument, "project", -1);
        Assert.That(valid.Errors, Is.Empty);
        Assert.That(valid.ParseResult.Command, Is.SameAs(build));
    }

    [Test]
    public void ArityValidation_ReportsTooFewOptionAndArgumentValues()
    {
        IArgApp optionApp = new ArgApp("tool");
        optionApp.Option("--pair").Arity(2, 2);
        IArgApp argumentApp = new ArgApp("tool");
        argumentApp.Argument("files").Arity(2, ArgArity.Unbounded);

        ArgValidationResult optionResult = optionApp.Validate(new string[] { "--pair", "left" });
        ArgValidationResult argumentResult = argumentApp.Validate(new string[] { "one.txt" });

        Assert.That(optionResult.Errors.Count, Is.EqualTo(1));
        AssertError(optionResult.Errors[0], "too_few_values", ArgSymbolKind.Option, "--pair", 0);
        Assert.That(optionResult.ParseResult.Occurrences[0].Values, Is.EqualTo(new string[] { "left" }));
        Assert.That(argumentResult.Errors.Count, Is.EqualTo(1));
        AssertError(argumentResult.Errors[0], "too_few_values", ArgSymbolKind.Argument, "files", 0);
        Assert.That(argumentResult.ParseResult.Occurrences[0].Values, Is.EqualTo(new string[] { "one.txt" }));
    }

    [Test]
    public void UnknownInput_ReportsStructuredOptionAndCommandErrors()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Command("build");

        ArgValidationResult result = app.Validate(new string[] { "deploy", "--missing=value" });

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        AssertHasError(result, "unknown_option", ArgSymbolKind.Option, "--missing", 1);
        AssertHasError(result, "unknown_command", ArgSymbolKind.Command, "deploy", 0);
        Assert.That(result.ParseResult.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.ParseResult.Tokens[1] }));
    }

    [Test]
    public void DuplicateOptions_ReportTheLaterOccurrence()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--profile").Alias("-p").Value("name");

        ArgValidationResult result = app.Validate(new string[] { "--profile", "one", "-p", "two" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertError(result.Errors[0], "duplicate_option", ArgSymbolKind.Option, "--profile", 2);
        Assert.That(result.ParseResult.Occurrences.Count, Is.EqualTo(2));
    }

    [Test]
    public void Choices_ValidateOptionAndArgumentValues()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--mode").Value("name").Choice("debug").Choice("release");
        app.Argument("target").Choice("linux").Choice("macos");

        ArgValidationResult invalid = app.Validate(new string[] { "--mode", "staging", "windows" });
        ArgValidationResult valid = app.Validate(new string[] { "--mode", "release", "linux" });

        Assert.That(invalid.Errors.Count, Is.EqualTo(2));
        AssertError(invalid.Errors[0], "invalid_choice", ArgSymbolKind.Option, "--mode", 0);
        AssertError(invalid.Errors[1], "invalid_choice", ArgSymbolKind.Argument, "target", 2);
        Assert.That(valid.Errors, Is.Empty);
    }

    [Test]
    public void ConflictsAndDependencies_AggregateStructuredErrors()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--fast").Flag().Conflicts("--safe").Requires("--configuration");
        app.Option("--safe").Flag();
        app.Option("--configuration").Value("name");

        ArgValidationResult invalid = app.Validate(new string[] { "--fast", "--safe" });
        ArgValidationResult valid = app.Validate(new string[] { "--fast", "--configuration", "Release" });

        Assert.That(invalid.Errors.Count, Is.EqualTo(2));
        AssertHasError(invalid, "conflicting_option", ArgSymbolKind.Option, "--fast", 0);
        AssertHasError(invalid, "missing_dependent_option", ArgSymbolKind.Option, "--fast", 0);
        Assert.That(valid.Errors, Is.Empty);
    }

    [Test]
    public void Implications_ParticipateInRequiredDependencyAndGroupValidation()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--production").Flag().Implies("--color", "always").Requires("--color");
        app.Option("--color").Required().Value("mode");
        app.GetRootCommand().AtLeastOneRequiredGroup("display", new string[] { "--color" });

        ArgValidationResult result = app.Validate(new string[] { "--production" });
        IArgValues values = new ArgValues(result.ParseResult);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(values.String("--color"), Is.EqualTo("always"));
        Assert.That(values.Source("--color"), Is.EqualTo(ArgValueSource.Implied));
        Assert.That(values.IsSupplied("--color"), Is.False);
    }

    [Test]
    public void OptionGroups_ValidateExclusiveAndAtLeastOneRules()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--json").Flag();
        app.Option("--text").Flag();
        app.Option("--file").Value("path");
        app.Option("--stdin").Flag();
        app.GetRootCommand().MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        app.GetRootCommand().AtLeastOneRequiredGroup("input", new string[] { "--file", "--stdin" });

        ArgValidationResult invalid = app.Validate(new string[] { "--json", "--text" });
        ArgValidationResult valid = app.Validate(new string[] { "--json", "--file", "input.txt" });

        Assert.That(invalid.Errors.Count, Is.EqualTo(2));
        AssertError(invalid.Errors[0], "mutually_exclusive_group", ArgSymbolKind.Option, "output", -1);
        AssertError(invalid.Errors[1], "missing_required_option_group", ArgSymbolKind.Option, "input", -1);
        Assert.That(valid.Errors, Is.Empty);
    }

    [Test]
    public void CustomRules_RunOnlyForTheSelectedCommandPathAndPreserveResults()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        ArgCommand test = app.Command("test");
        app.GetRootCommand().GlobalOption("--left").Flag();
        app.GetRootCommand().GlobalOption("--right").Flag();
        ArgParseError rootError = new ArgParseError("root_rule", ArgSymbolKind.Command, "Root rule failed.", -1, "tool");
        ArgParseError optionError = new ArgParseError("option_rule", ArgSymbolKind.Option, "Option rule failed.", 1, "--left");
        ArgParseError buildError = new ArgParseError("build_rule", ArgSymbolKind.Command, "Build rule failed.", 0, "build");
        int siblingCalls = 0;

        app.GetRootCommand().ValidateCommand(delegate(ArgContext context)
        {
            Assert.That(context.Command, Is.SameAs(build));
            return rootError;
        });
        app.GetRootCommand().ValidateOptions(delegate(ArgValues values)
        {
            if (values.Has("--left") && values.Has("--right"))
            {
                return optionError;
            }

            return null;
        });
        build.ValidateCommand(delegate(ArgContext context)
        {
            return buildError;
        });
        build.ValidateOptions(delegate(ArgValues values)
        {
            return null;
        });
        test.ValidateCommand(delegate(ArgContext context)
        {
            siblingCalls = siblingCalls + 1;
            return null;
        });

        ArgValidationResult result = app.Validate(new string[] { "build", "--left", "--right" });

        Assert.That(result.Errors, Is.EqualTo(new ArgParseError[] { rootError, optionError, buildError }));
        Assert.That(siblingCalls, Is.EqualTo(0));
    }

    [Test]
    public void DeprecatedSymbols_ProduceDiagnosticsWithoutValidationErrors()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build").Deprecated("Use compile.");
        build.Option("--configuration").Alias("-c").Value("name").Deprecated("Use --mode.");
        build.Argument("project").Required().Deprecated("Use workspace.");

        ArgValidationResult result = app.Validate(new string[] { "build", "-c", "Release", "src" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Diagnostics, Is.SameAs(result.ParseResult.Diagnostics));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(3));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_command", ArgSymbolKind.Command, "build", 0);
        AssertDiagnostic(result.Diagnostics[1], "deprecated_option", ArgSymbolKind.Option, "--configuration", 1);
        AssertDiagnostic(result.Diagnostics[2], "deprecated_argument", ArgSymbolKind.Argument, "project", 3);
    }

    [Test]
    public void Validation_AggregatesParseAndBuiltInErrorsDeterministically()
    {
        IArgApp app = new ArgApp("ship");
        app.GetRootCommand().GlobalOption("--workspace").Value("path").Required();
        ArgCommand deploy = app.Command("deploy");
        deploy.Option("--image").Value("ref").Required();
        deploy.Option("--mode").Value("name").Choice("blue").Choice("green");
        deploy.Option("--force").Flag().Conflicts("--dry-run");
        deploy.Option("--dry-run").Flag();
        deploy.Option("--profile").Value("name");
        deploy.Option("--canary").Flag().Requires("--image");

        ArgValidationResult result = app.Validate(new string[]
        {
            "deploy",
            "--mode", "turbo",
            "--force",
            "--dry-run",
            "--profile", "prod",
            "--profile", "stage",
            "--canary",
            "--unknown"
        });

        Assert.That(result.Errors.Count, Is.EqualTo(7));
        AssertHasError(result, "unknown_option", ArgSymbolKind.Option, "--unknown", 10);
        AssertHasError(result, "missing_required_option", ArgSymbolKind.Option, "--image", -1);
        AssertHasError(result, "missing_required_option", ArgSymbolKind.Option, "--workspace", -1);
        AssertHasError(result, "duplicate_option", ArgSymbolKind.Option, "--profile", 7);
        AssertHasError(result, "invalid_choice", ArgSymbolKind.Option, "--mode", 1);
        AssertHasError(result, "conflicting_option", ArgSymbolKind.Option, "--force", 3);
        AssertHasError(result, "missing_dependent_option", ArgSymbolKind.Option, "--canary", 9);
    }

    [Test]
    public void HelpAndVersion_ShortCircuitBuiltInAndCustomValidation()
    {
        IArgApp app = new ArgApp("tool").Version("1.2.3");
        app.Option("--required").Required().Value("value");
        app.Argument("project").Required();
        app.GetRootCommand().RequiredSubcommand();
        app.Command("build");
        int commandCalls = 0;
        int optionCalls = 0;
        app.GetRootCommand().ValidateCommand(delegate(ArgContext context)
        {
            commandCalls = commandCalls + 1;
            return new ArgParseError("command_rule", ArgSymbolKind.Command, "Command rule failed.", -1, "tool");
        });
        app.GetRootCommand().ValidateOptions(delegate(ArgValues values)
        {
            optionCalls = optionCalls + 1;
            return new ArgParseError("option_rule", ArgSymbolKind.Option, "Option rule failed.", -1, "--required");
        });

        ArgValidationResult help = app.Validate(new string[] { "--help" });
        ArgValidationResult version = app.Validate(new string[] { "--version" });
        ArgValidationResult helpWithParseError = app.Validate(new string[] { "--missing", "--help" });

        Assert.That(help.ShortCircuited, Is.True);
        Assert.That(help.HelpRequested, Is.True);
        Assert.That(help.VersionRequested, Is.False);
        Assert.That(help.Errors, Is.Empty);
        Assert.That(version.ShortCircuited, Is.True);
        Assert.That(version.HelpRequested, Is.False);
        Assert.That(version.VersionRequested, Is.True);
        Assert.That(version.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(version.Errors, Is.Empty);
        Assert.That(helpWithParseError.ShortCircuited, Is.True);
        Assert.That(helpWithParseError.HelpRequested, Is.True);
        Assert.That(helpWithParseError.Errors.Count, Is.EqualTo(1));
        AssertError(helpWithParseError.Errors[0], "unknown_option", ArgSymbolKind.Option, "--missing", 0);
        Assert.That(commandCalls, Is.EqualTo(0));
        Assert.That(optionCalls, Is.EqualTo(0));
    }

    static void AssertError(ArgParseError error, string code, ArgSymbolKind kind, string symbolName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.Message, Is.Not.Empty);
    }

    static void AssertHasError(ArgValidationResult result, string code, ArgSymbolKind kind, string symbolName, int tokenIndex)
    {
        int index = 0;
        while (index < result.Errors.Count)
        {
            ArgParseError error = result.Errors[index];
            if (error.Code == code && error.SymbolKind == kind && error.SymbolName == symbolName && error.TokenIndex == tokenIndex)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected " + code + " for '" + symbolName + "' at token " + tokenIndex + ".");
    }

    static void AssertDiagnostic(ArgDiagnostic diagnostic, string code, ArgSymbolKind kind, string symbolName, int tokenIndex)
    {
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.SymbolKind, Is.EqualTo(kind));
        Assert.That(diagnostic.SymbolName, Is.EqualTo(symbolName));
        Assert.That(diagnostic.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
