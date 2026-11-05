using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDeprecationDiagnosticTests
{
    [Test]
    public void Parse_DeprecatedCommandOptionAndArgumentProduceStructuredDiagnostics()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build").Deprecated("Use compile.");
        build.Option("--configuration").Alias("-c").Value("name").Deprecated("Use --mode.");
        build.Argument("project").Required().Deprecated("Use workspace.");

        ArgParseResult result = app.Parse(new string[] { "build", "-c", "Release", "src" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(3));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_command", ArgSymbolKind.Command, "Use compile.", 0, "build");
        AssertDiagnostic(result.Diagnostics[1], "deprecated_option", ArgSymbolKind.Option, "Use --mode.", 1, "--configuration");
        AssertDiagnostic(result.Diagnostics[2], "deprecated_argument", ArgSymbolKind.Argument, "Use workspace.", 3, "project");
    }

    [Test]
    public void Parse_DeprecatedRootAndDefaultCommandUseNoTokenIndex()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().Deprecated("Use new-tool.");
        ArgCommand build = app.Command("build").Deprecated("Use compile.");
        app.GetRootCommand().DefaultCommand("build");

        ArgParseResult result = app.Parse(new string[] { });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_command", ArgSymbolKind.Command, "Use new-tool.", -1, "tool");
        AssertDiagnostic(result.Diagnostics[1], "deprecated_command", ArgSymbolKind.Command, "Use compile.", -1, "build");
    }

    [Test]
    public void Parse_HelpCommandTargetReportsDeprecatedCommandToken()
    {
        ArgApp app = new ArgApp("tool");
        app.Command("build").Deprecated("Use compile.");

        ArgParseResult result = app.Parse(new string[] { "help", "build" });

        Assert.That(result.HelpCommandRequested, Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_command", ArgSymbolKind.Command, "Use compile.", 1, "build");
    }

    [Test]
    public void Parse_UnusedDeprecatedSymbolsDoNotProduceDiagnostics()
    {
        string environmentName = "ARGS_TEST_DEPRECATED_OPTION";
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(environmentName, "env");
        ArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--defaulted").Value("value").Default("default").Deprecated("Avoid defaulted.");
        app.Option("--environment").Value("value").Env(environmentName).Deprecated("Avoid environment.");
        app.Argument("project").Default("src").Deprecated("Avoid project.");
        app.Command("old").Deprecated("Avoid old.");

        ArgParseResult result = app.Parse(new string[] { });

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
    }

    [Test]
    public void Parse_FallbackAndImpliedDeprecatedOptionsProduceDiagnostics()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--profile").Flag().Implies("--color", "always");
        app.Option("--color").Value("mode").Deprecated("Use --style.");
        app.Option("--log").OptionalValue("level").Fallback("info").Deprecated("Use --verbosity.");

        ArgParseResult result = app.Parse(new string[] { "--profile", "--log" });

        Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_option", ArgSymbolKind.Option, "Use --verbosity.", 1, "--log");
        AssertDiagnostic(result.Diagnostics[1], "deprecated_option", ArgSymbolKind.Option, "Use --style.", -1, "--color");
    }

    [Test]
    public void Parse_RepeatedDeprecatedOptionProducesOneDiagnostic()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--tag").Value("name").Repeatable().Deprecated("Use --label.");

        ArgParseResult result = app.Parse(new string[] { "--tag", "one", "--tag", "two" });

        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        AssertDiagnostic(result.Diagnostics[0], "deprecated_option", ArgSymbolKind.Option, "Use --label.", 0, "--tag");
    }

    [Test]
    public void ValidateAndRunExposeDiagnosticsWithoutPreventingInvocation()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--old").Flag().Deprecated("Use --new.");
        app.GetRootCommand().Handler(delegate(ArgContext context)
        {
            return context.Values.Bool("--old") ? 7 : 1;
        });

        ArgValidationResult validation = app.Validate(new string[] { "--old" });
        ArgRunResult run = app.Run(new string[] { "--old" });

        Assert.That(validation.Errors, Is.Empty);
        Assert.That(validation.Diagnostics, Is.SameAs(validation.ParseResult.Diagnostics));
        Assert.That(validation.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(run.ExitCode, Is.EqualTo(7));
        Assert.That(run.HandlerInvoked, Is.True);
        Assert.That(run.ValidationResult.Errors, Is.Empty);
        Assert.That(run.Diagnostics, Is.SameAs(run.ParseResult.Diagnostics));
        Assert.That(run.Diagnostics.Count, Is.EqualTo(1));
    }

    [Test]
    public void Parse_NonDeprecatedUsageKeepsDiagnosticsEmpty()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--verbose").Flag();

        ArgParseResult result = app.Parse(new string[] { "--verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    static void AssertDiagnostic(ArgDiagnostic diagnostic, string code, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.SymbolKind, Is.EqualTo(kind));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
        Assert.That(diagnostic.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(diagnostic.SymbolName, Is.EqualTo(symbolName));
    }
}
